using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble
{
    // The heightfield stays fixed. These sparse snow samples drive both tire support
    // and the visible surface, and outlive the nearby meshes used to display them.
    internal sealed class WinterSnowRuts : MonoBehaviour
    {
        private const int TileShift = 5;
        private const int TileSize = 1 << TileShift;
        private const float Spacing = .15f;
        private const float TreadRadius = .34f;
        private const float ShoulderRadius = .58f;
        private const float VisibleRange = 125f;
        private const float HeightTolerance = .65f;
        private const float MaxDepth = .27f;
        private const float PackedMaxDepth = .13f;
        private readonly Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<int> indices = new List<int>();
        private Material material;
        private Transform viewer;
        private Vector2Int visibleCenter;
        private bool hasVisibleCenter;

        private struct Node
        {
            public float Depth, Bank, Height;
            public Vector3 Normal;
            public bool Marked => Depth > 0 || Bank > 0;
        }

        private sealed class Tile
        {
            public readonly Node[] Nodes = new Node[TileSize * TileSize];
            public bool Dirty = true;
            public GameObject Object;
            public Mesh Mesh;
        }

        internal void Bind(Transform player)
        {
            viewer = player;
            var shader = Resources.Load<Shader>("WinterSnowRuts");
            if (shader != null) material = new Material(shader) { name = "Winter snow ruts" };
        }

        internal float ReadDepth(Vector3 point, Vector3 normal, out Vector3 gradient)
        {
            float depth = Sample(point, normal);
            gradient = Vector3.zero;
            if (depth <= .001f) return depth;
            gradient.x = (Sample(point + Vector3.right * Spacing, normal) -
                Sample(point - Vector3.right * Spacing, normal)) / (2 * Spacing);
            gradient.z = (Sample(point + Vector3.forward * Spacing, normal) -
                Sample(point - Vector3.forward * Spacing, normal)) / (2 * Spacing);
            return depth;
        }

        internal float ReadWheelProfile(Vector3 point, Vector3 normal, out Vector3 gradient)
        {
            const float reach = Spacing * 1.15f;
            float profile = Sample(point, normal, true);
            gradient = new Vector3(
                (Sample(point + Vector3.right * reach, normal, true) - Sample(point - Vector3.right * reach, normal, true)) / (2 * reach),
                0,
                (Sample(point + Vector3.forward * reach, normal, true) - Sample(point - Vector3.forward * reach, normal, true)) / (2 * reach));
            return profile;
        }

        internal void Stamp(RaycastHit contact, Vector3 previous, float metres, float spin, float pressure, bool packed, float dt)
        {
            if (pressure <= .05f) return;
            float deposit = (metres * (packed ? .041f : .104f) + spin * dt * (packed ? .085f : .175f)) * pressure;
            if (deposit <= .00005f) return;
            int steps = Mathf.Max(1, Mathf.CeilToInt(metres / (Spacing * .65f)));
            for (int step = 0; step < steps; step++)
            {
                var point = Vector3.Lerp(previous, contact.point, (step + 1f) / steps);
                StampTread(contact.collider, point, contact.normal, deposit / steps, packed ? PackedMaxDepth : MaxDepth);
            }
        }

        private void StampTread(Collider surface, Vector3 point, Vector3 normal, float deposit, float limit)
        {
            int left = Mathf.CeilToInt((point.x - ShoulderRadius) / Spacing);
            int right = Mathf.FloorToInt((point.x + ShoulderRadius) / Spacing);
            int bottom = Mathf.CeilToInt((point.z - ShoulderRadius) / Spacing);
            int top = Mathf.FloorToInt((point.z + ShoulderRadius) / Spacing);
            for (int z = bottom; z <= top; z++)
            for (int x = left; x <= right; x++)
            {
                float dx = x * Spacing - point.x, dz = z * Spacing - point.z;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                if (distance >= ShoulderRadius) continue;
                float height = PlaneHeight(point, normal, dx, dz);
                var key = TileAt(x, z);
                if (!tiles.TryGetValue(key, out var tile))
                {
                    tile = new Tile();
                    tiles.Add(key, tile);
                }
                ref Node node = ref tile.Nodes[Index(x, z, key)];
                if (node.Marked && Mathf.Abs(node.Height - height) > HeightTolerance) continue;
                float coverage = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.12f, TreadRadius, distance));
                float ridge = Mathf.Sin(Mathf.PI * Mathf.InverseLerp(.29f, ShoulderRadius, distance));
                float remaining = Mathf.Max(0, limit - node.Depth);
                float change = Mathf.Min(remaining, deposit * coverage * remaining / limit);
                float bankLimit = limit * .48f;
                float shove = deposit * ridge * .40f * (1 - coverage) * Mathf.Clamp01(1 - node.Bank / bankLimit);
                if (change + shove < .00002f) continue;
                if (!node.Marked)
                {
                    var ray = new Ray(new Vector3(x * Spacing, height + .35f, z * Spacing), Vector3.down);
                    bool found = surface.Raycast(ray, out var hit, .70f);
                    node.Height = found ? hit.point.y : height;
                    node.Normal = found ? hit.normal : normal;
                }
                node.Depth += change;
                node.Bank = Mathf.Clamp(node.Bank + shove - change * .70f, 0, Mathf.Max(node.Bank, bankLimit));
                tile.Dirty = true;
                // A cell across the border uses this tile's edge sample as a corner.
                int localX = x - key.x * TileSize, localZ = z - key.y * TileSize;
                if (localX <= 1) MarkDirty(key + Vector2Int.left, localX == 0);
                if (localZ <= 1) MarkDirty(key + Vector2Int.down, localZ == 0);
                if (localX == 0 && localZ == 0) MarkDirty(key + Vector2Int.left + Vector2Int.down, true);
                if (localX == TileSize - 1) MarkDirty(key + Vector2Int.right);
                if (localZ == TileSize - 1) MarkDirty(key + Vector2Int.up);
            }
        }

        private float Sample(Vector3 point, Vector3 normal, bool includeBank = false)
        {
            float u = point.x / Spacing, v = point.z / Spacing;
            int x = Mathf.FloorToInt(u), z = Mathf.FloorToInt(v);
            float a = Mathf.Lerp(DepthAt(x, z, point, normal, includeBank), DepthAt(x + 1, z, point, normal, includeBank), u - x);
            float b = Mathf.Lerp(DepthAt(x, z + 1, point, normal, includeBank), DepthAt(x + 1, z + 1, point, normal, includeBank), u - x);
            return Mathf.Lerp(a, b, v - z);
        }

        private float DepthAt(int x, int z, Vector3 point, Vector3 normal, bool includeBank)
        {
            var node = GetNode(x, z);
            float height = PlaneHeight(point, normal, x * Spacing - point.x, z * Spacing - point.z);
            return Mathf.Abs(node.Height - height) <= HeightTolerance ? node.Depth - (includeBank ? node.Bank : 0) : 0;
        }

        private static float PlaneHeight(Vector3 point, Vector3 normal, float dx, float dz)
            => point.y - (normal.x * dx + normal.z * dz) / Mathf.Max(.45f, normal.y);

        private static Vector2Int TileAt(int x, int z)
            => new Vector2Int(x >> TileShift, z >> TileShift);

        private static int Index(int x, int z, Vector2Int key)
            => (z - key.y * TileSize) * TileSize + x - key.x * TileSize;

        private Node GetNode(int x, int z)
        {
            var key = TileAt(x, z);
            return tiles.TryGetValue(key, out var tile) ? tile.Nodes[Index(x, z, key)] : default;
        }

        private void MarkDirty(Vector2Int key, bool create = false)
        {
            if (tiles.TryGetValue(key, out var tile)) tile.Dirty = true;
            else if (create) tiles.Add(key, new Tile());
        }

        private void LateUpdate()
        {
            if (viewer == null || material == null) return;
            Vector3 position = viewer.position;
            var center = TileAt(Mathf.FloorToInt(position.x / Spacing), Mathf.FloorToInt(position.z / Spacing));
            bool moved = !hasVisibleCenter || center != visibleCenter;
            hasVisibleCenter = true;
            visibleCenter = center;
            int rebuilt = 0;
            foreach (var entry in tiles)
            {
                var key = entry.Key;
                var tile = entry.Value;
                const float tilesInRange = VisibleRange / (TileSize * Spacing);
                if ((key - center).sqrMagnitude > tilesInRange * tilesInRange)
                {
                    if (moved && tile.Mesh != null) ReleaseMesh(tile);
                    continue;
                }
                if (tile.Dirty && rebuilt < 5)
                {
                    Rebuild(key, tile);
                    rebuilt++;
                }
            }
        }

        private void Rebuild(Vector2Int key, Tile tile)
        {
            vertices.Clear(); normals.Clear(); colors.Clear(); indices.Clear();
            int startX = key.x * TileSize, startZ = key.y * TileSize;
            for (int z = startZ; z < startZ + TileSize; z++)
            for (int x = startX; x < startX + TileSize; x++)
            {
                var a = GetNode(x, z);
                var b = GetNode(x + 1, z);
                var c = GetNode(x, z + 1);
                var d = GetNode(x + 1, z + 1);
                if (!a.Marked && !b.Marked && !c.Marked && !d.Marked) continue;
                int referenceX = x, referenceZ = z;
                Node reference = a;
                if (!reference.Marked && b.Marked) { reference = b; referenceX++; }
                else if (!reference.Marked && c.Marked) { reference = c; referenceZ++; }
                else if (!reference.Marked) { reference = d; referenceX++; referenceZ++; }

                int first = vertices.Count;
                AddVertex(x, z, a, reference, referenceX, referenceZ);
                AddVertex(x + 1, z, b, reference, referenceX, referenceZ);
                AddVertex(x, z + 1, c, reference, referenceX, referenceZ);
                AddVertex(x + 1, z + 1, d, reference, referenceX, referenceZ);
                indices.Add(first); indices.Add(first + 2); indices.Add(first + 1);
                indices.Add(first + 1); indices.Add(first + 2); indices.Add(first + 3);
            }
            if (vertices.Count == 0) { tile.Dirty = false; return; }
            if (tile.Mesh == null)
            {
                tile.Object = new GameObject("Snow rut patch", typeof(MeshFilter), typeof(MeshRenderer));
                tile.Object.transform.SetParent(transform, false);
                tile.Object.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                tile.Mesh = new Mesh { name = "Snow rut surface" };
                tile.Mesh.MarkDynamic();
                tile.Object.GetComponent<MeshFilter>().sharedMesh = tile.Mesh;
                var renderer = tile.Object.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            tile.Mesh.Clear();
            tile.Mesh.SetVertices(vertices);
            tile.Mesh.SetNormals(normals);
            tile.Mesh.SetColors(colors);
            tile.Mesh.SetTriangles(indices, 0);
            tile.Dirty = false;
        }

        private void AddVertex(int x, int z, Node node, Node reference, int referenceX, int referenceZ)
        {
            if (!node.Marked || Mathf.Abs(reference.Height - node.Height) > HeightTolerance)
            {
                node.Depth = 0;
                node.Bank = 0;
                node.Normal = reference.Normal;
                node.Height = PlaneHeight(new Vector3(0, reference.Height, 0), reference.Normal,
                    (x - referenceX) * Spacing, (z - referenceZ) * Spacing);
            }
            var point = new Vector3(x * Spacing, node.Height, z * Spacing);
            float west = ReliefAt(x - 1, z, point, node.Normal), east = ReliefAt(x + 1, z, point, node.Normal);
            float south = ReliefAt(x, z - 1, point, node.Normal), north = ReliefAt(x, z + 1, point, node.Normal);
            var grooveNormal = node.Normal - new Vector3(east - west, 0, north - south) / (2 * Spacing);
            float clumps = Mathf.Lerp(.78f, 1.24f, Mathf.PerlinNoise(point.x * 8.3f, point.z * 8.3f));
            vertices.Add(point + Vector3.up * (.017f + node.Bank * clumps));
            normals.Add(grooveNormal.normalized);
            colors.Add(new Color(node.Depth / MaxDepth, node.Bank / .13f, 0,
                Mathf.Clamp01(node.Depth * 38 + node.Bank * 42)));
        }

        private float ReliefAt(int x, int z, Vector3 point, Vector3 normal)
        {
            var node = GetNode(x, z);
            float height = PlaneHeight(point, normal, x * Spacing - point.x, z * Spacing - point.z);
            return Mathf.Abs(node.Height - height) <= HeightTolerance ? node.Bank - node.Depth * .65f : 0;
        }

        private static void ReleaseMesh(Tile tile)
        {
            Destroy(tile.Object);
            Destroy(tile.Mesh);
            tile.Object = null;
            tile.Mesh = null;
            tile.Dirty = true;
        }

        private void OnDestroy()
        {
            foreach (var tile in tiles.Values)
                if (tile.Mesh != null) ReleaseMesh(tile);
            if (material != null) Destroy(material);
        }
    }
}

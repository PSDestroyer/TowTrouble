using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowTrouble
{
    // Road endpoints with the same authored ID form junctions. Nearby switchbacks
    // can be meters apart without being connected.
    public sealed class RecoveryRoadNetwork
    {
        private readonly Dictionary<string, int> junctions = new Dictionary<string, int>();
        private readonly List<Vector3> positions = new List<Vector3>();
        private readonly List<List<int>> neighbors = new List<List<int>>();
        private readonly List<(int a, int b)> segments = new List<(int, int)>();
        private readonly SortedSet<(float distance, int node)> frontier = new SortedSet<(float, int)>();
        private float[] distance;
        private int[] previous;

        public static RecoveryRoadNetwork FromScene(Scene scene)
        {
            var network = new RecoveryRoadNetwork();
            foreach (var root in scene.GetRootGameObjects())
            {
                var group = root.transform.Find("Road network - 24 recovery spurs");
                if (group == null) continue;

                var terrain = root.GetComponentInChildren<Terrain>(true);
                var bridgeSurfaces = new List<BoxCollider>();
                foreach (var box in root.GetComponentsInChildren<BoxCollider>(true))
                {
                    if (box.name == "Bridge drive surface" || box.name == "Bridge entry ramp")
                        bridgeSurfaces.Add(box);
                }
                var samples = new List<Vector3>();
                foreach (Transform road in group)
                {
                    var ids = road.name.Split('-');
                    if (ids.Length != 4 || ids[0] != "Road" || !road.gameObject.activeSelf) continue;
                    var mesh = road.GetComponent<MeshCollider>()?.sharedMesh;
                    if (mesh == null || !mesh.isReadable || mesh.vertexCount < 4 || mesh.vertexCount % 2 != 0) continue;

                    // The authored drive strip has a left/right pair at each station.
                    // Keep both endpoints and sample roughly every 3.6 m along curves.
                    var vertices = mesh.vertices;
                    samples.Clear();
                    int last = vertices.Length / 2 - 1;
                    for (int station = 0; ; station = Mathf.Min(station + 4, last))
                    {
                        int v = station * 2;
                        var point = road.TransformPoint((vertices[v] + vertices[v + 1]) * .5f);
                        if (terrain != null) point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
                        foreach (var deck in bridgeSurfaces)
                        {
                            var bounds = deck.bounds;
                            if (point.x < bounds.min.x || point.x > bounds.max.x ||
                                point.z < bounds.min.z || point.z > bounds.max.z) continue;
                            var ray = new Ray(new Vector3(point.x, bounds.max.y + 1, point.z), Vector3.down);
                            if (deck.Raycast(ray, out var hit, bounds.size.y + 2))
                                point.y = Mathf.Max(point.y, hit.point.y);
                        }
                        samples.Add(point);
                        if (station == last) break;
                    }
                    network.AddRoad(ids[2], ids[3], samples);
                }
                break;
            }
            return network;
        }

        public void AddRoad(string from, string to, IList<Vector3> points)
        {
            if (points.Count < 2) return;
            int a = Junction(from, points[0]);
            for (int i = 1; i < points.Count; i++)
            {
                int b = i == points.Count - 1 ? Junction(to, points[i]) : NewNode(points[i]);
                if (a != b)
                {
                    neighbors[a].Add(b);
                    neighbors[b].Add(a);
                    segments.Add((a, b));
                }
                a = b;
            }
        }

        private int NewNode(Vector3 point)
        {
            positions.Add(point);
            neighbors.Add(new List<int>(2));
            return positions.Count - 1;
        }

        private int Junction(string id, Vector3 point)
        {
            if (junctions.TryGetValue(id, out int index)) return index;
            junctions.Add(id, index = NewNode(point));
            return index;
        }

        private (int segment, Vector3 point) Snap(Vector3 position)
        {
            int nearest = -1;
            var projection = position;
            float best = float.PositiveInfinity;
            for (int i = 0; i < segments.Count; i++)
            {
                var (a, b) = segments[i];
                var delta = positions[b] - positions[a];
                float t = Mathf.Clamp01(Vector3.Dot(position - positions[a], delta) / Mathf.Max(.00001f, delta.sqrMagnitude));
                var point = positions[a] + delta * t;
                float d = (point - position).sqrMagnitude;
                if (d >= best) continue;
                nearest = i;
                best = d;
                projection = point;
            }
            return (nearest, projection);
        }

        public bool TryRoute(Vector3 from, Vector3 to, List<Vector3> result)
        {
            result.Clear();
            if (segments.Count == 0) return false;
            var start = Snap(from);
            var finish = Snap(to);
            if (start.segment != finish.segment)
            {
                int end = FindPath(start, finish);
                if (end < 0) return false;
                for (int node = end; node >= 0; node = previous[node])
                    result.Add(positions[node]);
                result.Reverse();
            }
            if (result.Count == 0 || (result[0] - start.point).sqrMagnitude > .001f)
                result.Insert(0, start.point);
            if ((result[result.Count - 1] - finish.point).sqrMagnitude > .001f)
                result.Add(finish.point);
            return true;
        }

        private int FindPath((int segment, Vector3 point) start, (int segment, Vector3 point) finish)
        {
            if (distance == null || distance.Length != positions.Count)
            {
                distance = new float[positions.Count];
                previous = new int[positions.Count];
            }
            System.Array.Fill(distance, float.PositiveInfinity);
            frontier.Clear();
            var startEdge = segments[start.segment];
            Relax(startEdge.a, Vector3.Distance(start.point, positions[startEdge.a]), -1);
            Relax(startEdge.b, Vector3.Distance(start.point, positions[startEdge.b]), -1);
            var endEdge = segments[finish.segment];
            int end = -1;
            float best = float.PositiveInfinity;
            while (frontier.Count != 0)
            {
                var entry = frontier.Min;
                frontier.Remove(entry);
                if (entry.distance >= best) break;
                int node = entry.node;
                if (node == endEdge.a || node == endEdge.b)
                {
                    float score = entry.distance + Vector3.Distance(positions[node], finish.point);
                    if (score < best) { best = score; end = node; }
                }
                foreach (int next in neighbors[node])
                    Relax(next, entry.distance + Vector3.Distance(positions[node], positions[next]), node);
            }
            return end;
        }

        private void Relax(int node, float candidate, int predecessor)
        {
            if (candidate >= distance[node]) return;
            if (!float.IsPositiveInfinity(distance[node])) frontier.Remove((distance[node], node));
            previous[node] = predecessor;
            distance[node] = candidate;
            frontier.Add((candidate, node));
        }
    }
}

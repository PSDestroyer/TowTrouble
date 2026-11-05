using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        // Call after the existing court/road authoring. This changes decoration
        // only: both driver spawns, the garage radius and every collider stay intact.
        private static void BuildNaturalGarageParking(Transform root, Vector3 garage)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Parking authoring requires Edit Mode.");
            var shader = Shader.Find("TowTrouble/Road Wear");
            if (shader == null || terrain == null || terrain.terrainData == null)
                throw new InvalidOperationException("Workshop parking needs the existing ground and road-wear shader.");
            var roadRoot = environment.Find("Road network - 24 recovery spurs");
            var roads = roadRoot == null ? Array.Empty<MeshCollider>() : roadRoot.GetComponentsInChildren<MeshCollider>()
                .Where(c => Flat(c.bounds.ClosestPoint(garage) - garage).sqrMagnitude < 25 * 25).ToArray();

            RemoveLegacyGarageParking(root);
            var previous = root.Find("Natural workshop parking marks");
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            var parking = PolishGroup(root, "Natural workshop parking marks");
            Undo.RegisterCreatedObjectUndo(parking.gameObject, "Replace workshop parking decoration");

            const string paintName = "Workshop worn cream bay paint";
            var paint = new Material(shader) { name = paintName, enableInstancing = true, renderQueue = 3010 };
            paint.SetTexture("_BaseMap", Texture2D.whiteTexture);
            paint.SetColor("_BaseColor", new Color(.84f, .79f, .65f, .57f));
            paint = VisualAsset(paint, "GarageServiceBase/" + paintName + ".mat");
            var vertices = new List<Vector3>(); var colors = new List<Color>();
            var uv = new List<Vector2>(); var triangles = new List<int>();
            int stroke = 0;
            // These centres mirror the existing two authored driver slots. Only
            // four short corner hints per slot remain; there is no enclosing box.
            foreach (float slotX in new[] { -2.5f, 2.5f })
                foreach (float side in new[] { -1f, 1f })
                    foreach (float end in new[] { -1f, 1f })
                    {
                        var corner = garage + new Vector3(slotX + side * 1.65f, 0, -2 + end * 2.75f);
                        AddGarageParkingStroke(parking, corner, corner - Vector3.forward * end * .78f,
                            stroke++, roads, vertices, colors, uv, triangles);
                        // The perpendicular head is shorter and separated by a
                        // small worn gap, so corners do not become solid L decals.
                        AddGarageParkingStroke(parking, corner - Vector3.right * side * .10f,
                            corner - Vector3.right * side * .58f,
                            stroke++, roads, vertices, colors, uv, triangles);
                    }
            var mesh = new Mesh { name = "Workshop short worn parking corners" };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var marks = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
            marks.transform.SetParent(parking, false); marks.layer = 8; marks.isStatic = true;
            marks.GetComponent<MeshFilter>().sharedMesh = VisualAsset(mesh, "GarageServiceBase/" + mesh.name + ".asset");
            var renderer = marks.GetComponent<MeshRenderer>(); renderer.sharedMaterial = paint;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = true;
            ConfigureDetailLod(marks, 125);

            // Same ground texture and shader as the existing circulation routes,
            // but lighter and confined to the two short parking approaches.
            var wear = GarageWearMaterial("Workshop player bay compaction", .24f);
            GarageWheelPath(parking, "Workshop left player bay impressions", wear, .29f, roads, new[]
            {
                garage + new Vector3(-3.9f, 0, -7.8f), garage + new Vector3(-2.8f, 0, -5.8f),
                garage + new Vector3(-2.5f, 0, -3.2f), garage + new Vector3(-2.5f, 0, .60f)
            });
            GarageWheelPath(parking, "Workshop right player bay impressions", wear, .29f, roads, new[]
            {
                garage + new Vector3(4.8f, 0, -8.1f), garage + new Vector3(3.3f, 0, -5.5f),
                garage + new Vector3(2.5f, 0, -3.2f), garage + new Vector3(2.5f, 0, .60f)
            });
            foreach (var child in parking.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = 8;
                child.gameObject.isStatic = true;
            }
        }

        private static void RemoveLegacyGarageParking(Transform root)
        {
            var oldWorkshop = environment.Find("Central garage - Tow County");
            if (oldWorkshop != null)
                foreach (var child in oldWorkshop.Cast<Transform>().Where(t => t.childCount == 0 &&
                    (t.name == "Yard parking guide" || t.name == "Bay chevron")).ToArray())
                    Undo.DestroyObjectImmediate(child.gameObject);

            // This legacy root belongs to the old demo marker, not garage logic.
            // Remove exact visual leaves only; preserve any later-added siblings,
            // child props or components instead of deleting the whole subtree.
            foreach (var legacy in root.gameObject.scene.GetRootGameObjects().Where(g => g.name == "Garage service area"))
            {
                foreach (var child in legacy.transform.Cast<Transform>().Where(t => t.childCount == 0 &&
                    (t.name == "Service bay edge" || t.name == "Garage prompt")).ToArray())
                    Undo.DestroyObjectImmediate(child.gameObject);
                if (legacy.transform.childCount == 0 && legacy.GetComponents<Component>().Length == 1)
                    Undo.DestroyObjectImmediate(legacy);
            }
        }

        private static void AddGarageParkingStroke(Transform parent, Vector3 start, Vector3 end, int seed,
            MeshCollider[] roads, List<Vector3> vertices, List<Color> colors, List<Vector2> uv, List<int> triangles)
        {
            var forward = Flat(end - start).normalized;
            var across = Vector3.Cross(Vector3.up, forward);
            int steps = Mathf.Max(6, Mathf.CeilToInt(Vector3.Distance(start, end) / .085f));
            const int columns = 5;
            int first = vertices.Count;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                var middle = Vector3.Lerp(start, end, t);
                float noise = Mathf.PerlinNoise(seed * 2.31f + 13, t * 7.5f + 2);
                float width = .048f * Mathf.Lerp(.78f, 1.08f, noise);
                float worn = Mathf.SmoothStep(.12f, 1, Mathf.InverseLerp(.28f, .72f, noise));
                float tip = Mathf.SmoothStep(0, 1, t * 8) * Mathf.SmoothStep(0, 1, (1 - t) * 8);
                for (int column = 0; column < columns; column++)
                {
                    float u = column / (float)(columns - 1);
                    var p = middle + across * ((u * 2 - 1) * width);
                    p = Ground(p) - Vector3.up * .08f;
                    var ray = new Ray(p + Vector3.up * 3, Vector3.down);
                    foreach (var road in roads)
                        if (road.Raycast(ray, out var hit, 4) && hit.point.y > p.y) p.y = hit.point.y;
                    p.y += .026f;
                    vertices.Add(parent.InverseTransformPoint(p)); uv.Add(new Vector2(p.x, p.z));
                    float edge = Mathf.SmoothStep(0, 1, Mathf.Min(u, 1 - u) * 5);
                    colors.Add(new Color(1, 1, 1, tip * edge * worn));
                    if (i < steps && column < columns - 1)
                    {
                        int v = first + i * columns + column;
                        triangles.AddRange(new[] { v, v + columns, v + 1, v + 1, v + columns, v + columns + 1 });
                    }
                }
            }
        }
    }
}

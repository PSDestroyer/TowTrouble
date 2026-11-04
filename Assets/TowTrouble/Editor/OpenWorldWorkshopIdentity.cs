using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static void BuildWorkshopIdentity(Transform facade)
        {
            var sign = PolishGroup(facade, "Mounted Tow Trouble workshop sign");
            // The clipped shoulders fit below the gable: h = 8.15 - abs(x) * .30.
            // An actual shallow enclosure, narrow rim and mounting feet support the printed logo.
            WorkshopPlaque(sign, "Workshop sign metal enclosure", WorkshopFinish("Metal"), 1, 14.66f, .18f);
            WorkshopPlaque(sign, "Workshop sign enamel rim", WorkshopFinish("Trim"), .958f, 14.566f, .03f);
            WorkshopPlaque(sign, "Workshop sign painted face", WorkshopFinish("Door"), .916f, 14.541f, .025f);
            using (var shapes = new GarageFacadeShapes())
            {
                foreach (float x in new[] { -2.57f, 2.57f })
                    foreach (float y in new[] { 5.55f, 6.67f })
                    {
                        shapes.Box(WorkshopFinish("Metal"), new Vector3(x, y, 15.03f), new Vector3(.14f, .14f, .60f));
                        shapes.Box(WorkshopFinish("Trim"), new Vector3(x, y, 14.526f), new Vector3(.075f, .075f, .024f));
                        shapes.Box(WorkshopFinish("Metal"), new Vector3(x, y, 14.511f), new Vector3(.035f, .035f, .012f));
                    }
                shapes.Finish(sign, "Workshop sign fittings");
            }

            var image = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TowTrouble/Art/UI/TowTrouble-logo.png");
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (image == null || shader == null) throw new InvalidOperationException("The original Tow Trouble logo and URP Unlit shader are required.");
            var logo = new Material(shader) { name = "Mounted workshop original logo", enableInstancing = true };
            logo.SetTexture("_BaseMap", image); logo.SetColor("_BaseColor", Color.white);
            logo.SetFloat("_AlphaClip", 1); logo.SetFloat("_Cutoff", .2f); logo.EnableKeyword("_ALPHATEST_ON");
            logo.SetFloat("_Cull", 0); logo.renderQueue = 2450;
            logo = VisualAsset(logo, "GarageServiceBase/Mounted workshop original logo.mat");
            var print = GameObject.CreatePrimitive(PrimitiveType.Quad);
            print.name = "Original Tow Trouble enamel print";
            UnityEngine.Object.DestroyImmediate(print.GetComponent<Collider>());
            print.transform.SetParent(sign, false);
            print.transform.localPosition = new Vector3(0, 6.4f, 14.5235f);
            print.transform.localScale = new Vector3(4.8f, 4.8f * image.height / image.width, 1);
            print.isStatic = true;
            var renderer = print.GetComponent<MeshRenderer>(); renderer.sharedMaterial = logo;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            ConfigureDetailLod(sign.gameObject, 900);

            var lamps = PolishGroup(facade, "Warm exterior service lamps");
            using (var shapes = new GarageFacadeShapes())
            {
                foreach (float x in new[] { -6.7f, 6.7f })
                {
                    shapes.Box(WorkshopFinish("Metal"), new Vector3(x, 5.01f, 14.94f), new Vector3(.22f, .36f, .10f));
                    shapes.Box(WorkshopFinish("Metal"), new Vector3(x, 5.08f, 14.65f), new Vector3(.10f, .10f, .54f));
                    shapes.BevelBox(WorkshopFinish("RoofEdge"), new Vector3(x, 4.99f, 14.37f), new Vector3(.56f, .25f, .56f), .06f);
                    shapes.Box(WorkshopFinish("Trim"), new Vector3(x, 4.867f, 14.37f), new Vector3(.48f, .035f, .48f));
                    shapes.Box(WorkshopFinish("Lamp"), new Vector3(x, 4.843f, 14.37f), new Vector3(.38f, .025f, .38f));
                }
                shapes.Finish(lamps, "Exterior lamp housings");
            }
            // Emissive lenses provide the warm accent; no realtime Light or interior is created.
            ConfigureDetailLod(lamps.gameObject, 250);
        }

        private static void WorkshopPlaque(Transform parent, string name, Material material, float scale, float z, float depth)
        {
            var outline = new[] { new Vector2(-2.92f, -.97f), new Vector2(-2.74f, -1.15f),
                new Vector2(2.74f, -1.15f), new Vector2(2.92f, -.97f), new Vector2(2.92f, .38f),
                new Vector2(1.4f, 1.15f), new Vector2(-1.4f, 1.15f), new Vector2(-2.92f, .38f) };
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            // Independent vertices keep the sign's front and thin edge normals crisp.
            foreach (int face in new[] { -1, 1 })
            {
                int start = vertices.Count;
                vertices.Add(new Vector3(0, 6.4f, z + face * depth * .5f));
                foreach (var p in outline) vertices.Add(new Vector3(p.x * scale, 6.4f + p.y * scale, z + face * depth * .5f));
                for (int i = 0; i < outline.Length; i++)
                {
                    int a = start + 1 + i, b = start + 1 + (i + 1) % outline.Length;
                    triangles.AddRange(face < 0 ? new[] { start, b, a } : new[] { start, a, b });
                }
            }
            for (int i = 0; i < outline.Length; i++)
            {
                var a = outline[i] * scale; var b = outline[(i + 1) % outline.Length] * scale;
                int start = vertices.Count;
                vertices.Add(new Vector3(a.x, 6.4f + a.y, z - depth * .5f));
                vertices.Add(new Vector3(b.x, 6.4f + b.y, z - depth * .5f));
                vertices.Add(new Vector3(a.x, 6.4f + a.y, z + depth * .5f));
                vertices.Add(new Vector3(b.x, 6.4f + b.y, z + depth * .5f));
                triangles.AddRange(new[] { start, start + 1, start + 2, start + 1, start + 3, start + 2 });
            }
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false); go.isStatic = true;
            go.GetComponent<MeshFilter>().sharedMesh = VisualAsset(mesh, "GarageServiceBase/" + name + ".asset");
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void BuildWorkshopApronDetails(Transform facade)
        {
            var apron = PolishGroup(facade, "Workshop flush thresholds and drainage");
            using (var shapes = new GarageFacadeShapes())
            {
                foreach (float x in new[] { -4.45f, 4.45f })
                {
                    var threshold = WorkshopApronPoint(facade, new Vector3(x, 0, 14.7f), .008f);
                    shapes.Box(WorkshopFinish("Foundation"), threshold, new Vector3(6.2f, .025f, .6f));
                    // Short separate grates leave the pedestrian centre and door pillars unobstructed.
                    var drain = WorkshopApronPoint(facade, new Vector3(x, 0, 13.35f), .008f);
                    shapes.Box(WorkshopFinish("Metal"), drain, new Vector3(6.15f, .014f, .18f));
                    foreach (float side in new[] { -1f, 1f })
                        shapes.Box(WorkshopFinish("Foundation"), drain + Vector3.forward * side * .104f, new Vector3(6.2f, .018f, .028f));
                    for (int bar = 0; bar < 34; bar++)
                    {
                        var p = WorkshopApronPoint(facade, new Vector3(x - 2.97f + bar * .18f, 0, 13.35f), .021f);
                        shapes.Box(WorkshopFinish("RoofEdge"), p, new Vector3(.073f, .012f, .16f));
                    }
                }
                shapes.Finish(apron, "Flush service thresholds");
            }
            foreach (var renderer in apron.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.Off;
            ConfigureDetailLod(apron.gameObject, 180);
            BuildWorkshopPillarDust(facade);
        }

        private static Vector3 WorkshopApronPoint(Transform facade, Vector3 local, float lift)
        {
            // Ground includes an authoring offset of .08m; these details sit directly on the terrain.
            var point = Ground(facade.TransformPoint(local)) - Vector3.up * .08f;
            var slab = environment.Find("Central garage - Tow County/Workshop slab");
            var renderer = slab == null ? null : slab.GetComponent<Renderer>();
            if (renderer != null && slab.gameObject.activeInHierarchy)
            {
                // The retained apron slab is slightly above the terrain. Only that
                // specific surface counts; the new raised wall foundation must not lift these details.
                var bounds = renderer.bounds;
                if (point.x >= bounds.min.x && point.x <= bounds.max.x && point.z >= bounds.min.z && point.z <= bounds.max.z)
                    point.y = Mathf.Max(point.y, bounds.max.y);
            }
            return facade.InverseTransformPoint(point + Vector3.up * lift);
        }

        private static void BuildWorkshopPillarDust(Transform facade)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(VisualRoot + "/RoadWear.shader");
            if (shader == null) throw new InvalidOperationException("The existing terrain wear shader is required for soft pillar contact.");
            var material = new Material(shader) { name = "Workshop warm pillar contact dust", enableInstancing = true };
            var tint = WorkshopFinish("Dust").GetColor("_BaseColor"); tint.a = .24f;
            material.SetColor("_BaseColor", tint); material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            material = VisualAsset(material, "GarageServiceBase/Workshop warm pillar contact dust.mat");
            const int steps = 24;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var colors = new List<Color>(); var triangles = new List<int>();
            foreach (float x in new[] { -8.65f, 8.65f })
            {
                int start = vertices.Count;
                var center = new Vector3(x, 0, 14.8f);
                vertices.Add(WorkshopApronPoint(facade, center, .009f)); uv.Add(Vector2.one * .5f); colors.Add(new Color(1, 1, 1, .65f));
                for (int ring = 0; ring < 2; ring++)
                    for (int i = 0; i < steps; i++)
                    {
                        float angle = i / (float)steps * Mathf.PI * 2;
                        float radius = (ring == 0 ? .37f : 1) * (1 + .09f * Mathf.Sin(angle * 3 + x) + .06f * Mathf.Sin(angle * 5));
                        var offset = new Vector3(Mathf.Cos(angle) * .78f, 0, Mathf.Sin(angle) * .56f) * radius;
                        vertices.Add(WorkshopApronPoint(facade, center + offset, .009f));
                        uv.Add(new Vector2(offset.x, offset.z)); colors.Add(new Color(1, 1, 1, ring == 0 ? .48f : 0));
                    }
                for (int i = 0; i < steps; i++)
                {
                    int a = start + 1 + i, b = start + 1 + (i + 1) % steps;
                    triangles.AddRange(new[] { start, b, a, a, b, a + steps, b, b + steps, a + steps });
                }
            }
            var mesh = new Mesh { name = "Soft workshop pillar contact" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(facade, false); go.isStatic = true;
            go.GetComponent<MeshFilter>().sharedMesh = VisualAsset(mesh, "GarageServiceBase/Soft workshop pillar contact.asset");
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
            ConfigureDetailLod(go, 120);
        }
    }
}

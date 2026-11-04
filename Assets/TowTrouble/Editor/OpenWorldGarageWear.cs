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
        private static void BuildGarageCourtWear(Transform root, Vector3 garage)
        {
            var previous = root.Find("Workshop vehicle circulation wear");
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            var wear = PolishGroup(root, "Workshop vehicle circulation wear");
            Undo.RegisterCreatedObjectUndo(wear.gameObject, "Add workshop wheel impressions");
            var main = GarageWearMaterial("Workshop compacted circulation", .56f);
            var branch = GarageWearMaterial("Workshop service wheel impressions", .43f);
            var old = GarageWearMaterial("Workshop faded parking impressions", .32f);
            var roadRoot = environment.Find("Road network - 24 recovery spurs");
            var roads = roadRoot.GetComponentsInChildren<MeshCollider>()
                .Where(c => Flat(c.bounds.ClosestPoint(garage) - garage).magnitude < 85).ToArray();
            // Coordinates describe the existing yard. Offset the entire layout if
            // its garage anchor is moved; each branch has its own persistent mesh.
            Vector3 P(float x, float z) => garage + new Vector3(x + 9, 0, z - 9);
            GarageWheelPath(wear, "Workshop receiving loop", main, .30f, roads,
                new[] { P(3,12), P(8,3), P(8,-5), P(-7,-9), P(-27,-9), P(-44,-9), P(-51,-4), P(-52,5), P(-48,12), P(-38,14) });
            GarageWheelPath(wear, "Workshop sheltered parking approach", branch, .26f, roads,
                new[] { P(-44,-9), P(-47,-14), P(-47,-20), P(-47,-24) });
            GarageWheelPath(wear, "Workshop open parking approach", old, .25f, roads,
                new[] { P(-33,-10), P(-37,-15), P(-38,-20), P(-38,-24) });
            GarageWheelPath(wear, "Workshop inspection approach", branch, .27f, roads,
                new[] { P(-43,-13), P(-39,-18), P(-35,-22), P(-31,-26.5f), P(-30,-27) });
            GarageWheelPath(wear, "Workshop supplies dropoff approach", old, .26f, roads,
                new[] { P(1,-5), P(-4,-10), P(-8,-15), P(-10,-21) });
            GarageWheelPath(wear, "Workshop cable preparation approach", branch, .25f, roads,
                new[] { P(-24,-9), P(-30,-8), P(-33,-5), P(-33,-1) });
        }

        private static Material GarageWearMaterial(string name, float opacity)
        {
            var shader = Shader.Find("TowTrouble/Wheel Impressions");
            if (shader == null) throw new InvalidOperationException("Wheel impression shader is missing.");
            var material = new Material(shader) { name = name, enableInstancing = true };
            material.SetTexture("_BaseMap", terrain.terrainData.terrainLayers[1].diffuseTexture);
            material.SetColor("_BaseColor", new Color(.73f, .65f, .52f, opacity));
            material.SetFloat("_Compaction", .81f);
            material.SetFloat("_HardSurface", .36f);
            material.SetFloat("_RutDepth", .022f);
            return VisualAsset(material, "GarageServiceBase/" + name + ".mat");
        }

        private static void GarageWheelPath(Transform parent, string name, Material material,
            float halfWidth, MeshCollider[] roads, Vector3[] spine)
        {
            var points = new List<Vector3>();
            // Cubic Hermite interpolation with distance-limited tangents keeps
            // wheel pairs continuous through turns, without sharp polyline corners.
            for (int i = 0; i < spine.Length - 1; i++)
            {
                var a = spine[i]; var b = spine[i + 1];
                float length = Flat(b - a).magnitude;
                var incoming = i == 0 ? b - a : b - spine[i - 1];
                var outgoing = i == spine.Length - 2 ? b - a : spine[i + 2] - a;
                var ta = Flat(incoming).normalized * length * .8f;
                var tb = Flat(outgoing).normalized * length * .8f;
                int samples = Mathf.Max(8, Mathf.CeilToInt(length / .28f));
                for (int j = 0; j < samples; j++)
                {
                    float t = j / (float)samples, t2 = t * t, t3 = t2 * t;
                    points.Add((2*t3-3*t2+1)*a + (t3-2*t2+t)*ta + (-2*t3+3*t2)*b + (t3-t2)*tb);
                }
            }
            points.Add(spine.Last());
            var distance = new float[points.Count];
            for (int i = 1; i < points.Count; i++) distance[i] = distance[i-1] + Flat(points[i]-points[i-1]).magnitude;
            float total = distance.Last();
            var vertices = new List<Vector3>(); var uv = new List<Vector2>();
            var rut = new List<Vector2>(); var ends = new List<Vector2>();
            var colors = new List<Color>(); var triangles = new List<int>();
            for (int lane = 0; lane < 2; lane++)
            {
                int offset = vertices.Count;
                for (int i = 0; i < points.Count; i++)
                {
                    var tangent = Flat(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;
                    var across = Vector3.Cross(Vector3.up,tangent);
                    float d = distance[i];
                    float varied = .64f + .36f * Mathf.PerlinNoise(d*.41f + lane*5.3f, halfWidth*19);
                    float fade = Mathf.SmoothStep(0,1,d/2) * Mathf.SmoothStep(0,1,(total-d)/1.8f);
                    for (int c = 0; c < 7; c++)
                    {
                        float u = c / 6f;
                        var p = points[i] + across * ((lane==0 ? -.82f : .82f) + (u*2-1)*halfWidth);
                        // Ground includes a prop placement allowance. Remove it,
                        // then conform to drive meshes at junctions as well.
                        p = Ground(p) - Vector3.up * .08f;
                        var ray = new Ray(p + Vector3.up*3,Vector3.down);
                        foreach (var road in roads)
                            if (road.Raycast(ray,out var hit,4) && hit.point.y > p.y) p.y = hit.point.y;
                        p.y += .018f;
                        vertices.Add(parent.InverseTransformPoint(p)); uv.Add(new Vector2(p.x,p.z)*.3f);
                        rut.Add(new Vector2(u,d)); ends.Add(new Vector2((total-d)/1.2f,1));
                        colors.Add(new Color(1,1,1,varied*fade));
                        if (i < points.Count-1 && c < 6)
                        {
                            int v = offset + i*7+c;
                            triangles.AddRange(new[]{v,v+7,v+1,v+1,v+7,v+8});
                        }
                    }
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetUVs(1,rut); mesh.SetUVs(2,ends);
            mesh.SetColors(colors); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(parent,false); go.layer=8; go.isStatic=true;
            go.GetComponent<MeshFilter>().sharedMesh=VisualAsset(mesh,"GarageServiceBase/"+name+".asset");
            var renderer=go.GetComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=true;
            ConfigureDetailLod(go,210);
        }
    }
}

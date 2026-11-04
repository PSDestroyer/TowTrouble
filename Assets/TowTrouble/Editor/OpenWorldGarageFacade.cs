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
        // The workshop is an exterior landmark. Closed doors remove the need for
        // the old furnished interior, and keep the driving/Customize apron open.
        private static void BuildGarageFacade(Transform root, Vector3 garage)
        {
            var workshop = environment.Find("Central garage - Tow County");
            if (workshop == null) throw new InvalidOperationException("The existing workshop shell is missing.");
            var interior = new HashSet<string>(StringComparer.Ordinal)
            {
                "Mechanic workbench", "Workbench leg", "Pegboard", "Hanging tool",
                "Rolling tool cabinet", "Tool drawer handle", "Tire rack", "Hot Rod spare wheel",
                "Ceiling work light", "Workshop task spot", "Workshop warm task light",
                "Service bay floor stripe", "Service sign post", "Service information board",
                "SERVICE\nTOW COUNTY", "Visible roof cross beam", "Roof brace", "GARAJ  [G]",
                "Rear wall", "Side wall", "Rear timber batten", "Siding batten",
                "Pitched corrugated roof", "Roof seam", "Ridge cap", "Open bay pillar",
                "Pillar safety base", "Front timber fascia", "Service canopy", "Canopy post",
                "Tow Trouble enamel logo", "SERVICE  /  RECOVERY", "Bay lamp bracket", "Bay lamp lens",
                "Workshop side window", "Window mullion", "Window surround", "Workshop brick foundation"
            };
            foreach (var child in workshop.Cast<Transform>().ToArray())
                if (interior.Contains(child.name) ||
                    ((child.name == "Workshop service drum" || child.name == "Drum metal band") && child.position.x > garage.x))
                    Undo.DestroyObjectImmediate(child.gameObject);

            var previous = root.Find("Workshop exterior service frontage");
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            var facade = PolishGroup(root, "Workshop exterior service frontage");
            facade.position = garage + Vector3.down * .075f;
            Undo.RegisterCreatedObjectUndo(facade.gameObject, "Build workshop exterior frontage");
            var slab = workshop.Find("Workshop slab");
            if (slab != null)
            {
                var renderer = slab.GetComponent<MeshRenderer>();
                Undo.RecordObject(renderer, "Match workshop apron finish");
                renderer.sharedMaterial = WorkshopFinish("Foundation");
            }

            var blue = GarageFacadeMaterial("Workshop faded petrol blue");
            var metal = GarageFacadeMaterial("Workshop roof metal");
            var cream = GarageFacadeMaterial("Workshop ivory trim");
            var timber = GarageFacadeMaterial("Workshop aged timber");
            var red = GarageFacadeMaterial("Workshop painted boards");
            var glass = GarageFacadeMaterial("Workshop blue window glass");
            var rubber = GarageFacadeMaterial("Workshop black rubber");

            BuildPolishedWorkshopShell(facade);
            BuildPolishedWorkshopDoors(facade);
            BuildWorkshopIdentity(facade);
            BuildWorkshopApronDetails(facade);

            // Supplies sit beyond both corners of the open apron, leaving a clear
            // silhouette around the customised car and all front/rear camera views.
            var supplies = PolishGroup(facade, "Exterior maintenance equipment");
            using (var shapes = new GarageFacadeShapes())
            {
                var cabinet = new Vector3(-11.4f, 0, 16.4f);
                shapes.Box(red, cabinet + new Vector3(0, .82f, 0), new Vector3(2.05f, 1.42f, .84f));
                shapes.Box(metal, cabinet + new Vector3(0, 1.57f, 0), new Vector3(2.2f, .12f, .98f));
                for (int drawer = 0; drawer < 4; drawer++)
                {
                    shapes.Box(metal, cabinet + new Vector3(0, .37f + drawer * .3f, -.432f), new Vector3(1.88f, .025f, .025f));
                    shapes.Box(cream, cabinet + new Vector3(0, .51f + drawer * .3f, -.465f), new Vector3(1.15f, .055f, .065f));
                }
                foreach (float dx in new[] { -.77f, .77f })
                    foreach (float dz in new[] { -.25f, .25f })
                        shapes.Box(metal, cabinet + new Vector3(dx, .13f, dz), new Vector3(.16f, .25f, .16f));
                GarageFacadeCollider(supplies, "Tool cabinet collision", cabinet + Vector3.up * .82f, new Vector3(2.05f, 1.64f, .84f));

                // A low rack of recognisable spare wheels replaces featureless
                // cylinders. The timber cradle shares the workshop's material.
                var rack = new Vector3(11.35f, 0, 16.6f);
                foreach (float dz in new[] { -.42f, .42f })
                    shapes.Box(timber, rack + new Vector3(0, .22f, dz), new Vector3(3.65f, .15f, .14f));
                foreach (float dx in new[] { -1.72f, 1.72f })
                    shapes.Box(metal, rack + new Vector3(dx, .48f, 0), new Vector3(.12f, .96f, 1.02f));
                GarageFacadeCollider(supplies, "Spare wheel rack collision", rack + Vector3.up * .55f, new Vector3(3.65f, 1.1f, 1.02f));

                // Wall-mounted hose and a cable reel are small semantic clues,
                // grouped at the side of the building rather than scattered.
                shapes.Box(timber, new Vector3(-9.28f, 1.75f, 19.1f), new Vector3(.16f, 1.45f, 1.45f));
                shapes.Box(metal, new Vector3(-9.50f, 1.92f, 19.1f), new Vector3(.43f, .12f, .5f));
                for (int loop = 0; loop < 3; loop++)
                    shapes.Ring(rubber, new Vector3(-9.58f - loop * .07f, 1.6f, 19.1f), .47f, .035f, Quaternion.Euler(0, 90, 0));
                shapes.Box(rubber, new Vector3(-9.8f, .86f, 19.55f), new Vector3(.065f, .72f, .065f));
                shapes.Box(cream, new Vector3(-9.8f, .49f, 19.55f), new Vector3(.09f, .16f, .09f));

                var reel = new Vector3(11.4f, .63f, 19.3f);
                shapes.Ring(timber, reel + Vector3.forward * .28f, .52f, .075f, Quaternion.identity);
                shapes.Ring(timber, reel - Vector3.forward * .28f, .52f, .075f, Quaternion.identity);
                for (int loop = 0; loop < 7; loop++)
                    shapes.Ring(rubber, reel + Vector3.forward * (-.23f + loop * .077f), .35f, .048f, Quaternion.identity);
                shapes.Box(metal, reel, new Vector3(.14f, .14f, .82f));
                foreach (float dz in new[] { -.42f, .42f })
                    shapes.Box(metal, reel + new Vector3(0, -.32f, dz), new Vector3(.12f, .64f, .12f));
                shapes.Box(metal, reel + Vector3.down * .58f, new Vector3(.92f, .09f, 1.05f));
                GarageFacadeCollider(supplies, "Cable reel collision", reel, new Vector3(1.12f, 1.26f, 1.05f));
                shapes.Finish(supplies, "Maintenance equipment");
            }
            for (int wheel = 0; wheel < 3; wheel++)
                GarageFacadeWheel(supplies, wheel, new Vector3(10.2f + wheel * 1.15f, .28f, 16.6f));
            ConfigureDetailLod(supplies.gameObject, 180);
        }

        private static Material GarageFacadeMaterial(string name)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(VisualRoot + "/" + name + ".mat");
            if (material == null) throw new InvalidOperationException("Missing workshop exterior material: " + name);
            return material;
        }

        private static void GarageFacadeCollider(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name, typeof(BoxCollider));
            go.transform.SetParent(parent, false);
            go.layer = 8;
            go.GetComponent<BoxCollider>().center = center;
            go.GetComponent<BoxCollider>().size = size;
        }

        private static void GarageFacadeWheel(Transform parent, int index, Vector3 position)
        {
            string name = index == 1 ? "Wheel_HotRod_Stamp_Wider" : "Wheel_Offroad_Lvl2";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TowTrouble/Garage/Parts/" + name + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing workshop spare wheel " + name);
            // Copy mesh/renderers only; no vehicle controllers, rigidbodies,
            // colliders or imported prefab behaviours enter the decorative rack.
            var wheel = new GameObject("Spare wheel " + (index + 1));
            wheel.transform.SetParent(parent, false);
            foreach (var source in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = source.GetComponent<MeshRenderer>();
                if (source.sharedMesh == null || renderer == null) continue;
                var copy = new GameObject(source.name, typeof(MeshFilter), typeof(MeshRenderer));
                copy.transform.SetParent(wheel.transform, false);
                copy.transform.localPosition = prefab.transform.InverseTransformPoint(source.transform.position);
                copy.transform.localRotation = Quaternion.Inverse(prefab.transform.rotation) * source.transform.rotation;
                copy.transform.localScale = source.transform.lossyScale;
                copy.GetComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                copy.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            }
            wheel.transform.localRotation = Quaternion.Euler(0, 90, 0);
            var bounds = ScenicBounds(wheel);
            wheel.transform.localScale = Vector3.one * (1.02f / Mathf.Max(.01f, bounds.size.y));
            bounds = ScenicBounds(wheel);
            var destination = parent.TransformPoint(position);
            wheel.transform.position += destination - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        // Short-lived editor geometry builder. Result: one persistent mesh and
        // renderer per material, plus only the explicitly authored box colliders.
        private sealed class GarageFacadeShapes : IDisposable
        {
            private readonly Dictionary<Material, List<CombineInstance>> parts = new Dictionary<Material, List<CombineInstance>>();
            private readonly List<Mesh> temporary = new List<Mesh>();
            private readonly Mesh cube;

            internal GarageFacadeShapes()
            {
                var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube = primitive.GetComponent<MeshFilter>().sharedMesh;
                UnityEngine.Object.DestroyImmediate(primitive);
            }

            internal void Box(Material material, Vector3 center, Vector3 size)
            {
                Add(material, cube, Matrix4x4.TRS(center, Quaternion.identity, size));
            }

            internal void Box(Material material, Vector3 center, Vector3 size, Quaternion rotation)
            {
                Add(material, cube, Matrix4x4.TRS(center, rotation, size));
            }

            internal void Beam(Material material, Vector3 start, Vector3 end, float width)
            {
                var delta=end-start;
                if(delta.sqrMagnitude<.00001f)return;
                Box(material,(start+end)*.5f,new Vector3(width,delta.magnitude,width),Quaternion.FromToRotation(Vector3.up,delta));
            }

            internal void BevelBox(Material material, Vector3 center, Vector3 size, float bevel)
            {
                var h=size*.5f;
                float b=Mathf.Min(bevel,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.8f);
                var inner=h-Vector3.one*b;
                var vs=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
                void Face(params Vector3[] polygon)
                {
                    var normal=Vector3.Cross(polygon[1]-polygon[0],polygon[2]-polygon[0]);
                    var midpoint=polygon.Aggregate(Vector3.zero,(a,p)=>a+p)/polygon.Length;
                    if(Vector3.Dot(normal,midpoint)<0){Array.Reverse(polygon);normal=-normal;}
                    int offset=vs.Count;
                    int axis=Mathf.Abs(normal.x)>Mathf.Abs(normal.y)?0:1;
                    if(Mathf.Abs(normal.z)>Mathf.Abs(normal[axis]))axis=2;
                    int u=(axis+1)%3,v=(axis+2)%3;
                    foreach(var p in polygon){vs.Add(p);uv.Add(new Vector2(p[u]/size[u]+.5f,p[v]/size[v]+.5f));}
                    for(int i=1;i<polygon.Length-1;i++)tris.AddRange(new[]{offset,offset+i,offset+i+1});
                }
                foreach(int axis in new[]{0,1,2})foreach(int sign in new[]{-1,1})
                {
                    int u=(axis+1)%3,v=(axis+2)%3;
                    var face=new Vector3[4];int i=0;
                    foreach(var corner in new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(-1,1)})
                    {var p=Vector3.zero;p[axis]=sign*h[axis];p[u]=corner.x*inner[u];p[v]=corner.y*inner[v];face[i++]=p;}
                    Face(face);
                }
                for(int a=0;a<3;a++)for(int d=a+1;d<3;d++)foreach(int sa in new[]{-1,1})foreach(int sd in new[]{-1,1})
                {
                    int c=3-a-d;
                    var p=Vector3.zero;p[a]=sa*h[a];p[d]=sd*inner[d];p[c]=-inner[c];
                    var q=Vector3.zero;q[a]=sa*inner[a];q[d]=sd*h[d];q[c]=-inner[c];
                    var r=q;r[c]=inner[c];var s=p;s[c]=inner[c];Face(p,q,r,s);
                }
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                    Face(new Vector3(x*h.x,y*inner.y,z*inner.z),new Vector3(x*inner.x,y*h.y,z*inner.z),new Vector3(x*inner.x,y*inner.y,z*h.z));
                var mesh=new Mesh{name="Bevelled workshop detail"};
                mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                temporary.Add(mesh);Add(material,mesh,Matrix4x4.TRS(center,Quaternion.identity,Vector3.one));
            }

            private void Add(Material material, Mesh mesh, Matrix4x4 matrix)
            {
                if (!parts.TryGetValue(material, out var instances)) parts.Add(material, instances = new List<CombineInstance>());
                instances.Add(new CombineInstance { mesh = mesh, transform = matrix });
            }

            internal void Ring(Material material, Vector3 center, float radius, float tube, Quaternion rotation)
            {
                const int ringSteps = 32, tubeSteps = 6;
                var vertices = new Vector3[ringSteps * tubeSteps];
                var triangles = new int[ringSteps * tubeSteps * 6];
                int tri = 0;
                for (int r = 0; r < ringSteps; r++)
                    for (int t = 0; t < tubeSteps; t++)
                    {
                        float a = r * Mathf.PI * 2 / ringSteps, b = t * Mathf.PI * 2 / tubeSteps;
                        vertices[r * tubeSteps + t] = new Vector3(Mathf.Cos(a) * (radius + Mathf.Cos(b) * tube), Mathf.Sin(a) * (radius + Mathf.Cos(b) * tube), Mathf.Sin(b) * tube);
                        int i = r * tubeSteps + t, nextR = (r + 1) % ringSteps * tubeSteps + t;
                        int nextT = r * tubeSteps + (t + 1) % tubeSteps, diagonal = (r + 1) % ringSteps * tubeSteps + (t + 1) % tubeSteps;
                        triangles[tri++] = i; triangles[tri++] = nextR; triangles[tri++] = nextT;
                        triangles[tri++] = nextT; triangles[tri++] = nextR; triangles[tri++] = diagonal;
                    }
                var mesh = new Mesh { name = "Exterior cable loop", vertices = vertices, triangles = triangles };
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); temporary.Add(mesh);
                Add(material, mesh, Matrix4x4.TRS(center, rotation, Vector3.one));
            }

            internal void Finish(Transform parent, string name)
            {
                foreach (var pair in parts)
                {
                    string meshName = name + " - " + pair.Key.name;
                    var mesh = new Mesh { name = meshName, indexFormat = IndexFormat.UInt32 };
                    mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                    mesh.RecalculateBounds();
                    var go = new GameObject(meshName, typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(parent, false);
                    go.GetComponent<MeshFilter>().sharedMesh = VisualAsset(mesh, "GarageServiceBase/" + meshName + ".asset");
                    go.GetComponent<MeshRenderer>().sharedMaterial = pair.Key;
                }
            }

            public void Dispose()
            {
                foreach (var mesh in temporary) UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
    }
}

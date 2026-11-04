using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        // The delivery lane is deliberately empty. Storage occupies its southern edge;
        // the rest corner is on the far shoulder, outside the garage camera orbit.
        private static void BuildGarageYard(Transform root, Vector3 garage)
        {
            var parking = PolishGroup(root, "Recovered vehicle parking");
            var wood = AssetDatabase.LoadAssetAtPath<Material>(VisualRoot + "/Workshop aged timber.mat");
            var metal = AssetDatabase.LoadAssetAtPath<Material>(VisualRoot + "/Workshop roof metal.mat");
            var paint = PolishMat("Faded parking paint", new Color(.80f, .76f, .59f));
            if (wood == null || metal == null) throw new InvalidOperationException("Workshop exterior materials are missing.");

            BuildGarageParkingCanopy(parking, Ground(garage + new Vector3(-38, 0, -35)));
            var sources = environment.Find("Mountain cliffs and garage grounds/Garage working yard/Parked salvage vehicles");
            if (sources == null || sources.childCount < 2)
                throw new InvalidOperationException("The two authored salvage vehicle visuals are required before replacing the old yard.");
            for (int i = 0; i < 2; i++)
            {
                var position = Ground(garage + new Vector3(i == 0 ? -38 : -29, 0, -35));
                var car = UnityEngine.Object.Instantiate(sources.GetChild(i).gameObject, parking);
                car.name = i == 0 ? "Recovered hauler under shelter" : "Recovered compact awaiting collection";
                car.SetActive(true);
                StripGarageYardProp(car);
                car.transform.rotation = Quaternion.Euler(0, i == 0 ? 0 : -5, 0);
                SeatGarageYardProp(car, position);
                var bounds = ScenicBounds(car);
                var collider = car.AddComponent<BoxCollider>();
                collider.center = car.transform.InverseTransformPoint(bounds.center);
                collider.size = new Vector3(Mathf.Min(bounds.size.x, 2.2f), Mathf.Min(bounds.size.y, 2.3f), Mathf.Min(bounds.size.z, 4.6f)) / car.transform.lossyScale.x;
                ConfigureMountainVegetationLod(car, 180);
                GarageParkingMark(parking, position + new Vector3(-2, 0, .1f), paint);
                GarageParkingMark(parking, position + new Vector3(2, 0, .1f), paint);
            }
            BuildGarageTireRack(parking, Ground(garage + new Vector3(-23, 0, -38)), wood, metal);

            var boundary = PolishGroup(root, "South service boundary");
            foreach (float x in new[] { -50f, -46f, -42f, -38f, -34f })
                GarageYardFence(boundary, Ground(garage + new Vector3(x + 9, 0, -42)), 0, wood);
            BatchGarageYardGeometry(boundary, "South service boundary");
            ConfigureDetailLod(boundary.gameObject, 210);
            BuildGarageRestCorner(root, garage, wood, metal);
            foreach (var t in parking.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
            foreach (var t in boundary.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
        }

        private static void BuildGarageParkingCanopy(Transform parent, Vector3 position)
        {
            const string path = "Assets/TowTrouble/OpenWorld/Visuals/LandscapeComposition/SuntailExteriors/Open timber canopy.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("The audited exterior-only Suntail canopy is missing.");
            var canopy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            canopy.name = "Exterior timber shelter for recovered vehicle";
            StripGarageYardProp(canopy);
            var bounds = ScenicBounds(canopy);
            canopy.transform.localScale = Vector3.Scale(canopy.transform.localScale,
                new Vector3(8.2f / bounds.size.x, 3.9f / bounds.size.y, 6.4f / bounds.size.z));
            SeatGarageYardProp(canopy, position);
            // Native LOD0 vertex positions put the four post bases at these local
            // centres. Colliders inherit the fitted scale and match the timber,
            // rather than creating invisible obstacles at the roof overhang.
            foreach (float x in new[] { -1.53f, 1.54f }) foreach (float z in new[] { -.80f, .59f })
            {
                var post = new GameObject("Shelter post collision");
                post.transform.SetParent(canopy.transform, false);
                post.transform.localPosition = new Vector3(x, 1.2f, z);
                post.layer = 8;
                var collider = post.AddComponent<BoxCollider>();
                collider.size = new Vector3(.14f, 2.4f, .14f);
            }
            ConfigureMountainVegetationLod(canopy, 230);
        }

        private static void GarageParkingMark(Transform parent, Vector3 reference, Material paint)
        {
            // Short, worn-looking ends read as parking bays without boxing in the courtyard.
            var p = Ground(reference);
            var mark = PolishBox(parent, "Quiet parking guide", parent.InverseTransformPoint(p - Vector3.up * .055f), new Vector3(.065f, .012f, 4.8f), paint);
            mark.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ConfigureDetailLod(mark, 100);
        }

        private static void BuildGarageTireRack(Transform parent, Vector3 position, Material wood, Material metal)
        {
            var rack = PolishGroup(parent, "Organized spare wheel rack");
            rack.position = position;
            foreach (float x in new[] { -1.8f, 1.8f }) foreach (float z in new[] { -.48f, .48f })
                PolishBox(rack, "Rack upright", new Vector3(x, .9f, z), new Vector3(.09f, 1.8f, .09f), metal);
            foreach (float y in new[] { .25f, 1.15f })
            {
                PolishBox(rack, "Wheel shelf", new Vector3(0, y, 0), new Vector3(3.7f, .11f, 1.1f), wood);
                foreach (float z in new[] { -.48f, .48f })
                    PolishBox(rack, "Shelf rail", new Vector3(0, y + .15f, z), new Vector3(3.7f, .07f, .07f), metal);
            }
            var collider = rack.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, .9f, 0); collider.size = new Vector3(3.8f, 1.8f, 1.15f);
            rack.gameObject.layer = 8;
            BatchGarageYardGeometry(rack, "Spare wheel rack");
            var wheel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TowTrouble/Garage/Parts/Wheel_Offroad_Lvl2.prefab");
            if (wheel == null) throw new InvalidOperationException("The existing garage offroad wheel asset is missing.");
            for (int i = 0; i < 5; i++)
            {
                var copy = (GameObject)PrefabUtility.InstantiatePrefab(wheel, rack);
                copy.name = "Actual offroad spare wheel";
                StripGarageYardProp(copy);
                copy.transform.rotation = Quaternion.Euler(0, 90, 0);
                var b = ScenicBounds(copy);
                copy.transform.localScale *= .72f / Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                b = ScenicBounds(copy);
                var p = position + new Vector3(-1.2f + (i % 3) * 1.15f, i < 3 ? .31f : 1.21f, 0);
                copy.transform.position += new Vector3(p.x - b.center.x, p.y - b.min.y, p.z - b.center.z);
            }
            ConfigureDetailLod(rack.gameObject, 150);
        }

        private static void BuildGarageRestCorner(Transform root, Vector3 garage, Material wood, Material metal)
        {
            var corner = PolishGroup(root, "Workshop shaded rest corner");
            var seat = Ground(garage + new Vector3(-39, 0, 19));
            var bench = PolishGroup(corner, "Timber bench facing the service court");
            bench.position = seat;
            foreach (float x in new[] { -1.05f, 1.05f })
            {
                PolishBox(bench, "Bench leg", new Vector3(x, .32f, 0), new Vector3(.12f, .64f, .6f), metal);
                PolishBox(bench, "Back upright", new Vector3(x, .76f, .27f), new Vector3(.10f, .88f, .10f), metal);
            }
            for (int i = 0; i < 3; i++)
            {
                PolishBox(bench, "Seat slat", new Vector3(0, .65f, -.23f + i * .22f), new Vector3(2.7f, .1f, .19f), wood);
                PolishBox(bench, "Back slat", new Vector3(0, .83f + i * .17f, .31f), new Vector3(2.7f, .13f, .09f), wood);
            }
            var collider = bench.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, .6f, .05f); collider.size = new Vector3(2.7f, 1.2f, .72f);
            bench.gameObject.layer = 8;
            BatchGarageYardGeometry(bench, "Rest corner bench");
            ConfigureDetailLod(bench.gameObject, 160);

            var treePosition = Ground(garage + new Vector3(-39, 0, 24));
            var tree = ScenicPrefab("P_OakTree2", treePosition, 10.5f, 142, corner, true);
            if (tree == null) throw new InvalidOperationException("Workshop shade oak is missing.");
            foreach (var script in tree.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
            ConfigureMountainVegetationLod(tree, 280);

            var fence = PolishGroup(corner, "Partial garden boundary");
            GarageYardFence(fence, Ground(garage + new Vector3(-39, 0, 28)), 0, wood);
            GarageYardFence(fence, Ground(garage + new Vector3(-43, 0, 28)), 0, wood);
            GarageYardFence(fence, Ground(garage + new Vector3(-46, 0, 24)), 90, wood);
            BatchGarageYardGeometry(fence, "Rest corner boundary");
            ConfigureDetailLod(fence.gameObject, 190);

            // Two irregular planted edges leave the front and east approach open.
            var offsets = new[] { new Vector3(-44, 0, 24), new Vector3(-43.1f, 0, 25.1f), new Vector3(-42.5f, 0, 23.5f),
                new Vector3(-36, 0, 25), new Vector3(-35, 0, 26.1f), new Vector3(-34.7f, 0, 24.5f) };
            for (int i = 0; i < offsets.Length; i++)
            {
                var p = Ground(garage + offsets[i]);
                if (PolishRoadNear(p, 2)) continue;
                var plant = ScenicPrefab(i % 3 == 0 ? "P_Bush1_Light" : "P_Fern2_Yellow", p, i % 3 == 0 ? .85f : .45f, i * 47, corner, false);
                if (plant == null) continue;
                foreach (var script in plant.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
                ConfigureMountainVegetationLod(plant, 130);
            }
            foreach (var t in corner.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
        }

        private static void GarageYardFence(Transform parent, Vector3 point, float yaw, Material wood)
        {
            if (PolishRoadNear(point, 2)) return;
            var section = PolishGroup(parent, "Low timber boundary section");
            section.position = point; section.rotation = Quaternion.Euler(0, yaw, 0);
            foreach (float x in new[] { -1.85f, 1.85f })
                PolishBox(section, "Boundary post", new Vector3(x, .53f, 0), new Vector3(.15f, 1.06f, .15f), wood);
            foreach (float y in new[] { .38f, .78f })
                PolishBox(section, "Boundary rail", new Vector3(0, y, 0), new Vector3(3.85f, .13f, .10f), wood);
            var collider = section.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, .53f, 0); collider.size = new Vector3(3.85f, 1.06f, .16f);
            section.gameObject.layer = 8;
        }

        private static void StripGarageYardProp(GameObject go)
        {
            foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(behaviour);
            foreach (var rigidbody in go.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(rigidbody);
            foreach (var collider in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var light in go.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(light);
            foreach (var audio in go.GetComponentsInChildren<AudioSource>(true)) UnityEngine.Object.DestroyImmediate(audio);
            foreach (var camera in go.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(camera);
            foreach (var particles in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var renderer = particles.GetComponent<ParticleSystemRenderer>();
                if (renderer != null) UnityEngine.Object.DestroyImmediate(renderer);
                UnityEngine.Object.DestroyImmediate(particles);
            }
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 8;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true)) { renderer.forceRenderingOff = false; renderer.enabled = true; }
        }

        private static void SeatGarageYardProp(GameObject go, Vector3 position)
        {
            var bounds = ScenicBounds(go);
            go.transform.position += new Vector3(position.x - bounds.center.x, position.y - bounds.min.y, position.z - bounds.center.z);
        }

        private static void BatchGarageYardGeometry(Transform parent, string name)
        {
            // Only our readable primitive construction meshes are combined. Imported
            // vehicle, wheel and canopy assets retain their native shared meshes / LODs.
            var filters = parent.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh != null && f.sharedMesh.isReadable
                && f.GetComponent<MeshRenderer>() != null && f.GetComponent<MeshRenderer>().sharedMaterials.Length == 1).ToArray();
            int index = 0;
            foreach (var group in filters.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial))
            {
                var mesh = new Mesh { name = name + " " + index };
                mesh.CombineMeshes(group.Select(f => new CombineInstance { mesh = f.sharedMesh, subMeshIndex = 0,
                    transform = parent.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray(), true, true);
                mesh.RecalculateBounds();
                var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(parent, false); go.layer = 8;
                go.GetComponent<MeshFilter>().sharedMesh = VisualAsset(mesh, "GarageServiceBase/" + name + " " + index++ + ".asset");
                go.GetComponent<MeshRenderer>().sharedMaterial = group.Key;
            }
            foreach (var filter in filters)
            {
                // Keep a parent carrying collision; discard only its rendered primitive children.
                if (filter.GetComponent<Collider>() == null && filter.transform.childCount == 0)
                    UnityEngine.Object.DestroyImmediate(filter.gameObject);
                else
                {
                    UnityEngine.Object.DestroyImmediate(filter.GetComponent<MeshRenderer>());
                    UnityEngine.Object.DestroyImmediate(filter);
                }
            }
        }
    }
}

using System;
using UnityEditor;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        // Three readable working areas fill the broad court without turning the
        // receiving lane into a slalom. All props are static exterior dressing.
        private static void BuildGarageActivity(Transform root, Vector3 garage)
        {
            var previous = root.Find("Workshop service activity");
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            var activity = PolishGroup(root, "Workshop service activity");
            var wood = GarageFacadeMaterial("Workshop aged timber");
            var metal = GarageFacadeMaterial("Workshop roof metal");
            var cream = GarageFacadeMaterial("Workshop ivory trim");
            var red = GarageFacadeMaterial("Workshop painted boards");
            var blue = GarageFacadeMaterial("Workshop faded petrol blue");
            var rubber = GarageFacadeMaterial("Workshop black rubber");
            var concrete = GarageFacadeMaterial("Workshop concrete");

            BuildGarageRepairStand(activity, garage, wood, metal, cream, red, rubber, concrete);
            BuildGarageMaterialStore(activity, garage, wood, metal);
            BuildGarageCableStation(activity, garage, wood, metal, cream, blue, rubber);
            foreach (var t in activity.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = 8;
                t.gameObject.isStatic = true;
            }
            foreach (var collider in activity.GetComponentsInChildren<Collider>())
            {
                var bounds = collider.bounds;
                var relativeMin = bounds.min - garage; var relativeMax = bounds.max - garage;
                if (relativeMin.x < 23 && relativeMax.x > -45 && relativeMin.z < -11 && relativeMax.z > -25)
                    throw new InvalidOperationException("Workshop activity obstructs the receiving lane: " + collider.name);
            }
        }

        private static Transform GarageActivityGroup(Transform parent, string name, Vector3 position, Vector3 garage, float radius)
        {
            if (Flat(position - garage).magnitude < 13 + radius || PolishRoadNear(position, radius + .6f))
                throw new InvalidOperationException("Workshop activity is too close to a road or the garage camera: " + name);
            var group = PolishGroup(parent, name);
            // Ground already adds 8 cm. Work props sit almost flush, not floating
            // above the feathered gravel surface.
            group.position = Ground(position) - Vector3.up * .065f;
            return group;
        }

        private static void BuildGarageRepairStand(Transform parent, Vector3 garage, Material wood, Material metal,
            Material cream, Material red, Material rubber, Material concrete)
        {
            var repair = GarageActivityGroup(parent, "Outdoor inspection and wheel service", garage + new Vector3(-17, 0, -36), garage, 5.6f);
            var pad = PolishGroup(repair, "Low inspection pad");
            using (var shapes = new GarageFacadeShapes())
            {
                // A shallow working pad, with two darker service strips and wheel
                // stops, gives the parked vehicle a purpose at normal camera height.
                shapes.Box(concrete, new Vector3(0, .055f, 0), new Vector3(7.3f, .11f, 3.5f));
                foreach (float z in new[] { -.82f, .82f })
                {
                    shapes.Box(metal, new Vector3(0, .123f, z), new Vector3(5.7f, .035f, .28f));
                    shapes.Box(wood, new Vector3(-2.8f, .20f, z), new Vector3(.20f, .18f, .58f));
                }
                shapes.Finish(pad, "Outdoor service pad");
            }
            ConfigureDetailLod(pad.gameObject, 230);
            GarageActivityBox(repair, "Low inspection pad collision", new Vector3(0, .055f, 0), new Vector3(7.3f, .11f, 3.5f));

            var sources = environment.Find("Mountain cliffs and garage grounds/Garage working yard/Parked salvage vehicles");
            if (sources == null || sources.childCount == 0) throw new InvalidOperationException("Recovered vehicle source art is missing.");
            var car = UnityEngine.Object.Instantiate(sources.GetChild(0).gameObject, repair);
            car.name = "Recovered hauler awaiting wheel inspection"; car.SetActive(true);
            StripGarageYardProp(car);
            car.transform.rotation = Quaternion.Euler(0, 86, 0);
            SeatGarageYardProp(car, repair.TransformPoint(new Vector3(0, .145f, 0)));
            var carBounds = ScenicBounds(car);
            // An axis-aligned child avoids turning a world-space bounds size a
            // second time with the car's near-90-degree display rotation.
            GarageActivityBox(repair, "Inspection vehicle collision", repair.InverseTransformPoint(carBounds.center), carBounds.size);
            ConfigureMountainVegetationLod(car, 210);

            var tools = PolishGroup(repair, "Inspection trolley and removed wheels");
            var trolleyArt = PolishGroup(tools, "Trolley detail");
            using (var shapes = new GarageFacadeShapes())
            {
                var trolley = new Vector3(1.9f, 0, 2.7f);
                shapes.Box(red, trolley + Vector3.up * .55f, new Vector3(1.35f, .68f, .73f));
                shapes.Box(metal, trolley + Vector3.up * .95f, new Vector3(1.48f, .13f, .85f));
                for (int drawer = 0; drawer < 3; drawer++)
                {
                    shapes.Box(metal, trolley + new Vector3(0, .33f + drawer * .21f, .373f), new Vector3(1.21f, .028f, .025f));
                    shapes.Box(cream, trolley + new Vector3(0, .43f + drawer * .21f, .40f), new Vector3(.73f, .045f, .045f));
                }
                foreach (float x in new[] { -.5f, .5f }) foreach (float z in new[] { -.25f, .25f })
                    shapes.Ring(rubber, trolley + new Vector3(x, .16f, z), .105f, .05f, Quaternion.Euler(0, 90, 0));
                shapes.Box(metal, trolley + new Vector3(.82f, .78f, 0), new Vector3(.08f, .08f, .68f));
                for (int tool = 0; tool < 3; tool++)
                    shapes.Box(cream, trolley + new Vector3(-.34f + tool * .30f, 1.03f, -.08f), new Vector3(.07f, .045f, .40f));
                shapes.Finish(trolleyArt, "Wheel service trolley");
                GarageActivityBox(tools, "Service trolley collision", trolley + Vector3.up * .55f, new Vector3(1.5f, 1.1f, .85f));
            }
            ConfigureDetailLod(trolleyArt.gameObject, 165);
            GarageActivityWheel(tools, "Wheel ready to refit", new Vector3(-1.2f, 0, 2.5f), .92f, Quaternion.Euler(0, 12, -8));
            GarageActivityWheel(tools, "Spare tire beside inspection stand", new Vector3(-2.3f, 0, 2.8f), .84f, Quaternion.Euler(0, 90, 0));
            BuildGarageInspectionJack(repair, metal, cream, red, rubber);
            ConfigureDetailLod(tools.gameObject, 165);
            ConfigureDetailLod(repair.gameObject, 230);
        }

        private static void BuildGarageInspectionJack(Transform repair, Material metal, Material cream, Material red, Material rubber)
        {
            var jack = PolishGroup(repair, "Floor jack and paired wheel chocks");
            jack.localPosition = new Vector3(.8f, 0, -2.25f);
            using (var shapes = new GarageFacadeShapes())
            {
                // Park the jack beside the inspection pad with its saddle facing
                // the car. The chocks belong to this service set, not the lane.
                foreach (float x in new[] { -.18f, .18f })
                {
                    shapes.Box(red, new Vector3(x, .23f, 0), new Vector3(.095f, .18f, 1.03f));
                    shapes.Ring(rubber, new Vector3(x * 1.5f, .135f, .35f), .095f, .04f, Quaternion.Euler(0, 90, 0));
                    shapes.Ring(rubber, new Vector3(x * 1.5f, .105f, -.38f), .065f, .035f, Quaternion.Euler(0, 90, 0));
                }
                shapes.Box(metal, new Vector3(0, .24f, -.35f), new Vector3(.42f, .10f, .16f));
                shapes.Box(red, new Vector3(0, .32f, .16f), new Vector3(.22f, .12f, .58f));
                shapes.Ring(cream, new Vector3(0, .41f, .43f), .12f, .035f, Quaternion.Euler(90, 0, 0));
                foreach (float x in new[] { .62f, 1.02f })
                {
                    shapes.Box(rubber, new Vector3(x, .06f, .06f), new Vector3(.26f, .12f, .43f));
                    shapes.Box(rubber, new Vector3(x, .15f, .15f), new Vector3(.26f, .06f, .25f));
                    shapes.Box(cream, new Vector3(x, .184f, .17f), new Vector3(.12f, .018f, .16f));
                }
                shapes.Finish(jack, "Inspection jack chassis and chocks");
            }
            var handle = PolishGroup(jack, "Leaning jack handle");
            handle.localPosition = new Vector3(0, .26f, -.40f);
            handle.localRotation = Quaternion.Euler(-18, 0, 0);
            using (var shapes = new GarageFacadeShapes())
            {
                shapes.Box(metal, new Vector3(0, .48f, 0), new Vector3(.045f, .96f, .045f));
                shapes.Box(rubber, new Vector3(0, .91f, 0), new Vector3(.075f, .20f, .075f));
                shapes.Finish(handle, "Inspection jack handle");
            }
            GarageActivityBox(jack, "Floor jack collision", new Vector3(0, .30f, 0), new Vector3(.65f, .60f, 1.08f));
            ConfigureDetailLod(jack.gameObject, 150);
        }

        private static void BuildGarageMaterialStore(Transform parent, Vector3 garage, Material wood, Material metal)
        {
            var store = GarageActivityGroup(parent, "Covered workshop materials", garage + new Vector3(-1, 0, -36), garage, 5.8f);
            BuildGarageParkingCanopy(store, store.position);
            var shelter = store.GetChild(0); shelter.name = "Exterior material-store canopy";
            var timber = PolishGroup(store, "Stacked timber and packing pallets");
            using (var shapes = new GarageFacadeShapes())
            {
                // Individual slats, open gaps and packing battens read as pallets
                // and timber, instead of a pile of solid placeholder blocks.
                foreach (var center in new[] { new Vector3(-2.1f, 0, 1.3f), new Vector3(1.8f, 0, .95f) })
                {
                    foreach (float x in new[] { -.78f, 0, .78f })
                        shapes.Box(wood, center + new Vector3(x, .12f, 0), new Vector3(.18f, .24f, 1.35f));
                    for (int plank = 0; plank < 6; plank++)
                        shapes.Box(wood, center + new Vector3(-.94f + plank * .375f, .28f, 0), new Vector3(.30f, .10f, 1.5f));
                }
                for (int level = 0; level < 5; level++) for (int piece = 0; piece < 4; piece++)
                    shapes.Box(wood, new Vector3(1.8f + (level % 2 == 0 ? 0 : .12f), .41f + level * .16f, .42f + piece * .34f), new Vector3(2.3f, .13f, .28f));
                foreach (float x in new[] { .95f, 2.65f })
                    shapes.Box(metal, new Vector3(x, .76f, .93f), new Vector3(.06f, .85f, 1.44f));
                shapes.Finish(timber, "Stacked workshop timber and pallets");
            }
            ConfigureDetailLod(timber.gameObject, 180);
            GarageActivityBox(store, "Timber stack collision", new Vector3(1.86f, .62f, .93f), new Vector3(2.45f, 1.24f, 1.52f));
            GarageActivitySupplyCrate(store, "Spare parts packing crate", new Vector3(-2.1f, .33f, 1.3f),
                new Vector3(1.85f, 1.05f, 1.15f), 4, wood, metal);
            GarageActivitySupplyDrum(store, "Workshop lubricant drum", new Vector3(-2.3f, 0, -1.5f), 1.25f, 0);
            GarageActivitySupplyDrum(store, "Covered service fluid drum", new Vector3(-.8f, 0, -1.65f), 1.15f, 29);
            GarageActivitySupplyCrate(store, "Recovery equipment packing crate", new Vector3(1.5f, 0, -1.55f),
                new Vector3(1.8f, .85f, 1.05f), -3, wood, metal);
            BuildGarageLoadingDolly(store, metal);
            ConfigureDetailLod(store.gameObject, 220);
        }

        private static void BuildGarageLoadingDolly(Transform store, Material metal)
        {
            var dolly = PolishGroup(store, "Loading handtruck beside supply crates");
            dolly.localPosition = new Vector3(3.10f, 0, -1.55f);
            dolly.localRotation = Quaternion.Euler(0, -12, 0);
            var blue = GarageFacadeMaterial("Workshop faded petrol blue");
            var rubber = GarageFacadeMaterial("Workshop black rubber");
            using (var shapes = new GarageFacadeShapes())
            {
                foreach (float x in new[] { -.27f, .27f })
                {
                    shapes.Box(blue, new Vector3(x, .81f, 0), new Vector3(.065f, 1.34f, .065f));
                    shapes.Box(metal, new Vector3(x, 1.45f, -.12f), new Vector3(.065f, .065f, .30f));
                    shapes.Box(rubber, new Vector3(x, 1.45f, -.24f), new Vector3(.09f, .09f, .15f));
                    shapes.Ring(rubber, new Vector3(x * 1.40f, .21f, -.055f), .15f, .06f, Quaternion.Euler(0, 90, 0));
                    shapes.Ring(metal, new Vector3(x * 1.40f, .21f, -.055f), .075f, .028f, Quaternion.Euler(0, 90, 0));
                }
                foreach (float y in new[] { .36f, .73f, 1.14f })
                    shapes.Box(blue, new Vector3(0, y, 0), new Vector3(.57f, .06f, .06f));
                shapes.Box(metal, new Vector3(0, .18f, -.055f), new Vector3(.80f, .055f, .055f));
                shapes.Box(blue, new Vector3(0, .075f, .20f), new Vector3(.72f, .055f, .51f));
                shapes.Finish(dolly, "Supply loading handtruck");
            }
            GarageActivityBox(dolly, "Loading handtruck collision", new Vector3(0, .75f, .06f), new Vector3(.90f, 1.50f, .81f));
            ConfigureDetailLod(dolly.gameObject, 160);
        }

        private static void BuildGarageCableStation(Transform parent, Vector3 garage, Material wood, Material metal,
            Material cream, Material blue, Material rubber)
        {
            var station = GarageActivityGroup(parent, "Cable preparation station", garage + new Vector3(-24, 0, -6), garage, 2.8f);
            var bench = PolishGroup(station, "Cable bench and reel detail");
            using (var shapes = new GarageFacadeShapes())
            {
                foreach (float x in new[] { -1.5f, 1.5f }) foreach (float z in new[] { -.42f, .42f })
                    shapes.Box(metal, new Vector3(x, .52f, z), new Vector3(.12f, 1.04f, .12f));
                shapes.Box(wood, new Vector3(0, 1.08f, 0), new Vector3(3.45f, .15f, 1.20f));
                shapes.Box(blue, new Vector3(0, .45f, 0), new Vector3(1.3f, .62f, .80f));
                shapes.Box(cream, new Vector3(0, .62f, -.42f), new Vector3(.55f, .06f, .06f));
                for (int loop = 0; loop < 5; loop++)
                    shapes.Ring(rubber, new Vector3(-.82f, 1.20f + loop * .044f, 0), .34f, .04f, Quaternion.Euler(90, 0, 0));
                shapes.Box(metal, new Vector3(.52f, 1.22f, .10f), new Vector3(.48f, .13f, .30f));
                shapes.Ring(cream, new Vector3(1.05f, 1.20f, -.11f), .15f, .045f, Quaternion.Euler(90, 0, 0));

                // A low open rack keeps the recovery straps beside the cable
                // coils and leaves the station's work surface visible.
                foreach (float x in new[] { .05f, 1.42f })
                    shapes.Box(metal, new Vector3(x, 1.43f, .48f), new Vector3(.06f, .60f, .06f));
                shapes.Box(metal, new Vector3(.735f, 1.72f, .48f), new Vector3(1.47f, .06f, .06f));
                foreach (float x in new[] { .37f, .74f, 1.11f })
                {
                    shapes.Box(cream, new Vector3(x, 1.54f, .46f), new Vector3(.055f, .30f, .035f));
                    shapes.Ring(cream, new Vector3(x, 1.38f, .46f), .075f, .022f, Quaternion.identity);
                }

                // A horizontal cable drum below waist height gives the station a
                // useful towing silhouette without covering the vehicle/camera.
                var reel = new Vector3(-2.1f, .55f, 1.05f);
                for (int turn = 0; turn < 9; turn++)
                    shapes.Ring(rubber, reel + Vector3.forward * (turn - 4) * .055f, .36f, .04f, Quaternion.identity);
                foreach (float z in new[] { -.30f, .30f })
                {
                    shapes.Ring(wood, reel + Vector3.forward * z, .48f, .07f, Quaternion.identity);
                    shapes.Box(wood, reel + Vector3.forward * z, new Vector3(.12f, .96f, .08f));
                    shapes.Box(wood, reel + Vector3.forward * z, new Vector3(.96f, .12f, .08f));
                }
                shapes.Finish(bench, "Cable preparation bench and reel");
            }
            ConfigureDetailLod(bench.gameObject, 190);
            GarageActivityBox(station, "Cable station bench collision", new Vector3(0, .6f, 0), new Vector3(3.45f, 1.2f, 1.2f));
            GarageActivityBox(station, "Low recovery strap rack collision", new Vector3(.735f, 1.45f, .48f), new Vector3(1.53f, .61f, .09f));
            GarageActivityBox(station, "Cable reel collision", new Vector3(-2.1f, .55f, 1.05f), new Vector3(1.1f, 1.1f, .8f));
            GarageActivityWheel(station, "Service station spare wheel", new Vector3(1.95f, 0, 1.1f), .82f, Quaternion.Euler(0, 70, 0));
            ConfigureDetailLod(station.gameObject, 190);
        }

        private static void GarageActivityBox(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name, typeof(BoxCollider));
            go.transform.SetParent(parent, false); go.layer = 8;
            var collider = go.GetComponent<BoxCollider>(); collider.center = center; collider.size = size;
        }

        private static void GarageActivityWheel(Transform parent, string name, Vector3 position, float diameter, Quaternion rotation)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TowTrouble/Garage/Parts/Wheel_Offroad_Lvl2.prefab");
            if (source == null) throw new InvalidOperationException("Native offroad wheel is missing.");
            var wheel = (GameObject)PrefabUtility.InstantiatePrefab(source, parent); wheel.name = name;
            StripGarageYardProp(wheel); wheel.transform.rotation = rotation;
            var bounds = ScenicBounds(wheel);
            wheel.transform.localScale *= diameter / Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            SeatGarageYardProp(wheel, parent.TransformPoint(position));
            ConfigureDetailLod(wheel, 150);
        }

        private static void GarageActivitySupplyCrate(Transform parent, string name, Vector3 position,
            Vector3 size, float yaw, Material wood, Material metal)
        {
            var crate = PolishGroup(parent, name);
            crate.localPosition = position; crate.localRotation = Quaternion.Euler(0, yaw, 0);
            using (var shapes = new GarageFacadeShapes())
            {
                const float board = .085f;
                float halfX = size.x * .5f, halfZ = size.z * .5f;
                // Visible joints, recessed sides and open gaps make a packing
                // crate read correctly from the driving camera without interior art.
                for (int row = 0; row < 4; row++)
                {
                    float y = (row + .5f) * size.y / 4;
                    float height = size.y / 4 - .026f;
                    foreach (float z in new[] { -halfZ + board * .5f, halfZ - board * .5f })
                        shapes.Box(wood, new Vector3(0, y, z), new Vector3(size.x, height, board));
                    foreach (float x in new[] { -halfX + board * .5f, halfX - board * .5f })
                        shapes.Box(wood, new Vector3(x, y, 0), new Vector3(board, height, size.z - board * 2));
                }
                for (int plank = 0; plank < 6; plank++)
                    shapes.Box(wood, new Vector3((plank + .5f) * size.x / 6 - halfX, size.y - board * .5f, 0),
                        new Vector3(size.x / 6 - .022f, board, size.z));
                foreach (float x in new[] { -halfX + .16f, halfX - .16f })
                {
                    foreach (float z in new[] { -halfZ - .015f, halfZ + .015f })
                        shapes.Box(metal, new Vector3(x, size.y * .5f, z), new Vector3(.075f, size.y, .035f));
                    shapes.Box(metal, new Vector3(x, size.y + .01f, 0), new Vector3(.075f, .035f, size.z + .04f));
                }
                foreach (float x in new[] { -halfX + .065f, halfX - .065f })
                    foreach (float z in new[] { -halfZ + .065f, halfZ - .065f })
                        shapes.Box(wood, new Vector3(x, size.y * .5f, z), new Vector3(.13f, size.y, .13f));
                shapes.Finish(crate, name);
            }
            GarageActivityBox(crate, name + " collision", Vector3.up * size.y * .5f, size + new Vector3(.04f, .03f, .04f));
            ConfigureDetailLod(crate.gameObject, 170);
        }

        private static void GarageActivitySupplyDrum(Transform parent, string name, Vector3 position, float height, float yaw)
        {
            var drum = PolishGroup(parent, name);
            drum.localPosition = position; drum.localRotation = Quaternion.Euler(0, yaw, 0);
            float radius = height * .34f;
            var blue = GarageFacadeMaterial("Workshop faded petrol blue");
            var metal = GarageFacadeMaterial("Workshop roof metal");
            var cream = GarageFacadeMaterial("Workshop ivory trim");
            // Unity's built-in cylinder is always available; these exterior
            // drums do not depend on optional vendor Environment model imports.
            var shell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shell.name = "Closed painted drum shell"; shell.transform.SetParent(drum, false);
            shell.transform.localPosition = Vector3.up * height * .5f;
            shell.transform.localScale = new Vector3(radius * 2, height * .5f, radius * 2);
            shell.GetComponent<MeshRenderer>().sharedMaterial = blue;
            UnityEngine.Object.DestroyImmediate(shell.GetComponent<Collider>());
            using (var shapes = new GarageFacadeShapes())
            {
                foreach (float fraction in new[] { .025f, .30f, .70f, .975f })
                    shapes.Ring(metal, Vector3.up * height * fraction, radius, .022f, Quaternion.Euler(90, 0, 0));
                shapes.Ring(metal, new Vector3(radius * .45f, height + .012f, 0), .063f, .022f, Quaternion.Euler(90, 0, 0));
                shapes.Box(cream, new Vector3(0, height * .5f, -radius - .008f), new Vector3(radius * .65f, height * .19f, .025f));
                shapes.Finish(drum, name + " rolled seams and supply label");
            }
            GarageActivityBox(drum, name + " collision", Vector3.up * height * .5f,
                new Vector3(radius * 2 + .05f, height + .04f, radius * 2 + .05f));
            ConfigureDetailLod(drum.gameObject, 170);
        }
    }
}

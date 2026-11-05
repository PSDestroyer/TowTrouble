using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static void BuildNaturalReceivingDetails(Transform group, string id, string english, Vector3 center, float yaw)
        {
            var old = group.Find("Receiving site details");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            var details = PolishGroup(group, "Receiving site details");
            Undo.RegisterCreatedObjectUndo(details.gameObject, "Dress receiving site " + id);
            var rotation = Quaternion.Euler(0, yaw, 0);
            // All offsets are across the open towing axis, never at its entrance.
            // E stays south of its existing house; H stays east of the old road.
            Vector3 signOffset = id == "E" ? new Vector3(-10.8f, 0, -6) :
                id == "C" ? new Vector3(-10.6f, 0, 9) :
                id == "D" ? new Vector3(-10.6f, 0, -6) : new Vector3(10.8f, 0, -2);
            var signPosition = id == "A" ? center + new Vector3(0, 0, -10) : center + rotation * signOffset;
            BuildReceivingSign(details, id, english, center, signPosition);
            if (id != "A")
            {
                var offset = id == "H" ? new Vector3(15.5f, 0, 7) :
                    id == "E" ? new Vector3(14, 0, 0) :
                    id == "C" || id == "D" ? new Vector3(-11.2f, 0, 5) : new Vector3(-15.5f, 0, 0);
                var station = PolishGroup(details, "Receiving " + id + " exterior service corner");
                station.position = Ground(center + rotation * offset) - Vector3.up * .08f;
                station.rotation = rotation;
                if (id == "F" || id == "G" || id == "H")
                {
                    BuildGarageParkingCanopy(station, station.position);
                    var canopy = station.GetChild(0);
                    canopy.name = "Receiving " + id + " exterior only sunshade";
                    if (id == "F")
                    {
                        canopy.localScale = Vector3.Scale(canopy.localScale, new Vector3(.78f, .86f, .78f));
                        SeatGarageYardProp(canopy.gameObject, station.position);
                    }
                }
                var props = PolishGroup(station, "Receiving " + id + " compact service props");
                if (id == "C" || id == "D") props.localRotation = Quaternion.Euler(0, 90, 0);
                var geometry = PolishGroup(props, "Receiving " + id + " batched service geometry");
                using (var shapes = new GarageFacadeShapes())
                {
                    var wood = WorkshopFinish("Timber"); var trim = WorkshopFinish("Trim");
                    var metal = WorkshopFinish("Metal"); var blue = WorkshopFinish("Door");
                    var rubber = GarageFacadeMaterial("Workshop black rubber");
                    switch (id)
                    {
                        case "B":
                            ReceivingTimber(shapes, new Vector3(-1.5f, 0, 0), wood, metal);
                            ReceivingCabinet(shapes, new Vector3(1.35f, 0, 0), blue, metal, trim);
                            break;
                        case "C": case "D":
                            ReceivingBench(shapes, new Vector3(1.5f, 0, 0), wood, metal);
                            ReceivingFirewood(shapes, new Vector3(-1.65f, 0, 0), wood, WorkshopFinish("WallLight"));
                            break;
                        case "E":
                            ReceivingHose(shapes, new Vector3(-1.05f, 0, 0), wood, metal, rubber);
                            ReceivingCrate(shapes, new Vector3(1.1f, 0, 0), wood, metal);
                            break;
                        case "F":
                            ReceivingBench(shapes, new Vector3(0, 0, .5f), wood, metal);
                            break;
                        case "G":
                            foreach (float x in new[] { -.65f, .65f })
                                ReceivingWaterDrum(geometry, id + (x < 0 ? " west" : " east"), new Vector3(x, 0, .5f), blue, metal);
                            shapes.Box(wood, new Vector3(0, .06f, .5f), new Vector3(2.4f, .12f, 1.2f));
                            break;
                        case "H":
                            foreach (float z in new[] { -.40f, .40f })
                                shapes.Box(wood, new Vector3(0, .19f, z + .5f), new Vector3(2.6f, .12f, .12f));
                            foreach (float x in new[] { -1.23f, 1.23f })
                                shapes.Box(metal, new Vector3(x, .47f, .5f), new Vector3(.10f, .94f, .94f));
                            break;
                        default: throw new ArgumentException("Unknown receiving site " + id);
                    }
                    shapes.Finish(geometry, "Receiving " + id + " exterior service details");
                }
                ConfigureDetailLod(geometry.gameObject, 170);
                if (id == "H")
                    for (int i = 0; i < 3; i++)
                        GarageActivityWheel(props, "Receiving H spare tire " + (i + 1), new Vector3(-.82f + i * .82f, .25f, .5f),
                            .92f, Quaternion.Euler(0, yaw + 90, 0));
                ConfigureDetailLod(props.gameObject, 170);
            }
            foreach (var t in details.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = 8;
                t.gameObject.isStatic = true;
            }
            foreach (var collider in details.GetComponentsInChildren<Collider>())
                if (Flat(collider.bounds.ClosestPoint(center) - center).sqrMagnitude < 49)
                    throw new InvalidOperationException("Receiving post intrudes into the open bay: " + id + "/" + collider.name);
        }

        private static void BuildReceivingSign(Transform parent, string id, string english, Vector3 center, Vector3 position)
        {
            var sign = PolishGroup(parent, "Receiving " + id + " destination sign");
            sign.position = Ground(position) - Vector3.up * .08f;
            sign.rotation = Quaternion.LookRotation(Flat(position - center).normalized, Vector3.up);
            using (var shapes = new GarageFacadeShapes())
            {
                var wood = WorkshopFinish("Timber"); var cream = WorkshopFinish("Trim"); var blue = WorkshopFinish("Door");
                foreach (float x in new[] { -1.13f, 1.13f })
                {
                    shapes.BevelBox(wood, new Vector3(x, 1.28f, 0), new Vector3(.13f, 2.56f, .13f), .018f);
                    GarageFacadeCollider(sign, "Receiving " + id + " timber sign post", new Vector3(x, 1.28f, 0), new Vector3(.13f, 2.56f, .13f));
                }
                shapes.BevelBox(cream, new Vector3(0, 2.10f, 0), new Vector3(3.05f, 1.04f, .13f), .045f);
                shapes.BevelBox(blue, new Vector3(0, 2.10f, -.075f), new Vector3(2.91f, .91f, .035f), .02f);
                shapes.BevelBox(blue, new Vector3(0, 2.10f, .075f), new Vector3(2.91f, .91f, .035f), .02f);
                shapes.Finish(sign, "Receiving " + id + " modest destination sign");
            }
            BakeReceivingSignText(sign, id, "VEHICLE DROP-OFF\n<size=82%>" + english + "</size>");
            ConfigureDetailLod(sign.gameObject, 220);
        }

        private static void BakeReceivingSignText(Transform parent, string id, string text)
        {
            var temporary = new GameObject("Receiving sign text bake", typeof(TextMeshPro));
            try
            {
                var label = temporary.GetComponent<TextMeshPro>();
                label.font = font != null ? font : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TowTrouble/Art/UI/Theme/Fonts/TowText SDF.asset");
                if (label.font == null) throw new InvalidOperationException("Receiving sign font is missing.");
                label.text = text; label.fontSize = 3.2f; label.enableAutoSizing = true;
                label.fontSizeMin = 2.2f; label.fontSizeMax = 3.2f;
                label.alignment = TextAlignmentOptions.Center; label.color = new Color(1, .95f, .80f);
                label.rectTransform.sizeDelta = new Vector2(2.72f, .76f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.ForceMeshUpdate(true, true);
                var mesh = UnityEngine.Object.Instantiate(label.mesh); mesh.name = "Receiving " + id + " English destination lettering";
                mesh = VisualAsset(mesh, "GarageServiceBase/" + mesh.name + ".asset");
                foreach (float face in new[] { -1f, 1f })
                {
                    var go = new GameObject("Receiving " + id + " English sign face", typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(parent, false);
                    go.transform.localPosition = new Vector3(0, 2.10f, face * .10f);
                    go.transform.localRotation = Quaternion.Euler(0, face < 0 ? 0 : 180, 0);
                    go.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = label.fontSharedMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }
        }

        private static void ReceivingBench(GarageFacadeShapes shapes, Vector3 p, Material wood, Material metal)
        {
            foreach (float x in new[] { -1.03f, 1.03f })
            {
                shapes.Box(metal, p + new Vector3(x, .42f, 0), new Vector3(.12f, .84f, .67f));
                shapes.Box(metal, p + new Vector3(x, .99f, .28f), new Vector3(.09f, .89f, .09f));
            }
            foreach (float z in new[] { -.23f, 0, .23f })
                shapes.Box(wood, p + new Vector3(0, .86f, z), new Vector3(2.60f, .10f, .20f));
            foreach (float y in new[] { 1.10f, 1.34f })
                shapes.Box(wood, p + new Vector3(0, y, .29f), new Vector3(2.60f, .20f, .08f));
        }

        private static void ReceivingTimber(GarageFacadeShapes shapes, Vector3 p, Material wood, Material metal)
        {
            foreach (float x in new[] { -.85f, .85f }) shapes.Box(metal, p + new Vector3(x, .09f, 0), new Vector3(.12f, .18f, 1.1f));
            for (int row = 0; row < 4; row++) for (int plank = 0; plank < 4; plank++)
                shapes.BevelBox(wood, p + new Vector3((row % 2) * .06f, .27f + row * .19f, -.39f + plank * .26f),
                    new Vector3(2.2f - row * .08f, .17f, .22f), .035f);
            foreach (float x in new[] { -.85f, .85f })
                shapes.Box(metal, p + new Vector3(x, .90f, 0), new Vector3(.055f, .035f, 1.03f));
        }

        private static void ReceivingCabinet(GarageFacadeShapes shapes, Vector3 p, Material blue, Material metal, Material trim)
        {
            shapes.BevelBox(blue, p + Vector3.up * .74f, new Vector3(1.3f, 1.30f, .68f), .05f);
            shapes.BevelBox(metal, p + Vector3.up * 1.43f, new Vector3(1.46f, .12f, .82f), .03f);
            foreach (float y in new[] { .35f, .66f, .97f })
            {
                shapes.Box(metal, p + new Vector3(0, y, -.351f), new Vector3(1.15f, .024f, .024f));
                shapes.Box(trim, p + new Vector3(0, y + .13f, -.38f), new Vector3(.68f, .045f, .06f));
            }
        }

        private static void ReceivingFirewood(GarageFacadeShapes shapes, Vector3 p, Material bark, Material cutEnds)
        {
            foreach (float z in new[] { -.42f, .42f })
                shapes.Box(bark, p + new Vector3(0, .075f, z), new Vector3(1.75f, .15f, .15f));
            for (int row = 0; row < 4; row++)
                for (int log = 0; log < 5 - row; log++)
                {
                    float x = (log - (4 - row) * .5f) * .31f;
                    var middle = p + new Vector3(x, .29f + row * .26f, 0);
                    shapes.BevelBox(bark, middle, new Vector3(.29f, .25f, 1.05f), .065f);
                    foreach (float z in new[] { -.529f, .529f })
                        shapes.BevelBox(cutEnds, middle + Vector3.forward * z, new Vector3(.20f, .17f, .025f), .01f);
                }
        }

        private static void ReceivingCrate(GarageFacadeShapes shapes, Vector3 p, Material wood, Material metal)
        {
            for (int row = 0; row < 4; row++) foreach (float side in new[] { -1f, 1f })
            {
                shapes.Box(wood, p + new Vector3(0, .15f + row * .22f, side * .50f), new Vector3(1.45f, .18f, .08f));
                shapes.Box(wood, p + new Vector3(side * .69f, .15f + row * .22f, 0), new Vector3(.08f, .18f, 1.02f));
            }
            for (int i = 0; i < 5; i++) shapes.Box(wood, p + new Vector3(-.58f + i * .29f, .95f, 0), new Vector3(.26f, .08f, 1.08f));
            foreach (float x in new[] { -.48f, .48f })
            {
                shapes.Box(metal, p + new Vector3(x, .96f, 0), new Vector3(.05f, .04f, 1.11f));
                shapes.Box(metal, p + new Vector3(x, .50f, -.55f), new Vector3(.05f, .91f, .03f));
            }
        }

        private static void ReceivingHose(GarageFacadeShapes shapes, Vector3 p, Material wood, Material metal, Material rubber)
        {
            foreach (float x in new[] { -.55f, .55f }) shapes.Box(wood, p + new Vector3(x, .65f, 0), new Vector3(.14f, 1.30f, .14f));
            shapes.Box(wood, p + new Vector3(0, 1.20f, 0), new Vector3(1.30f, .16f, .14f));
            for (int loop = 0; loop < 4; loop++)
                shapes.Ring(rubber, p + new Vector3(0, .78f, -.13f - loop * .065f), .43f, .034f, Quaternion.identity);
            shapes.Box(metal, p + new Vector3(0, .78f, -.17f), new Vector3(.12f, .12f, .54f));
        }

        private static void ReceivingWaterDrum(Transform parent, string id, Vector3 p, Material blue, Material metal)
        {
            var drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder); drum.name = "Receiving " + id + " closed water drum";
            drum.transform.SetParent(parent, false); drum.transform.localPosition = p + Vector3.up * .73f;
            drum.transform.localScale = new Vector3(.82f, .61f, .82f);
            UnityEngine.Object.DestroyImmediate(drum.GetComponent<Collider>());
            drum.GetComponent<MeshRenderer>().sharedMaterial = blue;
            using (var shapes = new GarageFacadeShapes())
            {
                foreach (float y in new[] { .16f, .56f, .95f, 1.30f })
                    shapes.Ring(metal, p + Vector3.up * y, .412f, .021f, Quaternion.Euler(90, 0, 0));
                shapes.Finish(parent, "Receiving " + id + " water drum bands");
            }
        }
    }
}

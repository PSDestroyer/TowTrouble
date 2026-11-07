using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        [MenuItem("Tow Trouble/Visuals/Dress Workshop Forecourt")]
        public static void DressWorkshopForecourt()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Workshop dressing requires Edit Mode.");
            VisualContext();
            var workshop = environment.Find("Workshop service base");
            var world = UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>(FindObjectsInactive.Include);
            if (workshop == null || world == null) throw new InvalidOperationException("Workshop service base is missing.");
            var source = workshop.Find("Workshop activity and planted edges/Workshop service activity");
            if (source == null) throw new InvalidOperationException("Existing workshop prop library is missing.");
            Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Dress workshop forecourt");
            try
            {
                var old = workshop.Find("Workshop forecourt working details");
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                var root = PolishGroup(workshop, "Workshop forecourt working details");
                Undo.RegisterCreatedObjectUndo(root.gameObject, "Add workshop forecourt details");
                var garage = world.GaragePosition;
                var service = PolishGroup(root, "Mobile wheel service beside parking");
                ForecourtProp(source, "Outdoor inspection and wheel service/Inspection trolley and removed wheels/Trolley detail", service,
                    "Drawered tool trolley and laid-out spanners", garage + new Vector3(-7.2f, 0, -4), -12);
                ForecourtProp(source, "Outdoor inspection and wheel service/Inspection trolley and removed wheels/Wheel ready to refit", service,
                    "Removed wheel beside trolley", garage + new Vector3(-5.8f, 0, -4.4f), 28);
                ForecourtProp(source, "Outdoor inspection and wheel service/Floor jack and paired wheel chocks", service,
                    "Floor jack parked beside wheel work", garage + new Vector3(-6.7f, 0, -2.4f), -38);

                var rigging = PolishGroup(root, "Recovery cable preparation beside garage");
                ForecourtProp(source, "Cable preparation station/Cable bench and reel detail", rigging,
                    "Recovery workbench with coiled cable and straps", garage + new Vector3(-8.1f, 0, -5.8f), -8, .84f);
                ForecourtProp(source, "Cable preparation station/Service station spare wheel", rigging,
                    "Spare wheel awaiting refit", garage + new Vector3(-10.6f, 0, -2.8f), 12);

                var stock = PolishGroup(root, "Apron parts and fluids ready for service");
                ForecourtProp(source, "Covered workshop materials/Spare parts packing crate", stock,
                    "Delivered spare parts crate", garage + new Vector3(-3.6f, 0, 5.8f), 9);
                ForecourtProp(source, "Covered workshop materials/Workshop lubricant drum", stock,
                    "Service fluid drum beside parts", garage + new Vector3(-1.8f, 0, 6), -12);
                ForecourtProp(source, "Covered workshop materials/Covered service fluid drum", stock,
                    "Smaller fluid drum", garage + new Vector3(-.5f, 0, 5.7f), 18);
                ForecourtProp(source, "Covered workshop materials/Loading handtruck beside supply crates", stock,
                    "Handtruck returned beside delivery", garage + new Vector3(.7f, 0, 6.5f), -14);

                foreach (var t in root.GetComponentsInChildren<Transform>(true)) { t.gameObject.isStatic = true; t.gameObject.layer = 8; }
                Physics.SyncTransforms();
                foreach (var collider in root.GetComponentsInChildren<Collider>())
                {
                    var bounds = collider.bounds;
                    foreach (float slot in new[] { -2.5f, 2.5f })
                    {
                        // Reserve both parking bays plus a continuous approach to the yard.
                        var drive = new Bounds(garage + new Vector3(slot, 1, -3), new Vector3(3.8f, 4, 13));
                        if (bounds.Intersects(drive)) throw new InvalidOperationException("Workshop prop blocks a player bay: " + collider.name);
                    }
                }
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene())) throw new InvalidOperationException("Could not save workshop forecourt.");
                Undo.CollapseUndoOperations(undo);
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
        }

        private static void ForecourtProp(Transform source, string path, Transform parent, string name, Vector3 point, float yaw, float scale = 1)
        {
            var original = source.Find(path);
            if (original == null) throw new InvalidOperationException("Missing workshop prop: " + path);
            var prop = UnityEngine.Object.Instantiate(original.gameObject, parent);
            prop.name = name; prop.SetActive(true);
            // Shared authored meshes/materials; no cloned physics, sounds, scripts or interiors.
            StripGarageYardProp(prop);
            foreach (var lod in prop.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(lod);
            prop.transform.rotation = Quaternion.Euler(0, yaw, 0);
            prop.transform.localScale *= scale;
            SeatGarageYardProp(prop, Ground(point) - Vector3.up * .075f);
            foreach (var t in prop.GetComponentsInChildren<Transform>(true)) { t.gameObject.isStatic = true; t.gameObject.layer = 8; }
            var bounds = ScenicBounds(prop);
            // A compact collision envelope keeps permanent yard props physical, away from bays.
            GarageActivityBox(parent, name + " collision", parent.InverseTransformPoint(bounds.center), bounds.size);
            ConfigureDetailLod(prop, 130);
        }
    }
}

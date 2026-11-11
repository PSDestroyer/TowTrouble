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
            System.IO.Directory.CreateDirectory("/private/tmp/TowForecourtCorrection");
            if (!System.IO.File.Exists("/private/tmp/TowForecourtCorrection/Before.unity"))
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "/private/tmp/TowForecourtCorrection/Before.unity", true);
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
                var legacy = environment.Find("County landscape polish/Regional landmarks/Garage recovery supply yard");
                if (legacy != null)
                {
                    Undo.RecordObject(legacy.gameObject, "Retire primitive supply racks");
                    legacy.gameObject.SetActive(false);
                }
                var service = PolishGroup(root, "Wheel service along west wall");
                ForecourtProp(source, "Outdoor inspection and wheel service/Inspection trolley and removed wheels/Trolley detail", service,
                    "Drawered tool trolley and laid-out spanners", garage + new Vector3(-13.5f, 0, 18), 90);
                ForecourtProp(source, "Outdoor inspection and wheel service/Floor jack and paired wheel chocks", service,
                    "Floor jack parked beside wheel work", garage + new Vector3(-15.2f, 0, 18), 90);

                var rigging = PolishGroup(root, "Recovery cable preparation beside garage");
                ForecourtProp(source, "Cable preparation station/Cable bench and reel detail", rigging,
                    "Recovery workbench with coiled cable and straps", garage + new Vector3(-13.5f, 0, 23), 90, .84f);

                var stock = PolishGroup(root, "Parts storage along east wall");
                ForecourtProp(source, "Covered workshop materials/Spare parts packing crate", stock,
                    "Delivered spare parts crate", garage + new Vector3(13, 0, 27), 90);
                ForecourtProp(source, "Covered workshop materials/Workshop lubricant drum", stock,
                    "Service fluid drum beside parts", garage + new Vector3(12.6f, 0, 25), 0);
                ForecourtProp(source, "Covered workshop materials/Covered service fluid drum", stock,
                    "Smaller fluid drum", garage + new Vector3(13.7f, 0, 25), 0);
                ForecourtProp(source, "Covered workshop materials/Loading handtruck beside supply crates", stock,
                    "Handtruck returned beside delivery", garage + new Vector3(14.6f, 0, 27), 90);

                // The former free-standing station is the mesh source above. Retire its
                // scene instance after copying, so it no longer occupies the open court.
                var oldStation = source.Find("Cable preparation station");
                if (oldStation != null)
                {
                    Undo.RecordObject(oldStation.gameObject, "Consolidate cable station against workshop wall");
                    oldStation.gameObject.SetActive(false);
                }

                BuildGarageTireRack(stock, Ground(garage + new Vector3(13.2f, 0, 21)) - Vector3.up * .08f,
                    WorkshopFinish("Timber"), WorkshopFinish("Metal"));
                var rack = stock.Find("Organized spare wheel rack");
                rack.rotation = Quaternion.Euler(0, 90, 0);
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) { t.gameObject.isStatic = true; t.gameObject.layer = 8; }
                Physics.SyncTransforms();
                // Reserve the entire turning apron, not just narrow bay centre lines.
                var apron = new Bounds(garage + new Vector3(0, 2, -1), new Vector3(21, 12, 30));
                foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                    if (renderer.bounds.Intersects(apron)) throw new InvalidOperationException("Workshop art enters the turning apron: " + renderer.name);
                foreach (var collider in root.GetComponentsInChildren<Collider>())
                    if (collider.bounds.Intersects(apron)) throw new InvalidOperationException("Workshop collision enters the turning apron: " + collider.name);
                AssetDatabase.SaveAssets();
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

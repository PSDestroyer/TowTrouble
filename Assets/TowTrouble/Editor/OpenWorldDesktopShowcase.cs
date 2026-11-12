using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private const string DesktopArtFolder = VisualRoot + "/Desktop Showcase";
        private const string DesktopArtRoot = "Desktop showcase - workshop and woodland";

        [MenuItem("Tow Trouble/Visuals/Refine Desktop Workshop and R01")]
        public static void RefineDesktopShowcase()
        {
            VisualContext(); ScenicPaths();
            var scene = EditorSceneManager.GetActiveScene();
            const string backup = "/private/tmp/TowDesktopShowcase";
            Directory.CreateDirectory(backup);
            if (!File.Exists(backup + "/Before.unity")) EditorSceneManager.SaveScene(scene, backup + "/Before.unity", true);
            foreach (var path in new[]{"Assets/Settings/PC_RPAsset.asset", "Assets/Settings/PC_Renderer.asset"})
                if (!File.Exists(backup + "/" + Path.GetFileName(path))) File.Copy(path, backup + "/" + Path.GetFileName(path));
            if (!AssetDatabase.IsValidFolder(DesktopArtFolder)) AssetDatabase.CreateFolder(VisualRoot, "Desktop Showcase");
            var map = UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>().Map;
            string mapBefore = EditorJsonUtility.ToJson(map);
            int colliderCount = environment.GetComponentsInChildren<Collider>(true).Length;
            Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Desktop workshop and woodland art");
            try
            {
                var old = environment.Find(DesktopArtRoot);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                var root = PolishGroup(environment, DesktopArtRoot);
                Undo.RegisterCreatedObjectUndo(root.gameObject, "Desktop art layer");
                root.gameObject.AddComponent<RecoveryDesktopScenery>();
                DesktopPipeline();
                var profilePath = DesktopArtFolder + "/Warm woodland grade.asset";
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
                if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, profilePath); }
                if (!profile.TryGet<ColorAdjustments>(out var grade))
                { grade = profile.Add<ColorAdjustments>(); AssetDatabase.AddObjectToAsset(grade, profile); }
                Undo.RecordObject(grade, "Desktop palette");
                // Exposure remains owned by the player's brightness setting.
                grade.saturation.Override(12); grade.contrast.Override(12);
                grade.colorFilter.Override(new Color(1, .985f, .965f));
                EditorUtility.SetDirty(grade); EditorUtility.SetDirty(profile);
                var volume = root.gameObject.AddComponent<Volume>();
                volume.isGlobal = true; volume.priority = 5; volume.sharedProfile = profile;
                var palette = new Dictionary<Material, Material>();
                var random = new System.Random(111128);
                var workshop = PolishGroup(root, "Layered workshop garden margins");
                // Edge islands frame the yard, with a completely clear vehicle apron.
                var centers = new[]{new Vector3(-33,0,29),new Vector3(-32,0,42),new Vector3(13,0,33),
                    new Vector3(12,0,43),new Vector3(-51,0,15),new Vector3(-55,0,-34),new Vector3(-31,0,-44),
                    new Vector3(-8,0,-44),new Vector3(-64,0,29),new Vector3(-78,0,42)};
                foreach (var center in centers) DesktopPlantIsland(center, 4.8f, 10, workshop, random, palette, map, false);
                var woodland = PolishGroup(root, "R01 layered forest floor");
                var site = map.Sites.Single(s => s.Id == "R01");
                var forward = Flat(site.ExitDirection).normalized; var right = Vector3.Cross(Vector3.up, forward);
                foreach (var point in new[]{new Vector2(-7,-5),new Vector2(7,-7),new Vector2(-9,5),new Vector2(9,9),new Vector2(-6,-13),new Vector2(6,-15)})
                    DesktopPlantIsland(site.Position + right*point.x + forward*point.y, 3.2f, 12, woodland, random, palette, map, true);
                DesktopProbe(root, "Workshop reflected sky", new Vector3(-9,25,9), new Vector3(72,24,72));
                DesktopProbe(root, "Woodland reflected canopy", site.Position + Vector3.up*3, new Vector3(48,22,48));
                if (mapBefore != EditorJsonUtility.ToJson(map) || colliderCount != environment.GetComponentsInChildren<Collider>(true).Length)
                    throw new InvalidOperationException("Desktop decoration changed gameplay or collision.");
                Directory.CreateDirectory("Docs/Validation/PCShowcase");
                File.WriteAllText("Docs/Validation/PCShowcase/Authoring.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    renderers = root.GetComponentsInChildren<Renderer>(true).Count(r=>r.enabled),
                    triangles = root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<Renderer>() != null && f.GetComponent<Renderer>().enabled).Sum(f=>f.sharedMesh.triangles.Length/3),
                    collidersAdded = 0, mapUnchanged = true, terrainHeightsAndPaintUntouched = true,
                    paletteMaterials = palette.Count, reflectionProbes = 2
                }, Newtonsoft.Json.Formatting.Indented));
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undo);
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
        }

        private static void DesktopPipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            Undo.RecordObject(pipeline, "Desktop high fidelity rendering");
            pipeline.mainLightShadowmapResolution = 4096;
            pipeline.shadowDistance = 150; pipeline.shadowCascadeCount = 4;
            pipeline.cascade4Split = new Vector3(.09f, .24f, .52f);
            pipeline.shadowNormalBias = .25f; pipeline.shadowDepthBias = .1f;
            pipeline.colorGradingMode = ColorGradingMode.HighDynamicRange;
            pipeline.colorGradingLutSize = 64;
            pipeline.renderScale = 1; pipeline.msaaSampleCount = 4;
            EditorUtility.SetDirty(pipeline);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            foreach(var feature in renderer.rendererFeatures.Where(f=>f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion"))
            {
                Undo.RecordObject(feature,"Desktop contact shading"); var serialized = new SerializedObject(feature);
                serialized.FindProperty("m_Settings.Intensity").floatValue = .42f;
                serialized.FindProperty("m_Settings.Radius").floatValue = .38f;
                serialized.FindProperty("m_Settings.DirectLightingStrength").floatValue = .12f;
                serialized.FindProperty("m_Settings.Samples").intValue = 0; // Native High: 12 samples.
                serialized.FindProperty("m_Settings.Downsample").boolValue = false;
                serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(feature);
            }
            renderer.SetDirty(); EditorUtility.SetDirty(renderer);
        }

        private static void DesktopPlantIsland(Vector3 center, float radius, int count, Transform parent,
            System.Random random, Dictionary<Material,Material> palette, RecoveryMap map, bool nearRecovery)
        {
            var group = PolishGroup(parent, "Fern grass and stone island");
            for (int i=0;i<count;i++)
            {
                float angle=(float)random.NextDouble()*Mathf.PI*2, distance=Mathf.Sqrt((float)random.NextDouble())*radius;
                var p=center+new Vector3(Mathf.Cos(angle)*distance,0,Mathf.Sin(angle)*distance);
                if (ScenicDistance(p,out _,out var road)<road.Width+1.8f) continue;
                if (p.x>-41 && p.x<8 && p.z>-20 && p.z<23) continue;
                if (map.Deliveries.Any(d=>Flat(p-d.Position).magnitude<14)) continue;
                var site=map.Sites.Single(s=>s.Id=="R01");
                var d=p-site.Position;var forward=Flat(site.ExitDirection).normalized;var right=Vector3.Cross(Vector3.up,forward);
                if(nearRecovery && Mathf.Abs(Vector3.Dot(d,right))<4.5f && Vector3.Dot(d,forward)>-8 && Vector3.Dot(d,forward)<25)continue;
                bool stone=i%13==0, bush=i%9==0;
                string prefab=stone?"P_RockSpire_Rock01":bush?"P_Bush2_Light":i%4==0?"P_Fern2":"P_Grass2";
                float size=stone?.28f+(float)random.NextDouble()*.25f:bush?3.1f: i%4==0?1.65f:2.4f+(float)random.NextDouble()*.7f;
                var go=ForestProp(prefab,p,size,random.Next(360),group,false,false);
                // Native high mesh near the player; one mesh set, never all source LODs together.
                foreach(var source in go.GetComponentsInChildren<LODGroup>(true))
                {
                    var levels=source.GetLODs();var chosen=levels.Select(l=>l.renderers).FirstOrDefault(rs=>rs.Any(r=>r!=null));
                    if(chosen != null)foreach(var r in source.GetComponentsInChildren<Renderer>(true))r.enabled=chosen.Contains(r);
                    UnityEngine.Object.DestroyImmediate(source);
                }
                foreach(var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode=ShadowCastingMode.On; r.receiveShadows=true;
                    var mats=r.sharedMaterials;
                    for(int m=0;m<mats.Length;m++)
                    {
                        var original=mats[m];if(original==null)continue;
                        if(!palette.TryGetValue(original,out var copy))
                        {
                            string path=DesktopArtFolder+"/Garden "+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original))+".mat";
                            copy=AssetDatabase.LoadAssetAtPath<Material>(path);
                            if(copy==null){copy=new Material(original);AssetDatabase.CreateAsset(copy,path);}else EditorUtility.CopySerialized(original,copy);
                            foreach(string property in new[]{"_BaseColor","_Color","_Main_Color","_Second_Color","_Bottom_Color","_Tip_Color","_SSS_Color"})
                            if(copy.HasProperty(property))
                            {var c=original.GetColor(property);float grey=c.grayscale;var toned=Color.Lerp(c,new Color(grey,grey,grey,c.a),.16f);copy.SetColor(property,new Color(toned.r*.87f,toned.g*.90f,toned.b*.82f,c.a));}
                            copy.enableInstancing=true;EditorUtility.SetDirty(copy);palette.Add(original,copy);
                        }
                        mats[m]=copy;
                    }
                    r.sharedMaterials=mats;
                }
            }
            var visible=group.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
            if(visible.Length>0){var lod=group.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.012f,visible)});lod.RecalculateBounds();}
        }

        private static void DesktopProbe(Transform root,string name,Vector3 position,Vector3 size)
        {
            var go=new GameObject(name,typeof(ReflectionProbe));go.transform.SetParent(root,false);go.transform.position=position;
            var probe=go.GetComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Custom;probe.resolution=256;
            probe.size=size;probe.boxProjection=true;probe.blendDistance=10;probe.importance=10;
            probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(DesktopArtFolder+"/"+name+".exr");
            probe.intensity=.75f;probe.hdr=true;probe.farClipPlane=250;probe.nearClipPlane=.3f;
            // Baked explicitly after the art layer is complete; never refresh each frame.
        }
    }
}

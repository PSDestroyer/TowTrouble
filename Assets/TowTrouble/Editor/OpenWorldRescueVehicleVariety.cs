using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        [MenuItem("Tow Trouble/Recovery/Assign Varied Rescue Vehicles")]
        public static void AssignVariedRescueVehicles()
        {
            VisualContext();var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();var map=world.Map;
            if(world.Casualty.ModelCount<4)throw new InvalidOperationException("Four recovery vehicle models required");
            ConfigureRescuePaints(world);
            // Keep R01's tested vehicle/profile. Each body appears six times, with changing finishes.
            int[] models={0,2,1,3,2,0,3,1,0,3,2,1,3,0,1,2,0,2,3,1,2,3,0,1};
            var used=new int[4];Undo.RecordObject(map,"Varied recovery vehicles");
            for(int i=0;i<map.Sites.Length;i++)
            {
                var site=map.Sites[i];int model=models[i];site.TargetModel=model;site.TargetPaint=(model+used[model]++)%4;
                if(i>0)FitSituationCar(site,world.Casualty);
            }
            map.Configure(map.Sites,map.Deliveries,ReadSituationCheckpoints(map));EditorUtility.SetDirty(map);world.Casualty.SetPaint(0);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Directory.CreateDirectory("Docs/Validation/RescueVehicleVariety");File.WriteAllText("Docs/Validation/RescueVehicleVariety/Assignments.json",Newtonsoft.Json.JsonConvert.SerializeObject(map.Sites.Select(s=>new{s.Id,s.Tier,model=RecoveryVehicle.ModelNames[s.TargetModel],s.TargetModel,s.TargetPaint,s.Depth,s.Pitch,s.Roll}),Newtonsoft.Json.Formatting.Indented));
        }
        private static void ConfigureRescuePaints(RecoveryWorld world)
        {
            var driver=new SerializedObject(world.Drivers[0]);var source=driver.FindProperty("paintSlots");var lookup=new Dictionary<string,Material[]>();
            for(int i=0;i<source.arraySize;i++)
            {
                var slot=source.GetArrayElementAtIndex(i);var renderer=(Renderer)slot.FindPropertyRelative("Renderer").objectReferenceValue;var values=slot.FindPropertyRelative("Colors");
                string key=AnimationUtility.CalculateTransformPath(renderer.transform,world.Drivers[0].transform)+":"+slot.FindPropertyRelative("Slot").intValue;
                lookup[key]=Enumerable.Range(0,values.arraySize).Select(c=>(Material)values.GetArrayElementAtIndex(c).objectReferenceValue).ToArray();
            }
            var target=new SerializedObject(world.Casualty);var targetSlots=target.FindProperty("paintSlots");var result=new List<RecoveryPaintSlot>();
            for(int i=0;i<targetSlots.arraySize;i++)
            {
                var slot=targetSlots.GetArrayElementAtIndex(i);var renderer=(Renderer)slot.FindPropertyRelative("Renderer").objectReferenceValue;int index=slot.FindPropertyRelative("Slot").intValue;
                string key=AnimationUtility.CalculateTransformPath(renderer.transform,world.Casualty.transform)+":"+index;
                if(!lookup.TryGetValue(key,out var paints)||paints.Length<4)throw new InvalidOperationException("Missing vehicle finish palette "+key);
                var original=(Material)slot.FindPropertyRelative("Colors").GetArrayElementAtIndex(0).objectReferenceValue;
                result.Add(new RecoveryPaintSlot{Renderer=renderer,Slot=index,Colors=original!=null&&original.HasProperty("_Mud_Amount")?new[]{original,original,original,original}:new[]{original,paints[1],paints[2],paints[3]}});
            }
            Undo.RecordObject(world.Casualty,"Recovery finish palette");world.Casualty.ConfigurePaints(result.ToArray());EditorUtility.SetDirty(world.Casualty);
        }
    }
}

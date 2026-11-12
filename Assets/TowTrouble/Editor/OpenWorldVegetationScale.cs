using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        [MenuItem("Tow Trouble/Visuals/Correct Meadow Tree Proportions")]
        public static void CorrectMeadowTreeProportions()
        {
            VisualContext();ScenicPaths();
            var map=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>().Map;
            var trees=environment.GetComponentsInChildren<Transform>().Where(t=>
                (t.name.StartsWith("P_BirchTree")||t.name.StartsWith("P_FirTree")||t.name.StartsWith("P_OakTree")) &&
                PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).ToArray();
            var changes=new List<object>();int index=0;
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Mature meadow tree proportions");
            foreach(var tree in trees)
            {
                var bounds=ScenarioBounds(tree.gameObject);float height=bounds.size.y;
                if(height<1||height>=10||tree.position.z>300)continue;
                if(map.Sites.Any(s=>Flat(s.Position-tree.position).magnitude<27))continue;
                if(ScenicDistance(tree.position,out _,out var road)<road.Width+5)continue;
                if(map.Deliveries.Any(d=>Flat(d.Position-tree.position).magnitude<18))continue;
                bool meadow=tree.parent.name=="Meadow tree islands and alpine scrub";
                // Thin the repeated small-tree scatter only where another canopy remains nearby.
                if(meadow && index++%4==0 && trees.Any(t=>t!=tree&&t.gameObject.activeInHierarchy&&Flat(t.position-tree.position).magnitude<18))
                {Undo.RecordObject(tree.gameObject,"Thin meadow saplings");tree.gameObject.SetActive(false);changes.Add(new{name=tree.name,action="thin",height});continue;}
                float target=tree.name.StartsWith("P_Fir")?12.5f:11.5f;
                // Scale from the existing grounded base. Keep collider ownership with the tree.
                Undo.RecordObject(tree,"Mature canopy scale");tree.localScale*=target/height;
                var after=ScenarioBounds(tree.gameObject);tree.position+=Vector3.up*(bounds.min.y-after.min.y);
                changes.Add(new{name=tree.name,action="mature",before=height,after=target});
            }
            Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());Undo.CollapseUndoOperations(undo);
            Directory.CreateDirectory("Docs/Validation/VegetationScale");
            File.WriteAllText("Docs/Validation/VegetationScale/Trees.json",Newtonsoft.Json.JsonConvert.SerializeObject(changes,Newtonsoft.Json.Formatting.Indented));
        }
    }
}

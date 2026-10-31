using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        public static void ConfigureWheelTracks()
        {
            VisualContext();ScenicPaths();SolveRiver(RiverPlan(),out _);
            var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
            var go=environment.Find("Dynamic wheel impressions");if(go==null){go=new GameObject("Dynamic wheel impressions").transform;go.SetParent(environment,false);}
            var tracks=go.GetComponent<RecoveryWheelTracks>();if(tracks==null)tracks=go.gameObject.AddComponent<RecoveryWheelTracks>();
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(VisualRoot+"/WheelImpressions.shader");
            var mats=new Material[3];int[] layers={1,2,7};
            for(int i=0;i<3;i++){var layer=terrain.terrainData.terrainLayers[layers[i]];var tint=layer.diffuseRemapMax;tint.w=.60f;mats[i]=ScenicMaterial("Dynamic wheel tracks "+i,tint,shader);mats[i].SetTexture("_BaseMap",layer.diffuseTexture);EditorUtility.SetDirty(mats[i]);}
            var roads=environment.GetComponentsInChildren<MeshCollider>().Where(c=>c.name.StartsWith("Road-")).Cast<Collider>().ToArray();
            var bounds=riverCrossings.Select(c=>{var b=new Bounds(c.Center,new Vector3(0,14,0));foreach(int s in new[]{-1,1})foreach(int side in new[]{-1,1})b.Encapsulate(c.Center+c.Direction*s*(c.HalfLength+1)+Vector3.Cross(Vector3.up,Flat(c.Direction).normalized)*side*(c.Width+1));return b;}).ToArray();
            var map=AssetDatabase.LoadAssetAtPath<RecoveryGroundMap>(VisualRoot+"/Painted ground grip.asset");
            tracks.Configure(world,terrain,map,roads,mats,bounds);EditorUtility.SetDirty(tracks);SaveVisualScene();
        }
    }
}

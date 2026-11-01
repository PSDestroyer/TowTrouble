using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        // Only adds visual classification. Never changes grip cells, terrain heights or paint.
        private static void BakeWheelTrackKinds(RecoveryGroundMap map)
        {
            var data=terrain.terrainData;int n=map.Resolution;
            var paint=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);var kinds=new byte[n*n];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                int ax=Mathf.RoundToInt(x/(float)(n-1)*(data.alphamapWidth-1)),az=Mathf.RoundToInt(z/(float)(n-1)*(data.alphamapHeight-1));
                kinds[z*n+x]=(byte)RecoveryTrackSurface.FromPaint(paint[az,ax,0],paint[az,ax,1]+paint[az,ax,6],paint[az,ax,2]+paint[az,ax,5],paint[az,ax,3],paint[az,ax,4]+paint[az,ax,7]);
            }
            map.ConfigureTrackKinds(kinds);EditorUtility.SetDirty(map);
        }
        [MenuItem("Tow Trouble/Art Integration/10 - Refresh Surface Wheel Tracks")]
        public static void ConfigureWheelTracks()
        {
            VisualContext();
            var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
            var go=environment.Find("Dynamic wheel impressions");if(go==null){go=new GameObject("Dynamic wheel impressions").transform;go.SetParent(environment,false);}
            var tracks=go.GetComponent<RecoveryWheelTracks>();if(tracks==null)tracks=go.gameObject.AddComponent<RecoveryWheelTracks>();
            Undo.RecordObject(tracks,"Configure surface wheel tracks");
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(VisualRoot+"/WheelImpressions.shader");
            var mats=new Material[6];int[] layers={1,2,7,0,3,1};
            for(int i=0;i<mats.Length;i++)
            {
                var layer=terrain.terrainData.terrainLayers[layers[i]];var tint=layer.diffuseRemapMax;
                tint.w=i==3?.38f:i>=4?.30f:.54f;
                if(i>=4)tint=i==4?new Color(.16f,.17f,.16f,.30f):new Color(.22f,.18f,.12f,.34f);
                mats[i]=ScenicMaterial("Dynamic wheel tracks "+i,tint,shader);
                mats[i].SetTexture("_BaseMap",i>=4?Texture2D.whiteTexture:layer.diffuseTexture);
                mats[i].SetFloat("_Compaction",i==3?.86f:i>=4?1:.82f);
                mats[i].renderQueue=3005;mats[i].SetFloat("_RutDepth",i>=4?0:i==2?.115f:i==1?.085f:.06f);
                mats[i].SetFloat("_HardSurface",i>=4?1:0);EditorUtility.SetDirty(mats[i]);
            }
            // Explicit material response on the real deck/ramp colliders, not a volume exclusion.
            foreach(var collider in environment.GetComponentsInChildren<Collider>(true))
                if(collider.name=="Bridge drive surface"||collider.name=="Bridge entry ramp")
                {
                    var marker=collider.GetComponent<RecoveryTrackSurface>();
                    if(marker==null)marker=Undo.AddComponent<RecoveryTrackSurface>(collider.gameObject);
                    Undo.RecordObject(marker,"Mark timber contact surface");marker.Configure(RecoveryTrackKind.Wood);EditorUtility.SetDirty(marker);
                }
            var roads=environment.GetComponentsInChildren<MeshCollider>().Where(c=>c.name.StartsWith("Road-")).Cast<Collider>().ToArray();
            var map=AssetDatabase.LoadAssetAtPath<RecoveryGroundMap>(VisualRoot+"/Painted ground grip.asset");
            Undo.RecordObject(map,"Bake visual wheel surface map");BakeWheelTrackKinds(map);
            tracks.Configure(world,terrain,map,roads,mats);EditorUtility.SetDirty(tracks);SaveVisualScene();
        }
    }
}

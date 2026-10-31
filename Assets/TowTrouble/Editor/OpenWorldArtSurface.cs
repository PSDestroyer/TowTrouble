using System.Linq;
using UnityEditor;
using UnityEngine;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        [MenuItem("Tow Trouble/Art Integration/4 - Match Grip to Painted Ground")]
        public static void BakeGroundSurfaceMap()
        {
            VisualContext();ScenicPaths();ScenicRoadField(1025);var data=terrain.terrainData;const int n=512;
            var dropoffs=UnityEngine.Object.FindFirstObjectByType<RecoveryMap>().Deliveries;
            var paint=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);var values=new byte[n*n];var impressions=new byte[n*n];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                int ax=Mathf.RoundToInt(x/(float)(n-1)*(data.alphamapWidth-1)),az=Mathf.RoundToInt(z/(float)(n-1)*(data.alphamapHeight-1));
                float sand=paint[az,ax,2]+paint[az,ax,5],snow=paint[az,ax,4]+paint[az,ax,7];
                int rx=Mathf.RoundToInt(x/(float)(n-1)*1024),rz=Mathf.RoundToInt(z/(float)(n-1)*1024);
                var p=new Vector3(x/(float)(n-1)*1000-500,0,z/(float)(n-1)*1000-500);
                if(roadDistance[rz,rx]<roadWidth[rz,rx]+1||!ClearRecoveryAccess(p,-2))impressions[z*n+x]=1;
                if(paint[az,ax,1]>.4f&&dropoffs.Any(b=>Flat(p-b.Position).sqrMagnitude<225))impressions[z*n+x]=1;
                values[z*n+x]=(byte)(snow>.45f?RecoveryGroundKind.Snow:sand>.5f?RecoveryGroundKind.Sand:RecoveryGroundKind.Rock);
            }
            var map=ScriptableObject.CreateInstance<RecoveryGroundMap>();map.name="Tow County painted surface grip";
            map.Configure(new Vector2(terrain.transform.position.x,terrain.transform.position.z),new Vector2(data.size.x,data.size.z),n,values);
            map.ConfigureWheelImpressions(impressions);
            map=VisualAsset(map,"Painted ground grip.asset");
            foreach(var surface in environment.GetComponentsInChildren<RecoveryGroundSurface>(true)){surface.ConfigureMap(map);EditorUtility.SetDirty(surface);}
            // Match reset points to the actual terrain/deck contact surface after authoring.
            Physics.SyncTransforms();
            var checkpoints=scenicRoads.Where(r=>!r.Edge.Trail).SelectMany(r=>r.Points.Where((p,i)=>i%8==0||i==r.Points.Length-1)).Select(p=>
            {
                if(!Physics.Raycast(p+Vector3.up*8,Vector3.down,out var hit,20,1<<8,QueryTriggerInteraction.Ignore))
                    throw new System.InvalidOperationException("Unsupported road reset point at "+p);
                return hit.point+Vector3.up*.14f;
            }).ToArray();
            var recoveryMap=UnityEngine.Object.FindFirstObjectByType<RecoveryMap>();
            recoveryMap.Configure(recoveryMap.Sites,recoveryMap.Deliveries,checkpoints);EditorUtility.SetDirty(recoveryMap);
            SaveVisualScene();
        }
    }
}

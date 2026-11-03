using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static void PaintBiomes(TerrainData data)
        {
            int n=data.alphamapResolution;var alpha=new float[n,n,6];for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {float wx=-500+x/(float)(n-1)*1000,wz=-500+z/(float)(n-1)*1000,u=wx/900+.5f,v=.5f-wz/900,h=data.GetInterpolatedHeight(x/(float)(n-1),z/(float)(n-1))-50;float snow=Mathf.SmoothStep(0,1,Mathf.InverseLerp(117,140,h))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.26f,.35f,v)));float desert=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f+.025f*Mathf.Sin(v*15),.75f+.025f*Mathf.Sin(v*15),u))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f+.02f*Mathf.Sin(u*17),.39f+.02f*Mathf.Sin(u*17),v));float beach=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(16,42,LakeDistance(new Vector2(u,v))));beach=Mathf.Max(beach,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.84f+.015f*Mathf.Sin(u*15),.93f+.015f*Mathf.Sin(u*15),v))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.56f,.67f,u))));int baseLayer=data.GetSteepness(x/(float)(n-1),z/(float)(n-1))>32?3:0;alpha[z,x,baseLayer]=1;
            for(int k=0;k<6;k++)alpha[z,x,k]*=1-beach;alpha[z,x,2]+=beach;
            for(int k=0;k<6;k++)alpha[z,x,k]*=1-desert;alpha[z,x,5]+=desert;
            for(int k=0;k<6;k++)alpha[z,x,k]*=1-snow;alpha[z,x,4]+=snow;
            float distance=Corridor(new Vector3(wx,0,wz),out _);float road=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(4,10,distance));for(int k=0;k<6;k++)alpha[z,x,k]*=1-road;alpha[z,x,snow>.5f?4:desert>.5f?5:beach>.5f?2:1]+=road;}
            data.SetAlphamaps(0,0,alpha);
        }
        [MenuItem("Tow Trouble/Align Recovery Headings With Road Access")]
        public static void AlignRecoveryHeadings()
        {
            var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.path!=ScenePath)throw new InvalidOperationException("Open map in Edit Mode.");
            plan=JsonUtility.FromJson<Plan>(File.ReadAllText("Assets/TowTrouble/Editor/OpenWorldPlan.json"));nodes=plan.Nodes.ToDictionary(n=>n.Id);terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();environment=terrain.transform.parent;var map=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryMap>()).Single();
            foreach(var site in map.Sites)
            {
                var access=plan.Edges.First(e=>e.A==site.Id||e.B==site.Id);
                var toward=nodes[access.A==site.Id?access.B:access.A].Point-site.Position;
                toward.y=0;site.Heading=Quaternion.LookRotation(toward.normalized).eulerAngles.y;
            }
            map.Configure(map.Sites,map.Deliveries,plan.Edges.Where(e=>!e.Trail).SelectMany(e=>{var a=nodes[e.A].Point;var b=nodes[e.B].Point;int steps=Mathf.CeilToInt(Vector3.Distance(a,b)/8);return Enumerable.Range(0,steps+1).Select(i=>Vector3.Lerp(a,b,i/(float)steps)+Vector3.up*.14f);}).ToArray());
            EditorUtility.SetDirty(map);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Physics.SyncTransforms();
        }
        [MenuItem("Tow Trouble/Refine Open World Road Grades")]
        public static void RefineRoads()
        {
            var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.path!=ScenePath)throw new InvalidOperationException("Open the authored map in Edit Mode.");
            plan=JsonUtility.FromJson<Plan>(File.ReadAllText("Assets/TowTrouble/Editor/OpenWorldPlan.json"));nodes=plan.Nodes.ToDictionary(n=>n.Id);
            terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();environment=terrain.transform.parent;var data=terrain.terrainData;
            var props=environment.Cast<Transform>().Where(t=>t.name=="Mega - retained environment"||t.name=="Biome dressing - So Stylized").SelectMany(t=>t.Cast<Transform>()).Select(t=>new{transform=t,ground=Ground(t.position).y}).ToArray();
            int n=data.heightmapResolution;var hs=new float[n,n];for(int z=0;z<n;z++)for(int x=0;x<n;x++){float wx=-500+x/(float)(n-1)*1000,wz=-500+z/(float)(n-1)*1000;hs[z,x]=(DesignedHeight(wx/900+.5f,.5f-wz/900,0)+50)/350;}data.SetHeights(0,0,hs);
            PaintBiomes(data);
            var terrainMat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/URP terrain.mat");if(terrainMat==null){terrainMat=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));AssetDatabase.CreateAsset(terrainMat,Root+"/Materials/URP terrain.mat");}terrain.materialTemplate=terrainMat;
            var trees=data.treeInstances.Where(t=>Corridor(new Vector3(-500+t.position.x*1000,0,-500+t.position.z*1000),out _)>19).ToArray();for(int i=0;i<trees.Length;i++){var t=trees[i];t.position=new Vector3(t.position.x,data.GetInterpolatedHeight(t.position.x,t.position.z)/350,t.position.z);trees[i]=t;}data.SetTreeInstances(trees,false);
            foreach(var item in props){var p=item.transform.position;p.y+=Ground(p).y-item.ground;item.transform.position=p;var rs=item.transform.GetComponentsInChildren<Renderer>();if(rs.Length>0){var bounds=rs[0].bounds;foreach(var renderer in rs)bounds.Encapsulate(renderer.bounds);if(BlocksRoad(bounds,5))UnityEngine.Object.DestroyImmediate(item.transform.gameObject);}}
            foreach(var f in environment.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.StartsWith("Road-")||f.name.EndsWith(" - Living Mud")))
            {var mesh=f.sharedMesh;var vs=mesh.vertices;for(int i=0;i<vs.Length;i++){var p=f.transform.TransformPoint(vs[i]);if(f.name.StartsWith("Road-")){var parts=f.name.Split('-');var a=nodes[parts[2]].Point;var b=nodes[parts[3]].Point;float t=(i/2)/(float)(vs.Length/2-1);var direction=b-a;direction.y=0;var right=new Vector3(direction.z,0,-direction.x).normalized;float width=plan.Edges.First(e=>e.A==parts[2]&&e.B==parts[3]).Trail?3.2f:4.8f;p=Vector3.Lerp(a,b,t)+right*(i%2==0?-width:width);p.y=Mathf.Lerp(a.y,b.y,t)+.14f;}else p.y=Ground(p).y+.07f;vs[i]=f.transform.InverseTransformPoint(p);}mesh.vertices=vs;mesh.RecalculateNormals();mesh.RecalculateBounds();var c=f.GetComponent<MeshCollider>();if(c!=null){c.sharedMesh=null;c.sharedMesh=mesh;}EditorUtility.SetDirty(mesh);}
            var water=environment.GetComponentsInChildren<MeshFilter>().First(f=>f.name.StartsWith("Lake -"));if(water.sharedMesh.normals.Average(v=>v.y)<0){var triangles=water.sharedMesh.triangles;for(int i=0;i<triangles.Length;i+=3){int t=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=t;}water.sharedMesh.triangles=triangles;water.sharedMesh.RecalculateNormals();EditorUtility.SetDirty(water.sharedMesh);}
            var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryWorld>()).Single();var map=world.Map;
            foreach(var bay in map.Deliveries){var p=Ground(nodes[bay.Id].Point);var marker=environment.GetComponentsInChildren<Transform>().First(t=>t.name.StartsWith("Dropoff "+bay.Id+" -"));marker.position=p;bay.Position=p;}
            foreach(var site in map.Sites){site.Position=Ground(nodes[site.Id].Point)+Vector3.up*.12f;var access=plan.Edges.First(e=>e.A==site.Id||e.B==site.Id);var toward=nodes[access.A==site.Id?access.B:access.A].Point-site.Position;site.Heading=Quaternion.LookRotation(new Vector3(toward.x,0,toward.z)).eulerAngles.y;site.Delivery=map.Deliveries.First(b=>b.Name==site.DeliveryName).Position;var marker=environment.GetComponentsInChildren<Transform>().First(t=>t.name==site.Id+" - "+site.Name);marker.position=Ground(nodes[site.Id].Point);}
            map.Configure(map.Sites,map.Deliveries,plan.Edges.Where(e=>!e.Trail).SelectMany(e=>{var a=nodes[e.A].Point;var b=nodes[e.B].Point;int steps=Mathf.CeilToInt(Vector3.Distance(a,b)/8);return Enumerable.Range(0,steps+1).Select(i=>Vector3.Lerp(a,b,i/(float)steps)+Vector3.up*.14f);}).ToArray());
            foreach(var t in environment.GetComponentsInChildren<TextMeshPro>(true)){if(IsDropoffMark(t.text)||WorldSignCaption(t.text)=="GARAGE  [G]")t.transform.rotation=Quaternion.Euler(90,0,0);}
            // Keep roadside sign boards outside the tow lane.
            foreach(var t in environment.GetComponentsInChildren<Transform>().Where(t=>t.name=="Destination sign")){t.position=t.parent.position+new Vector3(14,3,9);var text=DestinationCaption(t.parent);text.transform.position=t.parent.position+new Vector3(14,3,8.8f);}
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=140;RenderSettings.fogEndDistance=1050;RenderSettings.fogColor=new Color(.63f,.77f,.82f);
            EditorUtility.SetDirty(map);EditorUtility.SetDirty(data);EditorUtility.SetDirty(world);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Physics.SyncTransforms();AlignRecoveryHeadings();
        }
    }
}

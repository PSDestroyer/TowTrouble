using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FishNet.Object;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace TowTrouble.Editor
{
    // Deterministic, scene-owned derivatives. Never writes to SoStylized or the tutorial.
    public static partial class OpenWorldAuthoring
    {
        public const string ScenePath="Assets/TowTrouble/Scenes/OpenWorldRecovery.unity";
        private const string Root="Assets/TowTrouble/OpenWorld";
        [Serializable] private class Plan{public Node[] Nodes;public Edge[] Edges;public Site[] Sites;public Bay[] Deliveries;}
        [Serializable] private class Node{public string Id;public float U,V,Height;public Vector3 Point=>new Vector3((U-.5f)*900,Height,(.5f-V)*900);}
        [Serializable] private class Edge{public string A,B;public bool Trail;}
        [Serializable] private class Site{public string Id,Name,English,Node,Delivery;public int Biome,Tier;public bool Cross;public float OffsetX,OffsetZ;}
        [Serializable] private class Bay{public string Id,Name,English,Node;}
        private static Plan plan;
        private static Dictionary<string,Node> nodes;
        private static Terrain terrain;
        private static Transform environment;
        private static TMP_FontAsset font;
        private static readonly Vector2[] lake={new Vector2(.08f,.73f),new Vector2(.14f,.67f),new Vector2(.22f,.69f),new Vector2(.28f,.73f),new Vector2(.29f,.8f),new Vector2(.39f,.83f),new Vector2(.44f,.88f),new Vector2(.38f,.90f),new Vector2(.30f,.88f),new Vector2(.20f,.86f),new Vector2(.11f,.82f)};
        [MenuItem("Tow Trouble/Create Open World from Approved Map")]
        public static void Create()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before authoring.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scene edits first.");
            if(File.Exists(ScenePath))throw new InvalidOperationException("Open world already exists; edit that scene instead of replacing it.");
            foreach(var folder in new[]{Root,Root+"/Terrain",Root+"/Meshes",Root+"/Materials",Root+"/Photos"})Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();plan=JsonUtility.FromJson<Plan>(File.ReadAllText("Assets/TowTrouble/Editor/OpenWorldPlan.json"));nodes=plan.Nodes.ToDictionary(n=>n.Id);
            if(!AssetDatabase.CopyAsset(DemoBuilder.ScenePath,ScenePath))throw new InvalidOperationException("Could not copy tutorial systems.");
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryWorld>(true)).Single();
            var oldEnvironment=(GameObject)new SerializedObject(world).FindProperty("environment").objectReferenceValue;
            var mudTemplate=oldEnvironment.GetComponentsInChildren<RecoveryMudSurface>(true).First().gameObject;
            oldEnvironment.SetActive(false);environment=new GameObject("Tow County — Mega open world").transform;
            var source=EditorSceneManager.OpenScene("Assets/SoStylized/Demo/M_Demonstration_Mega.unity",OpenSceneMode.Additive);
            try
            {
                var original=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).First();
                CreateTerrain(original,scene);CopyMegaDressing(source,original);
            }
            finally{EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(scene);}
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TowTrouble/Art/UI/Theme/Fonts/TowText SDF.asset");
            CreateRoads();CreateWater();DressBiomes();CreateWorkshop();CreateLocations();
            CreateMud(mudTemplate,world);
            UnityEngine.Object.DestroyImmediate(oldEnvironment);
            world.ConfigureEnvironment(environment.gameObject);world.ConfigureGarage(Ground(nodes["g"].Point));
            var map=world.gameObject.AddComponent<RecoveryMap>();
            var bays=plan.Deliveries.Select(d=>new RecoveryDropoff{Id=d.Id,Name=d.Name,English=BayEnglish(d.Id),Position=Ground(nodes[d.Node].Point)}).ToArray();
            var sites=plan.Sites.Select((s,i)=>{var p=Ground(RecoveryPoint(s))+Vector3.up*.12f;var bay=bays.Single(d=>d.Id==s.Delivery);var edge=plan.Edges.First(e=>e.A==s.Node||e.B==s.Node);var toward=nodes[edge.A==s.Node?edge.B:edge.A].Point-p;return new RecoverySite{Id=s.Id,Name=s.Name,English=s.English,Biome=s.Biome,Tier=s.Tier,Position=p,Delivery=bay.Position,DeliveryName=bay.Name,DeliveryEnglish=bay.English,CrossRegion=s.Cross,Heading=Quaternion.LookRotation(new Vector3(toward.x,0,toward.z)).eulerAngles.y};}).ToArray();
            var checkpoints=new List<Vector3>();foreach(var edge in plan.Edges.Where(e=>!e.Trail)){var a=nodes[edge.A].Point;var b=nodes[edge.B].Point;int n=Mathf.CeilToInt(Vector3.Distance(a,b)/8);for(int i=0;i<=n;i++)checkpoints.Add(Vector3.Lerp(a,b,i/(float)n)+Vector3.up*.14f);}
            map.Configure(sites,bays,checkpoints.ToArray());world.ConfigureMap(map);
            foreach(var car in world.Drivers.Concat(new[]{world.Casualty})){car.ConfigureMap(map);EditorUtility.SetDirty(car);}
            for(int i=0;i<2;i++)world.Drivers[i].transform.SetPositionAndRotation(world.GaragePosition+new Vector3(i==0?-2.5f:2.5f,.1f,-2),Quaternion.Euler(0,180,0));
            world.Casualty.transform.position=sites[0].Position;world.Casualty.gameObject.SetActive(false);
            UnityEditor.Lightmapping.ClearLightingDataAsset();LightmapSettings.lightmaps=Array.Empty<LightmapData>();
            foreach(var renderer in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))renderer.lightmapIndex=-1;
            var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First();camera.farClipPlane=1500;camera.transform.position=world.GaragePosition+new Vector3(12,5,8);camera.transform.LookAt(world.GaragePosition+Vector3.up);
            foreach(var probe in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ReflectionProbe>(true))){probe.transform.position=world.GaragePosition+Vector3.up*4;probe.size=new Vector3(1000,350,1000);}
            world.GetComponent<NetworkObject>().SetSceneId(9104737622UL);
            var buildScenes=EditorBuildSettings.scenes.ToList();if(buildScenes.All(s=>s.path!=ScenePath))buildScenes.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=buildScenes.ToArray();
            EditorUtility.SetDirty(world);EditorUtility.SetDirty(map);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            RefineRoads();FinishLandmarks();CaptureMapPhotos();
            Selection.activeGameObject=world.gameObject;SceneView.lastActiveSceneView?.Frame(new Bounds(nodes["g"].Point,new Vector3(160,80,160)),false);
            Debug.Log("[TowTrouble] OpenWorldRecovery authored: 24 sites, 8 dropoffs. Source scenes preserved. No Play Mode or build.");
        }
        private static string BayEnglish(string id)=>new[]{"Workshop yard","Forest depot","Mountain refuge","High cabin","Lakeside landing","Beach parking","Desert stop","Canyon depot"}[id[0]-'A'];
        private static Vector3 Ground(Vector3 p){p.y=terrain.SampleHeight(p)+terrain.transform.position.y+.08f;return p;}
        private static float Distance(Vector2 p,Vector2 a,Vector2 b,out float t){var d=b-a;t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));return Vector2.Distance(p,a+d*t);}
        private static bool InLake(Vector2 p){bool inside=false;for(int i=0,j=lake.Length-1;i<lake.Length;j=i++)if((lake[i].y>p.y)!=(lake[j].y>p.y)&&p.x<(lake[j].x-lake[i].x)*(p.y-lake[i].y)/(lake[j].y-lake[i].y)+lake[i].x)inside=!inside;return inside;}
        private static float LakeDistance(Vector2 p){float d=float.MaxValue;for(int i=0;i<lake.Length;i++)d=Mathf.Min(d,Distance(p,lake[i],lake[(i+1)%lake.Length],out _)*900);return InLake(p)?-d:d;}
        private static float Corridor(Vector3 p,out float h)
        {
            float best=float.MaxValue;h=22;var q=new Vector2(p.x,p.z);
            foreach(var e in plan.Edges){var a=nodes[e.A].Point;var b=nodes[e.B].Point;float d=Distance(q,new Vector2(a.x,a.z),new Vector2(b.x,b.z),out float t);if(d<best){best=d;h=Mathf.Lerp(a.y,b.y,t);}}
            return best;
        }
        private static float DesignedHeight(float u,float v,float original)
        {
            float mountain=190*Mathf.Exp(-Mathf.Pow((u-.48f)/.25f,2)-Mathf.Pow((v-.10f)/.19f,2));
            float ridge=45*Mathf.Exp(-Mathf.Pow((u-.24f)/.20f,2)-Mathf.Pow((v-.31f)/.25f,2));
            float h=22+mountain+ridge+(Mathf.PerlinNoise(u*12+41,v*12+7)-.5f)*9+Mathf.Clamp(original,-30,100)*.035f;
            if(u>.69f&&v>.34f)h=24+7*Mathf.Sin(u*45+v*14)+4*Mathf.Sin(v*55)+Mathf.Max(0,(u-.88f)*90);
            float lakeDistance=LakeDistance(new Vector2(u,v));if(lakeDistance<45)h=Mathf.Lerp(8.1f,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(-9,45,lakeDistance)));
            var point=new Vector3((u-.5f)*900,0,(.5f-v)*900);float road=Corridor(point,out float grade);if(road<75)h=Mathf.Lerp(grade,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(8f,75,road)));
            foreach(var n in plan.Nodes){float radius=n.Id=="g"?31:n.Id.Length==1&&n.Id!="g"?13:n.Id.StartsWith("R")?6:7;float distance=Vector2.Distance(new Vector2(point.x,point.z),new Vector2(n.Point.x,n.Point.z));if(distance<radius+7)h=Mathf.Lerp(n.Height,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(radius,radius+7,distance)));}
            if(road<75)h=Mathf.Lerp(grade,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(8f,75,road)));
            return h;
        }
        private static void CreateTerrain(Terrain original,Scene target)
        {
            var data=UnityEngine.Object.Instantiate(original.terrainData);data.name="Tow County - authored Mega terrain";data.size=new Vector3(1000,350,1000);data.heightmapResolution=513;
            var hs=new float[513,513];for(int z=0;z<513;z++)for(int x=0;x<513;x++){float wx=-500+x/512f*1000,wz=-500+z/512f*1000,u=wx/900+.5f,v=.5f-wz/900;float old=original.terrainData.GetInterpolatedHeight(x/512f,z/512f)+original.transform.position.y;hs[z,x]=(DesignedHeight(u,v,old)+50)/350;}
            data.SetHeights(0,0,hs);data.terrainLayers=original.terrainData.terrainLayers.Concat(new[]{AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/SoStylized/Environment/Landscape/Layer/TL_DesertSand.terrainlayer")}).ToArray();
            data.alphamapResolution=512;var alpha=new float[512,512,6];for(int z=0;z<512;z++)for(int x=0;x<512;x++){float wx=-500+x/511f*1000,wz=-500+z/511f*1000,u=wx/900+.5f,v=.5f-wz/900;float h=data.GetInterpolatedHeight(x/511f,z/511f)-50;int layer=h>124&&v<.3f?4:u>.70f&&v>.33f?5:LakeDistance(new Vector2(u,v))<36||v>.85f&&u<.61f?2:h>70?3:0;float road=Corridor(new Vector3(wx,0,wz),out _);float r=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(4,9,road));alpha[z,x,layer]=1-r;alpha[z,x,layer==4?4:1]+=r;}
            data.SetAlphamaps(0,0,alpha);
            var trees=new List<TreeInstance>();foreach(var tree in original.terrainData.treeInstances){var t=tree;var p=new Vector3(-500+t.position.x*1000,0,-500+t.position.z*1000);float u=p.x/900+.5f,v=.5f-p.z/900;float h=data.GetInterpolatedHeight(t.position.x,t.position.z)-50;if(Corridor(p,out _)<19||LakeDistance(new Vector2(u,v))<13||u>.68f||h>120)continue;t.position=new Vector3(t.position.x,(h+50)/350,t.position.z);trees.Add(t);}data.SetTreeInstances(trees.ToArray(),false);
            data.SetDetailResolution(256,16);for(int layer=0;layer<data.detailPrototypes.Length;layer++){var values=new int[256,256];if(layer<2)for(int z=0;z<256;z++)for(int x=0;x<256;x++){float wx=-500+x/255f*1000,wz=-500+z/255f*1000,u=wx/900+.5f,v=.5f-wz/900;if(u<.68f&&v>.32f&&v<.85f&&LakeDistance(new Vector2(u,v))>17&&Corridor(new Vector3(wx,0,wz),out _)>12)values[z,x]=(x*7+z*13+layer)%4==0?2:0;}data.SetDetailLayer(0,0,layer,values);}
            AssetDatabase.CreateAsset(data,Root+"/Terrain/TowCountyTerrain.asset");SceneManager.SetActiveScene(target);var go=Terrain.CreateTerrainGameObject(data);go.transform.SetParent(environment,false);go.transform.position=new Vector3(-500,-50,-500);go.layer=8;go.AddComponent<RecoveryGroundSurface>().ConfigureTerrain();go.name="Mega terrain - roads and biomes";terrain=go.GetComponent<Terrain>();terrain.materialTemplate=original.materialTemplate;terrain.drawInstanced=true;terrain.heightmapPixelError=5;terrain.detailObjectDistance=85;terrain.detailObjectDensity=.6f;terrain.treeDistance=500;terrain.treeBillboardDistance=180;terrain.basemapDistance=1300;EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
        }
        private static Material Mat(string name,Color color)
        {var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.color=color;m.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(m,Root+"/Materials/"+name+".mat");return m;}
        private static GameObject MeshObject(string name,Mesh mesh,Material material,Transform parent,bool collider=true)
        {AssetDatabase.CreateAsset(mesh,Root+"/Meshes/"+name+".asset");var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.layer=8;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;if(collider)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;}
        private static void Text(string text,Vector3 p,float yaw,float size,Transform parent)
        {var go=new GameObject(text,typeof(TextMeshPro));go.transform.SetParent(parent,false);go.transform.position=p;go.transform.rotation=yaw==90?Quaternion.Euler(90,0,0):Quaternion.Euler(0,yaw,0);var t=go.GetComponent<TextMeshPro>();t.font=font;t.text=text;t.fontSize=size*10;t.color=new Color(1,.94f,.77f);t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(20,5);}
        private static GameObject Cube(string name,Vector3 p,Vector3 size,Material material,Transform parent,bool collider=true)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=p;go.transform.localScale=size;go.layer=8;go.GetComponent<Renderer>().sharedMaterial=material;if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
    }
}

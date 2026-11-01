using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private const string PolishFolder=VisualRoot+"/CountyPolish";
        private static Dictionary<Vector2Int,List<Vector3>> polishRoads;
        private static RecoveryMap polishMap;
        [MenuItem("Tow Trouble/Visuals/Polish County Landscape and Regional Landmarks")]
        public static void PolishCountyLandscape()
        {
            VisualContext();ScenicPaths();polishMap=UnityEngine.Object.FindFirstObjectByType<RecoveryMap>();
            Directory.CreateDirectory(PolishFolder);AssetDatabase.Refresh();CachePolishRoads();
            var root=ReplaceArtGroup("County landscape polish");Undo.RegisterCreatedObjectUndo(root.gameObject,"County landscape polish");
            PolishCountySky();PolishCountyTerrain();PolishGroundResponse();PolishCountyGeology(root);PolishRoadEdges(root);PolishForestGroves(root);PolishRegionalLandmarks(root);PolishRecoveryStories(root);BuildMountainCliffsAndGarage();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Directory.CreateDirectory("Docs/Validation/CountyPolish");File.WriteAllText("Docs/Validation/CountyPolish/SceneBudget.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{props=root.GetComponentsInChildren<MeshFilter>().Length,renderers=root.GetComponentsInChildren<Renderer>().Length,lods=root.GetComponentsInChildren<LODGroup>().Length,colliders=root.GetComponentsInChildren<Collider>().Length,scripts=root.GetComponentsInChildren<MonoBehaviour>().Length},Newtonsoft.Json.Formatting.Indented));
        }
        private static void CachePolishRoads()
        {
            polishRoads=new Dictionary<Vector2Int,List<Vector3>>();
            foreach(var f in environment.Find("Road network - 24 recovery spurs").GetComponentsInChildren<MeshFilter>())
            {
                var v=f.sharedMesh.vertices;for(int i=0;i<v.Length/6;i++)
                {var p=f.transform.TransformPoint((v[i*6+2]+v[i*6+3])*.5f);var key=new Vector2Int(Mathf.FloorToInt(p.x/16),Mathf.FloorToInt(p.z/16));if(!polishRoads.TryGetValue(key,out var cell)){cell=new List<Vector3>();polishRoads[key]=cell;}cell.Add(p);}
            }
        }
        private static bool PolishRoadNear(Vector3 p,float margin)
        {
            int r=Mathf.CeilToInt((margin+5)/16);var key=new Vector2Int(Mathf.FloorToInt(p.x/16),Mathf.FloorToInt(p.z/16));
            for(int z=-r;z<=r;z++)for(int x=-r;x<=r;x++)if(polishRoads.TryGetValue(key+new Vector2Int(x,z),out var cell)&&cell.Any(q=>Flat(q-p).sqrMagnitude<(margin+4)*(margin+4)))return true;
            return false;
        }
        private static bool PolishClear(Vector3 p,float radius,bool allowRoad=false)
        {
            if(p.y<10.8f||Mathf.Abs(p.x)>455||Mathf.Abs(p.z)>455)return false;
            if(polishMap.Sites.Any(s=>Flat(s.Position-p).magnitude<(s.Id=="R01"?35:18)+radius))return false;
            if(polishMap.Deliveries.Any(d=>Flat(d.Position-p).magnitude<20+radius))return false;
            foreach(var site in polishMap.Sites){var d=Flat(p-site.Position);if(Vector3.Dot(d,site.ExitDirection)>0&&Vector3.Dot(d,site.ExitDirection)<38&&Mathf.Abs(Vector3.Dot(d,Vector3.Cross(Vector3.up,site.ExitDirection)))<7+radius)return false;}
            return allowRoad||!PolishRoadNear(p,radius);
        }
        private static Transform PolishGroup(Transform root,string name){var g=new GameObject(name).transform;g.SetParent(root,false);return g;}
        private static void PolishCountySky()
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(PolishFolder+"/Clear county sky.mat");if(material==null){material=new Material(Shader.Find("TowTrouble/County Sky"));AssetDatabase.CreateAsset(material,PolishFolder+"/Clear county sky.mat");}
            var light=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);var d=-light.transform.forward;material.SetVector("_SunDirection",new Vector4(d.x,d.y,d.z,0));RenderSettings.skybox=material;EditorUtility.SetDirty(material);
            var ground=AssetDatabase.LoadAssetAtPath<Material>(PolishFolder+"/Blended distant ground.mat");if(ground==null){ground=new Material(Shader.Find("TowTrouble/County Distant Ground"));AssetDatabase.CreateAsset(ground,PolishFolder+"/Blended distant ground.mat");}
            ground.SetTexture("_Grass",terrain.terrainData.terrainLayers[0].diffuseTexture);ground.SetTexture("_Snow",terrain.terrainData.terrainLayers[4].diffuseTexture);ground.SetTexture("_Sand",terrain.terrainData.terrainLayers[5].diffuseTexture);EditorUtility.SetDirty(ground);
            foreach(var r in environment.Find("County horizon").GetComponentsInChildren<MeshRenderer>())if(r.name.Contains("Foothills")||r.name.Contains("Distant ridges")||r.name.Contains("Surrounding landscape")){Undo.RecordObject(r,"Match distant landscape to biomes");r.sharedMaterial=ground;}
        }
        private static void PolishCountyTerrain()
        {
            var data=terrain.terrainData;const string path="Assets/TowTrouble/Editor/CountyPolishTerrainBaseline.asset";
            if(AssetDatabase.LoadAssetAtPath<TerrainData>(path)==null)AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(data),path);
            var source=AssetDatabase.LoadAssetAtPath<TerrainData>(path);if(source.heightmapResolution!=data.heightmapResolution)throw new InvalidOperationException("County terrain baseline mismatch");
            Undo.RegisterCompleteObjectUndo(data,"Soften mountain cuts and biome seams");
            int n=data.heightmapResolution;var h=data.GetHeights(0,0,n,n);var original=source.GetHeights(0,0,n,n);
            for(int z=3;z<n-3;z++)for(int x=3;x<n-3;x++)
            {
                if(original[z,x]*data.size.y+terrain.transform.position.y<55)continue;
                float dx=(original[z,x+1]-original[z,x-1])*data.size.y*.5f,dz=(original[z+1,x]-original[z-1,x])*data.size.y*.5f;
                if(dx*dx+dz*dz<1.8f)continue;
                var p=terrain.transform.position+new Vector3(x/(float)(n-1)*data.size.x,original[z,x]*data.size.y,z/(float)(n-1)*data.size.z);
                if(!PolishClear(p,20)||RiverDistance(p)<24)continue;
                float sum=0;for(int j=-2;j<=2;j++)for(int i=-2;i<=2;i++)sum+=original[z+j,x+i];
                h[z,x]=original[z,x]+Mathf.Clamp((sum/25-original[z,x])*.55f,-1.5f/data.size.y,1.5f/data.size.y);
            }
            data.SetHeights(0,0,h);
            int a=data.alphamapResolution;var paint=source.GetAlphamaps(0,0,a,a);var result=data.GetAlphamaps(0,0,a,a);
            for(int z=2;z<a-2;z++)for(int x=2;x<a-2;x++)
            {
                var p=Ground(terrain.transform.position+new Vector3(x/(float)(a-1)*data.size.x,0,z/(float)(a-1)*data.size.z));if(!PolishClear(p,8)||p.y<14)continue;
                bool boundary=false;for(int layer=0;layer<data.alphamapLayers;layer++)if(Mathf.Abs(paint[z,x,layer]-paint[z+2,x+2,layer])>.15f||Mathf.Abs(paint[z,x,layer]-paint[z-2,x-2,layer])>.15f){boundary=true;break;}if(!boundary)continue;
                float total=0;for(int layer=0;layer<data.alphamapLayers;layer++){float sum=0;for(int j=-2;j<=2;j++)for(int i=-2;i<=2;i++)sum+=paint[z+j,x+i,layer];result[z,x,layer]=Mathf.Lerp(paint[z,x,layer],sum/25,.85f);total+=result[z,x,layer];}for(int layer=0;layer<data.alphamapLayers;layer++)result[z,x,layer]/=Mathf.Max(.001f,total);
            }
            data.SetAlphamaps(0,0,result);EditorUtility.SetDirty(data);Physics.SyncTransforms();
        }
        private static void PolishGroundResponse()
        {
            var map=AssetDatabase.LoadAssetAtPath<RecoveryGroundMap>(VisualRoot+"/Painted ground grip.asset");var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var coverage=(byte[])typeof(RecoveryGroundMap).GetField("wheelImpressions",flags).GetValue(map);var kinds=(byte[])typeof(RecoveryGroundMap).GetField("trackKinds",flags).GetValue(map);
            int n=map.Resolution;var values=new byte[n*n];var tracks=(byte[])kinds.Clone();var data=terrain.terrainData;var paint=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                int ax=Mathf.RoundToInt(x/(float)(n-1)*(data.alphamapWidth-1)),az=Mathf.RoundToInt(z/(float)(n-1)*(data.alphamapHeight-1));float sand=paint[az,ax,2]+paint[az,ax,5],snow=paint[az,ax,4]+paint[az,ax,7];
                values[z*n+x]=(byte)(snow>.45f?RecoveryGroundKind.Snow:sand>.5f?RecoveryGroundKind.Sand:RecoveryGroundKind.Rock);
                if(tracks[z*n+x]<(byte)RecoveryTrackKind.Hard)tracks[z*n+x]=(byte)RecoveryTrackSurface.FromPaint(paint[az,ax,0],paint[az,ax,1],sand,paint[az,ax,3]+paint[az,ax,6],snow);
            }
            Undo.RecordObject(map,"Match surface response to blended biome paint");map.name="Painted ground grip";map.Configure(new Vector2(terrain.transform.position.x,terrain.transform.position.z),new Vector2(data.size.x,data.size.z),n,values);map.ConfigureWheelImpressions(coverage);map.ConfigureTrackKinds(tracks);EditorUtility.SetDirty(map);
        }
        private static GameObject PolishProp(string name,Vector3 p,float height,float yaw,Transform parent,float distance=180)
        {
            var go=ScenicPrefab(name,p,height,yaw,parent,false);if(go==null)throw new InvalidOperationException("Missing county prop "+name);
            foreach(var script in go.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
            foreach(var renderer in go.GetComponentsInChildren<Renderer>()){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;}
            ConfigureDetailLod(go,distance);return go;
        }
        private static void PolishForestGroves(Transform root)
        {
            var group=PolishGroup(root,"Forest groves and ecotones");var random=new System.Random(110126);int clusters=0;
            for(float z=-280;z<230;z+=38)for(float x=-410;x<170;x+=38)
            {
                var p=Ground(new Vector3(x,0,z));if(p.y<18||p.y>110||DesertAt(p)||Mathf.PerlinNoise(x*.012f+4,z*.012f+8)<.56f||!PolishClear(p,12)||Flat(p-nodes["g"].Point).magnitude<50)continue;
                if(clusters++>=42)return;var cluster=PolishGroup(group,"Grove "+clusters);
                for(int i=0;i<10;i++)
                {
                    float angle=(float)random.NextDouble()*Mathf.PI*2,r=(float)random.NextDouble()*11;var q=Ground(p+new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r));if(!PolishClear(q,i<4?4:1)||!DetailPositionAllowed(q,polishMap,i<4?4:1))continue;
                    string name=i<4?(i%2==0?"P_FirTree3":"P_BirchTreeB1"):i%3==0?"P_Fern1":"P_Bush1_Light";PolishProp(name,q,i<4?7+(float)random.NextDouble()*5:.65f+(float)random.NextDouble(),i*67,cluster,i<4?300:150);
                }
            }
        }
        private static void PolishRecoveryStories(Transform root)
        {
            var group=PolishGroup(root,"Recovery accident stories");var roads=polishRoads.Values.SelectMany(v=>v).ToArray();
            foreach(var site in polishMap.Sites.Skip(1))
            {
                if(RoadsideRecovery(site)||site.Kind==RecoverySituationKind.EngineFailure)continue;
                var road=roads.OrderBy(p=>Flat(p-site.Position).sqrMagnitude).First();if(Flat(road-site.Position).magnitude>45)continue;
                var previous=environment.Find("Distinct recovery situations").Cast<Transform>().Single(t=>t.name.StartsWith(site.Id+" -")).Find("Approach from existing road");if(previous!=null){Undo.RecordObject(previous.gameObject,"Replace generic approach with accident trail");previous.gameObject.SetActive(false);}
                var parent=PolishGroup(group,site.Id+" - incident trail");parent.position=site.Position;DressRoadsideAccidentTrail(parent,site,road,false);ConfigureDetailLod(parent.gameObject,220);
            }
        }
    }
}

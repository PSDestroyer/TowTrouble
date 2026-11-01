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
        private const string CliffYardFolder=VisualRoot+"/CliffsAndGarage";
        [MenuItem("Tow Trouble/Visuals/Mountain Cliff Belts and Garage Grounds")]
        public static void RefineMountainCliffsAndGarage()
        {
            VisualContext();ScenicPaths();polishMap=UnityEngine.Object.FindFirstObjectByType<RecoveryMap>();CachePolishRoads();
            Directory.CreateDirectory(CliffYardFolder);AssetDatabase.Refresh();
            // The county massif recipe now also uses native cliff forms.
            var polish=environment.Find("County landscape polish");if(polish!=null){var previous=polish.Find("Embedded geology and talus");if(previous!=null)Undo.DestroyObjectImmediate(previous.gameObject);PolishCountyGeology(polish);}
            BuildMountainCliffsAndGarage();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        private static void BuildMountainCliffsAndGarage()
        {
            Directory.CreateDirectory(CliffYardFolder);var root=ReplaceArtGroup("Mountain cliffs and garage grounds");Undo.RegisterCreatedObjectUndo(root.gameObject,"Mountain cliffs and garage grounds");
            BuildMountainCliffBelts(root);BuildGarageServiceGrounds(root);
            Directory.CreateDirectory("Docs/Validation/MountainCliffsGarage");File.WriteAllText("Docs/Validation/MountainCliffsGarage/Budget.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{meshes=root.GetComponentsInChildren<MeshFilter>().Length,renderers=root.GetComponentsInChildren<Renderer>().Length,colliders=root.GetComponentsInChildren<Collider>().Length,lods=root.GetComponentsInChildren<LODGroup>().Length,scripts=root.GetComponentsInChildren<MonoBehaviour>().Length},Newtonsoft.Json.Formatting.Indented));
        }
        private static void BuildMountainCliffBelts(Transform root)
        {
            var parent=PolishGroup(root,"Continuous mountain cliff belts");
            // Replace large detached boulders, while leaving the fine scree at the foot.
            foreach(string name in new[]{"Mountain outcrops and scree","Authored biome groves and rock formations"})
            {
                var old=environment.Find(name);if(old==null)continue;
                foreach(Transform child in old)
                {
                    if(!child.name.StartsWith("P_BoulderClassic"))continue;var bounds=ScenicBounds(child.gameObject);
                    if(bounds.center.y<105||bounds.center.z<175||bounds.size.y<2.7f)continue;
                    Undo.RecordObject(child.gameObject,"Replace isolated mountain boulder with cliff belt");child.gameObject.SetActive(false);
                }
            }
            var snow=GorgeRockMaterial("Snow ridge cliff faces",new Color(.80f,.84f,.86f),false,true);
            var bare=GorgeRockMaterial("Mountain ridge cliff faces",new Color(.71f,.73f,.69f),false,false);
            var rockTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SoStylized/Environment/Rocks/Textures/Classic/T_RockClassic_BC.PNG");var normalTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SoStylized/Environment/Rocks/Textures/Classic/T_RockClassicCliffs_N.PNG");
            foreach(var material in new[]{snow,bare}){material.shader=Shader.Find("TowTrouble/Alpine Cliff");material.SetTexture("_Rock_Texture",rockTexture);material.SetTexture("_Rock_Normal_Texture",normalTexture);material.SetTexture("_Snow_Texture",terrain.terrainData.terrainLayers[4].diffuseTexture);material.SetColor("_Snow_Tint",new Color(.92f,.96f,1));material.SetFloat("_MaskTopLayer",0);material.SetFloat("_Rock_Normal_Flatten",.18f);EditorUtility.SetDirty(material);}
            Vector3[] anchors={new Vector3(-131,0,340),new Vector3(-127,0,351),new Vector3(-124,0,363),new Vector3(-126,0,375),new Vector3(-120,0,388),new Vector3(-113,0,400),new Vector3(-103,0,412),new Vector3(-91,0,423),new Vector3(-79,0,417),new Vector3(-66,0,418),new Vector3(-53,0,415),new Vector3(-41,0,406),new Vector3(-35,0,394),new Vector3(-63,0,335),new Vector3(-40,0,345),new Vector3(15,0,350),new Vector3(53,0,357),new Vector3(84,0,369),new Vector3(124,0,363),new Vector3(154,0,347),new Vector3(217,0,303)};
            int count=0;
            foreach(var anchor in anchors)
            {
                var p=Ground(anchor);if(!PolishClear(p,7)||RiverDistance(p)<17)continue;
                var local=p-terrain.transform.position;var normal=terrain.terrainData.GetInterpolatedNormal(local.x/terrain.terrainData.size.x,local.z/terrain.terrainData.size.z);var outward=Flat(normal);if(outward.sqrMagnitude<.01f)outward=Vector3.left;
                int variant=count%6+1;var raw=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("P_CliffClassic"+variant)).GetComponentsInChildren<MeshFilter>().First(f=>f.sharedMesh!=null).sharedMesh;
                var bounds=raw.bounds;float width=17+count%3*2,height=8.5f+count%4,depth=7;
                var rotation=Quaternion.LookRotation(outward);outward.Normalize();float top=Ground(p-outward*3).y+.35f;var centre=Ground(p+outward*4);centre.y=top-height*.5f;var vs=raw.vertices;var mesh=UnityEngine.Object.Instantiate(raw);mesh.name="Ridge cliff "+count.ToString("00");
                for(int i=0;i<vs.Length;i++)
                {
                    float y=Mathf.InverseLerp(bounds.min.y,bounds.max.y,vs[i].y);var q=vs[i]-bounds.center;
                    var world=centre+rotation*Vector3.Scale(q,new Vector3(width/bounds.size.x,height/bounds.size.y,depth/bounds.size.z));
                    if(y<.70f)world.y=Mathf.Min(world.y,Ground(world).y-1.2f);
                    vs[i]=world;
                }
                mesh.vertices=vs;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
                var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent,false);go.layer=8;go.isStatic=true;
                go.GetComponent<MeshFilter>().sharedMesh=VisualAsset(mesh,"CliffsAndGarage/"+mesh.name+".asset");go.GetComponent<MeshCollider>().sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;go.GetComponent<MeshRenderer>().sharedMaterials=Enumerable.Repeat(p.y>125?snow:bare,go.GetComponent<MeshFilter>().sharedMesh.subMeshCount).ToArray();ConfigureDetailLod(go,450);count++;
            }
        }
        private static void BuildGarageServiceGrounds(Transform root)
        {
            var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();var parent=PolishGroup(root,"Garage working yard");parent.position=Ground(world.GaragePosition+new Vector3(-20,0,-23));
            var wood=PolishMat("Weathered wood",new Color(.34f,.25f,.15f));var metal=PolishMat("Oxidised metal",new Color(.23f,.28f,.29f));var rubber=PolishMat("Stored rubber",new Color(.06f,.07f,.065f));var paint=PolishMat("Faded parking paint",new Color(.80f,.76f,.59f));var red=PolishMat("Faded rescue red",new Color(.48f,.17f,.12f));
            BuildGarageGravel(parent);
            for(int bay=0;bay<4;bay++)
            {
                float x=-9+bay*5;foreach(float edge in new[]{-1.55f,1.55f})PolishBox(parent,"Parking bay edge",new Vector3(x+edge,.04f,-3.5f),new Vector3(.10f,.015f,6),paint);
                PolishBox(parent,"Parking stop",new Vector3(x,.12f,-6.7f),new Vector3(2,.22f,.3f),metal);
            }
            var parked=PolishGroup(parent,"Parked salvage vehicles");
            try{for(int i=0;i<2;i++)
            {
                int modelIndex=i==0?1:2;world.Casualty.SetPaint(i==0?1:0);var p=Ground(parent.TransformPoint(new Vector3(-9+i*5,0,-3.5f)));var model=world.Casualty.ModelAt(modelIndex);p.y+=model.Radius+.10f-model.Anchors[0].y;
                var car=UiSkinAuthoring.PreviewVehicle(world.Casualty,modelIndex,parked,p);car.name=i==0?"Stored salvage hauler":"Stored compact salvage";car.transform.rotation=Quaternion.identity;PoseSituationPhoto(car,world.Casualty,new RecoverySite{TargetModel=modelIndex});
                foreach(var script in car.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
                var collider=car.AddComponent<BoxCollider>();var b=ScenicBounds(car);collider.center=car.transform.InverseTransformPoint(b.center);collider.size=new Vector3(Mathf.Min(b.size.x,2.1f),Mathf.Min(b.size.y,2.2f),Mathf.Min(b.size.z,4.2f))/car.transform.lossyScale.x;car.layer=8;ConfigureDetailLod(car,190);
            }}finally{world.Casualty.SetPaint(0);}
            var storage=PolishGroup(parent,"Workshop storage and work area");
            for(int i=0;i<7;i++)
            {
                float x=-10+i*1.25f;PolishBox(storage,"Spare tire stack",new Vector3(x,.25f,5),new Vector3(.8f,.24f,.8f),rubber,default,PrimitiveType.Cylinder);if(i%2==0)PolishBox(storage,"Stacked tire",new Vector3(x,.7f,5),new Vector3(.8f,.22f,.8f),rubber,default,PrimitiveType.Cylinder);
            }
            for(int i=0;i<4;i++){PolishBox(storage,"Oil drum",new Vector3(5+i*.9f,.55f,5),new Vector3(.65f,.55f,.65f),i%2==0?red:metal,default,PrimitiveType.Cylinder);PolishBox(storage,"Timber storage box",new Vector3(8+i%2, .45f+i/2*.85f,1),new Vector3(.9f,.85f,.85f),wood);}
            PolishBox(storage,"Outdoor workbench",new Vector3(-1,1,5),new Vector3(3,.2f,1.3f),wood);foreach(float x in new[]{-2.2f,.2f})PolishBox(storage,"Bench legs",new Vector3(x,.5f,5),new Vector3(.2f,1,1.2f),metal);
            for(int i=0;i<2;i++){PolishBox(storage,"Cable spool rim",new Vector3(10,.5f+i*.65f,1.5f),new Vector3(1.2f,.06f,1.2f),wood,default,PrimitiveType.Cylinder);}PolishBox(storage,"Coiled recovery cable",new Vector3(10,.82f,1.5f),new Vector3(.9f,.26f,.9f),metal,default,PrimitiveType.Cylinder);
            var fences=PolishGroup(parent,"Yard boundary fences");
            for(int i=0;i<7;i++)BuildYardFence(fences,new Vector3(-12+i*4,0,-9),0,wood);
            for(int i=0;i<3;i++){BuildYardFence(fences,new Vector3(-14,0,-6+i*4),90,wood);BuildYardFence(fences,new Vector3(14,0,-6+i*4),90,wood);}
            // Open north side faces the garage: no gate across the manoeuvring route.
            var green=PolishGroup(parent,"Yard edge vegetation");for(int i=0;i<24;i++)
            {
                float angle=i/24f*Mathf.PI*2;var p=Ground(parent.position+new Vector3(Mathf.Cos(angle)*18,0,Mathf.Sin(angle)*14));if(PolishRoadNear(p,3)||world.Map.Deliveries.Any(d=>Flat(d.Position-p).magnitude<20))continue;
                PolishProp(i%3==0?"P_BirchSapling1":"P_Bush1_Light",p,i%3==0?3.2f:.8f+i%4*.2f,i*37,green,i%3==0?240:150);
            }
            ConfigureDetailLod(storage.gameObject,180);ConfigureDetailLod(fences.gameObject,220);
        }
        private static void BuildYardFence(Transform parent,Vector3 p,float yaw,Material wood)
        {
            if(PolishRoadNear(parent.TransformPoint(p),3))return;
            var root=PolishGroup(parent,"Weathered yard fence");root.localPosition=p;root.localRotation=Quaternion.Euler(0,yaw,0);
            foreach(float x in new[]{-1.8f,1.8f})PolishBox(root,"Fence post",new Vector3(x,.7f,0),new Vector3(.16f,1.4f,.16f),wood);
            foreach(float y in new[]{.5f,1.05f})PolishBox(root,"Fence rail",new Vector3(0,y,0),new Vector3(3.7f,.15f,.12f),wood);
            var collider=root.gameObject.AddComponent<BoxCollider>();collider.center=Vector3.up*.7f;collider.size=new Vector3(3.7f,1.4f,.18f);root.gameObject.layer=8;
        }
        private static void BuildGarageGravel(Transform parent)
        {
            var template=AssetDatabase.LoadAssetAtPath<Material>(SituationFolder+"/Mud soil transition.mat");var path=CliffYardFolder+"/Garage weathered gravel.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(template);AssetDatabase.CreateAsset(mat,path);}mat.CopyPropertiesFromMaterial(template);mat.SetFloat("_CoverEnabled",1);mat.SetTexture("_CoverMap",terrain.terrainData.terrainLayers[1].diffuseTexture);mat.SetColor("_CoverTint",new Color(.72f,.69f,.59f));mat.SetVector("_CoverTiling",Vector4.one);EditorUtility.SetDirty(mat);
            var root=new GameObject("Gravel service apron",typeof(MeshFilter),typeof(MeshRenderer));root.transform.SetParent(parent,false);const int nx=40,nz=32;var vs=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var ts=new List<int>();
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {
                float u=x/(float)nx,v=z/(float)nz;var p=Ground(parent.TransformPoint(new Vector3((u*2-1)*15,0,(v*2-1)*10)));p.y+=.028f;
                float edge=Mathf.Min(Mathf.Min(u,1-u)*30,Mathf.Min(v,1-v)*20);float alpha=Mathf.SmoothStep(0,1,edge/1.8f)*(.78f+.15f*Mathf.PerlinNoise(p.x*.3f,p.z*.3f));if(PolishRoadNear(p,0))alpha=0;vs.Add(root.transform.InverseTransformPoint(p));uv.Add(new Vector2(p.x,p.z)*.2f);colors.Add(new Color(1,1,1,alpha));
                if(x<nx&&z<nz){int a=z*(nx+1)+x;ts.AddRange(new[]{a,a+nx+1,a+1,a+1,a+nx+1,a+nx+2});}
            }
            var entrance=parent.TransformPoint(new Vector3(2.5f,0,10));var start=polishRoads.Values.SelectMany(v=>v).OrderBy(p=>Flat(p-entrance).sqrMagnitude).First();var end=parent.TransformPoint(new Vector3(2.5f,0,4));int offset=vs.Count;const int rows=32,columns=7;
            for(int row=0;row<=rows;row++)for(int col=0;col<columns;col++)
            {
                float t=row/(float)rows,u=col/(float)(columns-1);var center=Vector3.Lerp(start,end,t);var side=Vector3.Cross(Vector3.up,Flat(end-start).normalized);var p=Ground(center+side*((u*2-1)*2));p.y+=.032f;
                float alpha=(1-Mathf.SmoothStep(.45f,1,Mathf.Abs(u*2-1)))*.85f;if(PolishRoadNear(p,0))alpha=0;vs.Add(root.transform.InverseTransformPoint(p));uv.Add(new Vector2(p.x,p.z)*.2f);colors.Add(new Color(1,1,1,alpha));if(row<rows&&col<columns-1){int a=offset+row*columns+col;ts.AddRange(new[]{a,a+columns,a+1,a+1,a+columns,a+columns+1});}
            }
            var mesh=new Mesh{name="Garage service apron"};mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();mesh.RecalculateBounds();root.GetComponent<MeshFilter>().sharedMesh=VisualAsset(mesh,"CliffsAndGarage/Garage service apron.asset");root.GetComponent<MeshRenderer>().sharedMaterial=mat;root.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
    }
}

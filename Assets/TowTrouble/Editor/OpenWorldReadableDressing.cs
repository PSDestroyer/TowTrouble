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
        private const string ReadableFolder="Assets/TowTrouble/OpenWorld/Visuals/ReadableRecovery";
        private const string DressingRecipe="Assets/TowTrouble/Editor/RecoveryDressingSettings.asset";
        private static RecoveryDressingSettings DressingSettings()
        {
            var settings=AssetDatabase.LoadAssetAtPath<RecoveryDressingSettings>(DressingRecipe);
            if(settings==null){settings=ScriptableObject.CreateInstance<RecoveryDressingSettings>();AssetDatabase.CreateAsset(settings,DressingRecipe);}return settings;
        }
        [MenuItem("Tow Trouble/Recovery/Readable Wheel Blockages and Layered Map Details")]
        public static void DressReadableRecoveriesAndMap()
        {
            VisualContext();ScenicPaths();Directory.CreateDirectory(ReadableFolder);AssetDatabase.Refresh();
            DressReadableWheelBlockages();DressLayeredMapDetails();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        private static void DressReadableWheelBlockages()
        {
            if(environment.Find("Broad recovery ground zones")!=null){var narrow=environment.Find("Readable recovery wheel pockets");if(narrow!=null)narrow.gameObject.SetActive(false);return;}
            var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();var settings=DressingSettings();
            var old=environment.Find("Readable recovery wheel pockets");if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
            var parent=ScenicGroup("Readable recovery wheel pockets");Undo.RegisterCreatedObjectUndo(parent.gameObject,"Readable wheel blockages");
            foreach(var site in world.Map.Sites.Skip(1))
            {
                if(site.Kind==RecoverySituationKind.Water||site.Kind==RecoverySituationKind.EngineFailure||site.Kind==RecoverySituationKind.Rocks)continue;
                var root=new GameObject(site.Id+" - wheel shoulders").transform;root.SetParent(parent,false);root.SetPositionAndRotation(site.Position,Quaternion.LookRotation(site.ExitDirection));
                int layer=RecoveryGroundLayer(site);var native=terrain.terrainData.terrainLayers[layer];
                string path=ReadableFolder+"/Cut ground "+layer+(site.Kind==RecoverySituationKind.Mud?" wet":"")+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
                var tint=native.diffuseRemapMax;
                bool wet=site.Kind==RecoverySituationKind.Mud;var mudMaterial=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/County wet silt.mat");
                material.SetTexture("_BaseMap",wet?mudMaterial.GetTexture("_Albedo"):native.diffuseTexture);
                material.SetColor("_BaseColor",wet?new Color(.72f,.57f,.4f):new Color(tint.x,tint.y,tint.z).gamma);
                material.SetTextureScale("_BaseMap",wet?Vector2.one:new Vector2(1/(.3f*native.tileSize.x),1/(.3f*native.tileSize.y)));
                material.SetTextureOffset("_BaseMap",wet?Vector2.zero:new Vector2(500/native.tileSize.x,500/native.tileSize.y));material.SetFloat("_Smoothness",0);material.enableInstancing=true;EditorUtility.SetDirty(material);
                var vs=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
                bool oneSide=site.Kind==RecoverySituationKind.Ditch||site.Kind==RecoverySituationKind.Embankment;
                float height=settings.WheelBankHeight*(oneSide?1.2f:site.Kind==RecoverySituationKind.Mud?1.5f:site.Kind==RecoverySituationKind.Snow?1.15f:1);
                // Raised soil sits beside, never on top of, the wheel contact line. The existing terrain remains the physical trench.
                foreach(float lane in oneSide?new[]{-.81f}:new[]{-.81f,.81f})
                {
                    foreach(float side in new[]{-1f,1f})AddWheelShoulder(root,lane+side*.48f,-2.5f,2.9f,.38f,height,vs,uv,triangles,false);
                    AddWheelShoulder(root,lane,2.1f,2.75f,.82f,height*.7f,vs,uv,triangles,true);
                }
                var mesh=new Mesh{name=site.Id+" wheel pocket shoulders"};mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var filter=root.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=VisualAsset(mesh,"ReadableRecovery/"+mesh.name+".asset");var renderer=root.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;
                // A short, dark bottom makes the wheel's depth readable without returning to long black rails.
                var bed=SituationMaterial("Wheel pocket shade "+layer+(wet?" wet":""),layer==4?new Color(.68f,.76f,.83f,.5f):site.Kind==RecoverySituationKind.Mud?new Color(.42f,.32f,.22f,.68f):new Color(.78f,.69f,.52f,.40f),layer);
                foreach(float lane in oneSide?new[]{-.81f}:new[]{-.81f,.81f})SituationStrip(root,lane<0?"Caught left wheel bed":"Caught right wheel bed",lane,.43f,-2.4f,2.6f,bed);
                ConfigureDetailLod(root.gameObject,170);
            }
        }
        private static void AddWheelShoulder(Transform root,float across,float from,float to,float width,float height,List<Vector3> vs,List<Vector2> uv,List<int> triangles,bool lip)
        {
            const int rows=23,columns=9;int offset=vs.Count;
            for(int r=0;r<rows;r++)for(int c=0;c<columns;c++)
            {
                float t=r/(float)(rows-1),u=c/(float)(columns-1);float waviness=1+.10f*Mathf.Sin(t*18+across*9);
                var p=Ground(root.TransformPoint(new Vector3(across+(u*2-1)*width*waviness,0,Mathf.Lerp(from,to,t))));
                float crown=Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*Mathf.PI)),.65f)*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),lip?.8f:.7f);
                p.y+=crown*height*(.78f+.22f*Mathf.Cos(t*27+across*3))-.035f;
                vs.Add(root.InverseTransformPoint(p));uv.Add(new Vector2(p.x,p.z)*.3f);
                if(r<rows-1&&c<columns-1){int a=offset+r*columns+c;triangles.AddRange(new[]{a,a+columns,a+1,a+1,a+columns,a+columns+1});}
            }
        }
        private static void ConfigureDetailLod(GameObject go,float distance)
        {
            var groups=go.GetComponentsInChildren<LODGroup>();
            if(groups.Length==0){var group=go.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.01f,go.GetComponentsInChildren<Renderer>())});groups=new[]{group};}
            foreach(var group in groups)
            {
                group.RecalculateBounds();var lods=group.GetLODs();float cutoff=Mathf.Clamp(group.size/(distance*1.1547f),.001f,.12f);
                for(int i=0;i<lods.Length;i++)lods[i].screenRelativeTransitionHeight=cutoff*(1+(lods.Length-1-i)*3);
                group.SetLODs(lods);group.fadeMode=LODFadeMode.None;
            }
        }
        private static void DressLayeredMapDetails()
        {
            var settings=DressingSettings();var map=UnityEngine.Object.FindFirstObjectByType<RecoveryMap>();
            var old=environment.Find("Layered county details");if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
            var root=ScenicGroup("Layered county details");Undo.RegisterCreatedObjectUndo(root.gameObject,"Layered county details");
            var random=new System.Random(settings.Seed);Func<float> next=()=> (float)random.NextDouble();var cells=new Dictionary<Vector2Int,Transform>();int count=0,clusters=0;
            var centers=new List<Vector3>();
            if(settings.RoadsideClusters)foreach(var road in scenicRoads)
                for(int i=15;i<road.Points.Length-4;i+=Mathf.Max(15,Mathf.RoundToInt(34/settings.Density)))
                {
                    var direction=Flat(road.Points[i+1]-road.Points[i-1]).normalized;var side=Vector3.Cross(Vector3.up,direction)*(next()<.5f?-1:1);
                    centers.Add(road.Points[i]+side*(road.Width+5+next()*6));
                }
            if(settings.LandscapeClusters)for(float z=-420;z<430;z+=30)for(float x=-420;x<430;x+=30)
                if(next()<.75f*settings.Density)centers.Add(new Vector3(x+(next()-.5f)*24,0,z+(next()-.5f)*24));
            // Mix roadsides and all regions before applying the shared budget, so the
            // first roads/rows do not consume every instance while distant areas stay empty.
            for(int i=centers.Count-1;i>0;i--){int j=random.Next(i+1);var value=centers[i];centers[i]=centers[j];centers[j]=value;}
            foreach(var center in centers)
            {
                if(count>=settings.MaximumProps-(settings.StoryClusters?72:0))break;var anchor=Ground(center);if(!DetailPositionAllowed(anchor,map,2.5f))continue;
                bool snow=anchor.y>128,desert=DesertAt(anchor),shore=anchor.y<15||anchor.z< -330;
                int number=4+random.Next(4);bool placed=false;
                for(int j=0;j<number&&count<settings.MaximumProps-(settings.StoryClusters?72:0);j++)
                {
                    float angle=next()*Mathf.PI*2,radius=Mathf.Sqrt(next())*6.5f;var p=Ground(anchor+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));
                    if(!DetailPositionAllowed(p,map,1))continue;
                    bool accent=j==0;bool stone=j==1||j==4;string prefab=stone?(desert?"P_RockDesert_Clump03":"P_RockClumpClassic2"):
                        snow?(j%2==0?"P_BushSnowDead02":"P_Bush2_Snowy"):desert?(j%2==0?"P_DesertBush01":"P_GrassDesert1_Paintable"):shore?(j%2==0?"P_Marshtail03":"P_GrassDesert1_Paintable"):(j%3==0?"P_Bush1_Light":j%3==1?"P_Fern1":"P_Grass2");
                    if(accent)
                    {
                        if(!DetailPositionAllowed(p,map,4))continue;
                        prefab=snow?(next()<.5f?"P_PineFallen1_SnowCover":"P_RockClumpClassic2"):
                            desert?(next()<.5f?"P_Cactus03":"P_RockDesert_Clump04"):
                            shore?(next()<.5f?"P_DesertTwigRoots02":"P_RockClumpClassic2"):
                            next()<.30f?"P_BirchSapling1":next()<.5f?"P_FirFallen1":"P_Bush1_Light";
                    }
                    var key=new Vector2Int(Mathf.FloorToInt(p.x/64),Mathf.FloorToInt(p.z/64));
                    if(!cells.TryGetValue(key,out var cell)){cell=new GameObject("Cell "+key.x+", "+key.y).transform;cell.SetParent(root,false);cells[key]=cell;}
                    string category=accent?"Landscape accents":stone?"Small stones":"Understory";var parent=cell.Find(category);if(parent==null){parent=new GameObject(category).transform;parent.SetParent(cell,false);}
                    float height=accent?(prefab.Contains("Sapling")?2.8f+next()*1.5f:prefab.Contains("Fallen")?.55f+next()*.3f:prefab.Contains("Cactus")?1.8f+next()*1.2f:1+next()*1.1f):stone?.25f+next()*.4f:.45f+next()*.65f;
                    var go=ScenicPrefab(prefab,p,height,next()*360,parent,false);
                    if(go==null)throw new InvalidOperationException("Missing detail prefab "+prefab);
                    foreach(var renderer in go.GetComponentsInChildren<Renderer>()){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;}
                    ConfigureDetailLod(go,accent||stone?settings.StoneDistance:settings.FoliageDistance);
                    count++;placed=true;
                }
                if(placed)clusters++;
            }
            if(settings.StoryClusters)foreach(var site in map.Sites.Skip(1).Where((s,i)=>i%2==0))
            {
                var side=Vector3.Cross(Vector3.up,site.ExitDirection);var center=Ground(site.Position+side*16-site.ExitDirection*9);
                if(!DetailPositionAllowed(center,map,3)){center=Ground(site.Position-side*16-site.ExitDirection*9);if(!DetailPositionAllowed(center,map,3))continue;}
                var key=new Vector2Int(Mathf.FloorToInt(center.x/64),Mathf.FloorToInt(center.z/64));
                if(!cells.TryGetValue(key,out var cell)){cell=new GameObject("Cell "+key.x+", "+key.y).transform;cell.SetParent(root,false);cells[key]=cell;}
                var parent=new GameObject(site.Id+" - landscape vignette").transform;parent.SetParent(cell,false);
                for(int j=0;j<3&&count<settings.MaximumProps;j++)
                {
                    var p=Ground(center+side*(j==0?0:j==1?1.8f:-1.3f)+site.ExitDirection*(j==2?1.6f:-.8f));if(!DetailPositionAllowed(p,map,1.5f))continue;
                    string prefab=site.Biome==3?(j==0?"P_OakFallen1_SnowCover":"P_SnowDrift03"):site.Biome==5?(j==0?"P_DesertTwigRoots02":"P_RockDesert_Clump03"):site.Biome==4?(j==0?"P_PalmFallen01":"P_GrassDesert1_Paintable"):(j==0?"P_FirFallen1":j==1?"P_FirStump1":"P_Fern1");
                    var go=ScenicPrefab(prefab,p,j==0?.48f:.4f+next()*.3f,site.Heading+25+j*63,parent,false);if(go==null)throw new InvalidOperationException("Missing vignette prefab "+prefab);
                    foreach(var renderer in go.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=ShadowCastingMode.Off;
                    ConfigureDetailLod(go,settings.StoneDistance);count++;
                }
                clusters++;
            }
            Directory.CreateDirectory("Docs/Validation/ReadableRecoveries");
            File.WriteAllText("Docs/Validation/ReadableRecoveries/DecorBudget.json",JsonUtility.ToJson(new DetailBudget{Props=count,Clusters=clusters,Cells=cells.Count,Seed=settings.Seed,MaxProps=settings.MaximumProps},true));
        }
        [Serializable] private sealed class DetailBudget{public int Props,Clusters,Cells,Seed,MaxProps;}
        private static bool DetailPositionAllowed(Vector3 p,RecoveryMap map,float margin)
        {
            if(p.y<10.8f||Mathf.Abs(p.x)>455||Mathf.Abs(p.z)>455)return false;
            if(Flat(p-map.Sites[0].Position).magnitude<35||Flat(p-nodes["g"].Point).magnitude<45)return false;
            if(map.Deliveries.Any(d=>Flat(d.Position-p).magnitude<19))return false;
            foreach(var site in map.Sites)
            {
                var d=Flat(p-site.Position);if(d.magnitude<8+margin)return false;
                // Keep the full soft-ground area clear, not just the centre of the car.
                if(!RoadsideRecovery(site)&&site.Id!="R01"&&site.Kind!=RecoverySituationKind.Water&&site.Kind!=RecoverySituationKind.EngineFailure&&site.Kind!=RecoverySituationKind.Rocks)
                {
                    float width=site.Biome==3?12:site.Biome>=4?15:14,length=site.Biome==3?16:site.Biome>=4?19:18;
                    float alongZone=Vector3.Dot(d,site.ExitDirection)/(length+margin+2),acrossZone=Vector3.Dot(d,Vector3.Cross(Vector3.up,site.ExitDirection))/(width+margin+2);
                    if(alongZone*alongZone+acrossZone*acrossZone<1.1f)return false;
                }
                float along=Vector3.Dot(d,site.ExitDirection),side=Mathf.Abs(Vector3.Dot(d,Vector3.Cross(Vector3.up,site.ExitDirection)));
                if(along>0&&along<35&&side<4.5f+margin)return false;
            }
            float distance=ScenicDistance(p,out _,out var road);if(road!=null&&distance<road.Width+2.2f+margin)return false;
            if(RiverDistance(p)<9+margin)return false;
            var local=p-terrain.transform.position;return terrain.terrainData.GetSteepness(local.x/terrain.terrainData.size.x,local.z/terrain.terrainData.size.z)<32;
        }
    }
}

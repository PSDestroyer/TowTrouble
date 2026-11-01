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
        private const string SituationFolder="Assets/TowTrouble/OpenWorld/Visuals/RecoverySituations";
        private const string SituationEvidence="Docs/Validation/RecoverySituations";
        [Serializable] private sealed class SituationBaseline
        {public string TerrainGuid;public int Resolution;public SituationPatch[] Patches;}
        [Serializable] private sealed class SituationPatch
        {public string Before;public Vector3 Center;public int X,Z,Width,Height;public float[] Heights;}
        private static readonly RecoverySituationKind[] SituationKinds={
            RecoverySituationKind.Ditch,RecoverySituationKind.Mud,RecoverySituationKind.Mud,RecoverySituationKind.Ditch,
            RecoverySituationKind.Water,RecoverySituationKind.Mud,RecoverySituationKind.Embankment,RecoverySituationKind.Water,
            RecoverySituationKind.EngineFailure,RecoverySituationKind.Rocks,RecoverySituationKind.Embankment,RecoverySituationKind.Rocks,
            RecoverySituationKind.Snow,RecoverySituationKind.Ditch,RecoverySituationKind.Snow,RecoverySituationKind.EngineFailure,
            RecoverySituationKind.Sand,RecoverySituationKind.Ditch,RecoverySituationKind.Sand,RecoverySituationKind.Sand,
            RecoverySituationKind.DryWash,RecoverySituationKind.Sand,RecoverySituationKind.Rocks,RecoverySituationKind.EngineFailure};
        private static readonly string[] SituationRO={
            "Roata din față-stânga este prinsă într-un șanț. Trage progresiv spre potecă.",
            "Roțile au săpat două șleauri adânci în noroi. Eliberează mai întâi copacul, apoi trage drept din făgașe.",
            "Mașina stă cu burta pe o creastă de noroi; roțile sunt îngropate în mâl. Caută sprijin ferm pentru troliu.",
            "Roțile din stânga au căzut în șanțul pădurarului. Mută copacul și scoate mașina pe rampa din față.",
            "Mașina a intrat cu spatele în lac; roțile sunt în apă. Trage spre malul uscat din fața ei.",
            "Mâlul de lângă stuf a înghițit roțile din spate. Eliberează copacul și trage dinspre terenul tare.",
            "Mașina a alunecat pe muchia malului; roțile din stânga sunt mai jos. Trage spre rampa dintre bolovani.",
            "Mașina a rămas în apa golfului, cu partea de jos inundată. Folosește troliul de pe malul uscat.",
            "Motor defect; fumul iese de sub capotă. Mașina nu se poate deplasa singură: remorcheaz-o la atelier.",
            "O piatră plată a rămas sub șasiu. Aliniază cablul cu culoarul dintre stânci și trage lent.",
            "Mașina a alunecat cu o latură sub nivelul drumului. Scoate-o pe rampa de pământ, fără tragere laterală.",
            "Mașina este prinsă între doi bolovani, cu pietriș sub roți. Ieșirea liberă este în față.",
            "Un sul de zăpadă blochează puntea din față. Urmele roților arată direcția de ieșire.",
            "Roțile au căzut într-un șanț înghețat. Îndepărtează copacul și ține cablul aliniat cu șanțul.",
            "Viscolul a strâns zăpadă sub șasiu și în jurul roților. Trage progresiv pe culoarul tasat.",
            "Motor oprit pe platou; aburul de sub capotă indică defecțiunea. Remorcheaz-o spre zona sigură.",
            "Roțile din spate au săpat două gropi în nisip. Trage dinspre terenul compact, fără smucituri.",
            "Mașina a căzut cu o latură în șanțul falezei. Eliberează trunchiul și folosește rampa din față.",
            "Nisipul umed s-a adunat sub punte; roțile au lăsat făgașe lucioase. Poziționează-te pe nisip tare.",
            "Mașina stă cu burta pe creasta dunei. Scoate-o pe rampa lată din față.",
            "Mașina a alunecat în albia uscată; marginile vadului sunt deasupra roților. Ieși pe rampa din față.",
            "Puntea din spate este îngropată în nisip. Urmele de patinare și nisipul ridicat arată blocajul.",
            "Pietrele căzute au prins mașina într-un culoar îngust. Trage drept prin deschiderea din față.",
            "Motor supraîncălzit; fum vizibil de sub capotă. Mașina este pe teren liber și trebuie remorcată."};
        private static readonly string[] SituationEN={
            "The front-left wheel is caught in a rut. Pull progressively toward the trail.",
            "The wheels dug two deep muddy ruts. Clear the tree first, then pull straight out of the tracks.",
            "The car is bellied on a mud ridge, with its wheels sunk in silt. Find firm footing for the winch.",
            "The left wheels fell into the forest ditch. Move the tree and recover along the forward ramp.",
            "The car reversed into the lake; its wheels are underwater. Pull toward the dry bank ahead.",
            "Silt beside the reeds swallowed the rear wheels. Clear the tree and pull from firm ground.",
            "The car slid onto the bank edge, with its left wheels lower. Pull up the ramp between the rocks.",
            "The car is stranded in the cove with its lower body flooded. Winch it from the dry bank.",
            "Engine failure; smoke rises from under the hood. The car cannot drive itself: tow it to the workshop.",
            "A flat rock is caught beneath the chassis. Align with the gap between the rocks and pull slowly.",
            "One side slid below the road. Recover along the dirt ramp without pulling sideways.",
            "The car is wedged between boulders with loose gravel under its tires. The open exit is ahead.",
            "A snow bank blocks the front axle. The tire tracks show the escape direction.",
            "The wheels slid into an icy ditch. Clear the fallen tree and keep the cable aligned with the ditch.",
            "The blizzard packed snow under the chassis and around the wheels. Pull steadily along the packed tracks.",
            "Engine failure on the plateau; vapor under the hood marks the fault. Tow it to the safe zone.",
            "The rear wheels dug two pits in the sand. Pull from compact ground without jerking the cable.",
            "One side fell into the beach ditch. Clear the fallen trunk and use the forward ramp.",
            "Wet sand piled beneath the axle; the wheels left glossy ruts. Position on firmer sand.",
            "The car is bellied on the dune crest. Pull down the broad forward ramp.",
            "The car slid into a dry wash; its banks rise above the wheels. Use the forward escape ramp.",
            "The rear axle is buried in sand. Wheelspin marks and piled sand reveal the blockage.",
            "Fallen rocks trapped the car in a narrow passage. Pull straight through the opening ahead.",
            "Overheated engine; smoke rises from under the hood. The car is on clear ground and needs towing."};

        [MenuItem("Tow Trouble/Recovery/Dress Distinct Recovery Situations")]
        public static void DressDistinctRecoverySituations()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode only.");
            VisualContext();Directory.CreateDirectory(SituationEvidence);Directory.CreateDirectory(SituationFolder);AssetDatabase.Refresh();
            var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();var map=world.Map;
            if(map.Sites.Length!=24)throw new InvalidOperationException("Expected the authored 24-site map.");
            var data=terrain.terrainData;int n=data.heightmapResolution;string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data));
            string baselinePath=SituationEvidence+"/TerrainPatchesBaseline.json";SituationBaseline baseline;
            if(File.Exists(baselinePath))
            {baseline=JsonUtility.FromJson<SituationBaseline>(File.ReadAllText(baselinePath));if(baseline.TerrainGuid!=guid||baseline.Resolution!=n)throw new InvalidOperationException("Review terrain baseline before re-authoring.");}
            else
            {
                var patches=new List<SituationPatch>();
                for(int i=0;i<24;i++)
                {
                    var site=map.Sites[i];var center=(i==4||i==7)?LakeRecoveryCenter(site.Position):site.Position;
                    int rx=Mathf.CeilToInt(21/data.size.x*(n-1));var local=center-terrain.transform.position;
                    int x=Mathf.Clamp(Mathf.RoundToInt(local.x/data.size.x*(n-1))-rx,0,n-1),z=Mathf.Clamp(Mathf.RoundToInt(local.z/data.size.z*(n-1))-rx,0,n-1);
                    int width=Mathf.Min(2*rx+1,n-x),height=Mathf.Min(2*rx+1,n-z);var h=data.GetHeights(x,z,width,height);
                    var patch=new SituationPatch{Before=JsonUtility.ToJson(site),Center=center,X=x,Z=z,Width=width,Height=height,Heights=new float[width*height]};
                    for(int dz=0;dz<height;dz++)for(int dx=0;dx<width;dx++)patch.Heights[dz*width+dx]=h[dz,dx];patches.Add(patch);
                }
                baseline=new SituationBaseline{TerrainGuid=guid,Resolution=n,Patches=patches.ToArray()};File.WriteAllText(baselinePath,JsonUtility.ToJson(baseline,true));
            }
            Undo.RecordObject(map,"Distinct recovery situations");Undo.RecordObject(world,"Bind stranded engine smoke");Undo.RegisterCompleteObjectUndo(data,"Local recovery terrain");
            var heights=data.GetHeights(0,0,n,n);var checkpoints=ReadSituationCheckpoints(map);
            for(int i=1;i<24;i++)
            {
                var p=baseline.Patches[i];for(int dz=0;dz<p.Height;dz++)for(int dx=0;dx<p.Width;dx++)heights[p.Z+dz,p.X+dx]=p.Heights[dz*p.Width+dx];
            }
            // Restore only owned local patches. R01 and the rest of the world are untouched.
            for(int i=1;i<24;i++)
            {
                var original=JsonUtility.FromJson<RecoverySite>(baseline.Patches[i].Before);var site=map.Sites[i];var center=baseline.Patches[i].Center;
                site.Position=center;site.Kind=SituationKinds[i];site.ExitDirection=(i==4||i==7)?Flat(original.Position-center).normalized:original.ExitDirection;
                site.Heading=Quaternion.LookRotation(site.ExitDirection).eulerAngles.y;site.Situation=SituationRO[i];site.EnglishSituation=SituationEN[i];
                site.Resistance=site.Kind==RecoverySituationKind.EngineFailure?0:original.Resistance;
                ShapeSituationPocket(heights,baseline.Patches[i],site,original,i,checkpoints);
            }
            map.Sites[0].Kind=RecoverySituationKind.Ditch;
            data.SetHeights(0,0,heights);EditorUtility.SetDirty(data);Physics.SyncTransforms();
            var old=environment.Find("Distinct recovery situations");if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
            var root=new GameObject("Distinct recovery situations").transform;root.SetParent(environment,false);Undo.RegisterCreatedObjectUndo(root.gameObject,"Recovery situations");
            HideGenericScenarioDressing();
            for(int i=1;i<24;i++)
            {
                var site=map.Sites[i];FitSituationCar(site,world.Casualty);
                var group=new GameObject(site.Id+" - "+site.Kind).transform;group.SetParent(root,false);group.SetPositionAndRotation(site.Position,Quaternion.LookRotation(site.ExitDirection));
                DressSituation(group,site,i);
            }
            FitDistinctMud();ConfigureDistressSmoke(world);
            map.Configure(map.Sites,map.Deliveries,checkpoints);EditorUtility.SetDirty(map);EditorUtility.SetDirty(world);
            ReconcileRecoveryLocations();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());Physics.SyncTransforms();
            File.WriteAllText(SituationEvidence+"/Locations.json",Newtonsoft.Json.JsonConvert.SerializeObject(map.Sites.Select(s=>new{s.Id,s.Kind,s.Situation,s.EnglishSituation,position=new[]{s.Position.x,s.Position.y,s.Position.z},s.Heading,s.Depth,s.Pitch,s.Roll,s.Resistance,exit=new[]{s.ExitDirection.x,s.ExitDirection.y,s.ExitDirection.z}}),Newtonsoft.Json.Formatting.Indented));
        }
        private static Vector3[] ReadSituationCheckpoints(RecoveryMap map)
        {var roads=new SerializedObject(map).FindProperty("roadCheckpoints");var result=new Vector3[roads.arraySize];for(int i=0;i<result.Length;i++)result[i]=roads.GetArrayElementAtIndex(i).vector3Value;return result;}
        private static Vector3 LakeRecoveryCenter(Vector3 source)
        {
            var lakeFilter=environment.GetComponentsInChildren<MeshFilter>().First(f=>f.name.StartsWith("Lake -"));var polygon=lakeFilter.sharedMesh.vertices.Select(lakeFilter.transform.TransformPoint).ToArray();
            var center=polygon.Aggregate(Vector3.zero,(a,b)=>a+b)/polygon.Length;var nearest=source;float best=float.MaxValue;
            for(int i=0;i<polygon.Length;i++){var a=polygon[i];var b=polygon[(i+1)%polygon.Length];var ab=Flat(b-a);var p=a+ab*Mathf.Clamp01(Vector3.Dot(Flat(source-a),ab)/Mathf.Max(.001f,ab.sqrMagnitude));float d=Flat(source-p).sqrMagnitude;if(d<best){best=d;nearest=p;}}
            return nearest+Flat(center-nearest).normalized*3.5f;
        }
        private static void ShapeSituationPocket(float[,] heights,SituationPatch patch,RecoverySite site,RecoverySite original,int index,Vector3[] checkpoints)
        {
            var data=terrain.terrainData;var origin=terrain.transform.position;int n=data.heightmapResolution;var forward=site.ExitDirection;var side=Vector3.Cross(Vector3.up,forward);
            for(int dz=0;dz<patch.Height;dz++)for(int dx=0;dx<patch.Width;dx++)
            {
                int x=patch.X+dx,z=patch.Z+dz;var p=origin+new Vector3(x/(float)(n-1)*data.size.x,0,z/(float)(n-1)*data.size.z);var delta=p-site.Position;float a=Vector3.Dot(delta,forward),b=Vector3.Dot(delta,side);
                float ellipse=Mathf.Sqrt(b*b/(site.Kind==RecoverySituationKind.Water?100:30.25f)+a*a/(a>0?196:site.Kind==RecoverySituationKind.Water?400:49));float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,ellipse));if(blend<=0||checkpoints.Any(c=>Flat(c-p).sqrMagnitude<30.25f))continue;
                float floor=original.Position.y-.12f;float target=floor;
                float tracks=Mathf.Max(Mathf.Exp(-Mathf.Pow((b-.81f)/.45f,2)),Mathf.Exp(-Mathf.Pow((b+.81f)/.45f,2)));
                float escape=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,8,a));float back=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.4f,6,-a));
                switch(site.Kind)
                {
                    case RecoverySituationKind.Mud:target-=((index==2?.26f:.12f)+tracks*(index==2?.30f:.34f))*escape*back;break;
                    case RecoverySituationKind.Ditch:case RecoverySituationKind.Embankment:
                        target-=.7f*Mathf.Exp(-Mathf.Pow((b+.8f)/1.25f,2))*escape*back;break;
                    case RecoverySituationKind.Water:
                        target=(index==7?9.72f:9.92f)+Mathf.Max(0,a)*.18f;break;
                    case RecoverySituationKind.Snow:
                        target+=((Mathf.Abs(b)<.55f?.20f:0)-tracks*.35f)*escape*back;
                        target+=.5f*Mathf.Exp(-Mathf.Pow((a-2.4f)/.9f,2))*Mathf.Exp(-b*b/5);break;
                    case RecoverySituationKind.Sand:
                        target+=(index==19?.35f*Mathf.Exp(-a*a/9):.12f)*Mathf.Exp(-b*b/6)*escape*back-tracks*.30f*escape*back;break;
                    case RecoverySituationKind.DryWash:
                        target-=.55f*escape*back;target+=.35f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.2f,3.4f,Mathf.Abs(b)))*escape;break;
                    case RecoverySituationKind.Rocks:target-=tracks*.12f*escape*back;break;
                }
                heights[z,x]=Mathf.Lerp(heights[z,x],(target-origin.y)/data.size.y,blend);
            }
        }
        private static void FitSituationCar(RecoverySite site,RecoveryVehicle vehicle)
        {
            var model=vehicle.CurrentModel;
            var yaw=Quaternion.Euler(0,site.Heading,0);var ground=new float[4];for(int i=0;i<4;i++)ground[i]=Ground(site.Position+yaw*model.Anchors[i]).y;
            site.Pitch=-Mathf.Atan2((ground[0]+ground[1]-ground[2]-ground[3])*.5f,3.4f)*Mathf.Rad2Deg;
            site.Roll=Mathf.Atan2((ground[1]+ground[3]-ground[0]-ground[2])*.5f,1.62f)*Mathf.Rad2Deg;
            site.Pitch=Mathf.Clamp(site.Pitch,-24,24);site.Roll=Mathf.Clamp(site.Roll,-26,26);
            var rotation=Quaternion.Euler(site.Pitch,site.Heading,site.Roll);float rootY=0;
            for(int i=0;i<4;i++){var anchor=rotation*model.Anchors[i];rootY+=Ground(site.Position+anchor).y-anchor.y+model.Radius+.10f;}
            rootY*=.25f;
            // Start within the real suspension probes, including diagonal heightmap triangles.
            var wheelFields=new SerializedObject(vehicle).FindProperty("wheels");
            for(int attempt=0;attempt<10;attempt++)
            {
                int contacts=0;var root=new Vector3(site.Position.x,rootY,site.Position.z);var up=rotation*Vector3.up;
                for(int i=0;i<4;i++){var wheel=(WheelCollider)wheelFields.GetArrayElementAtIndex(i).objectReferenceValue;var anchor=root+rotation*model.Anchors[i];if(Physics.Raycast(anchor+up*RecoveryWheelPose.ProbeLift,-up,out _,wheel.radius+wheel.suspensionDistance+RecoveryWheelPose.ProbeLift,1<<8,QueryTriggerInteraction.Ignore))contacts++;}
                if(contacts==4)break;rootY-=.025f;
            }
            var p=site.Position;p.y=Ground(p).y+.12f;site.Position=p;site.Depth=p.y-rootY;
        }
        private static void HideGenericScenarioDressing()
        {
            var old=environment.Find("Recovery scenario dressing");if(old==null)return;
            foreach(Transform group in old)
            {if(group.name.StartsWith("R01 -"))continue;foreach(Transform child in group)if(child.GetComponent<RecoveryTowObstacle>()==null){Undo.RecordObject(child.gameObject,"Replace generic recovery decoration");child.gameObject.SetActive(false);}}
        }
        private static Material SituationMaterial(string name,Color tint,int layer)
        {
            string path=SituationFolder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("TowTrouble/Wheel Impressions"));if(material.shader==null)throw new InvalidOperationException("Missing wheel impressions shader");AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",tint);if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",terrain.terrainData.terrainLayers[layer].diffuseTexture);
            if(material.HasProperty("_Compaction"))material.SetFloat("_Compaction",.8f);EditorUtility.SetDirty(material);return material;
        }
        private static void DressSituation(Transform group,RecoverySite site,int index)
        {
            int layer=site.Biome==3?4:site.Biome==4?2:site.Biome==5?5:1;
            var tint=site.Biome==3?new Color(.71f,.8f,.86f,.96f):site.Biome>=4?new Color(.86f,.75f,.5f,.90f):new Color(.27f,.17f,.085f,.97f);
            var bed=SituationMaterial("Exposed "+site.Kind+" "+site.Biome,tint,layer);
            bool engine=site.Kind==RecoverySituationKind.EngineFailure;
            if(!engine&&site.Kind!=RecoverySituationKind.Water)
            {
                float left=site.Kind==RecoverySituationKind.Ditch||site.Kind==RecoverySituationKind.Embankment?-.8f:-.81f;
                SituationStrip(group,"Left wheel trench",left,.56f,-3.8f,7,bed);
                if(site.Kind!=RecoverySituationKind.Ditch&&site.Kind!=RecoverySituationKind.Embankment)SituationStrip(group,"Right wheel trench",.81f,.56f,-3.8f,7,bed);
                if(site.Kind==RecoverySituationKind.DryWash)SituationStrip(group,"Exposed wash bed",0,2.2f,-5,8,bed);
                if(site.Kind!=RecoverySituationKind.Rocks)
                {
                    SituationBank(group,site,-1.45f,layer);
                    if(site.Kind!=RecoverySituationKind.Ditch&&site.Kind!=RecoverySituationKind.Embankment)SituationBank(group,site,1.45f,layer);
                }
            }
            var exit=site.ExitDirection;var side=Vector3.Cross(Vector3.up,exit);
            if(site.Kind==RecoverySituationKind.Rocks)
            {
                string rock=site.Biome==5?"P_RockDesert_Rock01":"P_RockSpire_Rock01";
                for(int j=0;j<6;j++)PlaceScenarioProp(rock,site.Position+side*(j%2==0?-1:1)*(1.5f+(j/2)*.3f)+exit*(j/2-1)*1.8f,1.25f+(j%3)*.2f,index*41+j*67,group,true);
                if(index==9){var stone=PlaceScenarioProp(rock,site.Position+exit*.5f,1.1f,35,group,true);var bounds=ScenarioBounds(stone);var p=stone.transform.position;p.y+=site.Position.y-site.Depth+.39f-bounds.max.y;stone.transform.position=p;}
            }
            if(site.Kind==RecoverySituationKind.Ditch||site.Kind==RecoverySituationKind.Embankment||site.Kind==RecoverySituationKind.DryWash)
                for(int j=0;j<5;j++)PlaceScenarioProp(site.Biome==4?"P_PalmStump01":site.Biome==5?"P_RockDesert_Clump01":"P_FirStump1",site.Position+side*(-3.5f-j%2*.6f)+exit*(j-2)*1.8f,.35f+(j%3)*.15f,index*31+j*61,group,false);
            if(site.Kind==RecoverySituationKind.Water)
                for(int j=0;j<7;j++)PlaceScenarioProp("P_RockSpire_Rock01",site.Position+side*(j%2==0?-1:1)*(4+j%3*.5f)+exit*(j/2)*1.7f,.45f+j%3*.25f,j*49,group,false);
            if(engine)
            {
                var stain=SituationMaterial("Engine oil stain",new Color(.045f,.04f,.035f,.95f),1);SituationStrip(group,"Oil beneath engine",0,.48f,.2f,1.5f,stain);
                var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.name="Warning triangle support";marker.transform.SetParent(group);marker.transform.position=Ground(site.Position+exit*4)+Vector3.up*.08f;marker.transform.localScale=new Vector3(.6f,.12f,.2f);UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
                string path=SituationFolder+"/Warning orange.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",new Color(1,.25f,.035f));AssetDatabase.CreateAsset(mat,path);}marker.GetComponent<MeshRenderer>().sharedMaterial=mat;
                var lineObject=new GameObject("Roadside warning triangle");lineObject.transform.SetParent(group);var line=lineObject.AddComponent<LineRenderer>();line.sharedMaterial=mat;line.loop=true;line.positionCount=3;line.startWidth=line.endWidth=.065f;var p=marker.transform.position+Vector3.up*.1f;
                line.SetPositions(new[]{p-side*.32f,p+side*.32f,p+Vector3.up*.55f});
            }
        }
        private static void SituationStrip(Transform root,string name,float across,float halfWidth,float from,float to,Material material)
        {
            const int rows=33,columns=9;var vs=new List<Vector3>();var uv=new List<Vector2>();var rut=new List<Vector2>();var ends=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
            for(int r=0;r<rows;r++)for(int c=0;c<columns;c++)
            {
                float t=r/(float)(rows-1),u=c/(float)(columns-1);var p=Ground(root.TransformPoint(new Vector3(across+(u*2-1)*halfWidth*(1+.09f*Mathf.Sin(r*1.7f)),0,Mathf.Lerp(from,to,t))))+Vector3.up*(root.name.Contains("Mud")?.11f:.022f);
                vs.Add(root.InverseTransformPoint(p));uv.Add(new Vector2(p.x,p.z)*.35f);rut.Add(new Vector2(u,t*(to-from)+.65f));ends.Add(Vector2.one);colors.Add(new Color(1,1,1,Mathf.SmoothStep(0,1,Mathf.Min(t,1-t)*12)*Mathf.SmoothStep(0,1,Mathf.Min(u,1-u)*5)));
                if(r<rows-1&&c<columns-1){int a=r*columns+c;triangles.AddRange(new[]{a,a+columns,a+1,a+1,a+columns,a+columns+1});}
            }
            string path=SituationFolder+"/"+root.name.Split(' ')[0]+" "+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();mesh.name=Path.GetFileNameWithoutExtension(path);mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetUVs(1,rut);mesh.SetUVs(2,ends);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        private static void FitDistinctMud()
        {
            string backup="/Users/gheorghe/Desktop/TowTrublw/Backups/RecoverySituations-20261101/MudMeshes";Directory.CreateDirectory(backup);
            foreach(var filter in environment.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.EndsWith(" - Living Mud")))
            {
                if(filter.name.StartsWith("R05")||filter.name.StartsWith("R08")){Undo.RecordObject(filter.gameObject,"Move water recoveries into lake");filter.gameObject.SetActive(false);continue;}
                var mesh=filter.sharedMesh;string path=AssetDatabase.GetAssetPath(mesh);if(!File.Exists(backup+"/"+Path.GetFileName(path)))File.Copy(path,backup+"/"+Path.GetFileName(path));Undo.RecordObject(mesh,"Fit mud to visible recovery ruts");
                mesh.name=Path.GetFileNameWithoutExtension(path);var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++){var p=Ground(filter.transform.TransformPoint(vertices[i]))+Vector3.up*.07f;vertices[i]=filter.transform.InverseTransformPoint(p);}mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();var collider=filter.GetComponent<MeshCollider>();collider.sharedMesh=null;collider.sharedMesh=mesh;EditorUtility.SetDirty(mesh);
            }
        }
        private static void ConfigureDistressSmoke(RecoveryWorld world)
        {
            var root=world.Casualty.transform.Find("Engine failure smoke");
            if(root==null){root=new GameObject("Engine failure smoke").transform;root.SetParent(world.Casualty.transform,false);Undo.RegisterCreatedObjectUndo(root.gameObject,"Stranded engine smoke");}
            root.localPosition=new Vector3(0,1.2f,1.05f);root.localRotation=Quaternion.Euler(-90,0,0);
            var ps=root.GetComponent<ParticleSystem>();if(ps==null)ps=Undo.AddComponent<ParticleSystem>(root.gameObject);ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.duration=2;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=48;main.startLifetime=new ParticleSystem.MinMaxCurve(3,4.5f);main.startSize=new ParticleSystem.MinMaxCurve(.7f,1f);main.startSpeed=new ParticleSystem.MinMaxCurve(.8f,1.2f);main.startColor=new Color(.22f,.24f,.25f,.5f);main.gravityModifier=-.015f;
            var emission=ps.emission;emission.rateOverTime=0;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=14;shape.radius=.22f;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,2.8f));
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.65f,.68f,.7f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});color.color=gradient;
            string path=SituationFolder+"/Engine failure smoke.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("TowTrouble/Distress Smoke"));AssetDatabase.CreateAsset(material,path);}material.shader=Shader.Find("TowTrouble/Distress Smoke");material.SetColor("_BaseColor",Color.white);material.SetFloat("_UseParticleColor",1);material.renderQueue=3100;EditorUtility.SetDirty(material);
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var distress=world.Casualty.GetComponent<RecoveryDistressVisuals>();if(distress==null)distress=Undo.AddComponent<RecoveryDistressVisuals>(world.Casualty.gameObject);distress.Configure(ps);world.ConfigureDistressVisuals(distress);EditorUtility.SetDirty(distress);EditorUtility.SetDirty(ps);
        }

        private static void PoseSituationPhoto(GameObject car,RecoveryVehicle source,RecoverySite site)
        {
            var model=source.CurrentModel;var serialized=new SerializedObject(source).FindProperty("wheels");
            for(int i=0;i<4;i++)
            {
                var wheel=(WheelCollider)serialized.GetArrayElementAtIndex(i).objectReferenceValue;
                var anchor=car.transform.position+car.transform.rotation*model.Anchors[i];var up=car.transform.up;
                var path=AnimationUtility.CalculateTransformPath(model.Wheels[i],model.Root.transform);var visual=car.transform.Find(path);
                if(visual!=null&&Physics.Raycast(anchor+up*RecoveryWheelPose.ProbeLift,-up,out var hit,wheel.radius+wheel.suspensionDistance+RecoveryWheelPose.ProbeLift,1<<8,QueryTriggerInteraction.Ignore))
                    visual.position=RecoveryWheelPose.Position(anchor,up,RecoveryWheelPose.SuspensionOffset(hit.distance,wheel.radius,wheel.suspensionDistance));
            }
        }
        private static void PoseSituationSmoke(GameObject car,RecoverySite site,Camera camera)
        {
            foreach(Transform child in camera.transform.Cast<Transform>().ToArray())if(child.name=="Static posed engine smoke")UnityEngine.Object.DestroyImmediate(child.gameObject);
            if(site.Kind!=RecoverySituationKind.EngineFailure)return;
            string materialPath=SituationFolder+"/Static engine smoke.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("TowTrouble/Distress Smoke"));AssetDatabase.CreateAsset(material,materialPath);}
            material.SetColor("_BaseColor",new Color(.18f,.2f,.21f,.78f));material.SetFloat("_UseParticleColor",0);material.renderQueue=3100;EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
            for(int i=0;i<7;i++)
            {
                var puff=GameObject.CreatePrimitive(PrimitiveType.Quad);puff.name="Static posed engine smoke";UnityEngine.Object.DestroyImmediate(puff.GetComponent<Collider>());puff.transform.SetParent(camera.transform);
                puff.transform.position=car.transform.position+car.transform.rotation*new Vector3(Mathf.Sin(i*2.3f)*.14f,1.5f+i*.55f,1.05f)+Vector3.right*i*.035f;
                puff.transform.rotation=Quaternion.LookRotation(puff.transform.position-camera.transform.position);puff.transform.localScale=Vector3.one*(1.1f+i*.14f);
                var renderer=puff.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;

            }
        }

        private static void SituationBank(Transform root,RecoverySite site,float across,int layer)
        {
            string materialPath=SituationFolder+"/Raised bank "+site.Biome+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("TowTrouble/Wheel Impressions"));AssetDatabase.CreateAsset(material,materialPath);}
            material.shader=Shader.Find("TowTrouble/Wheel Impressions");material.SetColor("_BaseColor",site.Biome==3?new Color(.95f,.98f,1,.95f):site.Biome>=4?new Color(.95f,.89f,.72f,.9f):new Color(.78f,.57f,.34f,.9f));material.SetTexture("_BaseMap",terrain.terrainData.terrainLayers[layer].diffuseTexture);material.SetFloat("_Compaction",.94f);EditorUtility.SetDirty(material);
            var vs=new List<Vector3>();var uv=new List<Vector2>();var rut=new List<Vector2>();var ends=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();const int rows=25,cols=9;
            for(int r=0;r<rows;r++)for(int c=0;c<cols;c++)
            {
                float t=r/(float)(rows-1),u=c/(float)(cols-1);float along=Mathf.Lerp(-3.4f,4.2f,t);float width=.42f+.06f*Mathf.Sin(t*21);
                var point=Ground(root.TransformPoint(new Vector3(across+(u*2-1)*width,0,along)));
                float height=Mathf.Sin(u*Mathf.PI)*Mathf.Sin(t*Mathf.PI)*(.21f+.04f*Mathf.Sin(r*1.7f));
                point.y+=height+.025f;vs.Add(root.InverseTransformPoint(point));uv.Add(new Vector2(point.x,point.z)*.3f);rut.Add(new Vector2(u,t*7.6f+.65f));ends.Add(Vector2.one);colors.Add(new Color(1,1,1,Mathf.SmoothStep(0,1,Mathf.Min(t,1-t)*10)*Mathf.Sin(u*Mathf.PI)));
                if(r<rows-1&&c<cols-1){int a=r*cols+c;triangles.AddRange(new[]{a,a+cols,a+1,a+1,a+cols,a+cols+1});}
            }
            string name=across<0?"Left displaced soil bank":"Right displaced soil bank";string path=SituationFolder+"/"+root.name.Split(' ')[0]+" "+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetUVs(1,rut);mesh.SetUVs(2,ends);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
    }
}

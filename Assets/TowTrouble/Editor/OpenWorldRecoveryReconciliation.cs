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
        [MenuItem("Tow Trouble/Recovery/Reconcile Existing Recovery Locations")]
        public static void ReconcileRecoveryLocations()
        {
            VisualContext();
            var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
            var map=world.Map;
            RepairNaturalLakeRecoveries(world);
            DressRoadsideRecoveries(world);
            
            var dressing=environment.Find("Distinct recovery situations");
            if(dressing==null)throw new InvalidOperationException("Author the recovery situations first.");
            Undo.RecordObject(map,"Reconcile recovery locations");
            var surrounds=environment.Find("Recovery situations - 24 authored surrounds");
            var signs=environment.Find("24 recoveries and 8 delivery bays");
            for(int i=1;i<map.Sites.Length;i++)
            {
                var site=map.Sites[i];var forward=site.ExitDirection;var side=Vector3.Cross(Vector3.up,forward);
                var group=dressing.Cast<Transform>().Single(t=>t.name.StartsWith(site.Id+" -"));
                // Keep the authored landscape, but remove the synthetic row of tiny stumps.
                foreach(var prop in group.Cast<Transform>().Where(t=>t.name.StartsWith("P_")&&site.Kind!=RecoverySituationKind.Rocks&&site.Kind!=RecoverySituationKind.Water).ToArray())
                    Undo.DestroyObjectImmediate(prop.gameObject);
                int layer=RecoveryGroundLayer(site);
                Color tint=layer==4?new Color(.83f,.9f,.96f,.42f):layer==2||layer==5?new Color(.9f,.83f,.69f,.48f):new Color(.68f,.59f,.45f,.52f);
                var material=SituationMaterial("Reconciled tracks "+layer,tint,layer);
                foreach(var f in group.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.Contains("trench")||f.name.Contains("bank")).ToArray())
                {
                    f.GetComponent<MeshRenderer>().sharedMaterial=material;
                    // Shorten the marks instead of painting long black rails beyond the blockage.
                    
                    // Re-author from fixed dimensions so repeat runs never shrink the same mesh twice.
                    if(f.name.Contains("trench"))
                    {
                        string name=f.name;float across=name.StartsWith("Left")?-.81f:.81f;
                        Undo.DestroyObjectImmediate(f.gameObject);
                        SituationStrip(group,name,across,.39f,-3.1f,4.8f,material);
                    }
                    else if(f.name.Contains("bank"))
                    {f.GetComponent<MeshRenderer>().sharedMaterial=material;}
                }
                // Existing settings belong to these same jobs: move only stale markers, retain natural props.
                var setting=surrounds==null?null:surrounds.Find(site.Id+" recovery setting");
                if(setting!=null)
                {
                    
                    int stake=0,cap=0;
                    foreach(Transform child in setting)
                    {
                        if(child.name=="Recovery boundary stake"||child.name=="Reflective warning cap")
                        {
                            bool isCap=child.name=="Reflective warning cap";int index=isCap?cap++:stake++;
                            var point=Ground(site.Position+forward*(site.Kind==RecoverySituationKind.Water?12:7)+side*(index==0?-4.5f:4.5f));
                            Undo.RecordObject(child,"Align existing recovery approach markers");child.position=point+Vector3.up*(isCap?1.1f:.65f);
                        }
                        else if(site.Kind==RecoverySituationKind.Water)
                        {
                            // Retain reeds at the old shore; they no longer imply a second recovery bay.
                            var renderers=child.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;
                            var bounds=ScenarioBounds(child.gameObject);var center=bounds.center;
                            if(Flat(center-site.Position).magnitude<3.8f)center=site.Position+side*(child.GetSiblingIndex()%2==0?-5:5)+forward*(3+child.GetSiblingIndex()*.6f);
                            var point=Ground(center);
                            Undo.RecordObject(child,"Seat old shore props");child.position+=new Vector3(point.x-bounds.center.x,point.y-bounds.min.y-.06f,point.z-bounds.center.z);
                        }
                    }
                }
                var marker=signs.Cast<Transform>().First(t=>t.name.StartsWith(site.Id+" -"));
                var signPoint=Ground(site.Position+forward*(site.Kind==RecoverySituationKind.Water?13:6)+side*5.5f);
                var sign=marker.Find("Recovery sign");var post=marker.Find("Site marker post");var text=marker.GetComponentInChildren<TMPro.TextMeshPro>();
                Undo.RecordObjects(new UnityEngine.Object[]{marker,sign,post,text.transform},"Align site sign with recovery approach");
                // Set children in world space; changing the empty marker root does not move old child offsets twice.
                marker.position=site.Position;post.position=signPoint+Vector3.up*1.1f;sign.position=signPoint+Vector3.up*2.4f;sign.rotation=Quaternion.LookRotation(-forward);
                text.transform.SetPositionAndRotation(sign.position+forward*.11f,sign.rotation);
                var reverse=marker.Find("Recovery sign reverse label");
                if(reverse==null){reverse=UnityEngine.Object.Instantiate(text.gameObject,marker).transform;reverse.name="Recovery sign reverse label";Undo.RegisterCreatedObjectUndo(reverse.gameObject,"Readable sign reverse");}
                reverse.SetPositionAndRotation(sign.position-forward*.11f,sign.rotation*Quaternion.Euler(0,180,0));
                var spur=environment.Find("Road network - 24 recovery spurs").GetComponentsInChildren<MeshFilter>().FirstOrDefault(f=>f.name.EndsWith("-"+site.Id));
                var approach=spur!=null?spur.sharedMesh.vertices.Select(spur.transform.TransformPoint).OrderBy(p=>Flat(p-site.Position).sqrMagnitude).First():map.NearestRoad(site.Position);
                if(!RoadsideRecovery(site))ReconcileApproachTracks(group,site,material,approach);
            }
            if(environment.Find("Broad recovery ground zones")==null)ReconcileMudFootprints(map);
            ReconcileDuneSurface(map.Sites.Single(s=>s.Id=="R20"));
            ClearRecoveryGroundCover(map);
            SeatExistingLandscapeProps();
            Physics.SyncTransforms();
            for(int i=1;i<map.Sites.Length;i++)FitSituationCar(map.Sites[i],world.Casualty);
            if(environment.Find("Broad recovery ground zones")!=null)BuildBroadGroundZones();
            else if(environment.Find("Readable recovery wheel pockets")!=null)DressReadableWheelBlockages();
            map.Configure(map.Sites,map.Deliveries,ReadSituationCheckpoints(map));EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static int RecoveryGroundLayer(RecoverySite site)
        {
            var data=terrain.terrainData;var p=site.Position-terrain.transform.position;
            int x=Mathf.Clamp(Mathf.RoundToInt(p.x/data.size.x*(data.alphamapWidth-1)),0,data.alphamapWidth-1),z=Mathf.Clamp(Mathf.RoundToInt(p.z/data.size.z*(data.alphamapHeight-1)),0,data.alphamapHeight-1);
            var a=data.GetAlphamaps(x,z,1,1);int layer=0;for(int i=1;i<data.alphamapLayers;i++)if(a[0,0,i]>a[0,0,layer])layer=i;
            if(layer==7)return 4;if(layer==0||layer==6)return site.Kind==RecoverySituationKind.Sand?2:1;return layer;
        }

        private static void ReconcileMudFootprints(RecoveryMap map)
        {
            foreach(var f in environment.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.EndsWith(" - Living Mud")).ToArray())
            {
                var site=map.Sites.Single(s=>f.name.StartsWith(s.Id));
                Undo.RecordObject(f.gameObject,"Reconcile mud with current recovery situation");f.gameObject.SetActive(site.Kind==RecoverySituationKind.Mud);
                if(site.Kind!=RecoverySituationKind.Mud)continue;
                const int n=56;float width=site.Id=="R03"?5.2f:4.3f,length=site.Id=="R06"?6:7;
                var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();var yaw=Quaternion.LookRotation(site.ExitDirection);
                Undo.RecordObject(f.transform,"Align mud UV frame with recovery tracks");f.transform.SetPositionAndRotation(site.Position,yaw);
                for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
                {
                    float u=x/(float)n*2-1,v=z/(float)n*2-1;float radius=Mathf.Sqrt(u*u+v*v);
                    float irregular=radius+.055f*Mathf.Sin(u*11+v*7)+.035f*Mathf.Cos(v*17-u*3);
                    var p=Ground(site.Position+yaw*new Vector3(u*width,0,v*length));
                    // The old fit pass lifted every edge above ground, exposing the circular mesh border.
                    // Sink the outer ring smoothly; the existing terrain hides its triangles naturally.
                    p.y+=.055f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,.99f,irregular))*.24f;
                    vertices.Add(f.transform.InverseTransformPoint(p));uv.Add(new Vector2(x/(float)n,z/(float)n));
                    if(x<n&&z<n){int a=z*(n+1)+x;triangles.AddRange(new[]{a,a+n+1,a+1,a+1,a+n+1,a+n+2});}
                }
                var mesh=f.sharedMesh;Undo.RecordObject(mesh,"Natural mud footprint");mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);
                var collider=f.GetComponent<MeshCollider>();collider.sharedMesh=null;collider.sharedMesh=mesh;
                ReconcileMudEdge(f,site,width,length);
            }
        }

        [Serializable] private sealed class LandscapeSeat { public string Path,Name;public Vector3 Position;public float Delta; }
        [Serializable] private sealed class LandscapeSeats { public LandscapeSeat[] Items; }
        private static void SeatExistingLandscapeProps()
        {
            string path="Docs/Validation/MapReconciliation/LandscapeSeats.json";LandscapeSeats seats;
            if(File.Exists(path))seats=JsonUtility.FromJson<LandscapeSeats>(File.ReadAllText(path));
            else
            {
                var baseline=JsonUtility.FromJson<SituationBaseline>(File.ReadAllText(SituationEvidence+"/TerrainPatchesBaseline.json"));var data=terrain.terrainData;var entries=new List<LandscapeSeat>();
                foreach(string name in new[]{"Authored biome groves and rock formations","Roadside undergrowth and gravel","Lakeshore rocks reeds and driftwood","Embedded rocky road banks","Mountain outcrops and scree","Mountain approaches - refuges and meadow"})
                {
                    var group=environment.Find(name);if(group==null)continue;
                    foreach(Transform child in group)
                    {
                        var renderers=child.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;
                        var bounds=ScenarioBounds(child.gameObject);var point=bounds.center;var local=point-terrain.transform.position;
                        float x=local.x/data.size.x*(baseline.Resolution-1),z=local.z/data.size.z*(baseline.Resolution-1);
                        foreach(var patch in baseline.Patches.Skip(1))
                        {
                            float px=x-patch.X,pz=z-patch.Z;if(px<0||pz<0||px>=patch.Width-1||pz>=patch.Height-1)continue;
                            int ix=Mathf.FloorToInt(px),iz=Mathf.FloorToInt(pz);float tx=px-ix,tz=pz-iz;
                            float h=Mathf.Lerp(Mathf.Lerp(patch.Heights[iz*patch.Width+ix],patch.Heights[iz*patch.Width+ix+1],tx),Mathf.Lerp(patch.Heights[(iz+1)*patch.Width+ix],patch.Heights[(iz+1)*patch.Width+ix+1],tx),tz)*data.size.y+terrain.transform.position.y;
                            float delta=Ground(point).y-h;
                            if(Mathf.Abs(delta)>.12f)entries.Add(new LandscapeSeat{Path=name+"/"+child.GetSiblingIndex(),Name=child.name,Position=child.position,Delta=delta});break;
                        }
                    }
                }
                seats=new LandscapeSeats{Items=entries.ToArray()};File.WriteAllText(path,JsonUtility.ToJson(seats,true));
            }
            foreach(var entry in seats.Items)
            {
                int slash=entry.Path.LastIndexOf('/');var group=environment.Find(entry.Path.Substring(0,slash));var child=group.GetChild(int.Parse(entry.Path.Substring(slash+1)));
                if(child.name!=entry.Name)throw new InvalidOperationException("Review landscape seating baseline after hierarchy changes: "+entry.Path);
                Undo.RecordObject(child,"Seat existing landscape on changed recovery terrain");child.position=entry.Position+Vector3.up*entry.Delta;
            }
        }

        private static void ClearRecoveryGroundCover(RecoveryMap map)
        {
            var data=terrain.terrainData;int n=data.detailResolution;if(n==0)return;
            Undo.RegisterCompleteObjectUndo(data,"Clear vegetation through stranded cars");
            foreach(var site in map.Sites.Skip(1))
            {
                var p=site.Position-terrain.transform.position;int radius=Mathf.CeilToInt(8/data.size.x*n);
                int x0=Mathf.Clamp(Mathf.FloorToInt(p.x/data.size.x*n)-radius,0,n-1),z0=Mathf.Clamp(Mathf.FloorToInt(p.z/data.size.z*n)-radius,0,n-1),width=Mathf.Min(radius*2+1,n-x0),height=Mathf.Min(radius*2+1,n-z0);
                var side=Vector3.Cross(Vector3.up,site.ExitDirection);
                for(int layer=0;layer<data.detailPrototypes.Length;layer++)
                {
                    var details=data.GetDetailLayer(x0,z0,width,height,layer);
                    for(int z=0;z<height;z++)for(int x=0;x<width;x++)
                    {
                        var point=terrain.transform.position+new Vector3((x+x0+.5f)/n*data.size.x,0,(z+z0+.5f)/n*data.size.z);var delta=Flat(point-site.Position);
                        if(Mathf.Abs(Vector3.Dot(delta,side))<4.5f&&Mathf.Abs(Vector3.Dot(delta,site.ExitDirection))<5.5f)details[z,x]=0;
                    }
                    data.SetDetailLayer(x0,z0,layer,details);
                }
            }
            terrain.Flush();EditorUtility.SetDirty(data);
        }

        private static void ReconcileMudEdge(MeshFilter mud,RecoverySite site,float width,float length,bool followSurface=false)
        {
            var existing=mud.transform.Find("Blended soil margin");if(existing!=null)Undo.DestroyObjectImmediate(existing.gameObject);
            var root=new GameObject("Blended soil margin",typeof(MeshFilter),typeof(MeshRenderer)).transform;root.SetParent(mud.transform,false);
            var material=AssetDatabase.LoadAssetAtPath<Material>(SituationFolder+"/Mud soil transition.mat");
            if(material==null){material=new Material(Shader.Find("TowTrouble/Recovery Ground Blend"));AssetDatabase.CreateAsset(material,SituationFolder+"/Mud soil transition.mat");}
            var data=terrain.terrainData;material.SetTexture("_Control0",data.alphamapTextures[0]);material.SetTexture("_Control1",data.alphamapTextures[1]);
            material.SetVector("_TerrainSize",new Vector4(data.size.x,data.size.z,terrain.transform.position.x,terrain.transform.position.z));
            for(int i=0;i<Mathf.Min(8,data.terrainLayers.Length);i++){var layer=data.terrainLayers[i];material.SetTexture("_Ground"+i,layer.diffuseTexture);material.SetVector("_Tile"+i,new Vector4(layer.tileSize.x,layer.tileSize.y,layer.tileOffset.x,layer.tileOffset.y));material.SetVector("_Tint"+i,layer.diffuseRemapMax);}
            EditorUtility.SetDirty(material);
            var vs=new List<Vector3>();var colors=new List<Color>();var tri=new List<int>();const int n=64;var yaw=Quaternion.LookRotation(site.ExitDirection);var surfaceVertices=followSurface?mud.sharedMesh.vertices:null;
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
            {
                float u=x/(float)n*2-1,v=z/(float)n*2-1;float radius=Mathf.Sqrt(u*u+v*v)+.055f*Mathf.Sin(u*11+v*7)+.035f*Mathf.Cos(v*17-u*3);
                var p=Ground(site.Position+yaw*new Vector3(u*width,0,v*length))+Vector3.up*.09f;
                if(followSurface)p=mud.transform.TransformPoint(surfaceVertices[z*(n+1)+x])+Vector3.up*.015f;
                // Broad zones blend all the way into native terrain. Do not fade the native
                // terrain back out at the outside edge: that exposed a bright/dark outline.
                float organicRadius=radius+(Mathf.PerlinNoise(p.x*.43f,p.z*.43f)-.5f)*.12f;
                float alpha=followSurface?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f,.56f,organicRadius))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.66f,.97f,radius))):
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.75f,radius))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,1.05f,radius)));
                vs.Add(root.InverseTransformPoint(p));colors.Add(new Color(1,1,1,alpha));if(x<n&&z<n){int a=z*(n+1)+x;tri.AddRange(new[]{a,a+n+1,a+1,a+1,a+n+1,a+n+2});}
            }
            var mesh=new Mesh{name=site.Id+" mud margin"};mesh.SetVertices(vs);mesh.SetColors(colors);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            root.GetComponent<MeshFilter>().sharedMesh=VisualAsset(mesh,"RecoverySituations/"+site.Id+" mud margin.asset");var renderer=root.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }

        private static void ReconcileDuneSurface(RecoverySite site)
        {
            var data=terrain.terrainData;var p=site.Position-terrain.transform.position;int resolution=data.alphamapResolution;
            int radius=Mathf.CeilToInt(17/data.size.x*(resolution-1)),cx=Mathf.RoundToInt(p.x/data.size.x*(resolution-1)),cz=Mathf.RoundToInt(p.z/data.size.z*(resolution-1));
            int x0=cx-radius,z0=cz-radius,size=radius*2+1;var a=data.GetAlphamaps(x0,z0,size,size);
            string path="Docs/Validation/MapReconciliation/R20PaintBaseline.json";Directory.CreateDirectory(Path.GetDirectoryName(path));
            if(!File.Exists(path))File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(a));
            a=Newtonsoft.Json.JsonConvert.DeserializeObject<float[,,]>(File.ReadAllText(path));
            for(int z=0;z<size;z++)for(int x=0;x<size;x++)
            {
                var world=terrain.transform.position+new Vector3((x+x0)/(float)(resolution-1)*data.size.x,0,(z+z0)/(float)(resolution-1)*data.size.z);var delta=Flat(world-site.Position);
                float d=Mathf.Sqrt(Mathf.Pow(Vector3.Dot(delta,site.ExitDirection)/16,2)+Mathf.Pow(Vector3.Dot(delta,Vector3.Cross(Vector3.up,site.ExitDirection))/11,2));
                float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.32f,1,d));for(int l=0;l<data.alphamapLayers;l++)a[z,x,l]=Mathf.Lerp(a[z,x,l],l==2?1:0,blend);
            }
            Undo.RegisterCompleteObjectUndo(data,"Blend recovery dune into beach");data.SetAlphamaps(x0,z0,a);EditorUtility.SetDirty(data);
        }

        private static void ReconcileApproachTracks(Transform group,RecoverySite site,Material material,Vector3 road)
        {
            var old=group.Find("Approach from existing road");if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
            float length=Flat(road-site.Position).magnitude;if(length<9||length>65)return;
            var root=new GameObject("Approach from existing road").transform;root.SetParent(group,false);
            var points=new List<Vector3>();int rows=Mathf.CeilToInt(length/.7f);var start=site.Position+site.ExitDirection*3.5f;
            for(int r=0;r<=rows;r++){float t=r/(float)rows;var p=Vector3.Lerp(start,road,t);p+=site.ExitDirection*(Mathf.Sin(t*Mathf.PI)*2);points.Add(Ground(p));}
            var mesh=new Mesh{name=site.Id+" approach"};var vs=new List<Vector3>();var uv=new List<Vector2>();var rut=new List<Vector2>();var ends=new List<Vector2>();var colors=new List<Color>();var tri=new List<int>();
            for(int lane=0;lane<2;lane++)for(int r=0;r<=rows;r++)for(int c=0;c<5;c++)
            {
                var direction=Flat(points[Mathf.Min(rows,r+1)]-points[Mathf.Max(0,r-1)]).normalized;var side=Vector3.Cross(Vector3.up,direction);float u=c/4f;
                var p=Ground(points[r]+side*((lane==0?-.8f:.8f)+(u*2-1)*.34f))+Vector3.up*.025f;
                vs.Add(root.InverseTransformPoint(p));uv.Add(new Vector2(p.x,p.z)*.35f);rut.Add(new Vector2(u,r*.7f+.65f));ends.Add(Vector2.one);colors.Add(new Color(1,1,1,.55f*Mathf.Sin(r/(float)rows*Mathf.PI)));
                if(r<rows&&c<4){int a=lane*(rows+1)*5+r*5+c;tri.AddRange(new[]{a,a+5,a+1,a+1,a+5,a+6});}
            }
            mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetUVs(1,rut);mesh.SetUVs(2,ends);mesh.SetColors(colors);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var f=root.gameObject.AddComponent<MeshFilter>();f.sharedMesh=VisualAsset(mesh,"RecoverySituations/"+site.Id+" approach.asset");var renderer=root.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
    }
}

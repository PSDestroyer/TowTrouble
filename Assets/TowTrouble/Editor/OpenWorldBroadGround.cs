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
        private const string BroadFolder="Assets/TowTrouble/OpenWorld/Visuals/BroadGround";
        [MenuItem("Tow Trouble/Recovery/Build Broad Soft Ground Zones")]
        public static void ApplyBroadGroundZones()
        {
            VisualContext();Directory.CreateDirectory(BroadFolder);AssetDatabase.Refresh();BuildBroadGroundZones();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        private static void BuildBroadGroundZones()
        {
            var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();var map=world.Map;
            RestoreNaturalSoftGroundSlopes(map);
            var old=environment.Find("Broad recovery ground zones");if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
            var parent=ScenicGroup("Broad recovery ground zones");Undo.RegisterCreatedObjectUndo(parent.gameObject,"Broad soft ground zones");
            var pockets=environment.Find("Readable recovery wheel pockets");if(pockets!=null){Undo.RecordObject(pockets.gameObject,"Replace wheel-sized forms with landscape zones");pockets.gameObject.SetActive(false);}
            var reports=new List<object>();
            foreach(var site in map.Sites.Skip(1))
            {
                if(RoadsideRecovery(site)||site.Kind==RecoverySituationKind.Water||site.Kind==RecoverySituationKind.EngineFailure||site.Kind==RecoverySituationKind.Rocks)continue;
                bool snow=site.Biome==3,sand=site.Biome>=4,muddy=!snow&&!sand;
                float width=muddy?14:snow?12:15,length=muddy?18:snow?16:19;
                var oldGroup=environment.Find("Distinct recovery situations").Cast<Transform>().Single(t=>t.name.StartsWith(site.Id+" -"));
                foreach(Transform child in oldGroup)if(child.name.Contains("trench")||child.name.Contains("bank")||child.name=="Exposed wash bed")child.gameObject.SetActive(false);
                var existing=environment.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f=>f.name==site.Id+" - Living Mud");
                bool dynamic=muddy&&site.Kind==RecoverySituationKind.Mud&&existing!=null;
                MeshFilter filter;
                if(dynamic){filter=existing;Undo.RecordObject(filter.transform,"Expand existing mud pool");}
                else{var go=new GameObject(site.Id+(muddy?" - marsh basin":snow?" - deep snow field":" - loose sand basin"),typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent,false);filter=go.GetComponent<MeshFilter>();}
                filter.gameObject.SetActive(true);filter.gameObject.layer=8;filter.transform.SetPositionAndRotation(site.Position,Quaternion.LookRotation(site.ExitDirection));filter.transform.localScale=Vector3.one;
                int layer=snow?4:sand?RecoveryGroundLayer(site):1;var nativeLayer=terrain.terrainData.terrainLayers[layer];
                const int n=64;var vertices=new List<Vector3>();var supportVertices=new List<Vector3>();var blendColors=new List<Color>();var uv=new List<Vector2>();var triangles=new List<int>();var visibleTriangles=new List<int>();
                for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
                {
                    float u=x/(float)n*2-1,v=z/(float)n*2-1;float r=BroadRadius(u,v);var p=Ground(filter.transform.TransformPoint(new Vector3(u*width,0,v*length)));
                    blendColors.Add(new Color(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.24f,.73f,r)),1,1,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.66f,.97f,r))));
                    float deep=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,1,r));float drift=muddy?0:.035f*Mathf.PerlinNoise(u*4+7,v*5+9);
                    p.y+=.045f+drift*deep-(1-deep)*.22f;supportVertices.Add(filter.transform.InverseTransformPoint(p));p.y+=BroadCoverHeight(snow,sand,u,v);vertices.Add(filter.transform.InverseTransformPoint(p));uv.Add(muddy?new Vector2(x/(float)n,z/(float)n):new Vector2((p.x-terrain.transform.position.x+nativeLayer.tileOffset.x)/nativeLayer.tileSize.x,(p.z-terrain.transform.position.z+nativeLayer.tileOffset.y)/nativeLayer.tileSize.y));
                    if(x<n&&z<n&&r<1.08f){int a=z*(n+1)+x;var face=new[]{a,a+n+1,a+1,a+1,a+n+1,a+n+2};triangles.AddRange(face);if(!dynamic||r<.64f)visibleTriangles.AddRange(face);}
                }
                var mesh=new Mesh{name=site.Id+" broad ground"};mesh.SetVertices(vertices);mesh.SetColors(blendColors);mesh.SetUVs(0,uv);mesh.SetTriangles(visibleTriangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
                filter.sharedMesh=VisualAsset(mesh,"BroadGround/"+site.Id+" broad ground.asset");var collider=filter.GetComponent<MeshCollider>();var support=new Mesh{name=site.Id+" soft ground support"};support.SetVertices(supportVertices);support.SetTriangles(triangles,0);support.RecalculateBounds();collider.sharedMesh=null;collider.sharedMesh=VisualAsset(support,"BroadGround/"+site.Id+" soft ground support.asset");
                var renderer=filter.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
                if(!dynamic)
                {
                    // One feathered surface blends native terrain textures and loose material.
                    // Reuse the configured terrain sampler, without a second overlapping skirt.
                    ReconcileMudEdge(filter,site,width,length,true);
                    var margin=filter.transform.Find("Blended soil margin");var terrainMaterial=margin.GetComponent<MeshRenderer>().sharedMaterial;
                    var material=BroadMaterial(layer,muddy);material.shader=terrainMaterial.shader;material.CopyPropertiesFromMaterial(terrainMaterial);
                    material.SetFloat("_CoverEnabled",1);material.SetTexture("_CoverMap",muddy?AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/County wet silt.mat").GetTexture("_Albedo"):nativeLayer.diffuseTexture);
                    material.SetColor("_CoverTint",muddy?new Color(.56f,.41f,.26f):new Color(nativeLayer.diffuseRemapMax.x,nativeLayer.diffuseRemapMax.y,nativeLayer.diffuseRemapMax.z).gamma);
                    material.SetVector("_CoverTiling",muddy?new Vector4(7,9,0,0):new Vector4(1,1,0,0));material.renderQueue=2995;EditorUtility.SetDirty(material);renderer.sharedMaterial=material;UnityEngine.Object.DestroyImmediate(margin.gameObject);
                }
                if(muddy)
                {
                    if(filter.GetComponent<RecoveryMudTerrain>()==null)filter.gameObject.AddComponent<RecoveryMudTerrain>();
                    filter.GetComponent<RecoveryMudTerrain>().ConfigureTracks(dynamic);
                    var track=filter.GetComponent<RecoveryTrackSurface>();if(track==null)track=filter.gameObject.AddComponent<RecoveryTrackSurface>();track.Configure(RecoveryTrackKind.Dirt);
                    if(dynamic)ReconcileMudEdge(filter,site,width,length,true);
                }
                else
                {
                    var surface=filter.gameObject.AddComponent<RecoveryGroundSurface>();surface.Configure(snow?RecoveryGroundKind.Snow:RecoveryGroundKind.Sand);surface.ConfigureSoftness(1);
                    var track=filter.gameObject.AddComponent<RecoveryTrackSurface>();track.Configure(snow?RecoveryTrackKind.Snow:RecoveryTrackKind.Sand);
                }
                filter.GetComponent<RecoveryTrackSurface>().ConfigureVisualCover(filter.sharedMesh,collider.sharedMesh);
                ClearBroadVegetation(site,width,length);
                reports.Add(new{site.Id,kind=muddy?"Mud":snow?"Snow":"Sand",widthMeters=width*2,lengthMeters=length*2,dynamicMud=dynamic});
            }
            Physics.SyncTransforms();foreach(var site in map.Sites.Skip(1))FitSituationCar(site,world.Casualty);
            map.Configure(map.Sites,map.Deliveries,ReadSituationCheckpoints(map));EditorUtility.SetDirty(map);
            var mud=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/County wet silt.mat");var spray=AssetDatabase.LoadAssetAtPath<Material>(BroadFolder+"/Surface grains.mat");
            if(spray==null){spray=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));spray.SetColor("_BaseColor",Color.white);AssetDatabase.CreateAsset(spray,BroadFolder+"/Surface grains.mat");}
            foreach(var vehicle in world.Drivers.Concat(new[]{world.Casualty}))
            {
                var coating=vehicle.GetComponent<RecoveryWheelMud>();Undo.RecordObject(coating,"Snow and sand tire coating");coating.ConfigureGround(world,mud.GetTexture("_Albedo"),terrain.terrainData.terrainLayers[4].diffuseTexture,terrain.terrainData.terrainLayers[2].diffuseTexture);EditorUtility.SetDirty(coating);
                foreach(var ps in vehicle.GetComponentsInChildren<ParticleSystem>(true).Where(p=>p.name.StartsWith("Mud spray"))){ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=spray;var main=ps.main;main.playOnAwake=false;main.maxParticles=28;EditorUtility.SetDirty(ps);}
            }
            Directory.CreateDirectory("Docs/Validation/BroadSoftGround");File.WriteAllText("Docs/Validation/BroadSoftGround/Zones.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));
        }
        private static void RestoreNaturalSoftGroundSlopes(RecoveryMap map)
        {
            // Re-author from the original height samples, never flatten a sloping site
            // to the vehicle's centre altitude. That previously cut artificial cliff walls.
            var baseline=JsonUtility.FromJson<SituationBaseline>(File.ReadAllText(SituationEvidence+"/TerrainPatchesBaseline.json"));
            var data=terrain.terrainData;
            if(baseline.TerrainGuid!=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data))||baseline.Resolution!=data.heightmapResolution)throw new InvalidOperationException("Terrain baseline mismatch");
            Undo.RegisterCompleteObjectUndo(data,"Restore natural recovery slopes");
            var seats=new Dictionary<Transform,float>();
            foreach(var name in new[]{"Distinct recovery situations","Recovery situations - 24 authored surrounds"})
            {
                var root=environment.Find(name);if(root==null)continue;
                foreach(Transform group in root)foreach(Transform child in group)seats[child]=Ground(child.position).y;
            }
            var roads=ReadSituationCheckpoints(map);
            for(int i=1;i<map.Sites.Length;i++)
            {
                var site=map.Sites[i];if(RoadsideRecovery(site)||site.Kind==RecoverySituationKind.Water||site.Kind==RecoverySituationKind.EngineFailure||site.Kind==RecoverySituationKind.Rocks)continue;
                var patch=baseline.Patches[i];var heights=new float[patch.Height,patch.Width];var yaw=Quaternion.Inverse(Quaternion.LookRotation(site.ExitDirection));
                for(int z=0;z<patch.Height;z++)for(int x=0;x<patch.Width;x++)
                {
                    var p=terrain.transform.position+new Vector3((x+patch.X)/(float)(data.heightmapResolution-1)*data.size.x,0,(z+patch.Z)/(float)(data.heightmapResolution-1)*data.size.z);
                    var local=yaw*(p-site.Position);float tracks=Mathf.Max(Mathf.Exp(-Mathf.Pow((local.x-.81f)/.75f,2)),Mathf.Exp(-Mathf.Pow((local.x+.81f)/.75f,2)));
                    float rut=.16f*tracks*Mathf.Exp(-local.z*local.z/16);if(roads.Any(c=>Flat(c-p).sqrMagnitude<30.25f))rut=0;
                    heights[z,x]=patch.Heights[z*patch.Width+x]-rut/data.size.y;
                }
                data.SetHeights(patch.X,patch.Z,heights);
            }
            SmoothSnowRecoverySlopes(map);
            foreach(var seat in seats)if(seat.Key!=null){Undo.RecordObject(seat.Key,"Seat props on natural slope");seat.Key.position+=Vector3.up*(Ground(seat.Key.position).y-seat.Value);}
            EditorUtility.SetDirty(data);Physics.SyncTransforms();
        }
        private static void SmoothSnowRecoverySlopes(RecoveryMap map)
        {
            var data=terrain.terrainData;int n=data.heightmapResolution;
            const string path="Docs/Validation/GroundShapeRepair/SnowSlopeBaseline.json";
            SituationBaseline baseline;
            if(File.Exists(path))baseline=JsonUtility.FromJson<SituationBaseline>(File.ReadAllText(path));
            else
            {
                var patches=new List<SituationPatch>();
                foreach(var site in map.Sites.Where(s=>s.Biome==3&&s.Kind!=RecoverySituationKind.EngineFailure))
                {
                    int cx=Mathf.RoundToInt((site.Position.x-terrain.transform.position.x)/data.size.x*(n-1)),cz=Mathf.RoundToInt((site.Position.z-terrain.transform.position.z)/data.size.z*(n-1));
                    int rx=Mathf.CeilToInt(22/data.size.x*(n-1)),rz=Mathf.CeilToInt(27/data.size.z*(n-1));int x0=cx-rx,z0=cz-rz,w=rx*2+1,h=rz*2+1;
                    var heights=data.GetHeights(x0,z0,w,h);var samples=new float[w*h];for(int z=0;z<h;z++)for(int x=0;x<w;x++)samples[z*w+x]=heights[z,x];
                    patches.Add(new SituationPatch{Center=site.Position,X=x0,Z=z0,Width=w,Height=h,Heights=samples});
                }
                baseline=new SituationBaseline{Resolution=n,Patches=patches.ToArray(),TerrainGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data))};Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(baseline,true));
            }
            var roads=ReadSituationCheckpoints(map);
            foreach(var patch in baseline.Patches)
            {
                int w=patch.Width,h=patch.Height;var heights=new float[h,w];
                for(int z=0;z<h;z++)for(int x=0;x<w;x++)
                {
                    float u=x/(float)(w-1),v=z/(float)(h-1),r=Mathf.Sqrt(Mathf.Pow((u-.5f)*2,2)+Mathf.Pow((v-.5f)*2,2));
                    float slope=Mathf.Lerp(Mathf.Lerp(patch.Heights[0],patch.Heights[w-1],u),Mathf.Lerp(patch.Heights[(h-1)*w],patch.Heights[h*w-1],u),v);
                    float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.38f,1,r));
                    var point=terrain.transform.position+new Vector3((x+patch.X)/(float)(n-1)*data.size.x,0,(z+patch.Z)/(float)(n-1)*data.size.z);
                    float roadDistance=roads.Min(c=>Flat(c-point).magnitude);blend*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(7,13,roadDistance));
                    heights[z,x]=Mathf.Lerp(patch.Heights[z*w+x],slope,blend);
                }
                data.SetHeights(patch.X,patch.Z,heights);
            }
        }
        // The contact bed sits below a shallow visual layer of loose material. This buries
        // the tire tread without moving suspension, adding forces, or editing wheel transforms.
        private static float BroadCoverHeight(bool snow,bool sand,float u,float v)
        {
            float depth=snow?.30f:sand?.24f:.27f;
            return depth*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.88f,BroadRadius(u,v))));
        }
        private static float BroadRadius(float u,float v)=>Mathf.Sqrt(u*u+v*v)+.055f*Mathf.Sin(u*11+v*7)+.035f*Mathf.Cos(v*17-u*3);
        private static Material BroadMaterial(int layer,bool mud)
        {
            string path=BroadFolder+"/"+(mud?"Wet marsh":"Loose ground "+layer)+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.shader=Shader.Find("Universal Render Pipeline/Lit");var native=terrain.terrainData.terrainLayers[layer];var tint=native.diffuseRemapMax;
            mat.SetTexture("_BaseMap",mud?AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/County wet silt.mat").GetTexture("_Albedo"):native.diffuseTexture);
            mat.SetColor("_BaseColor",mud?new Color(.56f,.41f,.26f):new Color(tint.x,tint.y,tint.z).gamma);mat.SetFloat("_Smoothness",mud?.62f:.05f);mat.SetTextureScale("_BaseMap",mud?new Vector2(7,9):Vector2.one);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
        }
        private static void ClearBroadVegetation(RecoverySite site,float width,float length)
        {
            var data=terrain.terrainData;int n=data.detailResolution;var local=site.Position-terrain.transform.position;int radius=Mathf.CeilToInt(Mathf.Max(width,length)/data.size.x*n)+2;
            int x0=Mathf.Clamp(Mathf.FloorToInt(local.x/data.size.x*n)-radius,0,n-1),z0=Mathf.Clamp(Mathf.FloorToInt(local.z/data.size.z*n)-radius,0,n-1),w=Mathf.Min(radius*2+1,n-x0),h=Mathf.Min(radius*2+1,n-z0);var inverse=Quaternion.Inverse(Quaternion.LookRotation(site.ExitDirection));
            for(int layer=0;layer<data.detailPrototypes.Length;layer++){var cells=data.GetDetailLayer(x0,z0,w,h,layer);for(int z=0;z<h;z++)for(int x=0;x<w;x++){var p=terrain.transform.position+new Vector3((x+x0+.5f)/n*data.size.x,0,(z+z0+.5f)/n*data.size.z);var d=inverse*(p-site.Position);if(BroadRadius(d.x/width,d.z/length)<.9f)cells[z,x]=0;}data.SetDetailLayer(x0,z0,layer,cells);}EditorUtility.SetDirty(data);
            var decor=environment.Find("Layered county details");if(decor!=null)foreach(var prop in decor.Cast<Transform>().SelectMany(c=>c.Cast<Transform>()).SelectMany(c=>c.Cast<Transform>())){var d=inverse*(prop.position-site.Position);if(BroadRadius(d.x/width,d.z/length)<.9f)prop.gameObject.SetActive(false);}
        }
    }
}

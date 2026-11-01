using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static RecoveryLoadSettings Load(RecoveryLoadMaterial material,float burial,float buried,float lip,float lipWheels,float belly,float locked=0,float water=0)
            =>new RecoveryLoadSettings{Material=material,Burial=burial,BuriedWheels=buried,LipHeight=lip,LipWheels=lipWheels,BellyContact=belly,LockedWheels=locked,RetainedWater=water};

        private static float ProgressionCoverHeight(RecoverySite site,Vector3 point)
        {
            if(site.Kind!=RecoverySituationKind.EngineFailure||site.Load==null||site.Load.Burial<=0)return 0;
            var local=Quaternion.Inverse(Quaternion.LookRotation(site.ExitDirection))*(point-site.Position);
            return site.Load.Burial*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.24f,.92f,BroadRadius(local.x/7,local.z/9))));
        }
        private static void DressProgressionDamagePocket(Transform root,RecoverySite site)
        {
            var go=new GameObject(site.Id+" stranded wheel accumulation",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider),typeof(RecoveryTrackSurface),typeof(RecoveryGroundSurface));go.layer=8;go.transform.SetParent(root,false);go.transform.SetPositionAndRotation(site.Position,Quaternion.LookRotation(site.ExitDirection));
            const int n=40;var bed=new System.Collections.Generic.List<Vector3>();var skin=new System.Collections.Generic.List<Vector3>();var colors=new System.Collections.Generic.List<Color>();var uvs=new System.Collections.Generic.List<Vector2>();var faces=new System.Collections.Generic.List<int>();
            bool snow=site.Load.Material==RecoveryLoadMaterial.Snow;var layer=terrain.terrainData.terrainLayers[snow?4:5];
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
            {
                float u=x/(float)n*2-1,v=z/(float)n*2-1,r=BroadRadius(u,v);var point=Ground(go.transform.TransformPoint(new Vector3(u*7,0,v*9)));
                // Support stays on the existing ground; only the loose visual layer accumulates.
                point.y+=.012f;bed.Add(go.transform.InverseTransformPoint(point));point.y+=ProgressionCoverHeight(site,point);skin.Add(go.transform.InverseTransformPoint(point));
                colors.Add(new Color(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.24f,.73f,r)),1,1,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.66f,.97f,r))));
                uvs.Add(new Vector2((point.x-terrain.transform.position.x+layer.tileOffset.x)/layer.tileSize.x,(point.z-terrain.transform.position.z+layer.tileOffset.y)/layer.tileSize.y));
                if(x<n&&z<n&&r<1.08f){int a=z*(n+1)+x;faces.AddRange(new[]{a,a+n+1,a+1,a+1,a+n+1,a+n+2});}
            }
            var visual=new Mesh{name=site.Id+" damage pocket skin"};visual.SetVertices(skin);visual.SetUVs(0,uvs);visual.SetColors(colors);visual.SetTriangles(faces,0);visual.RecalculateNormals();visual.RecalculateBounds();
            var support=new Mesh{name=site.Id+" damage pocket support"};support.SetVertices(bed);support.SetTriangles(faces,0);support.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh=VisualAsset(visual,"BroadGround/"+visual.name+".asset");go.GetComponent<MeshCollider>().sharedMesh=VisualAsset(support,"BroadGround/"+support.name+".asset");
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(BroadFolder+"/Loose ground "+(snow?4:5)+".mat");renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var ground=go.GetComponent<RecoveryGroundSurface>();ground.Configure(snow?RecoveryGroundKind.Snow:RecoveryGroundKind.Sand);ground.ConfigureSoftness(.85f);
            var track=go.GetComponent<RecoveryTrackSurface>();track.Configure(snow?RecoveryTrackKind.Snow:RecoveryTrackKind.Sand);track.ConfigureVisualCover(go.GetComponent<MeshFilter>().sharedMesh,go.GetComponent<MeshCollider>().sharedMesh);
        }
        [MenuItem("Tow Trouble/Recovery/Calibrate Mission Loads")]
        public static void CalibrateMissionLoads()
        {
            VisualContext();var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();var map=world.Map;
            var firm=RecoveryLoadMaterial.Firm;var mud=RecoveryLoadMaterial.Mud;var snow=RecoveryLoadMaterial.Snow;var sand=RecoveryLoadMaterial.Sand;var silt=RecoveryLoadMaterial.Silt;
            // Measurable scenario severity rather than one force multiplier per tier.
            // The original R01 extraction settings remain the tested reference.
            var plans=new[]{
                Load(firm,.16f,.25f,.14f,.25f,0),
                Load(mud,.18f,.75f,.06f,.25f,.05f),
                Load(mud,.24f,.80f,.08f,.50f,.25f),
                Load(mud,.27f,.80f,.17f,.65f,.40f),
                Load(silt,.08f,.50f,.03f,.25f,0,0,35),
                Load(silt,.20f,.85f,.05f,.25f,.20f),
                Load(mud,.20f,.65f,.14f,.50f,.15f),
                Load(silt,.26f,.80f,.05f,.50f,.35f,0,180),
                Load(firm,0,0,0,0,0,.35f),
                Load(mud,.12f,.65f,.11f,.50f,.25f),
                Load(mud,.30f,.80f,.30f,.80f,.85f),
                Load(snow,.22f,.75f,.08f,.50f,.30f,.10f),
                Load(snow,.32f,.85f,.08f,.25f,.20f),
                Load(snow,.43f,.90f,.18f,.65f,.50f),
                Load(snow,.43f,.95f,.10f,.50f,.50f),
                Load(snow,.36f,.75f,0,0,.30f,1),
                Load(sand,.12f,.65f,.04f,.25f,.05f),
                Load(sand,.19f,.75f,.10f,.50f,.10f),
                Load(sand,.29f,.90f,.10f,.50f,.15f),
                Load(sand,.36f,.95f,.11f,.50f,.35f),
                Load(sand,.18f,.75f,.12f,.50f,.10f),
                Load(sand,.31f,.85f,.12f,.50f,.15f),
                Load(sand,.27f,.90f,.17f,.50f,.60f),
                Load(sand,.22f,.80f,0,0,.20f,.70f)
            };
            if(map.Sites.Length!=plans.Length)throw new InvalidOperationException("Review the 24-site load plan before authoring.");
            Undo.RecordObject(map,"Physical recovery progression");
            for(int i=0;i<map.Sites.Length;i++)
            {
                var site=map.Sites[i];var model=world.Casualty.ModelAt(site.TargetModel);var load=plans[i];load.Mass=model.Mass;site.Load=load;
                if(i==0)continue;
                var forces=RecoveryLoad.Forces(load,model.Radius);
                site.Resistance=0; // All world recoveries now share displacement/contact release rules.
                site.Extraction=new RecoveryExtractionSettings{Enabled=true,TerrainAware=true,BlockedWheel=load.BuriedWheels>.7f?2:0,
                    Travel=site.Kind==RecoverySituationKind.EngineFailure?3.2f:3.2f+site.Tier*.65f,
                    BreakawayForce=forces.Total,MovingForce=forces.Total*.88f,MovingDrag=load.Material==firm?180:320,
                    ClearHold=.16f,ReleaseDuration=.35f+site.Tier*.035f};
            }
            map.Sites[8].Situation="Motor defect și frâne spate gripate. Deblochează roțile prin tragere lină, apoi remorchează mașina la atelier.";
            map.Sites[8].EnglishSituation="Engine failure and sticking rear brakes. Free the wheels with a smooth pull, then tow the car to the workshop.";
            map.Sites[15].Situation="Motor defect și transmisie înțepenită; roțile au târât zăpada până la șasiu. Trage drept pentru deblocare, de pe sprijin ferm.";
            map.Sites[15].EnglishSituation="Engine failure and a seized transmission; the wheels dragged snow up to the chassis. Pull straight from firm ground to unjam it.";
            map.Sites[23].Situation="Motor defect și frâne blocate; roțile au săpat nisipul. Deblochează mașina prin tragere controlată, apoi remorcheaz-o la atelier.";
            map.Sites[23].EnglishSituation="Engine failure and jammed brakes; the wheels ploughed into the sand. Unjam the car with a controlled pull, then tow it to the workshop.";
            // Reuse visual trails to explain the two damaged-wheel recoveries.
            var parent=environment.Find("Progression damage trails");if(parent!=null)UnityEngine.Object.DestroyImmediate(parent.gameObject);
            parent=ScenicGroup("Progression damage trails");
            foreach(int i in new[]{8,15,23})
            {
                var site=map.Sites[i];var group=PolishGroup(parent,site.Id+" seized-wheel drag marks");
                if(site.Load.Burial>0)DressProgressionDamagePocket(group,site);
                DressRoadsideAccidentTrail(group,site,site.Position+site.ExitDirection*7,false);
            }
            map.Configure(map.Sites,map.Deliveries,ReadSituationCheckpoints(map));EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }
}

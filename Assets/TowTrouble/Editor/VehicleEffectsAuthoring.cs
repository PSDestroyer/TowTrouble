using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TowTrouble.Editor
{
    public static class VehicleEffectsAuthoring
    {
        private const string Root="Assets/TowTrouble/Art/VehicleEffects";
        private const string ProfilePath=Root+"/Hot Rod vehicle effects.asset";
        public static void Attach(RecoveryVehicle car)
        {
            var profile=AssetDatabase.LoadAssetAtPath<RecoveryVehicleEffectsProfile>(ProfilePath);if(profile==null)return;
            var effect=car.GetComponent<RecoveryVehicleEffects>();if(effect==null)effect=car.gameObject.AddComponent<RecoveryVehicleEffects>();
            effect.Configure(car,profile);EditorUtility.SetDirty(effect);
        }
        [MenuItem("Tow Trouble/Vehicles/Configure Brake Lights and Exhaust")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
            System.IO.Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var profile=AssetDatabase.LoadAssetAtPath<RecoveryVehicleEffectsProfile>(ProfilePath);
            if(profile==null){profile=ScriptableObject.CreateInstance<RecoveryVehicleEffectsProfile>();profile.name="Hot Rod vehicle effects";AssetDatabase.CreateAsset(profile,ProfilePath);}
            var paths=new[]{"Assets/Hot_Rod_Oldtimers/Materials/Oldtimers_Emission.png","Assets/Hot_Rod_OffRoad/Materials/HotRods_Emission.png"};
            profile.Atlases=paths.Select(p=>{var source=AssetDatabase.LoadAssetAtPath<Texture2D>(p);return new VehicleEmissionAtlas{Source=source,Running=MakeEmission(source,false),Braking=MakeEmission(source,true)};}).ToArray();
            var meshes=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/TowTrouble/Garage/Parts"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("Exhaust")).SelectMany(p=>AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponentsInChildren<MeshFilter>(true)).Select(f=>f.sharedMesh).Distinct();
            profile.Exhausts=meshes.Select(Ports).Where(p=>p!=null).ToArray();
            var smoke=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Exhaust vapor.mat");
            if(smoke==null){smoke=new Material(AssetDatabase.LoadAssetAtPath<Shader>(Root+"/ExhaustVapor.shader")){name="Exhaust vapor"};AssetDatabase.CreateAsset(smoke,Root+"/Exhaust vapor.mat");}
            profile.Smoke=smoke;EditorUtility.SetDirty(profile);
            foreach(string name in new[]{"Scout","Hauler","StrandedClassic"})
            {
                string path="Assets/TowTrouble/Prefabs/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try{Attach(root.GetComponent<RecoveryVehicle>());PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            var scene=EditorSceneManager.GetActiveScene();
            foreach(var car in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryVehicle>(true)))Attach(car);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        private static Texture2D MakeEmission(Texture2D source,bool braking)
        {
            var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);var previous=RenderTexture.active;
            var texture=new Texture2D(source.width,source.height,TextureFormat.RGBAHalf,false,true){name=source.name+(braking?" - braking":" - running"),wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            try
            {
                Graphics.Blit(source,rt);RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,source.width,source.height),0,0);texture.Apply();
                var pixels=texture.GetPixels();for(int i=0;i<pixels.Length;i++)
                {var c=pixels[i];if(c.r>c.g*1.45f&&c.r>c.b*1.45f&&c.r>.04f){float intensity=c.r*(braking?7f:.35f);c=new Color(intensity,intensity*.008f,intensity*.002f,1);}c.a=1;pixels[i]=c;}
                texture.SetPixels(pixels);texture.Apply();string path=Root+"/"+texture.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if(existing==null){AssetDatabase.CreateAsset(texture,path);return texture;}
                EditorUtility.CopySerialized(texture,existing);UnityEngine.Object.DestroyImmediate(texture);EditorUtility.SetDirty(existing);return existing;
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
        }
        // Mesh-local outlet positions measured from the authored pipe end rings/caps.
        // Right variants mirror X; no emitter is invented for an empty exhaust socket.
        private static VehicleExhaustPorts Ports(Mesh mesh)
        {
            string n=mesh.name;Vector3[] points=null,dirs=null;float side=n.Contains("Right")?1:-1;
            Vector3 P(float x,float y,float z)=>new Vector3(Mathf.Abs(x)*side,y,z);
            Vector3 D(float x,float y,float z)=>new Vector3(Mathf.Abs(x)*side,y,z).normalized;
            if(n=="Bumper_Rear_Exhaust_Mesh") {points=new[]{new Vector3(-.310f,-.136f,-.279f)};dirs=new[]{Vector3.back};}
            else if(n.Contains("Bumper_Rear_Exhaust_Bulldog")){points=new[]{new Vector3(-.310f,-.135f,-.342f),new Vector3(.322f,-.135f,-.342f)};dirs=new[]{Vector3.back,Vector3.back};}
            else if(n.Contains("Bumper_Rear_Exhaust_LeMans")){points=new[]{new Vector3(-.3615f,.0369f,-.4194f),new Vector3(.3615f,.0369f,-.4194f)};dirs=new[]{new Vector3(0,.532f,-.847f),new Vector3(0,.532f,-.847f)};}
            else if(n.Contains("Bumper_Rear_Exhaust_3")){points=new[]{new Vector3(-.306f,-.174f,-.207f),new Vector3(.306f,-.174f,-.207f)};dirs=new[]{Vector3.back,Vector3.back};}
            else if(n.Contains("Trucker_Short")){points=new[]{P(.706f,.733f,.149f)};dirs=new[]{Vector3.up};}
            else if(n.Contains("Trucker_Box")){points=new[]{P(.641f,.806f,.243f)};dirs=new[]{Vector3.up};}
            else if(n.Contains("Trucker")){points=new[]{P(.6724f,.8778f,.0295f)};dirs=new[]{D(.263f,.3f,-.917f)};}
            else if(n.Contains("T_Long")){points=new[]{P(.714f,-.016f,-1.036f)};dirs=new[]{D(.89f,-.02f,-.45f)};}
            else if(n.Contains("Side_Bulldog")){points=new[]{P(.432f,-.088f,-1.015f)};dirs=new[]{-D(1,0,0)};}
            else if(n.Contains("T_Cone")){points=new[]{P(.6577f,-.023f,-.2697f)};dirs=new[]{D(.24f,-.019f,-.97f)};}
            else if(n.Contains("T_Belly")){points=new[]{P(.451f,-.019f,-.211f)};dirs=new[]{-D(1,0,0)};}
            else if(n.Contains("Modern_Short")){points=new[]{P(.693f,.213f,-.125f),P(.758f,.213f,.226f)};dirs=new[]{D(.136f,0,-.991f),D(1,0,0)};}
            else if(n.Contains("Rear_Chrome_Left")){points=new[]{P(.406f,-.043f,-.088f)};dirs=new[]{-D(1,0,0)};}
            else if(n.Contains("Exhaust_Box")){points=new[]{P(.773f,.228f,.218f)};dirs=new[]{D(1,0,0)};}
            // T_Plug parts are capped trim rather than an exposed tailpipe.
            if(points==null)return null;
            return new VehicleExhaustPorts{Mesh=mesh,Positions=points,Directions=dirs};
        }
    }
}

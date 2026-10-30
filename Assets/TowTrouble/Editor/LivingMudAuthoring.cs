using System;
using System.IO;
using System.Linq;
using Dinopunch.LivingMud;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowTrouble.Editor
{
    public static class LivingMudAuthoring
    {
        private const string Root="Assets/TowTrouble/Art/Mud";
        private const string Vendor="Assets/Living Mud";
        [MenuItem("Tow Trouble/Integrate Living Mud in Tutorial")]
        public static void ApplyToTutorial()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before authoring the mud area.");
            var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(DemoBuilder.ScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(DemoBuilder.ScenePath,OpenSceneMode.Additive);
            if(scene.isDirty)throw new InvalidOperationException("Save tutorial edits before applying Living Mud.");
            try
            {
                SceneManager.SetActiveScene(scene);
                var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryWorld>(true)).Single();
                var serialized=new SerializedObject(world);var environment=(GameObject)serialized.FindProperty("environment").objectReferenceValue;
                ApplyToEnvironment(environment.transform,world);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }
        public static void ApplyToEnvironment(Transform environment,RecoveryWorld world)
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var baseMaterial=AssetDatabase.LoadAssetAtPath<Material>(Vendor+"/URP Materials/Dynamic Mud Surface Material/DMS Mud03 Material.mat");
            var stampShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/TowTrouble/Integrations/LivingMud/LivingMudWheelStamps.shader");
            var tread=AssetDatabase.LoadAssetAtPath<Texture2D>(Vendor+"/Prefabs/Stamp Prefabs/Tire Tread Stamp/Tire Tread Stamp Normal.png");
            if(baseMaterial==null||stampShader==null||tread==null)throw new InvalidOperationException("Import the Living Mud core dependencies first.");
            // The tread stores normal/alpha data; do not apply an sRGB conversion.
            var tireImporter=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tread));if(tireImporter.sRGBTexture){tireImporter.sRGBTexture=false;tireImporter.SaveAndReimport();}
            // A separate material and render texture preserve the imported examples.
            var mud=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Tutorial Living Mud.mat");
            if(mud==null){mud=new Material(baseMaterial);AssetDatabase.CreateAsset(mud,Root+"/Tutorial Living Mud.mat");}
            mud.SetFloat("_Flip_UV",1);mud.SetColor("_Tint",new Color(.62f,.52f,.4f,1));mud.SetFloat("_Smoothness",.67f);mud.SetFloat("_Normal_Strength",.48f);mud.SetFloat("_Stamp_Darken",.4f);mud.SetFloat("_Dynamic_Normal_Amount",.8f);mud.SetFloat("_Height_Amount",.025f);mud.SetVector("_Tiling",new Vector4(3,3,0,0));
            var stampMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Wheel Stamp Batch.mat");
            if(stampMaterial==null){stampMaterial=new Material(stampShader);AssetDatabase.CreateAsset(stampMaterial,Root+"/Wheel Stamp Batch.mat");}stampMaterial.SetTexture("_Tread",tread);
            var texture=AssetDatabase.LoadAssetAtPath<CustomRenderTexture>(Root+"/Tutorial Track Preview.renderTexture");
            if(texture==null){texture=new CustomRenderTexture(1024,1024,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);AssetDatabase.CreateAsset(texture,Root+"/Tutorial Track Preview.renderTexture");}
            texture.doubleBuffered=true;texture.updateMode=CustomRenderTextureUpdateMode.OnDemand;texture.initializationMode=CustomRenderTextureUpdateMode.OnDemand;texture.initializationSource=CustomRenderTextureInitializationSource.TextureAndColor;texture.initializationColor=Color.clear;texture.material=stampMaterial;texture.wrapMode=TextureWrapMode.Clamp;mud.SetTexture("_Stamp_Render_Texture_1",texture);
            var edge=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Mud bank.mat");
            if(edge==null){edge=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(edge,Root+"/Mud bank.mat");}edge.SetTexture("_BaseMap",baseMaterial.GetTexture("_Albedo"));edge.SetColor("_BaseColor",new Color(.56f,.45f,.3f));edge.SetFloat("_Smoothness",.22f);edge.SetTextureScale("_BaseMap",new Vector2(3,3));
            foreach(var asset in new UnityEngine.Object[]{mud,edge,stampMaterial,texture}){EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
            foreach(var t in environment.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Contract mud")).OrderByDescending(t=>t.childCount).ToArray())if(t!=null)Undo.DestroyObjectImmediate(t.gameObject);
            var patch=new GameObject("Contract mud - Living Mud",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));Undo.RegisterCreatedObjectUndo(patch,"Integrate Living Mud");patch.transform.SetParent(environment,false);patch.transform.localPosition=new Vector3(0,.12f,18);patch.layer=8;
            var mesh=CreateMudMesh();var stored=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Tutorial Mud Patch.asset");if(stored==null){stored=mesh;AssetDatabase.CreateAsset(stored,Root+"/Tutorial Mud Patch.asset");}else{EditorUtility.CopySerialized(mesh,stored);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(stored);AssetDatabase.SaveAssetIfDirty(stored);}
            patch.GetComponent<MeshFilter>().sharedMesh=stored;patch.GetComponent<MeshCollider>().sharedMesh=stored;patch.GetComponent<MeshRenderer>().sharedMaterials=new[]{mud,edge};
            var surface=patch.AddComponent<DynamicMudSurface>();surface.enabled=false;
            var settings=new SerializedObject(surface);settings.FindProperty("thickness").intValue=(int)MudType.Sloppy;settings.FindProperty("boundingBoxSizeInUnits").floatValue=20;settings.FindProperty("OverrideMaterialStampTexture").boolValue=false;settings.ApplyModifiedPropertiesWithoutUndo();
            ConfigureWheelCoating(world);
            patch.AddComponent<RecoveryMudSurface>().Configure(world,world.Drivers.Concat(new[]{world.Casualty}).ToArray(),surface,stampShader,tread);
            patch.SetActive(false);EditorUtility.SetDirty(patch);
            Selection.activeGameObject=patch;
        }
        [MenuItem("Tow Trouble/Fix Mud Alignment and Add Tire Coating")]
        public static void FixWheelsInTutorial()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before changing tire materials.");
            var scene=SceneManager.GetSceneByPath(DemoBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(DemoBuilder.ScenePath,OpenSceneMode.Additive);
            var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryWorld>(true)).Single();
            ConfigureWheelCoating(world);
            var patch=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryMudSurface>(true)).Single();var material=patch.GetComponent<Renderer>().sharedMaterial;material.SetFloat("_Flip_UV",1);EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        private static void ConfigureWheelCoating(RecoveryWorld world)
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(Vendor+"/Shaders/Texture with Mud.shadergraph");
            if(shader==null)throw new InvalidOperationException("Import Living Mud's tire coating shader first.");
            var mud=AssetDatabase.LoadAssetAtPath<Material>(Vendor+"/URP Materials/Dynamic Mud Surface Material/DMS Mud03 Material.mat");
            var derived=new System.Collections.Generic.Dictionary<Material,Material>();
            foreach(var vehicle in world.Drivers.Concat(new[]{world.Casualty}))
            {
                var data=new SerializedObject(vehicle);var models=data.FindProperty("models");var wheels=new System.Collections.Generic.HashSet<Renderer>();
                for(int model=0;model<models.arraySize;model++)
                {
                    var visuals=models.GetArrayElementAtIndex(model).FindPropertyRelative("Wheels");
                    for(int wheel=0;wheel<visuals.arraySize;wheel++)foreach(var renderer in ((Transform)visuals.GetArrayElementAtIndex(wheel).objectReferenceValue).GetComponentsInChildren<Renderer>(true))wheels.Add(renderer);
                }
                var slots=data.FindProperty("paintSlots");
                for(int slot=0;slot<slots.arraySize;slot++)
                {
                    var binding=slots.GetArrayElementAtIndex(slot);var renderer=(Renderer)binding.FindPropertyRelative("Renderer").objectReferenceValue;
                    if(!wheels.Contains(renderer))continue;
                    var colors=binding.FindPropertyRelative("Colors");
                    for(int color=0;color<colors.arraySize;color++)
                    {
                        var original=(Material)colors.GetArrayElementAtIndex(color).objectReferenceValue;
                        if(original==null||original.shader==shader)continue;
                        if(!derived.TryGetValue(original,out var material))
                        {
                            string path=Root+"/Tire_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original))+".mat";
                            material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
                            material.SetTexture("_Albedo_Map",original.HasProperty("_BaseMap")?original.GetTexture("_BaseMap"):original.mainTexture);
                            material.SetColor("_Tint",original.HasProperty("_BaseColor")?original.GetColor("_BaseColor"):Color.white);
                            if(original.HasProperty("_BumpMap"))material.SetTexture("_Normal_Map",original.GetTexture("_BumpMap"));
                            material.SetTexture("_Mud_Texture",mud.GetTexture("_Albedo"));material.SetColor("_Mud_Tint",new Color(.68f,.53f,.35f,1));material.SetFloat("_Mud_Amount",0);material.SetFloat("_Smoothness",.38f);
                            EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);derived[original]=material;
                        }
                        colors.GetArrayElementAtIndex(color).objectReferenceValue=material;
                    }
                    var materials=renderer.sharedMaterials;int materialIndex=binding.FindPropertyRelative("Slot").intValue;if(derived.TryGetValue(materials[materialIndex],out var current))materials[materialIndex]=current;renderer.sharedMaterials=materials;
                }
                data.ApplyModifiedPropertiesWithoutUndo();var coating=vehicle.GetComponent<RecoveryWheelMud>();if(coating==null)coating=Undo.AddComponent<RecoveryWheelMud>(vehicle.gameObject);coating.Configure(vehicle);EditorUtility.SetDirty(coating);EditorUtility.SetDirty(vehicle);
            }
        }

        private static Mesh CreateMudMesh()
        {
            const int count=81;var vertices=new Vector3[count*count];var uv=new Vector2[vertices.Length];var triangles=new[]{new System.Collections.Generic.List<int>(),new System.Collections.Generic.List<int>()};
            float Radius(float x,float z)=>Mathf.Sqrt(x*x/(7.8f*7.8f)+z*z/(9.6f*9.6f));
            for(int z=0;z<count;z++)for(int x=0;x<count;x++)
            {
                float px=Mathf.Lerp(-8.5f,8.5f,x/(float)(count-1)),pz=Mathf.Lerp(-10,10,z/(float)(count-1));
                float radius=Radius(px,pz);float rut=Mathf.Exp(-Mathf.Pow((Mathf.Abs(px)-1.1f)*3,2))*.038f;
                float y=-rut+(Mathf.PerlinNoise(px*.8f+7,pz*.6f+15)-.5f)*.018f;y-=Mathf.SmoothStep(0,.075f,Mathf.InverseLerp(.83f,1,radius));
                vertices[z*count+x]=new Vector3(px,y,pz);uv[z*count+x]=new Vector2(px/20+.5f,pz/20+.5f);
            }
            for(int z=0;z<count-1;z++)for(int x=0;x<count-1;x++)
            {
                int i=z*count+x;var center=(vertices[i]+vertices[i+1]+vertices[i+count]+vertices[i+count+1])*.25f;
                float radius=Radius(center.x,center.z);float edge=.975f+Mathf.Sin(center.x*1.7f+center.z*.8f)*.018f;
                if(radius>edge)continue;var t=triangles[radius>.85f?1:0];t.AddRange(new[]{i,i+count,i+1,i+1,i+count,i+count+1});
            }
            var mesh=new Mesh{name="Living Mud tutorial recovery patch"};mesh.vertices=vertices;mesh.uv=uv;mesh.subMeshCount=2;mesh.SetTriangles(triangles[0],0);mesh.SetTriangles(triangles[1],1);mesh.RecalculateNormals();mesh.bounds=new Bounds(Vector3.zero,new Vector3(20,.4f,20));return mesh;
        }
    }
}

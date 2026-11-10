using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble.Editor
{
    public static class WheelDebrisAuthoring
    {
        private const string Root="Assets/TowTrouble/Art/Mud/";
        [MenuItem("Tow Trouble/VFX/Configure Surface Wheel Debris")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryWorld>(true)).Single();
            var tracks=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryWheelTracks>(true)).Single();
            var mesh=Grain();
            var clods=Material("Wheel debris", "Universal Render Pipeline/Particles/Lit");
            var powder=Material("Wheel powder", "TowTrouble/Wheel Dust");
            foreach(var car in world.Drivers.Concat(new[]{world.Casualty}))
            {
                var coating=car.GetComponent<RecoveryWheelMud>();
                if(coating==null)throw new InvalidOperationException("Missing wheel coating on "+car.name);
                Undo.RecordObject(coating,"Configure surface wheel debris");
                var chunks=new ParticleSystem[4];var fines=new ParticleSystem[4];
                for(int i=0;i<4;i++)
                {
                    chunks[i]=Configure(car.transform,"Mud spray "+i,clods,mesh);
                    fines[i]=Configure(car.transform,"Wheel powder "+i,powder,null);
                }
                coating.ConfigureSplashes(chunks);coating.ConfigureDebris(tracks,fines);EditorUtility.SetDirty(coating);
            }
            AssetDatabase.SaveAssetIfDirty(clods);AssetDatabase.SaveAssetIfDirty(powder);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        private static Mesh Grain()
        {
            string path=Root+"Wheel debris grain.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
            // A small uneven, flat-shaded clod; avoids square blocks of snow or soil.
            var corners=new[]{new Vector3(-.5f,0,0),new Vector3(.55f,.05f,.04f),new Vector3(.03f,.35f,.01f),new Vector3(-.06f,-.4f,.05f),new Vector3(.03f,.04f,.4f),new Vector3(-.08f,-.02f,-.5f)};
            var faces=new[]{2,4,1,2,0,4,2,5,0,2,1,5,3,1,4,3,4,0,3,0,5,3,5,1};
            var vertices=new Vector3[faces.Length];var triangles=new int[faces.Length];
            for(int i=0;i<faces.Length;i++){vertices[i]=corners[faces[i]];triangles[i]=i;}
            mesh=new Mesh{name="Irregular wheel debris grain",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        private static Material Material(string name,string shaderName)
        {
            string path=Root+name+".mat";
            var shader=Shader.Find(shaderName);if(shader==null)throw new InvalidOperationException("Missing shader "+shaderName);
            var value=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(value==null)
            {
                value=new Material(shader){name=name};value.SetColor("_BaseColor",Color.white);
                if(value.HasProperty("_Smoothness"))value.SetFloat("_Smoothness",.05f);
                if(value.HasProperty("_Metallic"))value.SetFloat("_Metallic",0);
                AssetDatabase.CreateAsset(value,path);
            }
            if(value.shader!=shader)value.shader=shader;
            return value;
        }
        private static ParticleSystem Configure(Transform owner,string name,Material material,Mesh mesh)
        {
            var child=owner.Find(name);
            if(child==null){var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Add surface wheel powder");go.transform.SetParent(owner,false);child=go.transform;}
            child.gameObject.SetActive(false);var ps=child.GetComponent<ParticleSystem>();if(ps==null)ps=Undo.AddComponent<ParticleSystem>(child.gameObject);
            Undo.RecordObject(ps,"Configure wheel debris particles");ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.duration=1;main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;
            main.startSpeed=0;main.startLifetime=mesh!=null?.5f:.75f;main.startSize=mesh!=null?.08f:.18f;main.startColor=Color.white;
            main.gravityModifier=mesh!=null?1:.02f;main.maxParticles=mesh!=null?28:14;
            main.startRotation3D=mesh!=null;
            if(mesh!=null){main.startRotationX=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.startRotationY=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.startRotationZ=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);}
            else main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,mesh!=null?new AnimationCurve(new Keyframe(0,1),new Keyframe(.65f,1),new Keyframe(1,0)):AnimationCurve.Linear(0,.6f,1,1.8f));
            var color=ps.colorOverLifetime;color.enabled=mesh==null;
            if(mesh==null){var fade=new Gradient();fade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(.7f,.65f),new GradientAlphaKey(0,1)});color.color=fade;}
            var rotation=ps.rotationOverLifetime;rotation.enabled=mesh!=null;rotation.separateAxes=mesh!=null;
            if(mesh!=null){rotation.x=new ParticleSystem.MinMaxCurve(-5,5);rotation.y=new ParticleSystem.MinMaxCurve(-4,4);rotation.z=new ParticleSystem.MinMaxCurve(-5,5);}
            var renderer=ps.GetComponent<ParticleSystemRenderer>();Undo.RecordObject(renderer,"Bind shared wheel debris material");renderer.renderMode=mesh!=null?ParticleSystemRenderMode.Mesh:ParticleSystemRenderMode.Billboard;
            if(mesh!=null)renderer.mesh=mesh;renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;
            if(mesh==null)RecoveryWheelDebrisRules.ConfigurePowder(ps);
            EditorUtility.SetDirty(ps);EditorUtility.SetDirty(renderer);child.gameObject.SetActive(true);return ps;
        }
    }
}

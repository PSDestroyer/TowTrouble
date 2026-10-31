using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        [MenuItem("Tow Trouble/Art Integration/3 - Workshop Lighting and Collision Cleanup")]
        public static void IntegrateWorkshopAndLighting()
        {
            VisualContext(); ScenicPaths(); artRandom=new System.Random(72839);
            BuildWorkshopScenery();
            var parent=environment.Find("Central garage - Tow County");var yard=nodes["g"].Point;var center=yard+new Vector3(0,0,22);
            var red=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop painted boards.mat");red.SetColor("_BaseColor",new Color(.62f,.31f,.15f));EditorUtility.SetDirty(red);
            var concrete=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop concrete.mat");concrete.SetColor("_BaseColor",new Color(.48f,.48f,.42f));EditorUtility.SetDirty(concrete);
            var timber=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop aged timber.mat");timber.SetColor("_BaseColor",new Color(.54f,.38f,.21f));EditorUtility.SetDirty(timber);
            var trim=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop ivory trim.mat");var metal=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop roof metal.mat");
            var blue=ScenicMaterial("Workshop faded petrol blue",new Color(.17f,.35f,.37f));
            var glass=ScenicMaterial("Workshop blue window glass",new Color(.39f,.65f,.70f));glass.SetFloat("_Smoothness",.75f);
            var lamp=ScenicMaterial("Workshop warm enamel lamps",new Color(1,.88f,.62f));lamp.EnableKeyword("_EMISSION");lamp.SetColor("_EmissionColor",new Color(1,.71f,.35f)*1.7f);EditorUtility.SetDirty(lamp);
            foreach(float side in new[]{-1f,1f})
            {
                var p=center+new Vector3(side*9.2f,3.3f,1.5f);
                Cube("Workshop side window",p,new Vector3(.12f,1.75f,3.3f),glass,parent,false);
                foreach(float dz in new[]{-1.7f,0,1.7f})Cube("Window mullion",p+new Vector3(side*.08f,0,dz),new Vector3(.18f,1.9f,.09f),trim,parent,false);
                foreach(float dy in new[]{-.9f,.9f})Cube("Window surround",p+new Vector3(side*.08f,dy,0),new Vector3(.18f,.1f,3.55f),trim,parent,false);
                Cube("Workshop brick foundation",center+new Vector3(side*9.1f,.35f,0),new Vector3(.5f,.7f,14.3f),concrete,parent);
            }
            foreach(float z in new[]{-5f,0,5f})
            {
                Cube("Visible roof cross beam",center+new Vector3(0,5.35f,z),new Vector3(18,.24f,.22f),timber,parent,false);
                foreach(int side in new[]{-1,1}){var beam=Cube("Roof brace",center+new Vector3(side*4.3f,6.3f,z),new Vector3(9,.18f,.22f),trim,parent,false);beam.transform.rotation=Quaternion.Euler(0,0,-side*14);}
            }
            foreach(var l in parent.GetComponentsInChildren<Light>())UnityEngine.Object.DestroyImmediate(l.gameObject);
            foreach(float x in new[]{-4.8f,4.8f})
            {
                Cube("Ceiling work light",center+new Vector3(x,4.9f,0),new Vector3(2.3f,.13f,.7f),lamp,parent,false);
                var go=new GameObject("Workshop task spot",typeof(Light));go.transform.SetParent(parent,false);go.transform.position=center+new Vector3(x,4.65f,-1);go.transform.rotation=Quaternion.Euler(65,0,0);
                var light=go.GetComponent<Light>();light.type=LightType.Spot;light.spotAngle=110;light.innerSpotAngle=65;light.range=17;light.intensity=7;light.color=new Color(1,.86f,.64f);light.shadows=LightShadows.None;
                Cube("Service bay floor stripe",center+new Vector3(x,.055f,-2),new Vector3(4,.025f,.08f),trim,parent,false);
            }
            // Workshop supplies create scale and identity while the vehicle entrance stays clear.
            foreach(var offset in new[]{new Vector3(-12,0,14),new Vector3(-13.4f,0,15),new Vector3(12.1f,0,17)})
            {
                var p=Ground(yard+offset);
                var drum=GameObject.CreatePrimitive(PrimitiveType.Cylinder);drum.name="Workshop service drum";drum.transform.SetParent(parent,false);drum.transform.position=p+Vector3.up*.61f;drum.transform.localScale=new Vector3(.78f,.61f,.78f);drum.layer=8;drum.GetComponent<Renderer>().sharedMaterial=blue;
                foreach(float dy in new[]{.2f,1.02f}){var band=GameObject.CreatePrimitive(PrimitiveType.Cylinder);band.name="Drum metal band";band.transform.SetParent(parent,false);band.transform.position=p+Vector3.up*dy;band.transform.localScale=new Vector3(.82f,.026f,.82f);band.GetComponent<Renderer>().sharedMaterial=metal;UnityEngine.Object.DestroyImmediate(band.GetComponent<Collider>());}
            }
            for(int i=0;i<5;i++)
            {
                var p=Ground(yard+new Vector3(-13,0,23+i*.4f));Cube("Stored timber",p+Vector3.up*(.2f+(i%2)*.25f),new Vector3(3.3f,.18f,.28f),timber,parent,false);
            }
            var signPost=SafeSignPoint(yard,Vector3.back,26,1.7f);
            Cube("Service sign post",signPost+Vector3.up*1.7f,new Vector3(.16f,3.4f,.16f),timber,parent);
            Cube("Service information board",signPost+new Vector3(0,2.5f,-.08f),new Vector3(3.1f,1.5f,.16f),blue,parent,false);
            Text("SERVICE\nTOW COUNTY",signPost+new Vector3(0,2.5f,-.18f),0,.38f,parent);
            foreach(var r in environment.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Broken Vector - workshop approach"))r.enabled=false;
            // Retain the road-pack apron as a shallow, textured service surface with softened surroundings.
            var gravel=ScenicMaterial("Workshop road pack gravel",new Color(.70f,.61f,.43f));gravel.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SoStylized/Environment/Landscape/Textures/T_Dirt_BC.png"));gravel.SetTextureScale("_BaseMap",Vector2.one*4);EditorUtility.SetDirty(gravel);
            foreach(var t in environment.GetComponentsInChildren<Transform>().Where(t=>t.name=="Broken Vector - workshop approach"))foreach(var r in t.GetComponentsInChildren<Renderer>()){r.sharedMaterial=gravel;r.enabled=true;}
            // Dress the back of the building and the entrance, keeping a generous turning yard.
            foreach(var offset in new[]{new Vector3(-22,0,23),new Vector3(24,0,25),new Vector3(-26,0,36),new Vector3(23,0,38)})
            {
                var p=Ground(yard+offset);ArtPrefab("P_BirchTree1",p,Pick(10,13),parent,true,true);
                for(int i=0;i<5;i++)ArtPrefab(i%2==0?"P_Fern2_Yellow":"P_Grass2",Ground(p+new Vector3(Pick(-3,3),0,Pick(-3,3))),Pick(.35f,.7f),parent,false);
            }
            GradeSceneMaterials(); FinishLighting(); SimplifyEnvironmentColliders(); SaveVisualScene();
        }
        private static void FinishLighting()
        {
            var roots=SceneManager.GetActiveScene().GetRootGameObjects();
            var sun=roots.SelectMany(g=>g.GetComponentsInChildren<Light>()).First(l=>l.type==LightType.Directional);
            sun.transform.rotation=Quaternion.Euler(38,-145,0);sun.color=new Color(1,.94f,.82f);sun.intensity=1.65f;
            RenderSettings.ambientSkyColor=new Color(.57f,.68f,.79f);RenderSettings.ambientEquatorColor=new Color(.46f,.51f,.50f);RenderSettings.ambientGroundColor=new Color(.32f,.29f,.23f);
            RenderSettings.fogStartDistance=150;RenderSettings.fogEndDistance=1050;RenderSettings.fogColor=new Color(.66f,.77f,.82f);
            var volume=roots.SelectMany(g=>g.GetComponentsInChildren<Volume>()).First(v=>v.isGlobal);var profile=volume.sharedProfile;
            if(profile.TryGet<ColorAdjustments>(out var color)){color.postExposure.Override(.2f);color.contrast.Override(9);color.saturation.Override(-5);color.colorFilter.Override(new Color(1,.99f,.96f));EditorUtility.SetDirty(color);}
            if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.12f);EditorUtility.SetDirty(bloom);}
            EditorUtility.SetDirty(profile);DynamicGI.UpdateEnvironment();
        }
        private static void SimplifyEnvironmentColliders()
        {
            int removed=0;
            foreach(var lod in environment.GetComponentsInChildren<LODGroup>(true))
            {
                var colliders=lod.GetLODs().SelectMany(l=>l.renderers).Where(r=>r!=null).Distinct().Select(r=>r.GetComponent<MeshCollider>()).Where(c=>c!=null).Distinct().ToArray();
                for(int i=1;i<colliders.Length;i++){UnityEngine.Object.DestroyImmediate(colliders[i]);removed++;}
            }
            Debug.Log("[TowTrouble] Removed redundant LOD mesh colliders: "+removed);
        }
    }
}

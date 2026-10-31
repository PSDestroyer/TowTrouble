using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        [MenuItem("Tow Trouble/Visuals/2 - Dress Biomes Workshop and Lighting")]
        public static void PolishScenery()
        {
            VisualContext();ScenicPaths();
            foreach(string name in new[]{"Scenic biome details","County horizon"}){var old=environment.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            BuildWorkshopScenery();DressScenicBiomes();BuildHorizon();PolishWaterAndBridges();GradeScenicLighting();GradeSceneMaterials();SaveVisualScene();
        }
        private static Transform ScenicGroup(string name){var g=new GameObject(name).transform;g.SetParent(environment,false);return g;}
        private static Bounds ScenicBounds(GameObject go){var rs=go.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        private static GameObject ScenicPrefab(string name,Vector3 position,float height,float yaw,Transform parent,bool solid)
        {
            if(scenicPrefabs==null)scenicPrefabs=new Dictionary<string,GameObject>();if(!scenicPrefabs.TryGetValue(name,out var prefab)){string path=PrefabPath(name);prefab=path==null?null:AssetDatabase.LoadAssetAtPath<GameObject>(path);scenicPrefabs[name]=prefab;}if(prefab==null)return null;
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,yaw,0);var b=ScenicBounds(go);go.transform.localScale*=height/Mathf.Max(.1f,b.size.y);b=ScenicBounds(go);go.transform.position+=Vector3.up*(Ground(position).y-b.min.y-.08f);
            foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=8;
            if(solid){if(name.Contains("Tree")){b=ScenicBounds(go);var collider=go.AddComponent<CapsuleCollider>();collider.center=go.transform.InverseTransformPoint(new Vector3(position.x,Ground(position).y+height*.35f,position.z));collider.height=height*.7f/go.transform.lossyScale.y;collider.radius=Mathf.Max(.18f,height*.018f)/go.transform.lossyScale.x;}else{var mf=go.GetComponentsInChildren<MeshFilter>().FirstOrDefault(f=>f.sharedMesh!=null);if(mf!=null)mf.gameObject.AddComponent<MeshCollider>().sharedMesh=mf.sharedMesh;}}
            return go;
        }
        private static bool ScenicPlantAllowed(Vector3 p,float roadMargin,bool tree)
        {
            float d=ScenicDistance(p,out _,out var path);if(path!=null&&d<path.Width+roadMargin)return false;
            foreach(var node in plan.Nodes.Where(n=>n.Id=="g"||n.Id.Length==1||n.Id.StartsWith("R"))){float radius=node.Id=="g"?43:node.Id.Length==1?22:tree?15:10;if(Flat(p-node.Point).magnitude<radius)return false;}
            float u=p.x/900+.5f,v=.5f-p.z/900;if(LakeDistance(new Vector2(u,v))<5||p.y<12)return false;
            return terrain.terrainData.GetSteepness((p.x+500)/1000,(p.z+500)/1000)<(tree?29:39);
        }
        private static void DressScenicBiomes()
        {
            // Existing source formations that read as floating flat terraces are replaced by embedded boulders.
            var retained=environment.Find("Mega - retained environment");if(retained!=null)foreach(var child in retained.Cast<Transform>().ToArray())if(child.name.Contains("Platform")||child.name.Contains("Shelf")||child.name.Contains("Cliff"))UnityEngine.Object.DestroyImmediate(child.gameObject);
            if(retained!=null)foreach(var child in retained.Cast<Transform>().ToArray())
            {
                if(child.name.Contains("RockSpire")||child.name.Contains("RockHexic")){var b=ScenicBounds(child.gameObject);float target=12+Mathf.Repeat(Mathf.Abs(child.position.x)*.17f,8);if(b.size.y>target)child.localScale*=target/b.size.y;b=ScenicBounds(child.gameObject);child.position+=Vector3.up*(Ground(child.position).y-b.min.y-target*.22f);}
            }
            var oldLandmarks=environment.Find("World landmarks and river");if(oldLandmarks!=null)foreach(var child in oldLandmarks.Cast<Transform>().Where(t=>t.name.StartsWith("P_RockDesert")||t.name.StartsWith("P_Mountain")).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach(var tree in environment.GetComponentsInChildren<LODGroup>().Where(t=>t.name.Contains("FirTree")&&t.transform.parent.name!="Scenic biome details").ToArray()){var b=ScenicBounds(tree.gameObject);if(b.size.y>23){tree.transform.localScale*=23/b.size.y;b=ScenicBounds(tree.gameObject);tree.transform.position+=Vector3.up*(Ground(tree.transform.position).y-b.min.y);}}
            var root=ScenicGroup("Scenic biome details");var random=new System.Random(914263);Func<float> next=()=> (float)random.NextDouble();
            for(int i=0;i<1600;i++)
            {
                float u=.045f+next()*.91f,v=.04f+next()*.92f;var p=Ground(new Vector3((u-.5f)*900,0,(.5f-v)*900));bool tree=i<520,desert=u>.70f&&v>.34f,snowy=p.y>127&&v<.32f,shore=LakeDistance(new Vector2(u,v))<35||v>.87f&&u<.63f;
                if(!ScenicPlantAllowed(p,tree?5:1.5f,tree))continue;string name;float h;
                if(tree){if(shore||desert){if(next()<.62f)continue;name=desert?(next()<.5f?"P_Cactus05":"P_Cactus03"):"P_BoulderClassic2";h=desert?2.3f+next()*2:1.2f+next()*1.7f;}else if(snowy){name=next()<.5f?"P_OakTree2_Snow":"P_OakTree3_Snow";h=6+next()*5;}else{name=new[]{"P_BirchTree1","P_BirchTree3","P_OakTree2","P_FirTree4"}[random.Next(4)];h=7+next()*8;}}
                else if(desert){name=new[]{"P_DesertBush01","P_DesertTwigRoots02","P_GrassDesert1_Paintable","P_RockDesert_Clump09"}[random.Next(4)];h=.45f+next()*1.2f;}
                else if(snowy){name=next()<.7f?"P_BushSnowDead02":"P_BoulderClassic2";h=.45f+next()*1.1f;}
                else if(shore){name=next()<.65f?"P_Marshtail03":"P_BoulderClassic2";h=.5f+next()*.9f;}
                else{name=new[]{"P_Fern2","P_Bush1_Light","P_Grass2","P_Daisies","P_Foxtails1"}[random.Next(5)];h=.45f+next()*.8f;}
                ScenicPrefab(name,p,h,next()*360,root,tree);
            }
            // Deliberate shoulder clusters break up the road edge without filling the tow lane.
            foreach(var path in scenicRoads)for(int i=12;i<path.Points.Length-5;i+=15)
            {
                var p=path.Points[i];if(p.y>128||Flat(p-nodes["g"].Point).magnitude<45)continue;var direction=Flat(path.Points[i+1]-path.Points[i-1]).normalized;var side=Vector3.Cross(Vector3.up,direction)*(next()<.5f?-1:1);p=Ground(p+side*(path.Width+3+next()*3));if(!ScenicPlantAllowed(p,1.8f,false))continue;bool desert=p.x>180&&p.z<145;ScenicPrefab(desert?"P_DesertBush01":"P_Grass2",p,.35f+next()*.45f,next()*360,root,false);if(next()<.25f)ScenicPrefab(desert?"P_RockDesert_Layered01":"P_BoulderClassic2",p+side*2,.5f+next()*.6f,next()*360,root,true);
            }
            // A small set of landscape anchors, each clear of access roads and the mission pads.
            var anchors=new[]{new Vector3(300,0,100),new Vector3(380,0,-8),new Vector3(386,0,-300),new Vector3(250,0,-340),new Vector3(-180,0,235),new Vector3(135,0,250)};
            for(int i=0;i<anchors.Length;i++){var p=Ground(anchors[i]);if(ScenicDistance(p,out _,out _)<28)continue;var go=ScenicPrefab(i<4?new[]{"P_RockDesert_Rock03","P_RockDesert_Clump04"}[i%2]:"P_BoulderClassic3",p,i<4?13:9,i*71,root,true);if(go!=null)go.transform.position-=Vector3.up*2.5f;}
        }
        private static void BuildWorkshopScenery()
        {
            var old=environment.Find("Central garage - Tow County");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);var parent=ScenicGroup("Central garage - Tow County");var yard=nodes["g"].Point;var center=yard+new Vector3(0,0,22);
            var red=ScenicMaterial("Workshop painted boards",new Color(.31f,.13f,.085f));var dark=ScenicMaterial("Workshop roof metal",new Color(.09f,.145f,.16f));var timber=ScenicMaterial("Workshop aged timber",new Color(.32f,.24f,.15f));var trim=ScenicMaterial("Workshop ivory trim",new Color(.81f,.77f,.61f));var gold=ScenicMaterial("Workshop safety yellow",new Color(.91f,.52f,.12f));var concrete=ScenicMaterial("Workshop concrete",new Color(.3f,.31f,.28f));var rubber=ScenicMaterial("Workshop black rubber",new Color(.045f,.05f,.048f));
            Cube("Workshop slab",center+new Vector3(0,-.14f,0),new Vector3(21,.34f,18),concrete,parent);
            Cube("Rear wall",center+new Vector3(0,2.8f,7),new Vector3(18,5.6f,.35f),red,parent);foreach(float x in new[]{-9f,9f})Cube("Side wall",center+new Vector3(x,2.8f,0),new Vector3(.35f,5.6f,14),red,parent);
            for(int i=-8;i<=8;i++){Cube("Rear timber batten",center+new Vector3(i,2.8f,6.77f),new Vector3(.065f,5.6f,.1f),timber,parent,false);foreach(float x in new[]{-9.22f,9.22f})Cube("Siding batten",center+new Vector3(x,2.8f,i*.75f),new Vector3(.1f,5.6f,.06f),timber,parent,false);}
            float angle=18;foreach(int side in new[]{-1,1}){var roof=Cube("Pitched corrugated roof",center+new Vector3(side*4.8f,6.3f,0),new Vector3(10.2f,.2f,17),dark,parent);roof.transform.rotation=Quaternion.Euler(0,0,-side*angle);for(int z=-8;z<=8;z++){var rib=Cube("Roof seam",center+new Vector3(side*4.8f,6.43f,z),new Vector3(10.2f,.07f,.06f),dark,parent,false);rib.transform.rotation=roof.transform.rotation;}}
            Cube("Ridge cap",center+new Vector3(0,7.95f,0),new Vector3(.25f,.22f,17.2f),trim,parent,false);
            foreach(float x in new[]{-8.8f,0,8.8f}){Cube("Open bay pillar",center+new Vector3(x,2.7f,-7),new Vector3(.32f,5.4f,.32f),trim,parent);var baseBlock=Cube("Pillar safety base",center+new Vector3(x,.55f,-7),new Vector3(.5f,1.1f,.5f),gold,parent);}
            Cube("Front timber fascia",center+new Vector3(0,5.3f,-7),new Vector3(18.5f,.9f,.4f),timber,parent);
            var canopy=Cube("Service canopy",center+new Vector3(0,4.7f,-10),new Vector3(19,.18f,6.4f),dark,parent);canopy.transform.rotation=Quaternion.Euler(6,0,0);foreach(float x in new[]{-9f,9f})Cube("Canopy post",center+new Vector3(x,2.2f,-12.8f),new Vector3(.25f,4.4f,.25f),timber,parent);
            // The real logo is a scene-owned sign material; the source image remains untouched.
            var logo=ScenicMaterial("Workshop real logo",Color.white,Shader.Find("Universal Render Pipeline/Unlit"));logo.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TowTrouble/Art/UI/TowTrouble-logo.png"));logo.SetFloat("_AlphaClip",1);logo.SetFloat("_Cutoff",.2f);logo.EnableKeyword("_ALPHATEST_ON");logo.SetFloat("_Cull",0);logo.renderQueue=2450;EditorUtility.SetDirty(logo);
            var sign=GameObject.CreatePrimitive(PrimitiveType.Quad);sign.name="Tow Trouble enamel logo";sign.transform.SetParent(parent,false);sign.transform.position=center+new Vector3(0,6.15f,-7.35f);sign.transform.localScale=new Vector3(6.5f,3.1f,1);sign.GetComponent<Renderer>().sharedMaterial=logo;UnityEngine.Object.DestroyImmediate(sign.GetComponent<Collider>());
            foreach(float x in new[]{-5.6f,4.8f}){Cube("Mechanic workbench",center+new Vector3(x,1.05f,5.7f),new Vector3(4.4f,.22f,1.2f),timber,parent);foreach(float dx in new[]{-1.8f,1.8f})Cube("Workbench leg",center+new Vector3(x+dx,.5f,5.7f),new Vector3(.15f,1,.15f),dark,parent);Cube("Pegboard",center+new Vector3(x,2.65f,6.7f),new Vector3(4.3f,1.65f,.12f),dark,parent,false);for(int t=0;t<6;t++){var tool=Cube("Hanging tool",center+new Vector3(x-1.6f+t*.6f,2.7f,6.57f),new Vector3(.07f,.45f,.08f),trim,parent,false);tool.transform.rotation=Quaternion.Euler(0,0,t%2==0?15:-10);}}
            var chest=Cube("Rolling tool cabinet",center+new Vector3(-7.7f,.9f,2),new Vector3(1.8f,1.8f,.9f),red,parent);for(int i=0;i<5;i++)Cube("Tool drawer handle",center+new Vector3(-7.7f,.3f+i*.3f,1.53f),new Vector3(1.2f,.045f,.06f),trim,parent,false);
            var world=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryWorld>()).Single();var catalog=world.Drivers[0].GetComponent<RecoveryCustomization>().Catalog;var wheel=catalog.Find("tires_mud")?.Prefabs?.FirstOrDefault();
            Cube("Tire rack",center+new Vector3(7.7f,1.8f,2),new Vector3(1.5f,.15f,4.2f),timber,parent);if(wheel!=null)for(int i=0;i<4;i++){var copy=(GameObject)PrefabUtility.InstantiatePrefab(wheel,parent);copy.name="Hot Rod spare wheel";var b=ScenicBounds(copy);copy.transform.localScale*=1.25f/Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));copy.transform.rotation=Quaternion.Euler(0,90,0);copy.transform.position=center+new Vector3(7.7f,2.45f,.5f+i);foreach(var c in copy.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);}
            foreach(float x in new[]{-6f,6f}){Cube("Yard parking guide",yard+new Vector3(x,.08f,0),new Vector3(.12f,.06f,12),gold,parent,false);for(int i=0;i<3;i++)Cube("Bay chevron",yard+new Vector3(x*.85f,.11f,-4+i*3),new Vector3(.6f,.04f,1),gold,parent,false);}
            // Native road pack pieces remain in the apron and receive the new gravel material.
            int approachIndex=0;foreach(var t in environment.GetComponentsInChildren<Transform>().Where(t=>t.name=="Broken Vector - workshop approach")){var b=ScenicBounds(t.gameObject);var scale=t.localScale;t.localScale=new Vector3(scale.x*8/Mathf.Max(.01f,b.size.x),scale.y*.08f/Mathf.Max(.01f,b.size.y),scale.z*6/Mathf.Max(.01f,b.size.z));var p=yard+new Vector3(0,0,-approachIndex++*6);t.position=new Vector3(p.x,t.position.y,p.z);b=ScenicBounds(t.gameObject);t.position+=Vector3.up*(yard.y+.045f-b.max.y);foreach(var r in t.GetComponentsInChildren<Renderer>())r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/County compacted gravel.mat");}
            Text("SERVICE  /  RECOVERY",center+new Vector3(0,4.8f,-7.29f),0,.5f,parent);Text("GARAJ  [G]",yard+new Vector3(0,.16f,-3),90,.7f,parent);
            foreach(float x in new[]{-5.5f,5.5f}){var lamp=new GameObject("Workshop warm task light",typeof(Light));lamp.transform.SetParent(parent,false);lamp.transform.position=center+new Vector3(x,3.8f,-3);var light=lamp.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.8f,.52f);light.intensity=2.1f;light.range=9;light.shadows=LightShadows.None;Cube("Bay lamp bracket",center+new Vector3(x,4.4f,-7.5f),new Vector3(1.1f,.16f,.3f),dark,parent,false);Cube("Bay lamp lens",center+new Vector3(x,4.32f,-7.5f),new Vector3(.8f,.045f,.2f),trim,parent,false);}
        }
        private static void GradeScenicLighting()
        {
            var scene=SceneManager.GetActiveScene();var roots=scene.GetRootGameObjects();var sun=roots.SelectMany(g=>g.GetComponentsInChildren<Light>(true)).FirstOrDefault(l=>l.type==LightType.Directional);if(sun!=null){sun.transform.rotation=Quaternion.Euler(43,-32,0);sun.color=new Color(1,.9f,.73f);sun.intensity=1.25f;sun.shadows=LightShadows.Soft;sun.shadowBias=.035f;sun.shadowNormalBias=.3f;RenderSettings.sun=sun;}
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.47f,.59f,.69f);RenderSettings.ambientEquatorColor=new Color(.34f,.39f,.4f);RenderSettings.ambientGroundColor=new Color(.23f,.20f,.15f);RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=.7f;foreach(var probe in roots.SelectMany(g=>g.GetComponentsInChildren<ReflectionProbe>(true)))probe.enabled=false;
            var sky=ScenicMaterial("County afternoon sky",Color.white,Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.57f,.68f,.79f));sky.SetColor("_GroundColor",new Color(.42f,.42f,.36f));sky.SetFloat("_AtmosphereThickness",.9f);sky.SetFloat("_Exposure",1.05f);sky.SetFloat("_SunSize",.035f);RenderSettings.skybox=sky;EditorUtility.SetDirty(sky);DynamicGI.UpdateEnvironment();
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.62f,.72f,.77f);RenderSettings.fogStartDistance=180;RenderSettings.fogEndDistance=1250;
            var volume=roots.SelectMany(g=>g.GetComponentsInChildren<Volume>(true)).FirstOrDefault(v=>v.isGlobal);if(volume!=null)
            {
                string path=VisualRoot+"/County afternoon volume.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);if(profile==null){AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(volume.sharedProfile),path);profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);}volume.sharedProfile=profile;volume.name="Tow County afternoon grade";
                if(!profile.TryGet<Tonemapping>(out var tone))tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);
                if(!profile.TryGet<ColorAdjustments>(out var colors))colors=profile.Add<ColorAdjustments>();colors.postExposure.Override(.1f);colors.contrast.Override(6);colors.saturation.Override(-17);colors.colorFilter.Override(new Color(1,.98f,.94f));
                if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>();bloom.intensity.Override(.1f);bloom.threshold.Override(1.2f);bloom.scatter.Override(.65f);
                if(!profile.TryGet<Vignette>(out var vignette))vignette=profile.Add<Vignette>();vignette.intensity.Override(.11f);vignette.smoothness.Override(.5f);
                foreach(var component in profile.components)EditorUtility.SetDirty(component);EditorUtility.SetDirty(profile);
            }
            foreach(var camera in roots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true))){var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.requiresDepthTexture=true;data.requiresColorTexture=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;camera.farClipPlane=2300;}
        }
        [MenuItem("Tow Trouble/Visuals/4 - Refresh Native Clouds")]
        public static void RefreshNativeClouds()
        {
            VisualContext();var old=environment.Find("County cloud dome");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SoStylized/Environment/Sky/Prefabs/P_Sky.prefab").GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name.Contains("Clouds"));
            var go=new GameObject("County cloud dome",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(environment,false);go.transform.position=new Vector3(0,-110,0);go.transform.localScale=Vector3.one*3.2f;go.transform.rotation=source.transform.rotation;go.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=VisualAsset(new Material(source.sharedMaterial){name="County native clouds"},"County clouds.mat");renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;SaveVisualScene();
        }
        private static void GradeSceneMaterials()
        {
            var cache=new Dictionary<Material,Material>();foreach(var renderer in environment.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;bool changed=false;for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];if(source==null||!AssetDatabase.GetAssetPath(source).StartsWith("Assets/SoStylized/"))continue;
                    if(!cache.TryGetValue(source,out var owned))
                    {
                        owned=new Material(source){name="County "+source.name};bool foliage=source.name.Contains("Leaves")||source.name.Contains("Grass")||source.name.Contains("Bush")||source.name.Contains("Fern");
                        if(foliage)foreach(string property in new[]{"_Color","_Main_Color","_Second_Color","_Gradient_Color"})if(owned.HasProperty(property)){var c=owned.GetColor(property);Color.RGBToHSV(c,out float h,out float s,out float v);var color=Color.HSVToRGB(h,Mathf.Min(s*.63f,.56f),v*.83f);color.a=c.a;owned.SetColor(property,color);}
                        foreach(string property in new[]{"_Emission","_Emissive_Strength","_Moss_Emissive_Strength"})if(owned.HasProperty(property))owned.SetFloat(property,.015f);
                        owned=VisualAsset(owned,"Native "+source.name+".mat");cache[source]=owned;
                    }
                    materials[i]=owned;changed=true;
                }
                if(changed)renderer.sharedMaterials=materials;
            }
        }
        private static void BuildHorizon()
        {
            var parent=ScenicGroup("County horizon");var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();int count=160;float[] rings={0,70,230,550,1050};
            for(int r=0;r<rings.Length;r++)for(int i=0;i<count;i++){float angle=i/(float)count*Mathf.PI*2;var dir=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));float border=500/Mathf.Max(Mathf.Abs(dir.x),Mathf.Abs(dir.y));var edge=new Vector3(dir.x*border,0,dir.y*border);float baseH=Ground(edge).y;float radius=border+rings[r];float north=Mathf.Clamp01(dir.y);float h=baseH+(r==0?-.25f:Mathf.Sin(angle*7+r*.6f)*14+Mathf.Sin(angle*13)*8+north*(r==2?40:r==3?90:30)-r*8);vertices.Add(new Vector3(dir.x*radius,h,dir.y*radius));uv.Add(new Vector2(dir.x*radius*.012f,dir.y*radius*.012f));if(r<rings.Length-1){int a=r*count+i,b=r*count+(i+1)%count,c=a+count,d=b+count;triangles.AddRange(new[]{a,b,c,b,d,c});}}
            var mesh=new Mesh{name="County surrounding ridges"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var material=ScenicMaterial("County distant ridges",new Color(.39f,.43f,.38f));var go=new GameObject("Surrounding landscape - visual only",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=VisualAsset(mesh,"Horizon.asset");go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
    }
}

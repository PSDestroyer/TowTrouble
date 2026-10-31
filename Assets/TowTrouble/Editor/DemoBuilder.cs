using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TowTrouble.Editor
{
    public static partial class DemoBuilder
    {
        public const string ScenePath = "Assets/TowTrouble/Scenes/CanyonRecovery.unity";
        private const string Root = "Assets/TowTrouble";
        private const string Vendor = "Assets/SoStylized/Environment/";
        
        private static Material sand, grass, mud, rock, wood, dark, chrome, rubber, glass, lamp, stripe;
        private static TMP_FontAsset font;
        private static System.Random random;

        [MenuItem("Tow Trouble/Create or Rebuild Demo")]
        public static void CreateDemo()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding.");
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.isDirty) throw new InvalidOperationException("Save the current scene before rebuilding the demo.");
            foreach (var dir in new[] { "Scenes", "Art/Materials", "Art/Meshes", "Art/Audio", "Prefabs", "Settings" }) Directory.CreateDirectory(Root + "/" + dir);
            AssetDatabase.Refresh();
            random = new System.Random(6731);
            font = LoadFont();
            sand = Mat("Warm sand", new Color(.65f, .43f, .22f));
            grass = Mat("Sage meadow", new Color(.68f,.72f,.46f));
            grass.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Vendor+"Landscape/Textures/T_Grass_BC.png"));
            sand.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Vendor+"Landscape/Textures/T_Dirt_BC.png")); sand.SetColor("_BaseColor",new Color(.9f,.78f,.58f));
            mud = Mat("Wet mud", new Color(.19f, .115f, .06f), .45f);
            rock = Mat("Sandstone", new Color(.55f, .25f, .12f));
            wood = Mat("Old timber", new Color(.28f, .17f, .075f));
            dark = Mat("Charcoal", new Color(.07f, .085f, .08f), .25f);
            chrome = Mat("Brushed steel", new Color(.53f, .61f, .61f), .7f, .8f);
            rubber = Mat("Tire rubber", new Color(.035f, .042f, .046f), .13f);
            glass = Mat("Blue glass", new Color(.07f, .16f, .19f), .9f, .25f);
            lamp = Mat("Headlamp", new Color(1f, .85f, .45f), .8f);
            lamp.EnableKeyword("_EMISSION"); lamp.SetColor("_EmissionColor", new Color(1f, .72f, .25f) * 1.4f);
            stripe = Mat("Dispatch amber", new Color(1f, .54f, .08f));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            SetupLighting();
            var skyPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Vendor+"Sky/Prefabs/P_Sky.prefab");
            if(skyPrefab!=null){var skyGo=(GameObject)PrefabUtility.InstantiatePrefab(skyPrefab);skyGo.name="So Stylized sky and clouds";foreach(var l in skyGo.GetComponentsInChildren<Light>())l.enabled=false;}
            var probe=new GameObject("Recovery field reflection").AddComponent<ReflectionProbe>();probe.transform.position=new Vector3(0,3,25);probe.size=new Vector3(160,60,240);probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;probe.resolution=128;probe.intensity=1;
            var environment = new GameObject("Valea Verde - So Stylized").transform;
            ImportSoStylizedValley(environment); CreateGround(environment); CreateRiver(environment); DressWorld(environment); CreateWorkshop(environment);
            foreach(var t in environment.GetComponentsInChildren<Transform>(true))if(t.name.StartsWith("Contract mud"))t.gameObject.SetActive(false);
            var vehicles = new RecoveryVehicle[2];
            vehicles[0] = CreateCar("Scout", new Color(.93f, .84f, .58f), new Vector3(-2.3f, .05f, 30f), false);
            vehicles[1] = CreateCar("Hauler", new Color(.27f, .36f, .19f), new Vector3(2.5f, .05f, 30f), false);
            var target = CreateCar("Stranded Classic", new Color(.66f, .18f, .09f), new Vector3(-1f, .12f, 18f), true);
            var networkGo = new GameObject("FishNet - Session Network");
            networkGo.AddComponent<Tugboat>();
            var manager = networkGo.AddComponent<NetworkManager>();
            SetSerialized(manager, "_dontDestroyOnLoad", false);
            var prefabs = AssetDatabase.LoadAssetAtPath<SinglePrefabObjects>(Root + "/Settings/SceneOnlyPrefabs.asset");
            if (prefabs == null) { prefabs = ScriptableObject.CreateInstance<SinglePrefabObjects>(); AssetDatabase.CreateAsset(prefabs, Root + "/Settings/SceneOnlyPrefabs.asset"); }
            manager.SpawnablePrefabs = prefabs;
            var worldGo = new GameObject("Recovery - Server Authority");
            var networkObject = worldGo.AddComponent<NetworkObject>();
            var world = worldGo.AddComponent<RecoveryWorld>();
            var lines = new LineRenderer[2];
            for (int i = 0; i < 2; i++)
            {
                var lineGo = new GameObject("Tow cable " + (i + 1));
                var line = lineGo.AddComponent<LineRenderer>(); line.sharedMaterial = Mat("Cable steel", new Color(.08f, .1f, .11f), .5f, .5f);
                line.positionCount = 18; line.startWidth = line.endWidth = .055f; line.numCapVertices = 4; line.numCornerVertices = 3;
                line.enabled = false; lines[i] = line;
            }
            world.Configure(vehicles, target, lines);
            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
            cameraGo.tag = "MainCamera"; cameraGo.transform.position = new Vector3(15f, 8.7f, 39f);
            cameraGo.transform.LookAt(new Vector3(-1f, 1.1f, 22f));
            var camera = cameraGo.GetComponent<Camera>(); camera.fieldOfView = 57f; camera.farClipPlane = 650f; camera.nearClipPlane = .15f;
            cameraGo.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            cameraGo.GetComponent<UniversalAdditionalCameraData>().requiresColorTexture=true;cameraGo.GetComponent<UniversalAdditionalCameraData>().requiresDepthTexture=true;
            var rig = cameraGo.AddComponent<RecoveryCamera>();
            var sessionGo = new GameObject("Tow Trouble - Session");
            var session = sessionGo.AddComponent<DemoSession>(); session.Configure(manager, world, rig);
            var hud=sessionGo.AddComponent<RecoveryHud>();hud.Configure(session,font);
            hud.ConfigureLogo(AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/UI/TowTrouble-logo.png"));
            hud.ConfigureSkin(AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"));
            hud.ConfigureImages(new[]{AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/UI/Contract01.png"),AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/UI/Contract02.png"),AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/UI/Contract03.png")});
            UiSkinAuthoring.BindExisting(hud);
            world.ConfigureEnvironment(environment.gameObject);
            LivingMudAuthoring.ApplyToEnvironment(environment,world);
            networkObject.SetSceneId(9104737621UL);
            EditorUtility.SetDirty(networkObject); EditorUtility.SetDirty(manager);
            PlayerSettings.productName = "Tow Trouble";
            PlayerSettings.companyName = "Canyon County";
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = sessionGo;
            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(0, 0, 50), new Vector3(90, 30, 140)), false);
            Debug.Log("[TowTrouble] Demo authored: CanyonRecovery. Press Play to open dispatch.");
        }

        private static TMP_FontAsset LoadFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if(existing!=null){existing.atlasPopulationMode=AtlasPopulationMode.Dynamic;existing.TryAddCharacters("ăâîșțĂÂÎȘȚ→★✓□–",out string existingMissing);return existing;}
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();
            existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (existing == null) throw new InvalidOperationException("TMP essential font did not import.");
            existing.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            existing.TryAddCharacters("ăâîșțĂÂÎȘȚ→★✓□–",out string missing);
            return existing;
        }

        private static void SetupLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.55f, .68f, .78f);
            RenderSettings.ambientEquatorColor = new Color(.59f, .58f, .42f);
            RenderSettings.ambientGroundColor = new Color(.29f, .25f, .18f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .0037f; RenderSettings.fogColor = new Color(.65f, .76f, .79f);
            var sky = Mat("Canyon sky", Color.white);
            sky.shader = Shader.Find("Skybox/Procedural"); sky.SetColor("_SkyTint", new Color(.39f, .59f, .77f));
            sky.SetFloat("_AtmosphereThickness", .8f); sky.SetFloat("_Exposure", 1.1f); RenderSettings.skybox = sky;
            var light = new GameObject("Late afternoon sun").AddComponent<Light>();
            light.type = LightType.Directional; light.color = new Color(1f, .94f, .81f); light.intensity = 1.55f;
            light.shadows = LightShadows.Soft;light.shadowStrength=.65f; light.transform.rotation = Quaternion.Euler(42, -35, 0); RenderSettings.sun = light;
            var fill=new GameObject("Soft sky fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.35f;fill.color=new Color(.72f,.82f,1);fill.shadows=LightShadows.None;fill.transform.rotation=Quaternion.Euler(25,145,0);
            var volume = new GameObject("Canyon color grade").AddComponent<Volume>(); volume.isGlobal = true;
            string path = Root + "/Settings/CanyonVolume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path);
                var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
                var colors = profile.Add<ColorAdjustments>(true); colors.saturation.Override(8f); colors.contrast.Override(9f); colors.postExposure.Override(.25f);
                var bloom = profile.Add<Bloom>(true); bloom.intensity.Override(.15f); bloom.threshold.Override(1.25f);
                var vignette = profile.Add<Vignette>(true); vignette.intensity.Override(.17f);
                foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            }
            volume.sharedProfile = profile;
        }

        private static float Height(float x, float z)
        {
            float hills = (Mathf.PerlinNoise(x * .035f + 42f, z * .035f + 13f) - .5f) * 8f;
            float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(8f, 35f, Mathf.Abs(x)));
            float h = hills * edge;
            if (z > 48f && z < 64f) h -= Mathf.Sin((z - 48f) / 16f * Mathf.PI) * 2.4f;
            if(x < -26 && x>-112 && z>-60 && z<155) h-=Mathf.SmoothStep(0,5,Mathf.InverseLerp(-26,-40,x));
            if(x>-9 && x<-3 && z>9 && z<24)h-=.6f*Mathf.Sin((z-9)/15*Mathf.PI)*Mathf.Sin((x+9)/6*Mathf.PI);
            if(x<-112)h+=Mathf.SmoothStep(0,28,Mathf.InverseLerp(-112,-140,x));
            return h;
        }

        private static void CreateGround(Transform parent)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>();
            var triangles = new[] { new List<int>(), new List<int>() };
            for (int z = -10; z < 146; z += 2)
            for (int x = -12; x < 12; x += 2)
            {
                int index = vertices.Count;
                foreach (var v in new[] { new Vector3(x, Height(x, z), z), new Vector3(x, Height(x, z + 2), z + 2), new Vector3(x + 2, Height(x + 2, z + 2), z + 2), new Vector3(x + 2, Height(x + 2, z), z) })
                { vertices.Add(v); uv.Add(new Vector2(v.x * .1f, v.z * .1f)); }
                var list = triangles[Mathf.Abs(x + 1) < 8f ? 0 : 1];
                list.AddRange(new[] { index, index + 1, index + 2, index, index + 2, index + 3 });
            }
            var mesh = new Mesh { name = "Canyon ground", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.subMeshCount = 2;
            mesh.SetTriangles(triangles[0], 0); mesh.SetTriangles(triangles[1], 1); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = SaveMesh(mesh, "CanyonGround");
            var go = new GameObject("Sand road and meadow", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider)); go.layer = 8;
            go.transform.SetParent(parent);go.transform.localPosition=Vector3.up*.025f; go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshCollider>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterials = new[] { sand, grass };go.GetComponent<MeshRenderer>().enabled=false;
            CreateDirtRibbon(parent);
            Primitive("Contract tutorial grass",parent,PrimitiveType.Sphere,new Vector3(2,.05f,18),new Vector3(5,.035f,6),grass,false);
            Primitive("Contract mud", parent, PrimitiveType.Sphere, new Vector3(0, .015f, 18), new Vector3(14, .14f, 18), mud, false);
            var puddle = Mat("Muddy puddles", new Color(.27f, .2f, .1f), .9f);
            for (int i = 0; i < 20; i++)
            {
                float x = Range(-6f, 6f), z = Range(11, 25);
                Primitive("Contract mud puddle", parent, PrimitiveType.Sphere, new Vector3(x, -.005f, z), new Vector3(Range(.5f, 2.3f), .014f, Range(.4f, 1.3f)), puddle, false);
            }

        }

        private static void CreateDirtRibbon(Transform parent)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int z=-10;z<146;z+=2)
            {
                int start=vertices.Count;
                foreach(float zz in new[]{(float)z,z+2f})
                {
                    float left=-6.3f+Mathf.Sin(zz*.11f)*.35f,right=6.3f+Mathf.Sin(zz*.07f+2)*.45f;
                    vertices.Add(new Vector3(left,Height(left,zz)+.04f,zz));vertices.Add(new Vector3(right,Height(right,zz)+.04f,zz));uv.Add(new Vector2(left*.1f,zz*.1f));uv.Add(new Vector2(right*.1f,zz*.1f));
                }
                triangles.AddRange(new[]{start,start+2,start+3,start,start+3,start+1});
            }
            var mesh=new Mesh{name="Valley dirt trail"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=SaveMesh(mesh,"ValleyDirtTrail");
            var go=new GameObject("So Stylized dirt trail",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=sand;
        }

        private static void CreateRiver(Transform parent)
        {
            var water = Mat("Turquoise river", new Color(.055f, .48f, .53f), .85f, .2f);
            var authoredWater=AssetDatabase.LoadAssetAtPath<Material>(Vendor+"Water/Materials/M_StylizedWater.mat");
            if(authoredWater!=null)water=authoredWater;
            Primitive("So Stylized creek",parent,PrimitiveType.Plane,new Vector3(0,-.65f,56),new Vector3(5.4f,1,1.2f),water,false);

            var foam = Mat("Water glints", new Color(.56f, .86f, .79f), .6f);
            for (float z = 47.5f; z < 65; z += .72f)
                Primitive("Bridge plank", parent, PrimitiveType.Cube, new Vector3(0, .08f + Range(-.01f, .01f), z), new Vector3(10.6f, .3f, .67f), wood, true);
            foreach (float x in new[] { -5.3f, 5.3f })
            {
                for (int z = 48; z < 66; z += 4) Primitive("Bridge post", parent, PrimitiveType.Cube, new Vector3(x, .75f, z), new Vector3(.22f, 1.5f, .22f), wood, true);
                Primitive("Bridge rail", parent, PrimitiveType.Cube, new Vector3(x, 1.25f, 56), new Vector3(.18f, .15f, 18), wood, true);
            }
        }

        private static void DressWorld(Transform parent)
        {
            for(int z=35;z<112;z+=14)
                foreach(float x in new[]{-8f,8f})
                {
                    Primitive("Dispatch marker post",parent,PrimitiveType.Cylinder,new Vector3(x,.7f,z),new Vector3(.1f,.7f,.1f),dark,false);
                    Primitive("Amber route flag",parent,PrimitiveType.Cube,new Vector3(x+.3f,1.2f,z),new Vector3(.6f,.3f,.035f),stripe,false);
                }
            Sign("VALEA VERDE","RECUPERĂRI AUTO",new Vector3(-10,0,42),155,parent);
            Sign("PÂRÂUL","TRAGE ÎNCET. AJUNGE ACASĂ.",new Vector3(8,0,44),-15,parent);
            CreateRoadPack(parent);
        }

        private static void CreateRoadPack(Transform parent)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BrokenVector/LowPolyRoadPack/Prefabs/Road Straight 1.prefab");
            if(prefab==null)throw new InvalidOperationException("Required Low Poly Road Pack is missing.");
            var roadMat=Mat("Broken Vector - warm gravel",new Color(.58f,.43f,.26f));
            for(int i=0;i<3;i++)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.name="Broken Vector - workshop access road";
                var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                go.transform.localScale=new Vector3(12f/Mathf.Max(.1f,b.size.x),1,10f/Mathf.Max(.1f,b.size.z));
                go.transform.position=new Vector3(0,-b.min.y+.04f,99+i*10);
                foreach(var r in rs)r.sharedMaterials=r.sharedMaterials.Select(m=>roadMat).ToArray();
                foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=8;
            }
        }

        private static void CreateWorkshop(Transform parent)
        {
            var red = Mat("Workshop red", new Color(.42f, .12f, .075f));
            Primitive("Workshop", parent, PrimitiveType.Cube, new Vector3(16, 2.7f, 107), new Vector3(13, 5.4f, 16), red, true);
            var roof = Primitive("Workshop roof", parent, PrimitiveType.Cube, new Vector3(16, 5.6f, 107), new Vector3(14, .45f, 18), dark, true);
            roof.transform.rotation = Quaternion.Euler(0, 0, -7);
            Primitive("Garage opening", parent, PrimitiveType.Cube, new Vector3(16, 2.05f, 98.94f), new Vector3(9, 4.1f, .08f), dark, false);
            for (int i = 0; i < 10; i++) Primitive("Door slat", parent, PrimitiveType.Cube, new Vector3(16, .3f + i * .4f, 98.86f), new Vector3(8.8f, .035f, .1f), chrome, false);
            WorldText("VALEA VERDE", new Vector3(16, 4.75f, 98.65f), Quaternion.identity, 1f, 12, PaperColor(), parent);
            foreach (float x in new[] { -7f, 7f }) Primitive("Delivery boundary", parent, PrimitiveType.Cube, new Vector3(x, .04f, 108), new Vector3(.22f, .08f, 15), stripe, false);
            foreach (float z in new[] { 100.5f, 115.5f }) Primitive("Delivery boundary", parent, PrimitiveType.Cube, new Vector3(0, .04f, z), new Vector3(14, .08f, .22f), stripe, false);
            WorldText("RECOVERY BAY", new Vector3(0, .08f, 107), Quaternion.Euler(90, 0, 0), 1.2f, 12, new Color(1f, .69f, .2f), parent);
            Sign("WELCOME HOME", "SERVICIU DE RECUPERARE", new Vector3(-8, 0, 101), 0, parent);
            foreach(float x in new[]{11f,20f})
                Primitive("Timber crate",parent,PrimitiveType.Cube,new Vector3(x,.5f,94),Vector3.one,wood,true);
        }

        private static Color PaperColor() => new Color(1f, .88f, .62f);

        private static void Sign(string title, string subtitle, Vector3 position, float angle, Transform parent)
        {
            var sign = new GameObject(title).transform; sign.SetParent(parent); sign.position = position; sign.rotation = Quaternion.Euler(0, angle, 0);
            foreach (float x in new[] { -1.5f, 1.5f }) Primitive("Post", sign, PrimitiveType.Cube, new Vector3(x, 1.2f, 0), new Vector3(.16f, 2.4f, .16f), wood, false);
            Primitive("Board", sign, PrimitiveType.Cube, new Vector3(0, 2f, 0), new Vector3(4.2f, 1.25f, .16f), dark, false);
            var text = WorldText(title + "\n<size=45%>" + subtitle + "</size>", Vector3.zero, Quaternion.identity, .45f, 4f, PaperColor(), sign);
            text.transform.localRotation = Quaternion.identity;
            text.transform.localPosition = new Vector3(0, 2f, -.1f);
        }

        private static TextMeshPro WorldText(string value, Vector3 position, Quaternion rotation, float size, float width, Color color, Transform parent)
        {
            var go = new GameObject(value, typeof(RectTransform), typeof(TextMeshPro)); go.transform.SetParent(parent, false);
            go.transform.position = position; go.transform.rotation = rotation;
            var text = go.GetComponent<TextMeshPro>(); text.font = font; text.text = value; text.fontSize = size * 10; text.color = color;
            text.alignment = TextAlignmentOptions.Center; text.fontStyle = FontStyles.Bold; text.rectTransform.sizeDelta = new Vector2(width, 2f); return text;
        }

        private static GameObject PlaceAsset(string path, Vector3 position, float height, Transform parent, bool sandstone)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Vendor + path);
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition=Vector3.zero;
            var renderers = go.GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(); bool first = true;
            foreach (var r in renderers) { if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds); }
            float scale = height / Mathf.Max(.1f, bounds.size.y); go.transform.localScale *= scale;
            go.transform.position = position - Vector3.up * bounds.min.y * scale;
            go.transform.rotation = Quaternion.Euler(0, Range(0, 360), 0);
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                // Preserve the authored So Stylized URP shaders, textures and material variants.
                for(int i=0;i<materials.Length;i++) if(materials[i]==null) throw new InvalidOperationException("Missing material: "+path);
                renderer.sharedMaterials = materials;
            }
            foreach (var collider in go.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            return go;
        }

        private static RecoveryVehicle CreateCar(string name,Color color,Vector3 position,bool casualty)
        {
            var go=new GameObject(name);go.transform.position=position;
            var body=go.AddComponent<Rigidbody>();body.mass=casualty?950:1350;body.linearDamping=.08f;body.angularDamping=1.2f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var collision=go.AddComponent<BoxCollider>();collision.center=new Vector3(0,1.12f,0);collision.size=new Vector3(1.65f,1.4f,3.8f);
            var wheels=new WheelCollider[4];
            for(int i=0;i<4;i++){var w=new GameObject("Suspension "+i).AddComponent<WheelCollider>();w.transform.SetParent(go.transform,false);w.suspensionDistance=.23f;w.radius=.47f;w.enabled=false;wheels[i]=w;}
            string[] paths={"Assets/Hot_Rod_Oldtimers/Prefabs/Demo_Vehicles/Historic_Taxi.prefab","Assets/Hot_Rod_Oldtimers/Prefabs/Demo_Vehicles/Historic_Villager.prefab","Assets/Hot_Rod_Oldtimers/Prefabs/Demo_Vehicles/Historic_Bulldog.prefab","Assets/Hot_Rod_OffRoad/Prefabs/Demo_Vehicles/Goblin_Offroad.prefab"};
            var models=new RecoveryVehicleModel[paths.Length];var paintSlots=new List<RecoveryPaintSlot>();
            var paints=new[]{new Color(.98f,.93f,.74f),new Color(.43f,.48f,.24f),new Color(.9f,.6f,.12f),new Color(.38f,.43f,.2f)};
            for(int m=0;m<paths.Length;m++)
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(paths[m]);if(source==null)throw new InvalidOperationException("Required vehicle missing: "+paths[m]);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(source,go.transform);PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                model.name=RecoveryVehicle.ModelNames[m];model.transform.localPosition=Vector3.zero;model.transform.localScale=Vector3.one*1.25f;
                foreach(var c in model.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
                foreach(var a in model.GetComponentsInChildren<Animator>())UnityEngine.Object.DestroyImmediate(a);
                foreach(var t in model.GetComponentsInChildren<Transform>(true))if(t.name.Contains("Top_Sign"))t.gameObject.SetActive(false);
                foreach(var r in model.GetComponentsInChildren<Renderer>())
                {
                    var materials=r.sharedMaterials;
                    for(int slot=0;slot<materials.Length;slot++)
                    {
                        var original=materials[slot];if(original==null||original.name.Contains("Glass"))continue;
                        var variants=new Material[4];variants[0]=VehiclePaint(original,casualty?new Color(.46f,.19f,.08f):paints[m],casualty?"Rust":RecoveryVehicle.ModelNames[m]);
                        var palette=new[]{new Color(.98f,.93f,.74f),new Color(.43f,.48f,.24f),new Color(.95f,.65f,.16f)};
                        for(int c=1;c<4;c++)variants[c]=casualty?variants[0]:VehiclePaint(original,palette[c-1],"Paint"+c);
                        materials[slot]=variants[0];paintSlots.Add(new RecoveryPaintSlot{Renderer=r,Slot=slot,Colors=variants});
                    }
                    r.sharedMaterials=materials;
                }
                var parts=model.GetComponentsInChildren<Transform>(true);var visuals=new Transform[4];var anchors=new Vector3[4];float radius=.47f;
                string[] joints={"Wheel_Front_Left","Wheel_Front_Right","Wheel_Rear_Left","Wheel_Rear_Right"};
                for(int i=0;i<4;i++)
                {
                    var joint=parts.First(t=>t.name==joints[i]);
                    var rs=joint.GetComponentsInChildren<Renderer>();if(rs.Length>0)radius=rs[0].bounds.size.y*.5f;
                    var v=new GameObject("Suspended "+joints[i]).transform;v.SetParent(model.transform,false);v.position=joint.position;v.rotation=go.transform.rotation;
                    joint.SetParent(v,true);visuals[i]=v;anchors[i]=go.transform.InverseTransformPoint(v.position)+Vector3.up*.12f;
                }
                models[m]=new RecoveryVehicleModel{Root=model,Wheels=visuals,Anchors=anchors,Radius=radius,Mass=RecoveryVehicle.Masses[m],Drive=RecoveryVehicle.PullForces[m],Steering=RecoveryVehicle.SteeringAngles[m]};
                model.SetActive(m==0);
            }
            var front=new GameObject("Front tow eye").transform;front.SetParent(go.transform,false);front.localPosition=new Vector3(0,.66f,2.25f);
            var rear=new GameObject("Rear tow eye").transform;rear.SetParent(go.transform,false);rear.localPosition=new Vector3(0,.66f,-2.15f);
            foreach(var hitch in new[]{front,rear})Primitive("Tow eye",hitch,PrimitiveType.Sphere,Vector3.zero,new Vector3(.15f,.15f,.18f),chrome,false);
            var controller=go.AddComponent<RecoveryVehicle>();controller.Configure(wheels,models[0].Wheels,front,rear,casualty);controller.ConfigureModels(models);controller.ConfigurePaints(paintSlots.ToArray());
            if(name=="Hauler")controller.SetModel(1);
            var audio=go.AddComponent<AudioSource>();audio.clip=EngineClip();audio.loop=true;audio.playOnAwake=true;audio.spatialBlend=.8f;audio.minDistance=3;audio.maxDistance=45;audio.volume=.12f;
            AddDust(go.transform);PrefabUtility.SaveAsPrefabAssetAndConnect(go,Root+"/Prefabs/"+name.Replace(" ","")+".prefab",InteractionMode.AutomatedAction);return controller;
        }

        private static Material VehiclePaint(Material source,Color color,string label)
        {
            if(source==null||source.name.Contains("Glass"))return source;
            string path=Root+"/Art/Materials/HotRod_"+label+"_"+source.name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat!=null)return mat;
            mat=new Material(source);mat.name="HotRod "+label;AssetDatabase.CreateAsset(mat,path);
            var input=source.GetTexture("_BaseMap") as Texture2D;if(input==null)input=source.mainTexture as Texture2D;
            if(input!=null)
            {
                string texturePath=Root+"/Art/Materials/HotRod_"+label+"_"+input.name+".png";
                if(!File.Exists(texturePath))
                {
                    var rt=RenderTexture.GetTemporary(input.width,input.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;Graphics.Blit(input,rt);RenderTexture.active=rt;
                    var texture=new Texture2D(input.width,input.height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,input.width,input.height),0,0);texture.Apply();
                    var pixels=texture.GetPixels();for(int i=0;i<pixels.Length;i++){var p=pixels[i];if((p.r>p.g*1.15f&&p.r>p.b*1.08f&&p.r>.15f)||(input.name.Contains("OffRoad")&&p.g>p.r*1.08f&&p.g>p.b*1.12f)){float shade=Mathf.Clamp01(p.r/.68f);pixels[i]=new Color(color.r*shade,color.g*shade,color.b*shade,p.a);}}
                    texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(texturePath,texture.EncodeToPNG());RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(texturePath);
                }
                mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            }
            EditorUtility.SetDirty(mat);return mat;
        }

        private static AudioClip EngineClip()
        {
            string path = Root + "/Art/Audio/Engine.wav";
            if (!File.Exists(path))
            {
                const int rate=22050, count=22050; using var stream=new FileStream(path,FileMode.Create);using var writer=new BinaryWriter(stream);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                for(int i=0;i<count;i++){double t=(double)i/rate; double value=Math.Sin(t*70*Math.PI*2)*.45+Math.Sin(t*140*Math.PI*2)*.2+Math.Sin(t*210*Math.PI*2)*.1;writer.Write((short)(value*14000));}
            }
            AssetDatabase.ImportAsset(path); return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void AddDust(Transform parent)
        {
            var go = new GameObject("Road dust");go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(0,.35f,-1.8f);
            var particle=go.AddComponent<ParticleSystem>(); var main=particle.main;main.startLifetime=1f;main.startSpeed=.5f;main.startSize=.3f;main.startColor=new Color(.65f,.47f,.25f,.2f);main.maxParticles=60;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=particle.emission;emission.rateOverTime=0;var shape=particle.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(1.7f,.1f,.2f);
            var color=particle.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.68f,.47f,.24f),0)},new[]{new GradientAlphaKey(.18f,0),new GradientAlphaKey(0,1)});color.color=gradient;
            var size=particle.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1f,AnimationCurve.Linear(0,.3f,1,2));
            var material=Mat("Dust particles",new Color(.7f,.5f,.26f,.15f));material.shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.renderQueue=3000;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
        }

        private static Material Mat(string name, Color color, float smoothness=.12f, float metallic=0f)
        {
            string path=Root+"/Art/Materials/"+name.Replace("/","_")+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",smoothness);material.SetFloat("_Metallic",metallic);material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
        }

        private static Mesh SaveMesh(Mesh mesh,string name)
        {
            string path=Root+"/Art/Meshes/"+name+".asset";var known=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(known!=null){EditorUtility.CopySerialized(mesh,known);UnityEngine.Object.DestroyImmediate(mesh);return known;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }

        private static GameObject Primitive(string name,Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material material,bool collision)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            if(!collision)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());else go.layer=8;
            return go;
        }

        private static float Range(float min,float max)=>min+(float)random.NextDouble()*(max-min);
        private static void SetSerialized(UnityEngine.Object target,string property,bool value){var so=new SerializedObject(target);var p=so.FindProperty(property);if(p!=null){p.boolValue=value;so.ApplyModifiedPropertiesWithoutUndo();}}
    }
}

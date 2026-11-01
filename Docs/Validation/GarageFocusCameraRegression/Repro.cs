if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Edit Mode required");
var world=UnityEngine.Object.FindFirstObjectByType<TowTrouble.RecoveryWorld>();var sourceHud=UnityEngine.Object.FindFirstObjectByType<TowTrouble.RecoveryHud>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root=new GameObject("Static focus camera validation");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
var rt=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var rows=new System.Collections.Generic.List<object>();int assertions=0;
System.Action<bool,string> check=(condition,label)=>{assertions++;if(!condition)throw new System.Exception(label);};
try
{
 var dummy=new GameObject("Memory-only session");dummy.transform.SetParent(root.transform,false);var session=dummy.AddComponent<TowTrouble.DemoSession>();typeof(TowTrouble.DemoSession).GetField("world",flags).SetValue(session,world);
 var profile=TowTrouble.RecoveryProfile.Parse("");profile.credits=851;profile.vehicle=3;typeof(TowTrouble.DemoSession).GetField("<Profile>k__BackingField",flags).SetValue(session,profile);
 var camGo=new GameObject("Static camera",typeof(Camera),typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData),typeof(TowTrouble.RecoveryCamera));camGo.transform.SetParent(root.transform,false);var camera=camGo.GetComponent<Camera>();camera.enabled=false;camera.scene=preview;camera.targetTexture=rt;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.18f,.17f);camera.fieldOfView=57;camera.nearClipPlane=.1f;camera.farClipPlane=100;
 var controller=camGo.GetComponent<TowTrouble.RecoveryCamera>();controller.enabled=false;controller.Bind(session);
 var canvasGo=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));canvasGo.transform.SetParent(root.transform,false);var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
 var scaler=canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
 var holder=new GameObject("Kinematic geometry fixture");holder.transform.SetParent(root.transform,false);holder.SetActive(false);
 var carGo=UnityEngine.Object.Instantiate(world.Drivers[0].gameObject,holder.transform,false);var car=carGo.GetComponent<TowTrouble.RecoveryVehicle>();var custom=carGo.GetComponent<TowTrouble.RecoveryCustomization>();
 foreach(var component in carGo.GetComponentsInChildren<MonoBehaviour>(true))if(!(component is TowTrouble.RecoveryVehicle)&&!(component is TowTrouble.RecoveryCustomization)&&!(component is TowTrouble.RecoveryAssembly)&&!(component is TowTrouble.RecoveryVehicleEffects))UnityEngine.Object.DestroyImmediate(component);
 foreach(var ps in carGo.GetComponentsInChildren<ParticleSystem>(true))ps.gameObject.SetActive(false);
 carGo.GetComponent<Rigidbody>().isKinematic=true;carGo.GetComponent<Rigidbody>().detectCollisions=false;
 carGo.transform.position=new Vector3(10000,10000,10000);carGo.transform.rotation=Quaternion.Euler(0,180,0);carGo.transform.localScale=Vector3.one;holder.SetActive(true);custom.Configure(car,session.GarageCatalog);
 var sun=new GameObject("Preview sun",typeof(Light));sun.transform.SetParent(root.transform,false);sun.GetComponent<Light>().type=LightType.Directional;sun.GetComponent<Light>().intensity=2;sun.transform.rotation=Quaternion.Euler(40,-35,0);
 var fill=new GameObject("Preview fill",typeof(Light));fill.transform.SetParent(root.transform,false);fill.GetComponent<Light>().type=LightType.Directional;fill.GetComponent<Light>().intensity=.7f;fill.transform.rotation=Quaternion.Euler(30,150,0);
 var hud=dummy.AddComponent<TowTrouble.RecoveryHud>();foreach(string field in new[]{"font","displayFont","panelSprite"})typeof(TowTrouble.RecoveryHud).GetField(field,flags).SetValue(hud,typeof(TowTrouble.RecoveryHud).GetField(field,flags).GetValue(sourceHud));typeof(TowTrouble.RecoveryHud).GetField("session",flags).SetValue(hud,session);typeof(TowTrouble.RecoveryHud).GetField("root",flags).SetValue(hud,canvasGo.GetComponent<RectTransform>());

 car.SetModel(3);var build=profile.Build(3).Copy();var selected=session.GarageCatalog.Parts.Single(p=>p.Slot==TowTrouble.GarageSlot.Assembly&&p.EnglishName.Contains("Speedster")&&p.EnglishName.Contains("Electron"));build.Set(selected,session.GarageCatalog);carGo.transform.position=world.GaragePosition+Vector3.up;custom.Apply(build,0,true);
 var effects=carGo.GetComponent<TowTrouble.RecoveryVehicleEffects>();
 custom.GarageBounds(TowTrouble.GarageSlot.Assembly,selected.Id,out var before,out var beforeDetail);var beforeFrame=TowTrouble.RecoveryGarageView.Frame(TowTrouble.GarageSlot.Assembly,before,beforeDetail,57,16f/9);
 effects.Rebind();
 // Frozen conservative effect bounds: reproduces the mixed Renderer aggregation
 // without starting particles or advancing the simulation.
 foreach(var renderer in carGo.GetComponentsInChildren<ParticleSystemRenderer>(true))if(renderer.name=="Exhaust vapor")renderer.localBounds=new Bounds(Vector3.zero,new Vector3(24,24,24));
 custom.GarageBounds(TowTrouble.GarageSlot.Assembly,selected.Id,out var after,out var afterDetail);var afterFrame=TowTrouble.RecoveryGarageView.Frame(TowTrouble.GarageSlot.Assembly,after,afterDetail,57,16f/9);
 rows.Add(new{body=selected.EnglishName,before=before.ToString("F3"),after=after.ToString("F3"),beforeDistance=beforeFrame.Distance,afterDistance=afterFrame.Distance,renderers=carGo.GetComponentsInChildren<ParticleSystemRenderer>(true).Where(p=>p.gameObject.activeInHierarchy).Select(p=>new{p.name,bounds=p.localBounds.ToString("F3"),p.enabled,p.gameObject.activeInHierarchy,particles=p.GetComponent<ParticleSystem>().particleCount,playing=p.GetComponent<ParticleSystem>().isPlaying}).ToArray()});
}
finally {UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
var json=Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText("Docs/Validation/GarageFocusCameraRegression/Before.json",json);return json;

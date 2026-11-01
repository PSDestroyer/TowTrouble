if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Edit Mode required");
var world=UnityEngine.Object.FindFirstObjectByType<TowTrouble.RecoveryWorld>();var sourceHud=UnityEngine.Object.FindFirstObjectByType<TowTrouble.RecoveryHud>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root=new GameObject("Static focus camera validation");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
var rt=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var rows=new System.Collections.Generic.List<object>();int assertions=0;var hidden=new System.Collections.Generic.Dictionary<Renderer,bool>();
System.Action<bool,string> check=(condition,label)=>{assertions++;if(!condition)throw new System.Exception(label);};
try
{
 var dummy=new GameObject("Memory-only session");dummy.transform.SetParent(root.transform,false);var session=dummy.AddComponent<TowTrouble.DemoSession>();typeof(TowTrouble.DemoSession).GetField("world",flags).SetValue(session,world);
 var profile=TowTrouble.RecoveryProfile.Parse("");profile.credits=851;profile.vehicle=3;typeof(TowTrouble.DemoSession).GetField("<Profile>k__BackingField",flags).SetValue(session,profile);
 var camGo=new GameObject("Static camera",typeof(Camera),typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData),typeof(TowTrouble.RecoveryCamera));camGo.transform.SetParent(root.transform,false);var camera=camGo.GetComponent<Camera>();camera.enabled=false;camera.scene=preview;camera.targetTexture=rt;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.18f,.17f);camera.clearFlags=CameraClearFlags.Skybox;camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview)|UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(world.gameObject.scene);camera.fieldOfView=57;camera.nearClipPlane=.1f;camera.farClipPlane=100;
 var controller=camGo.GetComponent<TowTrouble.RecoveryCamera>();controller.enabled=false;controller.Bind(session);
 var canvasGo=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));canvasGo.transform.SetParent(root.transform,false);var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
 var scaler=canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
 var holder=new GameObject("Kinematic geometry fixture");holder.transform.SetParent(root.transform,false);holder.SetActive(false);
 var carGo=UnityEngine.Object.Instantiate(world.Drivers[0].gameObject,holder.transform,false);var car=carGo.GetComponent<TowTrouble.RecoveryVehicle>();var custom=carGo.GetComponent<TowTrouble.RecoveryCustomization>();
 foreach(var component in carGo.GetComponentsInChildren<MonoBehaviour>(true))if(!(component is TowTrouble.RecoveryVehicle)&&!(component is TowTrouble.RecoveryCustomization)&&!(component is TowTrouble.RecoveryAssembly)&&!(component is TowTrouble.RecoveryVehicleEffects))UnityEngine.Object.DestroyImmediate(component);
 foreach(var ps in carGo.GetComponentsInChildren<ParticleSystem>(true))ps.gameObject.SetActive(false);
 carGo.GetComponent<Rigidbody>().isKinematic=true;carGo.GetComponent<Rigidbody>().detectCollisions=false;
 carGo.transform.position=world.Drivers[0].transform.position;carGo.transform.rotation=Quaternion.Euler(0,180,0);carGo.transform.localScale=Vector3.one;holder.SetActive(true);custom.Configure(car,session.GarageCatalog);
 var sun=new GameObject("Preview sun",typeof(Light));sun.transform.SetParent(root.transform,false);sun.GetComponent<Light>().type=LightType.Directional;sun.GetComponent<Light>().intensity=0;sun.transform.rotation=Quaternion.Euler(40,-35,0);
 var fill=new GameObject("Preview fill",typeof(Light));fill.transform.SetParent(root.transform,false);fill.GetComponent<Light>().type=LightType.Directional;fill.GetComponent<Light>().intensity=0;fill.transform.rotation=Quaternion.Euler(30,150,0);
 var hud=dummy.AddComponent<TowTrouble.RecoveryHud>();foreach(string field in new[]{"font","displayFont","panelSprite"})typeof(TowTrouble.RecoveryHud).GetField(field,flags).SetValue(hud,typeof(TowTrouble.RecoveryHud).GetField(field,flags).GetValue(sourceHud));typeof(TowTrouble.RecoveryHud).GetField("session",flags).SetValue(hud,session);typeof(TowTrouble.RecoveryHud).GetField("root",flags).SetValue(hud,canvasGo.GetComponent<RectTransform>());
 foreach(var driver in world.Drivers)foreach(var renderer in driver.GetComponentsInChildren<Renderer>(true)){hidden[renderer]=renderer.forceRenderingOff;renderer.forceRenderingOff=true;}
 car.SetModel(3);var build=profile.Build(3).Copy();var speedster=session.GarageCatalog.Parts.Single(p=>p.Slot==TowTrouble.GarageSlot.Assembly&&p.EnglishName.Contains("Speedster")&&p.EnglishName.Contains("Electron"));build.Set(speedster,session.GarageCatalog);custom.Apply(build,0,true);
 var effects=carGo.GetComponent<TowTrouble.RecoveryVehicleEffects>();
 camera.transform.position=car.transform.TransformPoint(new Vector3(-9,4,12));
 var cameraPose=typeof(TowTrouble.RecoveryCamera).GetMethod("GarageCameraPose",flags);
 foreach(TowTrouble.GarageSlot slot in new[]{TowTrouble.GarageSlot.Assembly,TowTrouble.GarageSlot.FrontBumper,TowTrouble.GarageSlot.RearBumper})
 {
  // Apply twice to cover confirmation/replicated reapplication without profile writes or RPCs.
  custom.Apply(build,0,true);custom.Apply(build,0,true);effects.Rebind();
  foreach(var r in carGo.GetComponentsInChildren<ParticleSystemRenderer>(true))if(r.name=="Exhaust vapor")r.localBounds=new Bounds(Vector3.zero,Vector3.one*24);
  var partId=build.Item(slot,session.GarageCatalog);
  var content=new GameObject("Garage",typeof(RectTransform));content.transform.SetParent(canvasGo.transform,false);var rect=content.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;typeof(TowTrouble.RecoveryHud).GetField("pageRoot",flags).SetValue(hud,rect);
  typeof(TowTrouble.RecoveryHud).GetField("garageSlot",flags).SetValue(hud,slot);typeof(TowTrouble.RecoveryHud).GetField("garageSection",flags).SetValue(hud,slot==TowTrouble.GarageSlot.Cable||slot==TowTrouble.GarageSlot.Winch?2:slot==TowTrouble.GarageSlot.Wheels||slot==TowTrouble.GarageSlot.Engine||slot==TowTrouble.GarageSlot.Transmission||slot==TowTrouble.GarageSlot.Suspension||slot==TowTrouble.GarageSlot.Chassis?1:0);
  typeof(TowTrouble.RecoveryHud).GetField("garageDraft",flags).SetValue(hud,build);typeof(TowTrouble.RecoveryHud).GetField("garageSelected",flags).SetValue(hud,partId);typeof(TowTrouble.RecoveryHud).GetMethod("BuildGarage",flags).Invoke(hud,null);
  check(session.GarageFocusSlot==slot&&session.GarageFocusPart==partId,"UI category focus signal "+slot);
  custom.GarageBounds(slot,partId,out var body,out var detail);var frame=TowTrouble.RecoveryGarageView.Frame(slot,body,detail,57,16f/9);
  var direction=TowTrouble.RecoveryGarageView.Direction(frame.Yaw,frame.Elevation);
  var focus=car.transform.TransformPoint(frame.Focus);
  for(int step=0;step<180;step++)
  {
   var args=new object[]{car,0f,false,1f/60,Vector3.zero,Vector3.zero};cameraPose.Invoke(controller,args);var position=(Vector3)args[4];var look=(Vector3)args[5];
   camera.transform.SetPositionAndRotation(position,Quaternion.Slerp(camera.transform.rotation,Quaternion.LookRotation(look-position),1-Mathf.Exp(-7f/60)));
  }
  var local=car.transform.InverseTransformPoint(camera.transform.position);var viewport=camera.WorldToViewportPoint(focus);
  check(viewport.x>.35f&&viewport.x<.43f&&viewport.y>.4f&&viewport.y<.48f,"Open preview column "+slot);
  check(body.size.z>2&&body.size.x>1,"Actual visual body bounds "+slot);var expanded=body;expanded.Expand(.65f);check(!expanded.Contains(local),"Camera outside body "+slot);
  if(slot==TowTrouble.GarageSlot.FrontBumper)check(direction.z>0&&detail.center.z>body.center.z,"Front exterior");
  if(slot==TowTrouble.GarageSlot.RearBumper)check(direction.z<0&&detail.center.z<body.center.z,"Rear exterior");
  if(slot==TowTrouble.GarageSlot.Wheels)check(direction.x<0&&detail.size.y<1.5f,"Left front wheel face");
  Canvas.ForceUpdateCanvases();foreach(var text in content.GetComponentsInChildren<TMPro.TMP_Text>())text.ForceMeshUpdate(true,true);Canvas.ForceUpdateCanvases();camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();RenderTexture.active=old;System.IO.File.WriteAllBytes("Docs/Validation/GarageFocusCameraRegression/Stock-"+slot+".jpg",image.EncodeToJPG(90));
  rows.Add(new{slot=slot.ToString(),frame.Yaw,frame.Elevation,frame.Distance,actualDistance=Vector3.Distance(camera.transform.position,focus),bodySize=body.size.ToString("F3"),detailSize=detail.size.ToString("F3"),focus=detail.center.ToString("F3"),viewport=viewport.ToString("F3")});
  UnityEngine.Object.DestroyImmediate(content);((System.Collections.IDictionary)typeof(TowTrouble.RecoveryHud).GetField("textSizes",flags).GetValue(hud)).Clear();
 }

}
finally {foreach(var entry in hidden)if(entry.Key!=null)entry.Key.forceRenderingOff=entry.Value;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
System.IO.File.WriteAllText("Docs/Validation/GarageFocusCameraRegression/StockWorldChecks.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{assertions,rows},Newtonsoft.Json.Formatting.Indented));return new{assertions,captures=rows.Count};

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
 foreach(var component in carGo.GetComponentsInChildren<MonoBehaviour>(true))if(!(component is TowTrouble.RecoveryVehicle)&&!(component is TowTrouble.RecoveryCustomization)&&!(component is TowTrouble.RecoveryAssembly))UnityEngine.Object.DestroyImmediate(component);
 foreach(var ps in carGo.GetComponentsInChildren<ParticleSystem>(true))ps.gameObject.SetActive(false);
 carGo.GetComponent<Rigidbody>().isKinematic=true;carGo.GetComponent<Rigidbody>().detectCollisions=false;
 carGo.transform.position=new Vector3(10000,10000,10000);carGo.transform.rotation=Quaternion.Euler(0,180,0);carGo.transform.localScale=Vector3.one;holder.SetActive(true);custom.Configure(car,session.GarageCatalog);
 var sun=new GameObject("Preview sun",typeof(Light));sun.transform.SetParent(root.transform,false);sun.GetComponent<Light>().type=LightType.Directional;sun.GetComponent<Light>().intensity=2;sun.transform.rotation=Quaternion.Euler(40,-35,0);
 var fill=new GameObject("Preview fill",typeof(Light));fill.transform.SetParent(root.transform,false);fill.GetComponent<Light>().type=LightType.Directional;fill.GetComponent<Light>().intensity=.7f;fill.transform.rotation=Quaternion.Euler(30,150,0);
 var hud=dummy.AddComponent<TowTrouble.RecoveryHud>();foreach(string field in new[]{"font","displayFont","panelSprite"})typeof(TowTrouble.RecoveryHud).GetField(field,flags).SetValue(hud,typeof(TowTrouble.RecoveryHud).GetField(field,flags).GetValue(sourceHud));typeof(TowTrouble.RecoveryHud).GetField("session",flags).SetValue(hud,session);typeof(TowTrouble.RecoveryHud).GetField("root",flags).SetValue(hud,canvasGo.GetComponent<RectTransform>());
 car.SetModel(3);var build=profile.Build(3).Copy();custom.Apply(build,0,true);
 foreach(TowTrouble.GarageSlot slot in System.Enum.GetValues(typeof(TowTrouble.GarageSlot)))
 {
  var partId=build.Item(slot,session.GarageCatalog);
  var content=new GameObject("Garage",typeof(RectTransform));content.transform.SetParent(canvasGo.transform,false);var rect=content.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;typeof(TowTrouble.RecoveryHud).GetField("pageRoot",flags).SetValue(hud,rect);
  typeof(TowTrouble.RecoveryHud).GetField("garageSlot",flags).SetValue(hud,slot);typeof(TowTrouble.RecoveryHud).GetField("garageSection",flags).SetValue(hud,slot==TowTrouble.GarageSlot.Cable||slot==TowTrouble.GarageSlot.Winch?2:slot==TowTrouble.GarageSlot.Wheels||slot==TowTrouble.GarageSlot.Engine||slot==TowTrouble.GarageSlot.Transmission||slot==TowTrouble.GarageSlot.Suspension||slot==TowTrouble.GarageSlot.Chassis?1:0);
  typeof(TowTrouble.RecoveryHud).GetField("garageDraft",flags).SetValue(hud,build);typeof(TowTrouble.RecoveryHud).GetField("garageSelected",flags).SetValue(hud,partId);typeof(TowTrouble.RecoveryHud).GetMethod("BuildGarage",flags).Invoke(hud,null);
  check(session.GarageFocusSlot==slot&&session.GarageFocusPart==partId,"UI category focus signal "+slot);
  custom.GarageBounds(slot,partId,out var body,out var detail);var frame=TowTrouble.RecoveryGarageView.Frame(slot,body,detail,57,16f/9);
  var direction=TowTrouble.RecoveryGarageView.Direction(frame.Yaw,frame.Elevation);var local=TowTrouble.RecoveryGarageView.OutsideBody(frame.Focus+direction*frame.Distance,body);
  var focus=car.transform.TransformPoint(frame.Focus);var position=car.transform.TransformPoint(local);
  TowTrouble.RecoveryGarageView.Pose(focus,(position-focus).normalized,Vector3.Distance(position,focus),57,16f/9,out position,out var look);
  camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(look-position));var viewport=camera.WorldToViewportPoint(focus);
  check(viewport.x>.35f&&viewport.x<.43f&&viewport.y>.4f&&viewport.y<.48f,"Open preview column "+slot);
  check(body.size.z>2&&body.size.x>1,"Actual visual body bounds "+slot);var expanded=body;expanded.Expand(.65f);check(!expanded.Contains(local),"Camera outside body "+slot);
  if(slot==TowTrouble.GarageSlot.FrontBumper)check(direction.z>0&&detail.center.z>body.center.z,"Front exterior");
  if(slot==TowTrouble.GarageSlot.RearBumper)check(direction.z<0&&detail.center.z<body.center.z,"Rear exterior");
  if(slot==TowTrouble.GarageSlot.Wheels)check(direction.x<0&&detail.size.y<1.5f,"Left front wheel face");
  Canvas.ForceUpdateCanvases();foreach(var text in content.GetComponentsInChildren<TMPro.TMP_Text>())text.ForceMeshUpdate(true,true);Canvas.ForceUpdateCanvases();camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();RenderTexture.active=old;System.IO.File.WriteAllBytes("Docs/Validation/GarageFocusCamera/"+slot+".png",image.EncodeToPNG());
  rows.Add(new{slot=slot.ToString(),frame.Yaw,frame.Elevation,frame.Distance,bodySize=body.size.ToString("F3"),detailSize=detail.size.ToString("F3"),focus=detail.center.ToString("F3"),viewport=viewport.ToString("F3")});
  UnityEngine.Object.DestroyImmediate(content);((System.Collections.IDictionary)typeof(TowTrouble.RecoveryHud).GetField("textSizes",flags).GetValue(hud)).Clear();
 }
 // Invoke only the presentation helper, with explicit unscaled deltas; no PlayerLoop or physics simulation.
 var pose=typeof(TowTrouble.RecoveryCamera).GetMethod("GarageCameraPose",flags);camera.transform.position=car.transform.TransformPoint(new Vector3(-9,4,12));
 foreach(var slot in new[]{TowTrouble.GarageSlot.FrontBumper,TowTrouble.GarageSlot.RearBumper,TowTrouble.GarageSlot.Wheels})
 {
  session.FocusGarage(slot,build.Item(slot,session.GarageCatalog));custom.GarageBounds(slot,session.GarageFocusPart,out var body,out var detail);var expanded=body;expanded.Expand(.65f);
  for(int i=0;i<180;i++){var args=new object[]{car,0f,false,1f/60,Vector3.zero,Vector3.zero};pose.Invoke(controller,args);camera.transform.position=(Vector3)args[4];check(!expanded.Contains(car.transform.InverseTransformPoint(camera.transform.position)),"Circular transition outside vehicle "+slot+" "+i);}
  check(Mathf.Abs((float)typeof(TowTrouble.RecoveryCamera).GetField("garageYaw",flags).GetValue(controller)-TowTrouble.RecoveryGarageView.Frame(slot,body,detail,57,16f/9).Yaw)<.1f,"Paused/unscaled transition converges "+slot);
 }
 session.FocusGarage(TowTrouble.GarageSlot.Wheels,build.Item(TowTrouble.GarageSlot.Wheels,session.GarageCatalog));var manualArgs=new object[]{car,32f,false,1f/60,Vector3.zero,Vector3.zero};pose.Invoke(controller,manualArgs);check(Mathf.Abs((float)typeof(TowTrouble.RecoveryCamera).GetField("garageManualOrbit",flags).GetValue(controller)-32)<.01,"Manual orbit accepted");
 session.FocusGarage(TowTrouble.GarageSlot.Wheels,"different-preview-id");manualArgs[1]=0f;pose.Invoke(controller,manualArgs);check(Mathf.Abs((float)typeof(TowTrouble.RecoveryCamera).GetField("garageManualOrbit",flags).GetValue(controller)-32)<.01,"Same-category preview retains manual orbit");
 session.FocusGarage(TowTrouble.GarageSlot.RearBumper,build.Item(TowTrouble.GarageSlot.RearBumper,session.GarageCatalog));pose.Invoke(controller,manualArgs);check(Mathf.Abs((float)typeof(TowTrouble.RecoveryCamera).GetField("garageManualOrbit",flags).GetValue(controller))<.01,"New category resets manual offset");
}
finally {UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
System.IO.File.WriteAllText("Docs/Validation/GarageFocusCamera/Checks.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{assertions,rows},Newtonsoft.Json.Formatting.Indented));return new{assertions,captures=rows.Count};

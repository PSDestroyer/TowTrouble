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

 car.SetModel(3);
 foreach(var bodyPart in session.GarageCatalog.Parts.Where(p=>p.Slot==TowTrouble.GarageSlot.Assembly))
 {
  var build=profile.Build(3).Copy();build.Set(bodyPart,session.GarageCatalog);custom.Apply(build,0,true);
  foreach(var slot in new[]{TowTrouble.GarageSlot.Assembly,TowTrouble.GarageSlot.FrontBumper,TowTrouble.GarageSlot.RearBumper,TowTrouble.GarageSlot.Wheels,TowTrouble.GarageSlot.Roof})
  {
   custom.GarageBounds(slot,build.Item(slot,session.GarageCatalog),out var body,out var detail);var frame=TowTrouble.RecoveryGarageView.Frame(slot,body,detail,57,16f/9);
   var local=TowTrouble.RecoveryGarageView.OutsideBody(frame.Focus+TowTrouble.RecoveryGarageView.Direction(frame.Yaw,frame.Elevation)*frame.Distance,body);var focus=car.transform.TransformPoint(frame.Focus);var position=car.transform.TransformPoint(local);
   TowTrouble.RecoveryGarageView.Pose(focus,(position-focus).normalized,Vector3.Distance(position,focus),57,16f/9,out position,out var look);camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(look-position));var vp=camera.WorldToViewportPoint(focus);
   check(vp.x>.35f&&vp.x<.43f&&vp.y>.4f&&vp.y<.48f,"Part focus outside UI column "+bodyPart.Id+" "+slot);
   check(body.size.z>2&&body.size.x>1,"Missing body geometry "+bodyPart.Id);var expanded=body;expanded.Expand(.65f);check(!expanded.Contains(local),"Camera inside body "+bodyPart.Id);
   var min=Vector2.one;var max=Vector2.zero;
   for(int i=0;i<8;i++){var corner=detail.center+Vector3.Scale(detail.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var p=camera.WorldToViewportPoint(car.transform.TransformPoint(corner));min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
   rows.Add(new{body=bodyPart.EnglishName,slot=slot.ToString(),frame.Distance,size=body.size.ToString("F3"),min=min.ToString("F3"),max=max.ToString("F3"),fits=min.x>.222f&&max.x<.56f&&min.y>.277f&&max.y<.614f});
  }
 }

}
finally {UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
System.IO.File.WriteAllText("Docs/Validation/GarageFocusCamera/BodyVariants.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{assertions,rows},Newtonsoft.Json.Formatting.Indented));return new{assertions,variants=rows.Count};

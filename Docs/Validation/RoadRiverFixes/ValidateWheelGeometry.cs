if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit Mode required");
var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();var env=terrain.transform.parent;var host=new GameObject("Temporary wheel geometry preview");
var mats=new UnityEditor.SerializedObject(env.GetComponentInChildren<TowTrouble.RecoveryWheelTracks>()).FindProperty("materials");
var chunkType=typeof(TowTrouble.RecoveryWheelTracks).GetNestedType("Chunk",System.Reflection.BindingFlags.NonPublic);var chunk=System.Activator.CreateInstance(chunkType,new object[]{host.transform,mats.GetArrayElementAtIndex(0).objectReferenceValue});
var road=env.GetComponentsInChildren<MeshCollider>().First(c=>c.name=="Road-21-g-centerEast");var vs=road.sharedMesh.vertices;int middle=vs.Length/4;middle=Mathf.Clamp(middle,20,vs.Length/2-22);
for(int side=-1;side<=1;side+=2)for(int i=middle-15;i<middle+15;i++)
{
    var a=(vs[i*2]+vs[i*2+1])*.5f;var b=(vs[(i+1)*2]+vs[(i+1)*2+1])*.5f;var right=Vector3.Cross(Vector3.up,(b-a).normalized).normalized;
    a+=right*(side*.82f+.5f*Mathf.Sin(i*.12f));b+=right*(side*.82f+.5f*Mathf.Sin((i+1)*.12f));
    chunkType.GetMethod("Append").Invoke(chunk,new object[]{a,b,terrain,0,i*.9f,(i+1)*.9f});
}
chunkType.GetMethod("Upload").Invoke(chunk,null);var mesh=host.GetComponentInChildren<MeshFilter>().sharedMesh;var positions=mesh.vertices;bool finite=positions.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z));int count=(int)chunkType.GetField("Count").GetValue(chunk);
var center=(vs[middle*2]+vs[middle*2+1])*.5f;var forward=((vs[(middle+1)*2]+vs[(middle+1)*2+1])*.5f-center).normalized;var sideway=Vector3.Cross(Vector3.up,forward);
var camGo=new GameObject("Temporary track review camera");var camera=camGo.AddComponent<Camera>();camera.enabled=false;camera.farClipPlane=1600;camera.nearClipPlane=.15f;camera.fieldOfView=55;camera.transform.position=center-forward*10+sideway*6+Vector3.up*4;camera.transform.LookAt(center+forward*2);
var urp=camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();urp.renderPostProcessing=true;urp.requiresDepthTexture=true;urp.requiresColorTexture=true;
var rt=new RenderTexture(1440,900,24);var tex=new Texture2D(1440,900,TextureFormat.RGB24,false);camera.targetTexture=rt;
try{camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();RenderTexture.active=old;System.IO.File.WriteAllBytes("Docs/Validation/RoadRiverFixes/StaticWheelImpressions.png",tex.EncodeToPNG());}
finally{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(camGo);UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
var passed=new System.Collections.Generic.List<string>();foreach(string typeName in new[]{"TowTrouble.Tests.RecoveryWheelTracksTests","TowTrouble.Tests.RecoveryGroundMapTests"}){var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(typeName)).First(t=>t!=null);var test=System.Activator.CreateInstance(type);foreach(var method in type.GetMethods().Where(m=>m.GetCustomAttributes(false).Any(a=>a.GetType().Name=="TestAttribute"))){method.Invoke(test,null);passed.Add(method.Name);}}
var report=new{playMode=false,simulation=false,geometrySegments=count,finite,pooledVertexCapacity=positions.Length,passed};System.IO.File.WriteAllText("Docs/Validation/RoadRiverFixes/WheelChecks.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;

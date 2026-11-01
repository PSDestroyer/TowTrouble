using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static void PolishCountyGeology(Transform root)
        {
            var group=PolishGroup(root,"Embedded geology and talus");var source=environment.Find("Authored gorge and mountain geology");int fixedMeshes=0;
            foreach(var material in source.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&m.HasProperty("_Rock_Texture")&&AssetDatabase.GetAssetPath(m).StartsWith(VisualRoot+"/")).Distinct())
            {
                material.shader=Shader.Find("TowTrouble/Alpine Cliff");material.SetTexture("_Rock_Texture",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SoStylized/Environment/Rocks/Textures/Classic/T_RockClassic_BC.PNG"));material.SetTexture("_Rock_Normal_Texture",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SoStylized/Environment/Rocks/Textures/Classic/T_RockClassicCliffs_N.PNG"));material.SetTexture("_Snow_Texture",terrain.terrainData.terrainLayers[4].diffuseTexture);material.SetColor("_Snow_Tint",new Color(.92f,.96f,1));material.SetFloat("_MaskTopLayer",0);EditorUtility.SetDirty(material);
            }
            foreach(var f in source.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Massif")))
            {
                string originalPath=PolishFolder+"/Baseline "+f.name.Substring(0,10)+".asset";
                if(AssetDatabase.LoadAssetAtPath<Mesh>(originalPath)==null)AssetDatabase.CopyAsset(VisualRoot+"/Gorge rock "+f.name.Substring(7,3)+".asset",originalPath);
                var original=AssetDatabase.LoadAssetAtPath<Mesh>(originalPath);var bounds=original.bounds;
                var native=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("P_CliffClassic"+(int.Parse(f.name.Substring(7,3))%6+1))).GetComponentsInChildren<MeshFilter>().First(m=>m.sharedMesh!=null).sharedMesh;
                var mesh=UnityEngine.Object.Instantiate(native);mesh.name=f.name.Substring(0,10)+" refined";var nativeBounds=native.bounds;var vs=mesh.vertices;var nativeVertices=native.vertices;
                var centre=f.transform.TransformPoint(bounds.center);float height=Mathf.Clamp(bounds.size.y*.68f,9,23);
                var local=centre-terrain.transform.position;var surfaceNormal=terrain.terrainData.GetInterpolatedNormal(local.x/terrain.terrainData.size.x,local.z/terrain.terrainData.size.z);var outward=Flat(surfaceNormal);if(outward.sqrMagnitude<.01f)outward=Vector3.back;outward.Normalize();var orientation=Quaternion.LookRotation(outward);
                float crest=Ground(centre-outward*5).y;centre+=outward*3;centre.y=crest-height*.45f;
                for(int i=0;i<vs.Length;i++)
                {
                    var q=vs[i]-nativeBounds.center;var p=centre+orientation*Vector3.Scale(q,new Vector3(bounds.size.x*.9f/nativeBounds.size.x,height/nativeBounds.size.y,bounds.size.z*.9f/nativeBounds.size.z));
                    // Native cliff faces are embedded into the mountain; roads and water retain clearance.
                    for(int station=0;station<gorgeStations.Length-1;station++)
                    {
                        var start=gorgeStations[station];var delta=Flat(gorgeStations[station+1]-start);float t=Mathf.Clamp01(Vector3.Dot(Flat(p-start),delta)/Mathf.Max(.01f,delta.sqrMagnitude));var near=start+delta*t;var away=Flat(p-near);float clearance=gorgeWidths[station]+2.5f;
                        if(away.magnitude<clearance){if(away.sqrMagnitude<.001f)away=Vector3.Cross(Vector3.up,delta);var shift=away.normalized*(clearance-away.magnitude);p+=shift;}
                    }
                    float vertical=Mathf.InverseLerp(nativeBounds.min.y,nativeBounds.max.y,nativeVertices[i].y);
                    if(vertical<.90f)p.y=Mathf.Min(p.y,Ground(p).y-2.2f);
                    if(PolishRoadNear(p,7)||polishMap.Sites.Any(site=>Flat(site.Position-p).magnitude<26))p.y=Mathf.Min(p.y,Ground(p).y-1.2f);
                    vs[i]=f.transform.InverseTransformPoint(p);
                }
                mesh.vertices=vs;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();var asset=VisualAsset(mesh,"CountyPolish/"+f.name.Substring(0,10)+" refined.asset");Undo.RecordObject(f,"Embed rounded rock outcrops");f.sharedMesh=asset;
                var renderer=f.GetComponent<MeshRenderer>();renderer.sharedMaterials=Enumerable.Repeat(renderer.sharedMaterial,asset.subMeshCount).ToArray();
                var collider=f.GetComponent<MeshCollider>();if(collider!=null){Undo.RecordObject(collider,"Match refined geology collision");collider.sharedMesh=null;collider.sharedMesh=asset;}fixedMeshes++;
            }
            var random=new System.Random(55126);int stones=0;
            foreach(var f in source.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Massif")))
            {
                var b=f.GetComponent<Renderer>().bounds;for(int i=0;i<12;i++)
                {
                    float angle=i/12f*Mathf.PI*2;var p=Ground(b.center+new Vector3(Mathf.Cos(angle)*(b.extents.x+1.5f),0,Mathf.Sin(angle)*(b.extents.z+1.5f)));
                    if(!PolishClear(p,2.5f)||RiverDistance(p)<12||p.y> b.max.y+1)continue;
                    var go=PolishProp("P_RockClumpClassic2",p,.5f+(float)random.NextDouble()*1.3f,i*47,group,230);go.transform.position-=Vector3.up*.15f;stones++;
                }
            }
            // Small ornaments formerly seated on detached upper faces follow actual colliders.
            Physics.SyncTransforms();foreach(Transform child in source)
            {
                if(child.name.StartsWith("Massif"))continue;var b=ScenicBounds(child.gameObject);if(b.size.y<.1f)continue;
                var point=new Vector3(b.center.x,b.min.y+1,b.center.z);
                if(Physics.Raycast(point,Vector3.down,out var hit,50,1<<8,QueryTriggerInteraction.Ignore)&&hit.collider.transform!=child&&!hit.collider.transform.IsChildOf(child))
                {Undo.RecordObject(child,"Seat cliff vegetation");child.position+=Vector3.up*(hit.point.y-b.min.y-.05f);}
            }
            Debug.Log("[TowTrouble] Embedded "+fixedMeshes+" rock formations; "+stones+" talus clusters.");
        }
        private static void PolishRoadEdges(Transform root)
        {
            var parent=PolishGroup(root,"Weathered road shoulders and hazard markers");int markers=0;
            foreach(var f in environment.Find("Road network - 24 recovery spurs").GetComponentsInChildren<MeshFilter>())
            {
                var verts=f.sharedMesh.vertices;int rows=verts.Length/6;if(rows<2)continue;
                var v=new List<Vector3>();var uv=new List<Vector2>();var rut=new List<Vector2>();var ends=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
                var anchor=f.transform.TransformPoint(verts[verts.Length/2]);bool snow=anchor.y>128,sand=DesertAt(anchor);int layer=snow?4:sand?5:1;
                var mat=SituationMaterial("County weathered shoulder "+layer,snow?new Color(.81f,.86f,.89f,.36f):sand?new Color(.88f,.77f,.61f,.34f):new Color(.82f,.75f,.61f,.38f),layer);
                for(int side=0;side<2;side++)
                {
                    int start=v.Count;float arc=0;Vector3 last=default;
                    for(int row=0;row<rows;row++)
                    {
                        var edge=f.transform.TransformPoint(verts[row*6+(side==0?0:5)]);var center=f.transform.TransformPoint((verts[row*6+2]+verts[row*6+3])*.5f);var outward=Flat(edge-center).normalized;
                        if(row>0)arc+=Flat(edge-last).magnitude;last=edge;
                        for(int column=0;column<5;column++)
                        {
                            float u=column/4f;var p=Ground(edge+outward*(u*(1.15f+.85f*Mathf.PerlinNoise(edge.x*.27f,edge.z*.27f))-.12f));p.y+=.035f;
                            float strength=PolishClear(p,.3f,true)?1:0;v.Add(p);uv.Add(new Vector2(p.x,p.z)*.35f);rut.Add(new Vector2(u,arc+.7f));ends.Add(new Vector2((rows-1-row)*.7f,1));colors.Add(new Color(1,1,1,strength));
                            if(row<rows-1&&column<4){int a=start+row*5+column;triangles.AddRange(new[]{a,a+5,a+1,a+1,a+5,a+6});}
                        }
                        if(row%24!=0||markers>=96||!PolishClear(edge+outward*2,1,true))continue;
                        var p2=Ground(edge+outward*2.3f);if(center.y-Ground(p2+outward*5).y<3.2f)continue;
                        var point=PolishGroup(parent,"Drop edge marker "+(++markers));point.position=p2;point.rotation=Quaternion.LookRotation(outward);
                        PolishBox(point,"Weathered post",new Vector3(0,.65f,0),new Vector3(.14f,1.3f,.14f),PolishMat("Weathered wood",new Color(.34f,.25f,.15f)));
                        PolishBox(point,"Reflective cap",new Vector3(0,1.17f,0),new Vector3(.16f,.18f,.16f),PolishMat("Warning amber",new Color(.94f,.66f,.16f)));ConfigureDetailLod(point.gameObject,220);
                        if(row%48==0&&PolishClear(p2+outward*1.5f,2))PolishProp("P_RockClumpClassic2",p2+outward*1.5f,.32f,row*13,parent,150);
                    }
                }
                var mesh=new Mesh{name=f.name+" weathered verge"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetUVs(1,rut);mesh.SetUVs(2,ends);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=VisualAsset(mesh,"CountyPolish/"+mesh.name+".asset");go.GetComponent<MeshRenderer>().sharedMaterial=mat;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;ConfigureDetailLod(go,240);
            }
        }
    }
}

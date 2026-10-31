using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private const string VisualRoot=Root+"/Visuals";
        private sealed class ScenicRoad
        {
            public Edge Edge;public string Name;public Vector3[] Points;public float Width;
        }
        private static List<ScenicRoad> scenicRoads;
        private static float[,] roadDistance,roadHeight,roadWidth;
        private static Dictionary<string,GameObject> scenicPrefabs;
        private static T VisualAsset<T>(T asset,string name) where T:UnityEngine.Object
        {
            string path=VisualRoot+"/"+name;var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing!=null)
            {
                // CopySerialized alone leaves stale native GPU buffers for persistent Mesh assets.
                // Update the mesh buffers through the Mesh API while preserving its asset GUID.
                if(asset is Mesh source && existing is Mesh target)
                {
                    target.Clear();target.indexFormat=source.indexFormat;target.name=source.name;
                    target.vertices=source.vertices;target.normals=source.normals;target.tangents=source.tangents;target.colors=source.colors;
                    var channel=new List<Vector4>();for(int i=0;i<8;i++){source.GetUVs(i,channel);if(channel.Count>0)target.SetUVs(i,channel);}
                    target.subMeshCount=source.subMeshCount;for(int i=0;i<source.subMeshCount;i++)target.SetIndices(source.GetIndices(i,false),source.GetTopology(i),i,false,(int)source.GetBaseVertex(i));
                    target.bounds=source.bounds;target.UploadMeshData(false);
                }
                else EditorUtility.CopySerialized(asset,existing);
                UnityEngine.Object.DestroyImmediate(asset);EditorUtility.SetDirty(existing);return existing;
            }
            AssetDatabase.CreateAsset(asset,path);return asset;
        }
        private static Material ScenicMaterial(string name,Color color,Shader shader=null)
        {
            var material=new Material(shader??Shader.Find("Universal Render Pipeline/Lit")){name=name};material.SetColor("_BaseColor",color);if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.13f);return VisualAsset(material,name+".mat");
        }
        private static void VisualContext()
        {
            var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.path!=ScenePath)throw new InvalidOperationException("Open OpenWorldRecovery in Edit Mode.");
            plan=JsonUtility.FromJson<Plan>(File.ReadAllText("Assets/TowTrouble/Editor/OpenWorldPlan.json"));nodes=plan.Nodes.ToDictionary(n=>n.Id);terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();environment=terrain.transform.parent;font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TowTrouble/Art/UI/Theme/Fonts/TowText SDF.asset");Directory.CreateDirectory(VisualRoot);AssetDatabase.Refresh();
        }
        private static Vector3 Flat(Vector3 p)=>new Vector3(p.x,0,p.z);
        private static Vector3 EndpointHandle(string from,string to)
        {
            var delta=Flat(nodes[to].Point-nodes[from].Point);var adjacent=plan.Edges.Where(e=>e.A==from||e.B==from).Select(e=>e.A==from?e.B:e.A).ToArray();
            if(adjacent.Length==2){string other=adjacent.First(n=>n!=to);var incoming=Flat(nodes[from].Point-nodes[other].Point).normalized;var tangent=(incoming+delta.normalized).normalized;if(Vector3.Dot(tangent,delta.normalized)>.2f)return tangent*Mathf.Min(delta.magnitude*.28f,28);}
            return delta.normalized*Mathf.Min(delta.magnitude*.28f,28);
        }
        private static void ScenicPaths()
        {
            scenicRoads=new List<ScenicRoad>();int index=0;
            foreach(var edge in plan.Edges)
            {
                var a=nodes[edge.A].Point;var b=nodes[edge.B].Point;var c=a+EndpointHandle(edge.A,edge.B);var d=b+EndpointHandle(edge.B,edge.A);int count=Mathf.Max(12,Mathf.CeilToInt(Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z))/.9f));var points=new Vector3[count+1];var length=new float[count+1];
                for(int i=0;i<=count;i++){float t=i/(float)count;points[i]=(1-t)*(1-t)*(1-t)*a+3*(1-t)*(1-t)*t*c+3*(1-t)*t*t*d+t*t*t*b;var side=Vector3.Cross(Vector3.up,Flat(b-a).normalized);points[i]+=side*(Mathf.Sin(t*Mathf.PI)*Mathf.Sin(t*Mathf.PI)*Mathf.Min(10f,Flat(b-a).magnitude*.07f)*(index%2==0?1:-1));if(i>0)length[i]=length[i-1]+Flat(points[i]-points[i-1]).magnitude;}
                for(int i=0;i<=count;i++)points[i].y=Mathf.Lerp(a.y,b.y,length[i]/length[count]);
                scenicRoads.Add(new ScenicRoad{Edge=edge,Name="Road-"+(index++).ToString("00")+"-"+edge.A+"-"+edge.B,Points=points,Width=edge.Trail?2.9f:4.1f});
            }
        }
        private static float ScenicDistance(Vector3 point,out Vector3 nearest,out ScenicRoad road)
        {
            float best=float.MaxValue;nearest=Vector3.zero;road=null;var p=new Vector2(point.x,point.z);
            foreach(var path in scenicRoads)for(int i=1;i<path.Points.Length;i+=3){var a=path.Points[i-1];var b=path.Points[Mathf.Min(i+2,path.Points.Length-1)];float distance=Distance(p,new Vector2(a.x,a.z),new Vector2(b.x,b.z),out float t);if(distance<best){best=distance;nearest=Vector3.Lerp(a,b,t);road=path;}}
            return best;
        }
        private static void ScenicRoadField(int resolution)
        {
            roadDistance=new float[resolution,resolution];roadHeight=new float[resolution,resolution];roadWidth=new float[resolution,resolution];
            for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)roadDistance[z,x]=10000;
            float scale=(resolution-1)/1000f;
            foreach(var path in scenicRoads)for(int i=1;i<path.Points.Length;i+=3)
            {
                var a=path.Points[i-1];var b=path.Points[Mathf.Min(i+2,path.Points.Length-1)];const float radius=37;
                int x0=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.x,b.x)-radius+500)*scale),0,resolution-1),x1=Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.x,b.x)+radius+500)*scale),0,resolution-1);int z0=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.z,b.z)-radius+500)*scale),0,resolution-1),z1=Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.z,b.z)+radius+500)*scale),0,resolution-1);
                for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){var p=new Vector2(x/scale-500,z/scale-500);float distance=Distance(p,new Vector2(a.x,a.z),new Vector2(b.x,b.z),out float t);if(distance<roadDistance[z,x]){roadDistance[z,x]=distance;roadHeight[z,x]=Mathf.Lerp(a.y,b.y,t);roadWidth[z,x]=path.Width;}}
            }
        }
        private static float[,] SmoothHeights(float[,] source)
        {
            int n=source.GetLength(0);var horizontal=new float[n,n];var result=new float[n,n];const int radius=24;var kernel=Enumerable.Range(-radius,radius*2+1).Select(i=>Mathf.Exp(-i*i/240f)).ToArray();float sum=kernel.Sum();
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){float value=0;for(int k=-radius;k<=radius;k++)value+=source[z,Mathf.Clamp(x+k,0,n-1)]*kernel[k+radius];horizontal[z,x]=value/sum;}
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){float value=0;for(int k=-radius;k<=radius;k++)value+=horizontal[Mathf.Clamp(z+k,0,n-1),x]*kernel[k+radius];result[z,x]=value/sum;}return result;
        }
        [MenuItem("Tow Trouble/Visuals/1 - Sculpt Terrain and Scenic Roads")]
        public static void PolishTerrainAndRoads()
        {
            VisualContext();ScenicPaths();var data=terrain.terrainData;
            string baseline=VisualRoot+"/Terrain before visual pass.asset";if(AssetDatabase.LoadAssetAtPath<TerrainData>(baseline)==null)AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(data),baseline);
            var original=AssetDatabase.LoadAssetAtPath<TerrainData>(baseline);var smoothed=SmoothHeights(original.GetHeights(0,0,original.heightmapResolution,original.heightmapResolution));int oldN=smoothed.GetLength(0);const int n=1025;ScenicRoadField(n);var heights=new float[n,n];
            // A wide smooth shoulder removes the polygonal road terraces without moving mission pads.
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                float sx=x/(float)(n-1)*(oldN-1),sz=z/(float)(n-1)*(oldN-1);int ix=Mathf.Min((int)sx,oldN-2),iz=Mathf.Min((int)sz,oldN-2);float h=Mathf.Lerp(Mathf.Lerp(smoothed[iz,ix],smoothed[iz,ix+1],sx-ix),Mathf.Lerp(smoothed[iz+1,ix],smoothed[iz+1,ix+1],sx-ix),sz-iz)*350-50;
                float wx=-500+x/(float)(n-1)*1000,wz=-500+z/(float)(n-1)*1000,u=wx/900+.5f,v=.5f-wz/900;float d=roadDistance[z,x],width=roadWidth[z,x];if(d<36)h=Mathf.Lerp(roadHeight[z,x],h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(width+1.25f,36,d)));
                foreach(var node in plan.Nodes.Where(p=>p.Id=="g"||p.Id.Length==1||p.Id.StartsWith("R"))){float radius=node.Id=="g"?32:node.Id.Length==1?13:6;float nd=Vector2.Distance(new Vector2(wx,wz),new Vector2(node.Point.x,node.Point.z));if(nd<radius+9)h=Mathf.Lerp(node.Height,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(radius,radius+9,nd)));}
                float ld=LakeDistance(new Vector2(u,v));if(ld<0&&d>width+3)h=Mathf.Lerp(8.1f,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(-10,0,ld)));
                if(d<width+1.1f)h=roadHeight[z,x];
                heights[z,x]=(h+50)/350;
            }
            data.heightmapResolution=n;data.size=new Vector3(1000,350,1000);data.SetHeights(0,0,heights);terrain.heightmapPixelError=1.5f;terrain.detailObjectDistance=110;terrain.treeBillboardDistance=220;terrain.treeDistance=700;terrain.basemapDistance=1600;
            PaintScenicTerrain();BuildScenicRoadMeshes();RegroundExistingDressing();VisualAsset(UnityEngine.Object.Instantiate(data),"Road graded terrain snapshot.asset");
            var map=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryMap>()).Single();map.Configure(map.Sites,map.Deliveries,scenicRoads.Where(r=>!r.Edge.Trail).SelectMany(r=>r.Points.Where((p,i)=>i%8==0||i==r.Points.Length-1).Select(p=>p+Vector3.up*.14f)).ToArray());EditorUtility.SetDirty(map);EditorUtility.SetDirty(data);SaveVisualScene();
        }
        private static void PaintScenicTerrain()
        {
            var data=terrain.terrainData;var sources=AssetDatabase.LoadAssetAtPath<TerrainData>(VisualRoot+"/Terrain before visual pass.asset").terrainLayers;var colors=new[]{new Color(.78f,.88f,.76f,1),new Color(.95f,.91f,.83f,1),new Color(.95f,.95f,.9f,1),new Color(.85f,.9f,.95f,1),new Color(.97f,.99f,1,1),new Color(.98f,.91f,.81f,1)};var layers=new List<TerrainLayer>();
            for(int i=0;i<sources.Length;i++){var l=UnityEngine.Object.Instantiate(sources[i]);l.name="County "+sources[i].name;l.metallic=0;l.smoothness=.04f;l.specular=Color.black;l.tileSize=Vector2.one*(i==3?17:i==4?15:i==1?7:10);l.diffuseRemapMin=Vector4.zero;l.diffuseRemapMax=colors[i];l.normalScale=.6f;layers.Add(VisualAsset(l,"Ground "+i+".terrainlayer"));}
            var undergrowth=UnityEngine.Object.Instantiate(layers[1]);undergrowth.name="County woodland soil";undergrowth.diffuseRemapMax=new Color(.61f,.63f,.49f,1);undergrowth.tileSize=Vector2.one*9;layers.Add(VisualAsset(undergrowth,"Ground 6.terrainlayer"));var packed=UnityEngine.Object.Instantiate(layers[4]);packed.name="County packed snow road";packed.diffuseRemapMax=new Color(.64f,.75f,.84f,1);packed.tileSize=Vector2.one*8;layers.Add(VisualAsset(packed,"Ground 7.terrainlayer"));data.terrainLayers=layers.ToArray();const int n=512;data.alphamapResolution=n;var alpha=new float[n,n,8];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                float u0=x/(float)(n-1),v0=z/(float)(n-1),wx=-500+u0*1000,wz=-500+v0*1000,u=wx/900+.5f,v=.5f-wz/900,h=data.GetInterpolatedHeight(u0,v0)-50;float noise=Mathf.PerlinNoise(wx*.022f+19,wz*.022f+28);float slope=data.GetSteepness(u0,v0);float rock=Mathf.SmoothStep(0,1,Mathf.InverseLerp(22,43,slope));float snowCover=Mathf.SmoothStep(0,1,Mathf.InverseLerp(115+noise*10,140+noise*9,h));float desert=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.67f,.76f,u));desert*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.29f,.39f,v));
                float shore=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,37,LakeDistance(new Vector2(u,v))));float beach=Mathf.Max(shore,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.85f,.94f,v))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.56f,.67f,u))));float woodland=Mathf.Clamp01((noise-.39f)*1.15f)*(1-rock);
                alpha[z,x,0]=1-woodland;alpha[z,x,6]=woodland;BlendLayer(alpha,z,x,3,rock);BlendLayer(alpha,z,x,2,beach);BlendLayer(alpha,z,x,5,desert);BlendLayer(alpha,z,x,4,snowCover);
                int rx=Mathf.RoundToInt(u0*1024),rz=Mathf.RoundToInt(v0*1024);float road=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(roadWidth[rz,rx]-1,roadWidth[rz,rx]+5,roadDistance[rz,rx]));BlendLayer(alpha,z,x,snowCover>.5f?7:1,road*.88f);
            }
            data.SetAlphamaps(0,0,alpha);
            var trees=data.treeInstances.Where(t=>ScenicDistance(new Vector3(-500+t.position.x*1000,0,-500+t.position.z*1000),out _,out _)>15).ToArray();for(int i=0;i<trees.Length;i++){var t=trees[i];t.position=new Vector3(t.position.x,data.GetInterpolatedHeight(t.position.x,t.position.z)/350,t.position.z);trees[i]=t;}data.SetTreeInstances(trees,false);
        }
        private static void BlendLayer(float[,,] alpha,int z,int x,int layer,float blend){for(int k=0;k<alpha.GetLength(2);k++)alpha[z,x,k]*=1-blend;alpha[z,x,layer]+=blend;}
        private static void BuildScenicRoadMeshes()
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(VisualRoot+"/CountyRoad.shader");if(shader==null)throw new InvalidOperationException("Road shader missing");
            var materials=new Material[3];string landscape="Assets/SoStylized/Environment/Landscape/Textures/";
            for(int i=0;i<3;i++){var m=new Material(shader){name=new[]{"County compacted gravel","County dune trail","County snow track"}[i]};m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(landscape+new[]{"T_Dirt_BC.png","T_DesertSand_BC.png","T_Snow_BC.PNG"}[i]));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(landscape+(i==1?"T_DesertSand_N.png":i==0?"T_Dirt_N.png":"T_Snow_N.PNG")));m.SetColor("_BaseColor",new[]{new Color(.94f,.9f,.8f),new Color(.96f,.91f,.81f),new Color(.92f,.96f,1)}[i]);m.SetFloat("_BumpScale",.3f);m.SetFloat("_Smoothness",i==2?.18f:.07f);materials[i]=VisualAsset(m,m.name+".mat");}
            foreach(var path in scenicRoads)
            {
                var go=environment.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name==path.Name).gameObject;int count=path.Points.Length;var vertices=new List<Vector3>();var uv=new List<Vector2>();var lane=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();var collision=new List<Vector3>();var ct=new List<int>();float arc=0;
                for(int i=0;i<count;i++)
                {
                    var center=path.Points[i];if(i>0)arc+=Flat(center-path.Points[i-1]).magnitude;var direction=Flat(path.Points[Mathf.Min(i+1,count-1)]-path.Points[Mathf.Max(0,i-1)]).normalized;var right=new Vector3(direction.z,0,-direction.x);float w=path.Width*(1+.025f*Mathf.Sin(arc*.13f));float[] widths={-w-2,-w,-w*.4f,w*.4f,w,w+2};
                    for(int k=0;k<6;k++){var p=center+right*widths[k];p.y=Ground(p).y-.06f;vertices.Add(p);uv.Add(new Vector2(p.x*.24f,p.z*.24f));lane.Add(new Vector2(widths[k]/w,arc));colors.Add(new Color(1,1,1,k==0||k==5?0:1));if(i<count-1&&k<5){int a=i*6+k;triangles.AddRange(new[]{a,a+6,a+1,a+1,a+6,a+7});}}
                    collision.Add(center-right*w+Vector3.up*.1f);collision.Add(center+right*w+Vector3.up*.1f);if(i<count-1){int a=i*2;ct.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
                }
                var mesh=new Mesh{name=path.Name+" feathered",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,lane);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();go.GetComponent<MeshFilter>().sharedMesh=VisualAsset(mesh,path.Name+"-visual.asset");
                var colliderMesh=new Mesh{name=path.Name+" drive surface"};colliderMesh.SetVertices(collision);colliderMesh.SetTriangles(ct,0);colliderMesh.RecalculateNormals();colliderMesh.RecalculateBounds();go.GetComponent<MeshCollider>().sharedMesh=VisualAsset(colliderMesh,path.Name+"-collision.asset");
                // TerrainCollider is the driving surface of painted roads. An invisible planar
                // overlay would hide ruts and create steps at junctions. Bridges have their own deck colliders.
                go.GetComponent<MeshCollider>().enabled=false;var mid=path.Points[count/2];int kind=mid.y>124?2:mid.x/900+.5f>.70f||.5f-mid.z/900>.85f?1:0;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=materials[kind];renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.enabled=false;
            }
        }
        public static void RefreshScenicGroundPaint(){VisualContext();ScenicPaths();ScenicRoadField(terrain.terrainData.heightmapResolution);PaintScenicTerrain();EditorUtility.SetDirty(terrain.terrainData);SaveVisualScene();}
        [MenuItem("Tow Trouble/Visuals/3 - Refresh Road Surface Materials")]
        public static void RefreshScenicRoadSurfaces(){VisualContext();ScenicPaths();BuildScenicRoadMeshes();SaveVisualScene();}
        private static void RegroundExistingDressing()
        {
            foreach(var parent in environment.Cast<Transform>().Where(t=>t.name=="Mega - retained environment"||t.name=="Biome dressing - So Stylized"||t.name=="World landmarks and river"))foreach(var t in parent.Cast<Transform>().ToArray())
            {
                if(t.name.StartsWith("River")||t.name.StartsWith("Bridge"))continue;var p=t.position;var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);p.y+=Ground(p).y-b.min.y-(t.name.Contains("Rock")||t.name.Contains("Cliff")?.6f:.04f);t.position=p;b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);float clearance=ScenicDistance(b.center,out _,out var path);float radius=Mathf.Min(12,Mathf.Min(b.extents.x,b.extents.z));if(path!=null&&clearance<path.Width+radius+2)UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
            foreach(var f in environment.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.EndsWith(" - Living Mud"))){var mesh=f.sharedMesh;var vs=mesh.vertices;for(int i=0;i<vs.Length;i++){var p=f.transform.TransformPoint(vs[i]);p.y=Ground(p).y+.07f;vs[i]=f.transform.InverseTransformPoint(p);}mesh.vertices=vs;mesh.RecalculateNormals();mesh.RecalculateBounds();f.GetComponent<MeshCollider>().sharedMesh=null;f.GetComponent<MeshCollider>().sharedMesh=mesh;EditorUtility.SetDirty(mesh);}
        }
        [MenuItem("Tow Trouble/Visuals/5 - Finish Ground Integration")]
        public static void FinishScenicGround()
        {
            VisualContext();foreach(var renderer in environment.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("Road-")))renderer.enabled=false;
            // Terrain-splatted roads avoid coplanar overlay seams; the separate drive colliders stay active.
            var data=terrain.terrainData;var trees=data.treeInstances.Where(t=>{var p=new Vector3(-500+t.position.x*1000,0,-500+t.position.z*1000);return !plan.Nodes.Any(n=>(n.Id=="g"||n.Id.Length==1||n.Id.StartsWith("R"))&&Flat(p-n.Point).magnitude<(n.Id=="g"?44:n.Id.Length==1?19:11));}).ToArray();data.SetTreeInstances(trees,false);
            var retained=environment.Find("Mega - retained environment");if(retained!=null)foreach(var child in retained.Cast<Transform>().Where(t=>t.name.Contains("Boulder")||t.name.Contains("Rock")).ToArray()){var b=ScenicBounds(child.gameObject);float height=Mathf.Min(18,b.size.y);if(b.size.y>18)child.localScale*=18/b.size.y;b=ScenicBounds(child.gameObject);child.position+=Vector3.up*(Ground(child.position).y-b.min.y-height*.33f);}
            terrain.Flush();EditorUtility.SetDirty(data);SaveVisualScene();
        }
        private static void SaveVisualScene(){Physics.SyncTransforms();var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);}
    }
}

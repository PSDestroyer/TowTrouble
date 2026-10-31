using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private sealed class RiverCrossing { public Vector3 Center,Direction;public float HalfLength,Width,Arc; }
        private static List<RiverCrossing> riverCrossings=new List<RiverCrossing>();
        private static List<Vector3> RiverPlan()
        {
            // The last reach overlaps the lake. Heights are solved separately from the terrain.
            var raw=new[]{new Vector2(.36f,.19f),new Vector2(.35f,.29f),new Vector2(.34f,.38f),new Vector2(.24f,.46f),new Vector2(.20f,.56f),new Vector2(.24f,.65f),new Vector2(.22f,.69f),new Vector2(.215f,.72f)};
            var points=raw.Select(p=>new Vector3((p.x-.5f)*900,0,(.5f-p.y)*900)).ToList();for(int i=0;i<2;i++)points=SmoothShore(points,false);
            var result=new List<Vector3>();for(int s=1;s<points.Count;s++){int n=Mathf.CeilToInt(Vector3.Distance(points[s-1],points[s])/1.5f);for(int i=0;i<n;i++)result.Add(Vector3.Lerp(points[s-1],points[s],i/(float)n));}result.Add(points.Last());return ComposeHeadwaters(result);
        }
        private static List<Vector3> cachedRiverPlan;
        private static float RiverDistance(Vector3 p)
        {
            if(cachedRiverPlan==null)cachedRiverPlan=RiverPlan();float best=float.MaxValue;
            for(int i=1;i<cachedRiverPlan.Count;i+=4){var a=cachedRiverPlan[i-1];var b=cachedRiverPlan[Mathf.Min(i+3,cachedRiverPlan.Count-1)];best=Mathf.Min(best,Distance(new Vector2(p.x,p.z),new Vector2(a.x,a.z),new Vector2(b.x,b.z),out _));}return best;
        }
        private static bool OnRiverBridge(Vector3 p,float extra=0)
        {
            if(riverCrossings.Count==0&&scenicRoads!=null)SolveRiver(RiverPlan(),out _);
            foreach(var bridge in riverCrossings){var offset=p-bridge.Center;var forward=Flat(bridge.Direction).normalized;if(Mathf.Abs(Vector3.Dot(offset,forward))<bridge.HalfLength+extra&&Mathf.Abs(Vector3.Dot(offset,Vector3.Cross(Vector3.up,forward)))<bridge.Width+extra)return true;}return false;
        }
        private static bool SegmentCross(Vector3 a,Vector3 b,Vector3 c,Vector3 d,out float t,out float u)
        {
            float ax=b.x-a.x,az=b.z-a.z,bx=d.x-c.x,bz=d.z-c.z,den=ax*bz-az*bx;t=u=0;if(Mathf.Abs(den)<.00001f)return false;
            t=((c.x-a.x)*bz-(c.z-a.z)*bx)/den;u=((c.x-a.x)*az-(c.z-a.z)*ax)/den;return t>=0&&t<=1&&u>=0&&u<=1;
        }
        private static void SolveRiver(List<Vector3> centers,out float[] arcs)
        {
            arcs=new float[centers.Count];for(int i=1;i<arcs.Length;i++)arcs[i]=arcs[i-1]+Flat(centers[i]-centers[i-1]).magnitude;
            riverCrossings.Clear();var anchors=new List<Vector2>{new Vector2(0,108),new Vector2(arcs.Last()-80,10.4f),new Vector2(arcs.Last(),10.4f)};
            foreach(var road in scenicRoads)for(int j=1;j<road.Points.Length;j++)for(int i=1;i<centers.Count;i++)
            {
                if(!SegmentCross(road.Points[j-1],road.Points[j],centers[i-1],centers[i],out float a,out float b))continue;
                var p=Vector3.Lerp(road.Points[j-1],road.Points[j],a);if(riverCrossings.Any(c=>Flat(c.Center-p).magnitude<8))continue;
                var dir=(road.Points[j]-road.Points[j-1]).normalized;var flow=Flat(centers[i]-centers[i-1]).normalized;
                float angle=Mathf.Abs(Vector3.Dot(Flat(dir).normalized,Vector3.Cross(Vector3.up,flow)));
                float arc=Mathf.Lerp(arcs[i-1],arcs[i],b);
                riverCrossings.Add(new RiverCrossing{Center=p,Direction=dir,HalfLength=Mathf.Clamp(11/Mathf.Max(.3f,angle),13,32),Width=road.Width+1.3f,Arc=arc});
                anchors.Add(new Vector2(arc,p.y-2.8f));
            }
            // A rock cascade connects the high headwater pool to the valley. The lower reach
            // follows a gentle continuous gradient rather than ending in a floating rectangle.
            AddHeadwaterAnchors(centers,arcs,anchors);
            anchors=anchors.OrderBy(a=>a.x).ToList();
            for(int i=1;i<anchors.Count;i++){var a=anchors[i];a.y=Mathf.Min(a.y,anchors[i-1].y);a.y=Mathf.Max(10.4f,a.y);anchors[i]=a;}
            for(int i=0;i<centers.Count;i++)
            {
                int k=1;while(k<anchors.Count-1&&anchors[k].x<arcs[i])k++;
                var a=anchors[k-1];var b=anchors[k];var p=centers[i];p.y=Mathf.Lerp(a.y,b.y,Mathf.InverseLerp(a.x,b.x,arcs[i]));centers[i]=p;
            }
        }
        private static float RiverHalfWidth(float arc)=>HeadwaterHalfWidth(arc);
        private static void BuildConnectedRiver(Material water,Transform landmarks)
        {
            ScenicRoadField(terrain.terrainData.heightmapResolution);
            var centers=RiverPlan();SolveRiver(centers,out var arcs);
            var data=terrain.terrainData;int n=data.heightmapResolution;var h=data.GetHeights(0,0,n,n);var baseline=AssetDatabase.LoadAssetAtPath<TerrainData>(VisualRoot+"/Road graded terrain snapshot.asset");var original=gorgeRiverBaseline??baseline.GetHeights(0,0,n,n);float scale=(n-1)/1000f;
            var nearest=new float[n,n];var level=new float[n,n];var width=new float[n,n];for(int z=0;z<n;z++)for(int x=0;x<n;x++)nearest[z,x]=10000;
            for(int i=1;i<centers.Count;i++)
            {
                var a=centers[i-1];var b=centers[i];const float radius=23;
                int x0=Mathf.Clamp((int)((Mathf.Min(a.x,b.x)+500-radius)*scale),0,n-1),x1=Mathf.Clamp((int)((Mathf.Max(a.x,b.x)+500+radius)*scale),0,n-1);
                int z0=Mathf.Clamp((int)((Mathf.Min(a.z,b.z)+500-radius)*scale),0,n-1),z1=Mathf.Clamp((int)((Mathf.Max(a.z,b.z)+500+radius)*scale),0,n-1);
                for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)
                {float d=Distance(new Vector2(x/scale-500,z/scale-500),new Vector2(a.x,a.z),new Vector2(b.x,b.z),out float t);if(d>=nearest[z,x])continue;nearest[z,x]=d;level[z,x]=Mathf.Lerp(a.y,b.y,t);width[z,x]=RiverHalfWidth(Mathf.Lerp(arcs[i-1],arcs[i],t));}
            }
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                float d=nearest[z,x];if(d>23)continue;float w=width[z,x],waterY=level[z,x];var p=new Vector3(x/scale-500,0,z/scale-500);
                float bed=waterY-1.1f+.45f*Mathf.Pow(Mathf.Clamp01(d/w),3);
                float bank=waterY+.6f+.18f*Mathf.Max(0,d-w-3);
                float target=d<w?bed:Mathf.Lerp(bed,bank,Smooth01(w,w+3,d));
                float blend=1-Smooth01(w+3,23,d);
                // The bridge carries the road over the channel; outside its abutments retain the lane.
                if(roadDistance[z,x]<roadWidth[z,x]+.8f&&!OnRiverBridge(p,1))continue;
                float value=Mathf.Lerp(original[z,x]*350-50,target,blend);
                if(waterY<=10.401f&&ShoreDistance(p)<0)value=Mathf.Min(value,original[z,x]*350-50);
                if(OnRiverBridge(p)&&d>w+2)
                {
                    var bridge=riverCrossings.OrderBy(c=>Flat(c.Center-p).sqrMagnitude).First();
                    float along=Mathf.Abs(Vector3.Dot(p-bridge.Center,Flat(bridge.Direction).normalized));
                    float ramp=Smooth01(bridge.HalfLength-5,bridge.HalfLength,along);
                    value=Mathf.Lerp(value,roadHeight[z,x],ramp);
                }
                h[z,x]=(value+50)/350;
            }
            // Smooth each bridge approach into the original graded road. A hard corridor
            // boundary here otherwise leaves a short, steep lip beyond the bridge ramp.
            foreach(var crossing in riverCrossings)
            {
                var forward=Flat(crossing.Direction).normalized;var right=Vector3.Cross(Vector3.up,forward);
                float extent=crossing.HalfLength+15;
                int x0=Mathf.Clamp((int)((crossing.Center.x+500-extent)*scale),1,n-2),x1=Mathf.Clamp((int)((crossing.Center.x+500+extent)*scale),1,n-2);
                int z0=Mathf.Clamp((int)((crossing.Center.z+500-extent)*scale),1,n-2),z1=Mathf.Clamp((int)((crossing.Center.z+500+extent)*scale),1,n-2);
                for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)
                {
                    var delta=new Vector3(x/scale-500,0,z/scale-500)-crossing.Center;
                    float along=Vector3.Dot(delta,forward),lateral=Mathf.Abs(Vector3.Dot(delta,right)),distance=Mathf.Abs(along);
                    if(distance<crossing.HalfLength-3||distance>crossing.HalfLength+12||lateral>crossing.Width+5)continue;
                    float deck=crossing.Center.y+along*crossing.Direction.y/Flat(crossing.Direction).magnitude-.08f;
                    float originalY=original[z,x]*350-50;
                    float target=Mathf.Lerp(deck,originalY,Smooth01(crossing.HalfLength-3,crossing.HalfLength+12,distance));
                    h[z,x]=Mathf.Lerp((target+50)/350,h[z,x],Smooth01(crossing.Width-1,crossing.Width+5,lateral));
                }
            }
            data.SetHeights(0,0,h);EditorUtility.SetDirty(data);
            var cascadeSource=AssetDatabase.LoadAssetAtPath<Material>("Assets/SoStylized/Environment/Water/Materials/M_Waterfall.mat");
            var cascade=VisualAsset(new Material(cascadeSource){name="County river cascade"},"County river cascade.mat");
            cascade.SetColor("_Top_Color",new Color(.55f,.83f,.82f,1));cascade.SetColor("_Bottom_Color",new Color(.70f,.90f,.85f,1));cascade.SetColor("_Foam_Color",new Color(.90f,1,.95f,1));
            cascade.SetFloat("_Opacity_Top",1);cascade.SetFloat("_Opacity_Bottom",1);cascade.SetFloat("_Noise_Opacity",.9f);cascade.SetFloat("_Noise_Size",.8f);cascade.SetFloat("_Detail_Noise_Opacity",.35f);
            var vs=new List<Vector3>();var uv=new List<Vector2>();var flatTr=new List<int>();var fallTr=new List<int>();
            for(int i=0;i<centers.Count;i++)
            {
                var dir=Flat(centers[Mathf.Min(i+1,centers.Count-1)]-centers[Mathf.Max(0,i-1)]).normalized;var right=Vector3.Cross(Vector3.up,dir);float w=RiverSurfaceHalfWidth(centers,arcs,i);
                vs.Add(centers[i]-right*w);vs.Add(centers[i]+right*w);uv.Add(new Vector2(0,1-Mathf.Clamp01(arcs[i]/140)));uv.Add(new Vector2(1,1-Mathf.Clamp01(arcs[i]/140)));
                if(i<centers.Count-1&&(centers[i].y>10.401f||ShoreDistance(centers[i])>-5)){float grade=(centers[i].y-centers[i+1].y)/Mathf.Max(.01f,arcs[i+1]-arcs[i]);if(grade>.38f&&centers[i].z>210)continue;var tr=grade>.38f?fallTr:flatTr;int a=i*2;tr.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            }
            var cascadeVertices=vs.ToList();
            flatTr=RemoveLakeOverlap(vs,flatTr);
            // Lake and river use identical world UVs; the native waterfall requires normalized
            // longitudinal UVs. Separate meshes prevent a visible texture seam at the mouth.
            var mesh=new Mesh{name="Continuous river channel"};mesh.SetVertices(vs);mesh.SetUVs(0,vs.Select(p=>new Vector2(p.x*.02f,p.z*.02f)).ToList());mesh.SetTriangles(flatTr,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var go=new GameObject("River - continuous channel and cascade",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(landmarks,false);go.GetComponent<MeshFilter>().sharedMesh=VisualAsset(mesh,"Winding river.asset");go.GetComponent<MeshRenderer>().sharedMaterial=water;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            var fall=new Mesh{name="Headwater cascade"};fall.SetVertices(cascadeVertices);fall.SetUVs(0,uv);fall.SetColors(Enumerable.Repeat(Color.white,cascadeVertices.Count).ToList());fall.SetTriangles(fallTr,0);fall.RecalculateNormals();fall.RecalculateTangents();fall.RecalculateBounds();
            var fallGo=new GameObject("River - headwater cascade",typeof(MeshFilter),typeof(MeshRenderer));fallGo.transform.SetParent(landmarks,false);fallGo.GetComponent<MeshFilter>().sharedMesh=VisualAsset(fall,"Headwater cascade.asset");fallGo.GetComponent<MeshRenderer>().sharedMaterial=cascade;fallGo.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            BuildAuthoredWaterfalls(landmarks);
            foreach(var crossing in riverCrossings)BuildRiverBridge(crossing,landmarks);
            Debug.Log("[TowTrouble] Connected river to lake; "+riverCrossings.Count+" surveyed road crossings.");
        }
        private static List<Vector3> ArtLakeOutline()
        {
            var points=lake.Select(p=>new Vector3((p.x-.5f)*900,10.4f,(.5f-p.y)*900)).ToList();
            var center=points.Aggregate(Vector3.zero,(a,b)=>a+b)/points.Count;
            for(int i=0;i<points.Count;i++)points[i]+=Flat(points[i]-center).normalized*24;
            for(int i=0;i<2;i++)points=SmoothShore(points,true);
            return points;
        }
        private static List<Vector3> ClipWaterPolygon(List<Vector3> polygon,Vector3 a,Vector3 b,bool inside)
        {
            var result=new List<Vector3>();if(polygon.Count==0)return result;
            var previous=polygon.Last();float last=Cross2(a,b,previous);bool previousIn=inside?last>=0:last<=0;
            foreach(var current in polygon)
            {
                float distance=Cross2(a,b,current);bool currentIn=inside?distance>=0:distance<=0;
                if(currentIn!=previousIn)result.Add(Vector3.Lerp(previous,current,last/(last-distance)));
                if(currentIn)result.Add(current);
                previous=current;last=distance;previousIn=currentIn;
            }
            return result;
        }
        private static List<int> RemoveLakeOverlap(List<Vector3> vertices,List<int> triangles)
        {
            var outline=ArtLakeOutline();var lakeTriangles=ShoreTriangles(outline);var result=new List<int>();
            for(int i=0;i<triangles.Count;i+=3)
            {
                var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
                if(a.y>10.401f||b.y>10.401f||c.y>10.401f){result.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});continue;}
                var pieces=new List<List<Vector3>>{new List<Vector3>{a,b,c}};
                for(int j=0;j<lakeTriangles.Count&&pieces.Count>0;j+=3)
                {
                    var cut=new[]{outline[lakeTriangles[j]],outline[lakeTriangles[j+2]],outline[lakeTriangles[j+1]]};
                    var remainder=new List<List<Vector3>>();
                    foreach(var piece in pieces)
                    {
                        var interior=piece;
                        for(int edge=0;edge<3&&interior.Count>=3;edge++)
                        {
                            var outside=ClipWaterPolygon(interior,cut[edge],cut[(edge+1)%3],false);
                            if(outside.Count>=3)remainder.Add(outside);
                            interior=ClipWaterPolygon(interior,cut[edge],cut[(edge+1)%3],true);
                        }
                    }
                    pieces=remainder;
                }
                foreach(var piece in pieces)for(int k=1;k<piece.Count-1;k++)
                {
                    if(Mathf.Abs(Cross2(piece[0],piece[k],piece[k+1]))<.0001f)continue;
                    int start=vertices.Count;vertices.Add(piece[0]);vertices.Add(piece[k]);vertices.Add(piece[k+1]);result.AddRange(new[]{start,start+1,start+2});
                }
            }
            return result;
        }
        private static void BuildRiverBridge(RiverCrossing bridge,Transform parent)
        {
            var timber=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop aged timber.mat");
            var planks=new[]{ScenicMaterial("Bridge timber warm",new Color(.48f,.33f,.18f)),ScenicMaterial("Bridge timber pale",new Color(.55f,.40f,.24f)),ScenicMaterial("Bridge timber weathered",new Color(.46f,.36f,.25f))};
            var metal=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop roof metal.mat");
            var group=new GameObject("Bridge - surveyed crossing "+riverCrossings.IndexOf(bridge)).transform;group.SetParent(parent,false);
            var dir=bridge.Direction;var right=Vector3.Cross(Vector3.up,Flat(dir).normalized);var rot=Quaternion.LookRotation(dir);int count=Mathf.CeilToInt(bridge.HalfLength*2/.72f);float step=bridge.HalfLength*2/count;
            for(int i=0;i<count;i++){var p=bridge.Center+dir*(-bridge.HalfLength+(i+.5f)*step);var plank=Cube("Grounded timber deck",p+Vector3.up*.015f,new Vector3(bridge.Width*2+.5f,.17f,step-.012f),planks[(i*7)%3],group,false);plank.transform.rotation=rot;}
            // One uninterrupted driving collider, with its top exactly matching the existing road.
            var deck=Cube("Bridge drive surface",bridge.Center-Vector3.up*.015f,new Vector3(bridge.Width*2,.23f,bridge.HalfLength*2),timber,group);deck.transform.rotation=rot;deck.GetComponent<MeshRenderer>().enabled=false;
            foreach(int side in new[]{-1,1})
            {
                var beam=Cube("Bridge load-bearing beam",bridge.Center+right*side*(bridge.Width-.45f)-Vector3.up*.35f,new Vector3(.34f,.55f,bridge.HalfLength*2),timber,group,false);beam.transform.rotation=rot;
                var rail=Cube("Bridge safety rail",bridge.Center+right*side*(bridge.Width+.4f)+Vector3.up*1.05f,new Vector3(.15f,.17f,bridge.HalfLength*2),metal,group);rail.transform.rotation=rot;
                for(float offset=-bridge.HalfLength+.5f;offset<bridge.HalfLength;offset+=3.5f)
                {var p=bridge.Center+dir*offset+right*side*(bridge.Width+.4f);float bottom=Ground(p).y-.55f,top=p.y+1.15f;var post=Cube("Bridge pile to riverbed",new Vector3(p.x,(bottom+top)*.5f,p.z),new Vector3(.28f,top-bottom,.28f),timber,group);post.transform.rotation=Quaternion.LookRotation(Flat(dir));}
            }
            foreach(int endSide in new[]{-1,1})
            {
                var start=bridge.Center+dir*endSide*bridge.HalfLength+Vector3.up*.1f;
                var end=Ground(start+Flat(dir).normalized*endSide*4.5f);end.y+=.02f;
                var delta=end-start;var ramp=Cube("Bridge entry ramp",(start+end)*.5f-Vector3.up*.085f,new Vector3(bridge.Width*2+.5f,.17f,delta.magnitude+.05f),timber,group);
                ramp.transform.rotation=Quaternion.LookRotation(delta);
            }
            foreach(int side in new[]{-1,1}){var p=bridge.Center+dir*side*(bridge.HalfLength-1.4f);var abutment=Cube("Bridge stone abutment",p-Vector3.up*.8f,new Vector3(bridge.Width*2+.9f,1.7f,2.8f),AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop foundation.mat")??timber,group,false);abutment.transform.rotation=rot;}
        }
    }
}

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
        // Surveyed lip / plunge-pool pairs. Heights decrease independently of terrain.
        private static readonly Vector3[] gorgeStations={
            new Vector3(-126,108,279),new Vector3(-126,107.9f,270),new Vector3(-126,107.8f,266),
            new Vector3(-123,89.8f,261),new Vector3(-115,89.6f,252),new Vector3(-116,89.4f,246),
            new Vector3(-119,73.7f,242),new Vector3(-132,73.5f,232),new Vector3(-139,73.3f,224),
            new Vector3(-140,52.4f,218),new Vector3(-136,52.1f,207),new Vector3(-135.5f,51.6f,195)};
        private static readonly float[] gorgeWidths={3.1f,4.8f,2.5f,3.4f,5.5f,3.8f,3.0f,5.8f,3.0f,3.5f,7.1f,4};
        private static float[] gorgeArcs;
        private static float[,] gorgeRiverBaseline;
        private static List<Vector3> ComposeHeadwaters(List<Vector3> downstream)
        {
            var result=new List<Vector3>();
            // Exact lips retained; interpolating only XZ avoids overshoot in the water level.
            for(int k=0;k<gorgeStations.Length-1;k++)
            {
                var a=gorgeStations[k];var b=gorgeStations[k+1];int n=Mathf.CeilToInt(Flat(b-a).magnitude/.8f);
                for(int j=0;j<n;j++){var p=GorgeChannelPoint(k,j/(float)n);p.y=0;result.Add(p);}
            }
            var end=gorgeStations.Last();end.y=0;result.Add(end);result.AddRange(downstream.Where(p=>p.z<195));return result;
        }
        private static Vector3 GorgeChannelPoint(int k,float t)
        {
            var a=gorgeStations[k];var b=gorgeStations[k+1];
            if(k==2||k==5||k==8)return Vector3.Lerp(a,b,t);
            Func<int,Vector3> tangent=i=>
            {
                if(i==2||i==5||i==8)return Flat(gorgeStations[i+1]-gorgeStations[i]).normalized;
                if(i==3||i==6||i==9)return Flat(gorgeStations[i]-gorgeStations[i-1]).normalized;
                return Flat(gorgeStations[Mathf.Min(i+1,gorgeStations.Length-1)]-gorgeStations[Mathf.Max(i-1,0)]).normalized;
            };
            float length=Flat(b-a).magnitude*.85f,t2=t*t,t3=t2*t;
            var p=(2*t3-3*t2+1)*a+(t3-2*t2+t)*tangent(k)*length+(-2*t3+3*t2)*b+(t3-t2)*tangent(k+1)*length;
            p.y=Mathf.Lerp(a.y,b.y,t);return p;
        }
        private static void AddHeadwaterAnchors(List<Vector3> centers,float[] arcs,List<Vector2> anchors)
        {
            gorgeArcs=new float[gorgeStations.Length];
            for(int j=0;j<gorgeStations.Length;j++)
            {
                int i=Enumerable.Range(0,centers.Count).OrderBy(k=>Flat(centers[k]-gorgeStations[j]).sqrMagnitude).First();
                gorgeArcs[j]=arcs[i];anchors.Add(new Vector2(arcs[i],gorgeStations[j].y));
            }
        }
        private static float HeadwaterHalfWidth(float arc)
        {
            if(gorgeArcs!=null&&arc<=gorgeArcs.Last())
            {
                int j=1;while(j<gorgeArcs.Length-1&&arc>gorgeArcs[j])j++;
                return Mathf.Lerp(gorgeWidths[j-1],gorgeWidths[j],Mathf.SmoothStep(0,1,Mathf.InverseLerp(gorgeArcs[j-1],gorgeArcs[j],arc)));
            }
            return 3.5f+.45f*Mathf.Sin(arc*.043f)+.18f*Mathf.Sin(arc*.12f);
        }
        private static float RiverSurfaceHalfWidth(List<Vector3> centers,float[] arcs,int i)
        {
            float width=RiverHalfWidth(arcs[i])+1.95f;
            if(centers[i].z>195&&i>0&&i<centers.Count-1)
            {
                var a=Flat(centers[i]-centers[i-1]);var b=Flat(centers[i+1]-centers[i]);
                float sine=Mathf.Sin(Vector3.Angle(a,b)*Mathf.Deg2Rad*.5f);
                if(sine>.001f){float radius=(a.magnitude+b.magnitude)*.25f/sine;width=Mathf.Min(width,Mathf.Max(.8f,radius*.8f));}
            }
            return width;
        }
        private static void BuildAuthoredWaterfalls(Transform parent)
        {
            var group=new GameObject("River - three composed cascade drops").transform;group.SetParent(parent,false);
            var cascadeShader=Shader.Find("TowTrouble/Gorge Falling Water");if(cascadeShader==null)throw new InvalidOperationException("Water shader missing");
            var falling=new Material(cascadeShader){name="Gorge falling water"};falling=VisualAsset(falling,"Gorge falling water.mat");
            var foamShader=Shader.Find("TowTrouble/Gorge Foam");if(foamShader==null)throw new InvalidOperationException("Gorge foam shader missing");
            var foam=new Material(foamShader){name="Gorge aerated water"};foam.SetColor("_Color",new Color(.84f,.96f,.91f,.65f));foam.SetFloat("_Mist",0);foam=VisualAsset(foam,"Gorge aerated water.mat");
            var mist=new Material(foamShader){name="Gorge fine spray"};mist.SetColor("_Color",new Color(.88f,.96f,.94f,.16f));mist.SetFloat("_Mist",1);mist=VisualAsset(mist,"Gorge fine spray.mat");
            int index=0;
            foreach(int lip in new[]{2,5,8})
            {
                index++;var a=gorgeStations[lip];var b=gorgeStations[lip+1];var forward=Flat(b-a).normalized;var right=Vector3.Cross(Vector3.up,forward);
                // A subdivided sheet joins the complete lip width to its plunge pool.
                // The vendor waterfall includes a horizontal splash submesh; warping that with
                // a tall sheet produced floating triangles. Foam has its own surface below.
                var mesh=new Mesh{name="Gorge flowing curtain "+index};var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
                const int columns=16,rows=32;
                for(int row=0;row<=rows;row++)for(int col=0;col<=columns;col++)
                {
                    float t=row/(float)rows,u=col/(float)columns;
                    float width=Mathf.Lerp(gorgeWidths[lip]+1.95f,gorgeWidths[lip+1]+1.95f,t);
                    float bulge=Mathf.Sin(t*Mathf.PI)*(.65f+.1f*Mathf.Sin(u*25));
                    vertices.Add(Vector3.Lerp(a,b,t)+right*((u*2-1)*width)+forward*bulge+Vector3.up*.065f);uv.Add(new Vector2(u,1-t));
                    if(row<rows&&col<columns){int v=row*(columns+1)+col;triangles.AddRange(new[]{v,v+columns+1,v+1,v+1,v+columns+1,v+columns+2});}
                }
                mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
                var go=new GameObject("Cascade "+index+" - fitted flowing curtain",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(group,false);go.GetComponent<MeshFilter>().sharedMesh=VisualAsset(mesh,"Gorge native fall "+index+".asset");go.GetComponent<MeshRenderer>().sharedMaterial=falling;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                // Local foam fans, aligned with the gentle downstream pool grade.
                var pool=gorgeStations[lip+2];var along=(pool-b).normalized;var side=Vector3.Cross(Vector3.up,along).normalized;
                for(int patch=0;patch<3;patch++)
                {
                    var center=Vector3.Lerp(b,pool,.16f+patch*.23f)+Vector3.up*.11f;
                    var verts=new[]{center-side*(3.8f-patch*.6f)-along*2,center+side*(3.8f-patch*.6f)-along*2,center-side*(3.8f-patch*.6f)+along*3,center+side*(3.8f-patch*.6f)+along*3};
                    var fm=new Mesh{name="Plunge pool foam"};fm.vertices=verts;fm.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};fm.triangles=new[]{0,2,1,1,2,3};fm.RecalculateNormals();fm.RecalculateBounds();
                    var fg=new GameObject("Impact foam "+index+"."+patch,typeof(MeshFilter),typeof(MeshRenderer));fg.transform.SetParent(group,false);fg.GetComponent<MeshFilter>().sharedMesh=VisualAsset(fm,"Gorge foam "+index+"-"+patch+".asset");fg.GetComponent<MeshRenderer>().sharedMaterial=foam;fg.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                }
                var spray=new GameObject("Impact mist "+index);spray.transform.SetParent(group,false);spray.transform.position=b+Vector3.up*.65f;spray.transform.rotation=Quaternion.LookRotation(Vector3.up);
                var ps=spray.AddComponent<ParticleSystem>();var main=ps.main;main.playOnAwake=true;main.loop=true;main.duration=3;main.startLifetime=new ParticleSystem.MinMaxCurve(1.4f,2.2f);main.startSpeed=new ParticleSystem.MinMaxCurve(.6f,1.8f);main.startSize=new ParticleSystem.MinMaxCurve(1.2f,3.2f);main.startColor=Color.white;main.maxParticles=24;main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=ps.emission;emission.rateOverTime=8;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=35;shape.radius=2.2f;
                var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.65f,.2f),new GradientAlphaKey(0,1)});color.color=gradient;
                var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=mist;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
        }
    }
}

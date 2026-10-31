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
        private static List<Vector3> SmoothShore(List<Vector3> polygon,bool closed)
        {
            var result=new List<Vector3>();if(!closed)result.Add(polygon[0]);int end=closed?polygon.Count:polygon.Count-1;
            for(int i=0;i<end;i++){var a=polygon[i];var b=polygon[(i+1)%polygon.Count];result.Add(Vector3.Lerp(a,b,.25f));result.Add(Vector3.Lerp(a,b,.75f));}if(!closed)result.Add(polygon.Last());return result;
        }
        private static float Cross2(Vector3 a,Vector3 b,Vector3 c)=>(b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x);
        private static List<int> ShoreTriangles(List<Vector3> polygon)
        {
            var indices=Enumerable.Range(0,polygon.Count).ToList();float area=0;for(int i=0;i<polygon.Count;i++){var a=polygon[i];var b=polygon[(i+1)%polygon.Count];area+=a.x*b.z-b.x*a.z;}if(area<0)indices.Reverse();var triangles=new List<int>();int guard=polygon.Count*polygon.Count;
            while(indices.Count>2&&guard-->0)for(int i=0;i<indices.Count;i++)
            {
                int a=indices[(i+indices.Count-1)%indices.Count],b=indices[i],c=indices[(i+1)%indices.Count];if(Cross2(polygon[a],polygon[b],polygon[c])<=.0001f)continue;
                bool contains=indices.Any(p=>p!=a&&p!=b&&p!=c&&Cross2(polygon[a],polygon[b],polygon[p])>=0&&Cross2(polygon[b],polygon[c],polygon[p])>=0&&Cross2(polygon[c],polygon[a],polygon[p])>=0);if(contains)continue;
                triangles.AddRange(new[]{a,c,b});indices.RemoveAt(i);break;
            }
            if(indices.Count>2)throw new InvalidOperationException("Shore triangulation failed");return triangles;
        }
        private static void PolishWaterAndBridges()
        {
            var lakeFilter=environment.GetComponentsInChildren<MeshFilter>().First(f=>f.name.StartsWith("Lake -"));var points=lake.Select(p=>new Vector3((p.x-.5f)*900,10.4f,(.5f-p.y)*900)).ToList();for(int i=0;i<3;i++)points=SmoothShore(points,true);
            var mesh=new Mesh{name="Soft natural lake shore"};mesh.SetVertices(points);mesh.SetTriangles(ShoreTriangles(points),0);mesh.SetUVs(0,points.Select(p=>new Vector2(p.x*.02f,p.z*.02f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();lakeFilter.sharedMesh=VisualAsset(mesh,"Natural shoreline.asset");
            var renderer=lakeFilter.GetComponent<MeshRenderer>();var material=new Material(renderer.sharedMaterial){name="County turquoise water"};material.SetFloat("_Water_Saturation_Multiplier",.72f);material.SetFloat("_Caustic_Strength",7);material.SetFloat("_Shoreline_Fade_Depth",1.2f);material.SetFloat("_Shoreline_Foam_Opacity",.47f);material.SetFloat("_Reflection_Multiplier",.65f);material.SetFloat("_Normal_Strength",.2f);material=VisualAsset(material,"County water.mat");renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            var landmarks=environment.Find("World landmarks and river");if(landmarks==null)landmarks=ScenicGroup("World landmarks and river");foreach(var t in landmarks.Cast<Transform>().Where(t=>t.name.StartsWith("River")||t.name.StartsWith("Bridge")).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
            BuildConnectedRiver(material,landmarks);
        }
    }
}

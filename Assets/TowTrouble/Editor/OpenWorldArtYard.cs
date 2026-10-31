using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        [MenuItem("Tow Trouble/Art Integration/7 - Blend Workshop Approaches")]
        public static void BlendWorkshopApproaches()
        {
            VisualContext();ScenicPaths();ScenicRoadField(1025);
            var data=terrain.terrainData;var heights=data.GetHeights(0,0,1025,1025);
            var yard=nodes["g"].Point;int cx=Mathf.RoundToInt((yard.x+500)*1.024f),cz=Mathf.RoundToInt((yard.z+500)*1.024f);const int radius=62,n=125;
            var h=new float[n,n];var next=new float[n,n];var fixedHeight=new float[n,n];var fixedCell=new bool[n,n];
            var groups=new[]{"Authored biome groves and rock formations","Roadside undergrowth and gravel","Embedded rocky road banks"};
            var props=groups.Select(name=>environment.Find(name)).Where(t=>t!=null).SelectMany(t=>t.Cast<Transform>()).Where(t=>Flat(t.position-yard).magnitude<92).Select(t=>new{t,ground=Ground(t.position).y}).ToArray();
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                int ix=cx-radius+x,iz=cz-radius+z;var p=new Vector3(ix/1.024f-500,0,iz/1.024f-500);var relative=p-yard;
                h[z,x]=heights[iz,ix]*350-50;fixedHeight[z,x]=h[z,x];
                if(Mathf.Abs(relative.x)<11.5f&&relative.z>10&&relative.z<33||Flat(relative).magnitude<13.5f){fixedCell[z,x]=true;fixedHeight[z,x]=yard.y;}
                foreach(var pad in plan.Nodes.Where(v=>v.Id!="g"&&(v.Id.Length==1||v.Id.StartsWith("R"))))if(Flat(p-pad.Point).magnitude<(pad.Id.Length==1?8:5)){fixedCell[z,x]=true;fixedHeight[z,x]=pad.Height;}
                if(roadDistance[iz,ix]<roadWidth[iz,ix]+.65f){fixedCell[z,x]=true;fixedHeight[z,x]=roadHeight[iz,ix];}
                if(x==0||z==0||x==n-1||z==n-1){fixedCell[z,x]=true;fixedHeight[z,x]=h[z,x];}
            }
            for(int pass=0;pass<400;pass++)
            {
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)next[z,x]=fixedCell[z,x]?fixedHeight[z,x]:(h[z,x-1]+h[z,x+1]+h[z-1,x]+h[z+1,x])*.25f;
                var swap=h;h=next;next=swap;
            }
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)heights[cz-radius+z,cx-radius+x]=(h[z,x]+50)/350;
            data.SetHeights(0,0,heights);
            foreach(var prop in props)prop.t.position+=Vector3.up*(Ground(prop.t.position).y-prop.ground);
            var workshop=environment.Find("Central garage - Tow County");
            foreach(var beam in workshop.Cast<Transform>().Where(t=>t.name=="Roof brace")){float side=Mathf.Sign(beam.position.x-yard.x);beam.rotation=Quaternion.Euler(0,0,-side*14);}
            PaintFinishedGround();data.terrainLayers[0].diffuseRemapMax=new Color(.64f,.72f,.51f);EditorUtility.SetDirty(data.terrainLayers[0]);
            BuildRoadWear();data.SetBaseMapDirty();terrain.Flush();EditorUtility.SetDirty(data);SaveVisualScene();
        }
    }
}

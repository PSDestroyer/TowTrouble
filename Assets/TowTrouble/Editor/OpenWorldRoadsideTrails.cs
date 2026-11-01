using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static void DressRoadsideAccidentTrail(Transform group,RecoverySite site,Vector3 road)
        {
            bool snow=site.Id=="R12",sand=site.Id=="R23";int layer=snow?4:sand?5:1;
            var tint=snow?new Color(.61f,.72f,.80f,.96f):sand?new Color(.77f,.57f,.36f,.93f):new Color(.54f,.40f,.27f,.85f);
            var material=SituationMaterial("Roadside skid "+layer,tint,layer);
            material.SetFloat("_Compaction",snow?.85f:.76f);material.enableInstancing=true;EditorUtility.SetDirty(material);
            var forward=Quaternion.Euler(0,site.Heading,0)*Vector3.forward;var side=Vector3.Cross(Vector3.up,site.ExitDirection);
            var p0=road+side*(2+site.Tier*.5f);var p1=road-site.ExitDirection*2+side*(2+site.Tier*.7f);
            var p2=site.Position+forward*4;var p3=site.Position-forward*2.8f;
            int rows=Mathf.CeilToInt(Vector3.Distance(p0,p3)/.3f);var points=new List<Vector3>();float length=0;
            for(int i=0;i<=rows;i++){float t=i/(float)rows;points.Add(Ground(Mathf.Pow(1-t,3)*p0+3*Mathf.Pow(1-t,2)*t*p1+3*(1-t)*t*t*p2+t*t*t*p3));}
            for(int lane=0;lane<2;lane++)
            {
                var root=new GameObject(lane==0?"Left accident skid and wheel rut":"Right accident skid and wheel rut",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));root.transform.SetParent(group,false);root.layer=8;
                var vs=new List<Vector3>();var uv=new List<Vector2>();var rut=new List<Vector2>();var ends=new List<Vector2>();var colors=new List<Color>();var ts=new List<int>();length=0;
                for(int i=0;i<=rows;i++)
                {
                    if(i>0)length+=Vector3.Distance(points[i],points[i-1]);var direction=Flat(points[Mathf.Min(rows,i+1)]-points[Mathf.Max(0,i-1)]).normalized;var across=Vector3.Cross(Vector3.up,direction);
                    float t=i/(float)rows;float churn=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,.9f,t));float halfWidth=Mathf.Lerp(.46f,.46f+site.Tier*.065f,churn);
                    for(int c=0;c<7;c++)
                    {
                        float u=c/6f;float offset=(lane==0?-.81f:.81f)+(u*2-1)*halfWidth;
                        var p=Ground(points[i]+across*offset);float bank=(snow?.11f:sand?.08f:.045f)*(site.Tier/4f)*churn*Mathf.Pow(Mathf.Abs(u*2-1),2);
                        p.y+=.026f+bank;vs.Add(root.transform.InverseTransformPoint(p));uv.Add(new Vector2(p.x,p.z)*.35f);rut.Add(new Vector2(u,length+.05f));ends.Add(new Vector2((1-t)*Vector3.Distance(p0,p3)/.7f,1));colors.Add(new Color(1,1,1,Mathf.Lerp(.72f,1,churn)));
                        if(i<rows&&c<6){int a=i*7+c;ts.AddRange(new[]{a,a+7,a+1,a+1,a+7,a+8});}
                    }
                }
                var mesh=new Mesh{name=site.Id+" accident skid "+lane};mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetUVs(1,rut);mesh.SetUVs(2,ends);mesh.SetColors(colors);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                root.GetComponent<MeshFilter>().sharedMesh=VisualAsset(mesh,"RecoverySituations/"+mesh.name+".asset");root.GetComponent<MeshRenderer>().sharedMaterial=material;root.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                root.GetComponent<MeshCollider>().sharedMesh=root.GetComponent<MeshFilter>().sharedMesh;
                root.AddComponent<RecoveryTrackSurface>().Configure(snow?RecoveryTrackKind.Snow:sand?RecoveryTrackKind.Sand:RecoveryTrackKind.Dirt);
                if(!snow&&!sand){var mud=root.AddComponent<RecoveryMudTerrain>();mud.ConfigureTracks(false);}
                else{var surface=root.AddComponent<RecoveryGroundSurface>();surface.Configure(snow?RecoveryGroundKind.Snow:RecoveryGroundKind.Sand);surface.ConfigureSoftness(.25f+site.Tier*.12f);}
            }
            var lod=group.GetComponent<LODGroup>();if(lod==null)lod=group.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.006f,group.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
        }
    }
}

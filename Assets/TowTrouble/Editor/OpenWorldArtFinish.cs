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
        [MenuItem("Tow Trouble/Art Integration/5 - Ground Cover and Geological Backdrop")]
        public static void FinishArtComposition()
        {
            VisualContext();ScenicPaths();ScenicRoadField(1025);artRandom=new System.Random(92343);
            var horizon=ReplaceArtGroup("County horizon");
            for(int i=0;i<22;i++)
            {
                float angle=i/22f*Mathf.PI*2;var dir=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var p=dir*((840+Pick(-30,70))/Mathf.Max(Mathf.Abs(dir.x),Mathf.Abs(dir.z)));bool desert=p.x>340&&p.z<220;
                var go=ArtPrefab(desert?"P_RockDesert_CliffA01":i%2==0?"P_Mountain03":"P_Mountain02",p,desert?Pick(140,210):Pick(150,255),horizon,false);
                var bounds=ScenicBounds(go);float width=Mathf.Max(bounds.size.x,bounds.size.z);if(width>500)go.transform.localScale*=500/width;bounds=ScenicBounds(go);go.transform.position+=new Vector3(p.x-bounds.center.x,-18-bounds.min.y,p.z-bounds.center.z);
                foreach(var r in go.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
            }
            BuildHorizonApron(horizon);
            var banks=ReplaceArtGroup("Embedded rocky road banks");
            foreach(var path in scenicRoads)for(int i=8;i<path.Points.Length-8;i+=17)
            {
                var p=path.Points[i];if(p.y<45&&!DesertAt(p))continue;
                var forward=Flat(path.Points[i+1]-path.Points[i-1]).normalized;
                foreach(int s in new[]{-1,1})
                {
                    var side=Vector3.Cross(Vector3.up,forward)*s;var q=Ground(p+side*(path.Width+8));float rise=q.y-p.y;
                    if(rise<3.2f)continue;
                    float height=Mathf.Clamp(rise*1.6f,4,12);var go=ArtPrefab(DesertAt(q)?"P_RockDesert_Rock08":"P_BoulderClassic3",q,height,banks,true);
                    var b=ScenicBounds(go);float radius=Mathf.Max(b.extents.x,b.extents.z);
                    if(ScenicDistance(b.center,out _,out var near)<near.Width+radius+1.4f)
                    {q=Ground(p+side*(path.Width+radius+2));go.transform.position+=q-b.center; b=ScenicBounds(go);go.transform.position+=Vector3.up*(q.y-b.min.y);}
                    go.transform.position-=Vector3.up*height*.38f;
                }
            }
            BuildMountainFormations(); BuildRiverBanks(); BuildInstancedGroundCover();
            var grass=terrain.terrainData.terrainLayers[0];grass.diffuseRemapMax=new Color(.64f,.72f,.51f);EditorUtility.SetDirty(grass);
            GradeSceneMaterials();
            foreach(var lod in environment.GetComponentsInChildren<LODGroup>())
            {
                var levels=lod.GetLODs();for(int i=0;i<levels.Length;i++)levels[i].screenRelativeTransitionHeight=i==levels.Length-1?.004f:.22f*Mathf.Pow(.32f,i);
                lod.SetLODs(levels);
            }
            // Remove decorations whose actual mesh footprint intersects another nearby route.
            foreach(var groupName in new[]{"Embedded rocky road banks","Authored biome groves and rock formations","Roadside undergrowth and gravel","Lakeshore rocks reeds and driftwood"})
            {
                var group=environment.Find(groupName);if(group==null)continue;
                foreach(var child in group.Cast<Transform>().ToArray())
                {
                    var colliders=child.GetComponentsInChildren<Collider>();if(colliders.Length==0)continue;
                    var b=colliders[0].bounds;foreach(var c in colliders)b.Encapsulate(c.bounds);
                    float radius=Mathf.Min(b.extents.x,b.extents.z);
                    if(ScenicDistance(b.center,out var p,out var road)<road.Width+radius+.8f&&b.max.y>p.y+.3f&&b.min.y<p.y+4)UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
            SaveVisualScene();
        }
        private static void BuildHorizonApron(Transform parent)
        {
            var vs=new List<Vector3>();var uv=new List<Vector2>();var ts=new List<int>();const int count=192;
            for(int r=0;r<3;r++)for(int i=0;i<count;i++)
            {
                float a=i/(float)count*Mathf.PI*2;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));float edge=500/Mathf.Max(Mathf.Abs(d.x),Mathf.Abs(d.z));var p=d*(edge+r*180);p.y=Ground(d*edge).y-.4f-r*12;vs.Add(p);uv.Add(new Vector2(p.x*.07f,p.z*.07f));
                if(r<2){int a0=r*count+i,b=r*count+(i+1)%count,c=a0+count,e=b+count;ts.AddRange(new[]{a0,b,c,b,e,c});}
            }
            var mesh=new Mesh{name="Distant foothill apron"};mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var mat=ScenicMaterial("Distant foothill soil",new Color(.57f,.62f,.46f));mat.SetTexture("_BaseMap",terrain.terrainData.terrainLayers[0].diffuseTexture);EditorUtility.SetDirty(mat);
            var go=new GameObject("Foothills below mountain silhouettes",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=VisualAsset(mesh,"Distant foothill apron.asset");go.GetComponent<MeshRenderer>().sharedMaterial=mat;
        }
        private static void BuildInstancedGroundCover()
        {
            var data=terrain.terrainData;var old=AssetDatabase.LoadAssetAtPath<TerrainData>(VisualRoot+"/Art integration source terrain.asset").detailPrototypes;var prototypes=new[]{old[0],old[1],old[5]};
            for(int i=0;i<3;i++){prototypes[i].minHeight=i==1?.5f:.6f;prototypes[i].maxHeight=i==1?.9f:1.0f;prototypes[i].minWidth=.65f;prototypes[i].maxWidth=1.1f;prototypes[i].useInstancing=true;}
            for(int i=0;i<prototypes.Length;i++)
            {
                var copy=UnityEngine.Object.Instantiate(prototypes[i].prototype);copy.name="County ground cover "+i;
                foreach(var r in copy.GetComponentsInChildren<Renderer>())
                {
                    var mats=r.sharedMaterials;
                    for(int m=0;m<mats.Length;m++)
                    {
                        var owned=new Material(mats[m]);owned.name="County ground cover "+i+" material "+m;
                        if(i==0)foreach(string prop in new[]{"_Bottom_Color","_Tip_Color","_Tip_Color_2"})if(owned.HasProperty(prop)){var c=owned.GetColor(prop);var tint=prop=="_Bottom_Color"?new Color(.31f,.40f,.18f,c.a):prop=="_Tip_Color"?new Color(.63f,.65f,.30f,c.a):new Color(.48f,.56f,.25f,c.a);owned.SetColor(prop,tint);}
                        mats[m]=VisualAsset(owned,owned.name+".mat");
                    }
                    r.sharedMaterials=mats;
                }
                foreach(var c in copy.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
                prototypes[i].prototype=PrefabUtility.SaveAsPrefabAsset(copy,VisualRoot+"/County ground cover "+i+".prefab");UnityEngine.Object.DestroyImmediate(copy);
            }
            data.detailPrototypes=prototypes;const int n=512;data.SetDetailResolution(n,16);
            var layers=new[]{new int[n,n],new int[n,n],new int[n,n]};
            var pads=plan.Nodes.Where(p=>p.Id=="g"||p.Id.Length==1||p.Id.StartsWith("R")).ToArray();
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                var p=Ground(new Vector3(x/(float)(n-1)*1000-500,0,z/(float)(n-1)*1000-500));int rx=Mathf.RoundToInt(x/(float)(n-1)*1024),rz=Mathf.RoundToInt(z/(float)(n-1)*1024);
                float distance=roadDistance[rz,rx],width=roadWidth[rz,rx],noise=ArtNoise(p.x,p.z,.075f);
                if(RiverDistance(p)<9||!ClearRecoveryAccess(p,1)||distance<width+.8f||distance>65||ShoreDistance(p)<11||DesertAt(p)||p.z<-365||noise<.36f)continue;
                if(pads.Any(pad=>Flat(p-pad.Point).magnitude<(pad.Id=="g"?30:pad.Id.Length==1?17:8)))continue;
                if(data.GetSteepness((p.x+500)/1000,(p.z+500)/1000)>34)continue;
                if(p.y>125){if(noise>.6f)layers[2][z,x]=1;}
                else {layers[0][z,x]=noise>.55f?3:1;if((x*13+z*7)%19==0)layers[1][z,x]=1;}
            }
            for(int i=0;i<3;i++)data.SetDetailLayer(0,0,i,layers[i]);
            terrain.detailObjectDensity=1;terrain.detailObjectDistance=105;EditorUtility.SetDirty(data);
        }
    }
}

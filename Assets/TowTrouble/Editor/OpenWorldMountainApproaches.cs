using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static readonly Vector3 AlpineDelivery = new Vector3(-11, 114, 254);
        private static readonly Vector3 AlpineShelter = new Vector3(9, 114, 250);
        private static readonly Vector3 MeadowShelter = new Vector3(77, 65, 137);
        private static readonly Vector3[] MeadowIslands = {
            new Vector3(-7,0,112), new Vector3(8,0,68), new Vector3(35,0,123),
            new Vector3(78,0,90), new Vector3(119,0,116), new Vector3(112,0,62),
            new Vector3(-30,0,150), new Vector3(104,0,158)
        };
        [MenuItem("Tow Trouble/Art Integration/9 - Compose Mountain Approaches and Refuges")]
        public static void ComposeMountainApproaches()
        {
            RecomposeMountainApproaches(); BakeGroundSurfaceMap(); ConfigureWheelTracks();
            CaptureMapPhotosWhere(p => p.x > -120 && p.x < 210 && p.z > 100 && p.z < 355);
            Debug.Log("[TowTrouble] Mountain junction, delivery refuges, lower escarpment and meadow composed in Edit Mode.");
        }
        private static float ApproachMask(Vector3 p)
        {
            float wall = Smooth01(-90,-72,p.x)*(1-Smooth01(135,157,p.x))*Smooth01(180,192,p.z)*(1-Smooth01(231,243,p.z));
            float meadow = Smooth01(-47,-30,p.x)*(1-Smooth01(135,158,p.x))*Smooth01(45,64,p.z)*(1-Smooth01(155,175,p.z));
            float bend = 1-Smooth01(33,48,Vector2.Distance(new Vector2(p.x,p.z),new Vector2(-11,254)));
            float junction = 1-Smooth01(31,55,Vector2.Distance(new Vector2(p.x,p.z),new Vector2(99,289)));
            float platform = 1-Smooth01(24,54,Vector2.Distance(new Vector2(p.x,p.z),new Vector2(99,279)));
            return Mathf.Max(wall,meadow,bend,junction,platform);
        }
        private static void RecomposeMountainApproaches()
        {
            VisualContext(); Undo.IncrementCurrentGroup(); int undoGroup=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Compose mountain approaches"); ScenicPaths(); ScenicRoadField(1025); artRandom = new System.Random(742119);
            var data = terrain.terrainData; int n = data.heightmapResolution;
            if(n != 1025 || data.alphamapWidth != 1024) throw new InvalidOperationException("Mountain approach pass expects the authored Tow County terrain resolution.");
            Undo.RegisterCompleteObjectUndo(data,"Compose mountain approaches");
            // Saved baselines are project-owned; reapplying never accumulates cuts or paint.
            string hp = VisualRoot + "/Approaches original heights.bytes";
            if(!File.Exists(hp)){var values=data.GetHeights(0,0,n,n);var bytes=new byte[n*n*4];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);File.WriteAllBytes(hp,bytes);AssetDatabase.ImportAsset(hp);}
            var raw=File.ReadAllBytes(hp);if(raw.Length!=n*n*4)throw new InvalidOperationException("Approaches height baseline mismatch");
            var original=new float[n,n];Buffer.BlockCopy(raw,0,original,0,raw.Length);
            var h=data.GetHeights(0,0,n,n);
            for(int z=540;z<866;z++)for(int x=400;x<680;x++)
            {
                var p=new Vector3(x/1.024f-500,0,z/1.024f-500);float mask=ApproachMask(p);
                if(mask<=0)continue;h[z,x]=original[z,x];
                float clear=roadDistance[z,x]-roadWidth[z,x];
                if(clear<1.1f || !ClearRecoveryAccess(p,1))continue;
                float elevation=h[z,x]*350-50;
                // Distinct ribs and small gullies break the old straight, uniform escarpment.
                if(p.z>185&&p.z<242)
                {
                    float wall=Smooth01(186,199,p.z)*(1-Smooth01(223,242,p.z))*Smooth01(-90,-72,p.x)*(1-Smooth01(135,157,p.x));
                    float detail=0;
                    foreach(var f in new[]{new Vector4(-54,211,14,4),new Vector4(-27,214,9,-4),new Vector4(0,207,16,5),new Vector4(32,210,10,-4),new Vector4(56,211,20,4),new Vector4(88,216,11,-3)})
                    {float dx=(p.x-f.x)/f.z,dz=(p.z-f.y)/16;detail+=f.w*Mathf.Exp(-dx*dx-dz*dz);}
                    elevation+=detail*wall*Smooth01(5,13,clear);
                }
                if(p.z<174)
                {
                    float undulation=(ArtNoise(p.x,p.z,.052f)-.5f)*.8f;
                    elevation+=undulation*mask*Smooth01(10,20,clear);
                    float meadowBay=1-Smooth01(10,18,Flat(p-nodes["C"].Point).magnitude);
                    elevation=Mathf.Lerp(elevation,nodes["C"].Height,meadowBay);
                }
                float pad=1-Smooth01(10,24,Flat(p-AlpineDelivery).magnitude);
                float cabin=1-Smooth01(6,16,Flat(p-AlpineShelter).magnitude);
                if(pad>0||cabin>0)elevation=Mathf.Lerp(elevation,114,Mathf.Max(pad,cabin)*Smooth01(1.1f,4,clear));
                float mc=1-Smooth01(6,14,Flat(p-MeadowShelter).magnitude);
                elevation=Mathf.Lerp(elevation,MeadowShelter.y,mc*Smooth01(1.1f,4,clear));
                h[z,x]=(elevation+50)/350;
            }
            // The two meeting roads previously overlapped at different heights. Give the
            // shared junction one gently banked platform before blending back to each grade.
            for(int z=740;z<862;z++)for(int x=555;x<675;x++)
            {
                var p=new Vector3(x/1.024f-500,0,z/1.024f-500);
                float distance=Flat(p-new Vector3(99,0,279)).magnitude;
                float weight=(1-Smooth01(24,54,distance))*(1-Smooth01(291,306,p.z));
                float plane=126+(p.x-99)*.04f+(p.z-279)*.32f;
                if(weight>0)h[z,x]=Mathf.Lerp(h[z,x],(plane+50)/350,weight);
            }
            // Constrained relaxation smooths shoulders while keeping the newly graded cores.
            var next=(float[,])h.Clone();
            for(int pass=0;pass<340;pass++)
            {
                for(int z=740;z<862;z++)for(int x=555;x<675;x++)
                {
                    var p=new Vector3(x/1.024f-500,0,z/1.024f-500);float d=Flat(p-new Vector3(99,0,289)).magnitude;
                    if(d>55||roadDistance[z,x]<roadWidth[z,x]+1.1f){next[z,x]=h[z,x];continue;}
                    float mean=(h[z,x-1]+h[z,x+1]+h[z-1,x]+h[z+1,x])*.25f;
                    next[z,x]=Mathf.Lerp(h[z,x],mean,.6f*(1-Smooth01(37,55,d)));
                }
                var swap=h;h=next;next=swap;
            }
            data.SetHeights(0,0,h); terrain.Flush();
            RemoveApproachScatter();
            var previousGroup=environment.Find("Mountain approaches - refuges and meadow");
            if(previousGroup!=null)Undo.DestroyObjectImmediate(previousGroup.gameObject);
            var parent=new GameObject("Mountain approaches - refuges and meadow").transform;parent.SetParent(environment,false);
            var stone=GorgeRockMaterial("Approaches warm alpine rock",new Color(.79f,.80f,.71f),false,false);
            var shaded=GorgeRockMaterial("Approaches shaded alpine rock",new Color(.65f,.71f,.65f),false,false);
            BuildApproachEscarpment(parent,stone,shaded);
            DressMountainApproaches(parent,stone);
            BuildApproachRefuge(parent,AlpineShelter,"POSTUL ALPIN",-12);
            BuildApproachRefuge(parent,MeadowShelter,"REFUGIUL MONTAN",0);
            FitApproachDelivery("D",AlpineDelivery,AlpineShelter,parent);
            FitApproachDelivery("C",nodes["C"].Point,MeadowShelter,parent);
            PaintMountainApproaches(); RefitApproachWear();
            Undo.RegisterCreatedObjectUndo(parent.gameObject,"Create mountain refuges and scenery");
            Undo.CollapseUndoOperations(undoGroup);data.SetBaseMapDirty();EditorUtility.SetDirty(data);SaveVisualScene();
        }
        private static void RemoveApproachScatter()
        {
            foreach(var name in new[]{"Mountain outcrops and scree","Authored biome groves and rock formations","Roadside undergrowth and gravel","Embedded rocky road banks"})
            {
                var group=environment.Find(name);if(group==null)continue;
                foreach(var t in group.Cast<Transform>().ToArray())
                {
                    var p=ScenicBounds(t.gameObject).center;
                    if(ApproachMask(p)>.15f)Undo.DestroyObjectImmediate(t.gameObject);
                }
            }
        }
        private static void BuildApproachEscarpment(Transform parent,Material stone,Material shaded)
        {
            var group=new GameObject("Layered lower mountain escarpment").transform;group.SetParent(parent,false);
            var centers=new[]{new Vector3(-66,82,214),new Vector3(-48,78,203),new Vector3(-13,87,215),new Vector3(8,79,202),new Vector3(47,87,216),new Vector3(68,83,207),new Vector3(88,91,217)};
            var sizes=new[]{new Vector3(36,26,20),new Vector3(20,13,18),new Vector3(27,25,20),new Vector3(31,13,21),new Vector3(35,20,21),new Vector3(25,14,22),new Vector3(19,13,16)};
            for(int k=0;k<centers.Length;k++)AddApproachCliff("P_CliffClassic"+new[]{3,6,2,5,4,6,3}[k],centers[k],sizes[k],new[]{17,-12,27,-18,19,-9,33}[k],k%3==0?shaded:stone,group,k);
            foreach(var center in centers)
            for(int j=0;j<18;j++)
            {
                var p=Ground(center+new Vector3(Pick(-13,13),0,Pick(-12,9)));float size=j<4?Pick(2,4):Pick(.35f,1.25f);
                if(!ApproachClear(p,size+1)||p.z>236)continue;
                if(j<9){var rock=ArtPrefab("P_RockClumpClassic"+(j%10+1),p,size,group,size>1.6f);SettleApproachRock(rock,p,size);TintApproachRock(rock,j%3==0?shaded:stone);}
                else if(terrain.terrainData.GetSteepness((p.x+500)/1000,(p.z+500)/1000)<38)PlantApproach(j%2==0?"P_Grass2":"P_Fern2",p,Pick(.4f,.85f),group,false);
            }
        }
        private static void AddApproachCliff(string prefab,Vector3 center,Vector3 size,float yaw,Material material,Transform parent,int index)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(prefab));var mf=source.GetComponentsInChildren<MeshFilter>().First(f=>f.sharedMesh!=null);
            var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);var b=mesh.bounds;var v=mesh.vertices;var normals=mesh.normals;var rotation=Quaternion.Euler(0,yaw,0);
            for(int i=0;i<v.Length;i++)
            {
                float t=Mathf.InverseLerp(b.min.y,b.max.y,v[i].y);var q=Vector3.Scale(v[i]-b.center,new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z));
                q.x*=1+(1-t)*.3f;q.z*=1+(1-t)*.25f;q=center+rotation*q;
                float ground=Ground(q).y;float bottom=Mathf.Min(center.y-size.y*.5f,ground-2.5f);
                float top=center.y+size.y*.5f+Mathf.Sin(q.x*.31f+q.z*.22f)*1.4f;
                q.y=Mathf.Lerp(bottom,top,t);
                if(t<.63f||normals[i].y<-.05f)q.y=Mathf.Min(q.y,ground-2);
                int ix=Mathf.Clamp(Mathf.RoundToInt((q.x+500)*1.024f),0,1024),iz=Mathf.Clamp(Mathf.RoundToInt((q.z+500)*1.024f),0,1024);
                float clear=roadDistance[iz,ix]-roadWidth[iz,ix];if(clear<8)q.y=Mathf.Lerp(Mathf.Min(q.y,roadHeight[iz,ix]-1.2f),q.y,Smooth01(3,8,clear));
                v[i]=q;
            }
            mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();mesh=VisualAsset(mesh,"Approach cliff "+index.ToString("00")+".asset");
            var go=new GameObject("Embedded escarpment "+index+" - "+prefab,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.layer=8;go.isStatic=true;go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshCollider>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=Enumerable.Repeat(material,mesh.subMeshCount).ToArray();
        }
        private static void SettleApproachRock(GameObject rock,Vector3 position,float height)
        {
            var normal=terrain.terrainData.GetInterpolatedNormal((position.x+500)/1000,(position.z+500)/1000);
            rock.transform.rotation=Quaternion.FromToRotation(Vector3.up,Vector3.Slerp(Vector3.up,normal,.8f))*rock.transform.rotation;
            var bounds=ScenicBounds(rock);
            rock.transform.position+=Ground(position)+normal*(height*.07f)-bounds.center;
        }
        private static bool ApproachClear(Vector3 p,float radius)
        {
            if(ScenicDistance(p,out _,out var road)-road.Width<radius+3||!ClearRecoveryAccess(p,radius)||RiverDistance(p)<radius+8)return false;
            foreach(var bay in plan.Deliveries){var center=bay.Id=="D"?AlpineDelivery:nodes[bay.Node].Point;if(Flat(p-center).magnitude<11+radius)return false;}
            if(Flat(p-AlpineShelter).magnitude<9+radius||Flat(p-MeadowShelter).magnitude<9+radius)return false;
            // Keep the new off-road delivery entrance and both refuge forecourts open.
            if(Distance(new Vector2(p.x,p.z),new Vector2(-27,265.5f),new Vector2(AlpineDelivery.x,AlpineDelivery.z),out _)<6+radius)return false;
            return true;
        }
        private static void TintApproachRock(GameObject go,Material material)
        {foreach(var r in go.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>material).ToArray();}
        private static GameObject PlantApproach(string name,Vector3 p,float height,Transform parent,bool tree)
        {
            var go=ArtPrefab(name,p,height,parent,tree,tree);
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m==null?null:AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Native "+m.name+".mat")??m).ToArray();
            return go;
        }
        private static void DressMountainApproaches(Transform parent,Material rock)
        {
            var groves=new GameObject("Meadow tree islands and alpine scrub").transform;groves.SetParent(parent,false);
            int id=0;
            foreach(var anchor in MeadowIslands.Concat(new[]{new Vector3(22,0,246),new Vector3(43,0,252),new Vector3(15,0,265),new Vector3(76,0,306),new Vector3(126,0,315)}))
            {
                id++;bool alpine=anchor.z>190;
                for(int i=0;i<7;i++)
                {
                    var p=Ground(anchor+new Vector3(Pick(-9,9),0,Pick(-8,8)));
                    if(!ApproachClear(p,3.4f)||terrain.terrainData.GetSteepness((p.x+500)/1000,(p.z+500)/1000)>29)continue;
                    PlantApproach(alpine?"P_FirTree4":i%3==0?"P_FirTree4":"P_BirchTree"+(i%2==0?1:3),p,Pick(alpine?7:6,alpine?11:10),groves,true);
                }
                for(int i=0;i<42;i++)
                {
                    var p=Ground(anchor+new Vector3(Pick(-14,14),0,Pick(-12,12)));
                    if(!ApproachClear(p,1.2f)||terrain.terrainData.GetSteepness((p.x+500)/1000,(p.z+500)/1000)>34)continue;
                    if(i<3){var go=ArtPrefab("P_RockClumpClassic"+(id%10+1),p,Pick(1.1f,2.6f),groves,true);SettleApproachRock(go,p,ScenicBounds(go).size.y);TintApproachRock(go,rock);}
                    else PlantApproach(i%6==0?"P_Bush1_Light":i%4==0?"P_Daisies":i%3==0?"P_Fern2":"P_Grass2",p,Pick(.35f,i%6==0?1.2f:.7f),groves,false);
                }
            }
            // Short, irregular timber barriers mark the dangerous outside of the hairpin.
            var timber=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop aged timber.mat");
            var bend=scenicRoads.First(r=>r.Name.Contains("turn1-turn2"));
            for(int i=bend.Points.Length-29;i<bend.Points.Length-4;i+=8)
            {
                var direction=Flat(bend.Points[Mathf.Min(i+1,bend.Points.Length-1)]-bend.Points[Mathf.Max(0,i-1)]).normalized;
                var side=Vector3.Cross(Vector3.up,direction);var p=Ground(bend.Points[i]+side*(bend.Width+3.5f));
                if(ScenicDistance(p,out _,out var r)-r.Width<3.1f)continue;
                var post=Cube("Hairpin timber verge post",p+Vector3.up*.65f,new Vector3(.2f,1.3f,.2f),timber,parent);
                post.transform.rotation=Quaternion.LookRotation(direction);
                var rail=Cube("Hairpin timber verge rail",p+Vector3.up*1.05f,new Vector3(.14f,.16f,4.7f),timber,parent,false);rail.transform.rotation=post.transform.rotation;
            }
        }
        private static void PaintMountainApproaches()
        {
            var data=terrain.terrainData;const int bx=400,bz=540,bw=280,bh=326;int layers=data.alphamapLayers;
            string path=VisualRoot+"/Approaches original paint.bytes";int length=bw*bh*layers*4;
            if(!File.Exists(path)){var old=data.GetAlphamaps(bx,bz,bw,bh);var bytes=new byte[length];Buffer.BlockCopy(old,0,bytes,0,length);File.WriteAllBytes(path,bytes);AssetDatabase.ImportAsset(path);}
            var saved=File.ReadAllBytes(path);if(saved.Length!=length)throw new InvalidOperationException("Approach paint baseline mismatch");
            var original=new float[bh,bw,layers];Buffer.BlockCopy(saved,0,original,0,length);var alpha=data.GetAlphamaps(bx,bz,bw,bh);
            for(int z=0;z<bh;z++)for(int x=0;x<bw;x++)
            {
                float wx=(x+bx)/1023f*1000-500,wz=(z+bz)/1023f*1000-500;var p=new Vector3(wx,0,wz);float mask=ApproachMask(p);if(mask<=0)continue;
                for(int l=0;l<layers;l++)alpha[z,x,l]=original[z,x,l];
                float slope=data.GetSteepness((wx+500)/1000,(wz+500)/1000);float noise=ArtNoise(wx,wz,.075f);
                int ix=Mathf.RoundToInt((wx+500)*1.024f),iz=Mathf.RoundToInt((wz+500)*1.024f);float d=roadDistance[iz,ix],width=roadWidth[iz,ix];
                float outside=Smooth01(width+1,width+6,d);
                if(wz<175)
                {
                    float mulch=0;foreach(var island in MeadowIslands){float r=Flat(p-island).magnitude;mulch=Mathf.Max(mulch,(1-Smooth01(5,17+(noise-.5f)*7,r))*.64f);}
                    BlendLayer(alpha,z,x,6,mulch*mask*outside);
                    BlendLayer(alpha,z,x,1,(1-Smooth01(.13f,.27f,noise))*.25f*mask*outside);
                }
                else
                {
                    BlendLayer(alpha,z,x,3,Smooth01(27,47,slope)*mask*.9f);
                    BlendLayer(alpha,z,x,6,(1-Smooth01(22,40,slope))*(.1f+noise*.15f)*mask*outside);
                }
                float shoulder=(1-Smooth01(width+.3f,width+3.7f+(noise-.5f)*2,d))*(1-Smooth01(24,42,slope));
                BlendLayer(alpha,z,x,1,shoulder*.8f*mask);
                foreach(var center in new[]{AlpineDelivery,nodes["C"].Point})
                {float dist=Flat(p-center).magnitude;BlendLayer(alpha,z,x,1,(1-Smooth01(9,15+(noise-.5f)*2,dist))*.93f);}
                foreach(var cabin in new[]{AlpineShelter,MeadowShelter})
                {float dist=Flat(p-cabin).magnitude;BlendLayer(alpha,z,x,6,(1-Smooth01(6,10,dist))*.65f);}
                float entry=Distance(new Vector2(wx,wz),new Vector2(-27,265.5f),new Vector2(AlpineDelivery.x,AlpineDelivery.z),out _);
                BlendLayer(alpha,z,x,1,(1-Smooth01(3.8f,7,entry))*.92f);
            }
            data.SetAlphamaps(bx,bz,alpha);
        }
        private static void RefitApproachWear()
        {
            var group=environment.Find("Road wheel tracks and gravel");if(group==null)return;
            foreach(var f in group.GetComponentsInChildren<MeshFilter>())
            {
                var old=f.sharedMesh;if(!old.vertices.Any(p=>ApproachMask(p)>0))continue;
                Undo.RegisterCompleteObjectUndo(old,"Refit mountain wheel wear");
                var mesh=UnityEngine.Object.Instantiate(old);var v=mesh.vertices;var c=mesh.colors;
                for(int i=0;i<v.Length;i++)
                {
                    var p=v[i];if(ApproachMask(p)<=0)continue;p.y=Ground(p).y-.025f;v[i]=p;
                    // Reconstruct rather than multiply alpha, so repeated authoring is stable.
                    float centerAlpha=i%3==1?.7f+ArtNoise(p.x,p.z,.12f)*.3f:0;
                    float junction=1-Smooth01(7,16,Flat(p-nodes["turn3"].Point).magnitude);
                    c[i].a=centerAlpha*(1-junction*.78f);
                }
                mesh.vertices=v;mesh.colors=c;mesh.RecalculateNormals();mesh.RecalculateBounds();f.sharedMesh=VisualAsset(mesh,Path.GetFileName(AssetDatabase.GetAssetPath(old)));
            }
        }
    }
}

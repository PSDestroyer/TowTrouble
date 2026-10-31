using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        [MenuItem("Tow Trouble/Art Integration/8 - Compose Mountain Gorge")]
        public static void ComposeMountainGorge()
        {
            RecomposeMountainGorge();RecomposeMountainApproaches();BakeGroundSurfaceMap();ConfigureWheelTracks();CaptureMapPhotosWhere(p=>p.z>165&&p.x>-235&&p.x<175);
            Debug.Log("[TowTrouble] Three cascade drops, plunge pools and native cliff massifs composed in Edit Mode.");
        }
        private static bool InGorgePilot(Vector3 p)=>p.x>-192&&p.x<-78&&p.z>183&&p.z<315;
        private static bool InMountainPilot(Vector3 p)=>p.x>-110&&p.x<156&&p.z>245&&p.z<385;
        private static void RecomposeMountainGorge()
        {
            VisualContext();ScenicPaths();ScenicRoadField(1025);cachedRiverPlan=null;artRandom=new System.Random(830125);
            var data=terrain.terrainData;int n=data.heightmapResolution;
            // Immutable height-only baseline makes this focused authoring pass repeatable.
            string baselinePath=VisualRoot+"/Gorge original heights.bytes";
            if(!File.Exists(baselinePath)){var initial=data.GetHeights(0,0,n,n);var bytes=new byte[n*n*4];Buffer.BlockCopy(initial,0,bytes,0,bytes.Length);File.WriteAllBytes(baselinePath,bytes);AssetDatabase.ImportAsset(baselinePath);}
            var saved=File.ReadAllBytes(baselinePath);if(saved.Length!=n*n*4)throw new InvalidOperationException("Gorge baseline resolution mismatch");
            var original=new float[n,n];Buffer.BlockCopy(saved,0,original,0,saved.Length);
            var h=data.GetHeights(0,0,n,n);
            var graded=AssetDatabase.LoadAssetAtPath<TerrainData>(VisualRoot+"/Road graded terrain snapshot.asset").GetHeights(0,0,n,n);
            // Remove only the former generated dressing inside the two composition areas.
            foreach(string name in new[]{"Mountain outcrops and scree","River banks - rocks and riparian cover","Authored biome groves and rock formations","Roadside undergrowth and gravel","Embedded rocky road banks"})
            {
                var old=environment.Find(name);if(old==null)continue;
                foreach(var t in old.Cast<Transform>().ToArray()){var p=ScenicBounds(t.gameObject).center;if(InGorgePilot(p)||InMountainPilot(p))UnityEngine.Object.DestroyImmediate(t.gameObject);}
            }
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                var p=new Vector3(x/(float)(n-1)*1000-500,0,z/(float)(n-1)*1000-500);
                if(!InGorgePilot(p)&&!InMountainPilot(p))continue;
                h[z,x]=original[z,x];float clearance=roadDistance[z,x]-roadWidth[z,x];
                if(clearance<5||!ClearRecoveryAccess(p,3))continue;
                float edge=Smooth01(5,18,clearance);
                // Restore the old straight channel before cutting the meandering stepped gorge.
                if(InGorgePilot(p))
                {
                    float blend=Smooth01(-182,-170,p.x)*(1-Smooth01(-100,-88,p.x))*Smooth01(203,214,p.z)*(1-Smooth01(288,302,p.z));
                    h[z,x]=Mathf.Lerp(h[z,x],graded[z,x],blend*edge);
                }
                if(InMountainPilot(p))
                {
                    float boundary=Smooth01(-110,-92,p.x)*(1-Smooth01(138,156,p.x))*Smooth01(245,265,p.z)*(1-Smooth01(366,385,p.z));
                    float variation=0;
                    foreach(var feature in new[]{new Vector4(-73,300,14,7),new Vector4(-36,306,8,-7),new Vector4(-7,305,18,8),new Vector4(30,316,9,-8),new Vector4(58,325,22,9),new Vector4(99,346,10,-6),new Vector4(115,364,21,8)})
                    {float dx=(p.x-feature.x)/feature.z,dz=(p.z-feature.y)/24;variation+=feature.w*Mathf.Exp(-dx*dx-dz*dz);}
                    h[z,x]+=variation*boundary*edge/350;
                }
            }
            data.SetHeights(0,0,h);
            // Downstream bridge approaches must always start from their graded baseline;
            // feeding the already-carved terrain back into the blend accumulates a ramp lip.
            gorgeRiverBaseline=(float[,])graded.Clone();
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){var p=new Vector3(x/(float)(n-1)*1000-500,0,z/(float)(n-1)*1000-500);if(InGorgePilot(p)||InMountainPilot(p))gorgeRiverBaseline[z,x]=h[z,x];}
            try{PolishWaterAndBridges();BuildIrregularMud();}finally{gorgeRiverBaseline=null;}
            var parent=ReplaceArtGroup("Authored gorge and mountain geology");
            var dry=GorgeRockMaterial("Gorge alpine stone",new Color(.86f,.86f,.80f),false,false);
            var wet=GorgeRockMaterial("Gorge wet moss stone",new Color(.57f,.64f,.57f),true,false);
            var snow=GorgeRockMaterial("Gorge snow shelves",new Color(.72f,.75f,.76f),false,true);
            // Broad, intersecting cliff silhouettes. Each center/extent is authored to the road terraces.
            AddGorgeRock("P_CliffClassic5",new Vector3(-78,124,285),new Vector3(48,30,24),-9,dry,parent);
            AddGorgeRock("P_CliffClassic2",new Vector3(-46,128,290),new Vector3(28,33,22),16,dry,parent);
            AddGorgeRock("P_CliffClassic6",new Vector3(-5,129,294),new Vector3(46,32,19),-6,dry,parent);
            AddGorgeRock("P_CliffClassic4",new Vector3(34,129,299),new Vector3(25,28,21),24,dry,parent);
            AddGorgeRock("P_CliffClassic5",new Vector3(68,132,308),new Vector3(48,29,25),-19,dry,parent);
            AddGorgeRock("P_CliffClassic3",new Vector3(112,139,329),new Vector3(31,35,26),23,snow,parent);
            AddGorgeRock("P_CliffClassic6",new Vector3(-67,146,339),new Vector3(42,23,25),18,snow,parent);
            AddGorgeRock("P_CliffClassic5",new Vector3(-8,154,364),new Vector3(48,23,22),-15,snow,parent);
            AddGorgeRock("P_ShelfClassic4",new Vector3(41,139,325),new Vector3(28,10,22),13,snow,parent);
            // Gorge walls alternate large buttresses and low wet shelves, rather than two boulder rows.
            AddGorgeRock("P_CliffClassic3",new Vector3(-147,95,273),new Vector3(24,36,28),18,dry,parent);
            AddGorgeRock("P_CliffClassic6",new Vector3(-101,93,269),new Vector3(31,28,27),-21,dry,parent);
            AddGorgeRock("P_CliffClassic2",new Vector3(-139,80,252),new Vector3(20,29,21),-15,wet,parent);
            AddGorgeRock("P_CliffClassic4",new Vector3(-99,78,246),new Vector3(22,30,23),24,dry,parent);
            AddGorgeRock("P_CliffClassic5",new Vector3(-158,70,239),new Vector3(24,28,24),12,wet,parent);
            AddGorgeRock("P_CliffClassic1",new Vector3(-111,66,225),new Vector3(23,31,24),-12,wet,parent);
            AddGorgeRock("P_CliffClassic3",new Vector3(-160,52,216),new Vector3(21,27,24),28,wet,parent);
            DressGorgePockets(parent,dry,wet,snow);DressGorgeShelves(parent,dry,wet,snow);
            PaintGorgeGround();data.SetBaseMapDirty();terrain.Flush();EditorUtility.SetDirty(data);SaveVisualScene();
        }
        private static Material GorgeRockMaterial(string name,Color tint,bool wet,bool snow)
        {
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/SoStylized/Environment/Rocks/Materials/Classic/MV_RockClassic_Cliff.mat");
            var m=VisualAsset(new Material(source.shader){name=name},name+".mat");m.CopyPropertiesFromMaterial(source);m.SetColor("_Rock_Tint",tint);m.SetFloat("_Rock_Scale",18);m.SetFloat("_RockMetallic",0);m.SetFloat("_Smoothness",wet?.43f:.12f);m.SetFloat("_Emissive_Strength",.045f);m.SetFloat("_Rock_Brightness",.08f);m.SetFloat("_Rock_Normal_Flatten",.5f);
            m.SetFloat("_TopGrass",0);m.SetFloat("_TopSnow",snow?1:0);m.SetFloat("_TopLayer_Blend_Sharpness",3.2f);m.SetFloat("_TopLayer_Blend_Offset",.48f);m.SetColor("_Grass_Tint",new Color(.54f,.65f,.34f));m.SetFloat("_Moss",wet?1:0);m.SetFloat("_Moss_Multiply",.8f);m.SetFloat("_Moss_Offset",-.35f);m.SetColor("_Moss_Color",new Color(.26f,.37f,.16f));m.SetColor("_Moss_Color_2",new Color(.18f,.26f,.10f));
            EditorUtility.SetDirty(m);return m;
        }
        private static int gorgeRockIndex;
        private static GameObject AddGorgeRock(string prefab,Vector3 center,Vector3 size,float yaw,Material material,Transform parent)
        {
            if(parent.childCount==0)gorgeRockIndex=0;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(prefab));if(source==null)throw new InvalidOperationException(prefab);
            var mf=source.GetComponentsInChildren<MeshFilter>().First(f=>f.sharedMesh!=null);var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);var b=mesh.bounds;var sourceNormals=mesh.normals;var rotation=Quaternion.Euler(0,yaw,0);var vs=mesh.vertices;
            for(int i=0;i<vs.Length;i++)
            {
                float vertical=Mathf.InverseLerp(b.min.y,b.max.y,vs[i].y);
                var q=vs[i]-b.center;q=Vector3.Scale(q,new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z));
                if(prefab.Contains("Cliff")){float foot=1+(1-vertical)*.28f;q.x*=foot;q.z*=foot;}
                q=center+rotation*q;
                if(prefab.Contains("Cliff"))
                {
                    float baseY=Mathf.Min(center.y-size.y*.5f,Ground(q).y-4);
                    float topY=center.y+size.y*.5f+Mathf.Sin(q.x*.27f+q.z*.14f)*1.1f;
                    q.y=Mathf.Lerp(baseY,topY,vertical);
                    if(vertical<.65f||sourceNormals[i].y<-.05f)q.y=Mathf.Min(q.y,Ground(q).y-3);
                }
                // Recess any geometry approaching a reserved lane below its road surface.
                int ix=Mathf.Clamp(Mathf.RoundToInt((q.x+500)*1.024f),0,1024),iz=Mathf.Clamp(Mathf.RoundToInt((q.z+500)*1.024f),0,1024);
                float clear=roadDistance[iz,ix]-roadWidth[iz,ix];
                if(clear<8)q.y=Mathf.Lerp(Mathf.Min(q.y,roadHeight[iz,ix]-1),q.y,Smooth01(3,8,clear));
                vs[i]=q;
            }
            mesh.vertices=vs;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            int id=++gorgeRockIndex;mesh=VisualAsset(mesh,"Gorge rock "+id.ToString("000")+".asset");
            var go=new GameObject("Massif "+id.ToString("000")+" - "+prefab,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.layer=8;go.isStatic=true;go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshCollider>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=Enumerable.Repeat(material,mesh.subMeshCount).ToArray();return go;
        }
        private static void DressGorgePockets(Transform parent,Material dry,Material wet,Material snow)
        {
            var anchors=new[]{new Vector3(-93,0,284),new Vector3(-55,0,283),new Vector3(-28,0,293),new Vector3(15,0,303),new Vector3(53,0,310),new Vector3(96,0,333),new Vector3(-156,0,275),new Vector3(-106,0,278),new Vector3(-146,0,245),new Vector3(-106,0,234),new Vector3(-154,0,218)};
            int cluster=0;
            foreach(var anchor in anchors)
            {
                cluster++;
                for(int j=0;j<12;j++)
                {
                    var p=Ground(anchor+new Vector3(Pick(-8,8),0,Pick(-6,6)));float height=j<3?Pick(1.8f,4.2f):Pick(.4f,1.3f);
                    float clear=ScenicDistance(p,out _,out var road)-road.Width;
                    if(clear<height+4||!ClearRecoveryAccess(p,height)||RiverDistance(p)<8)continue;
                    if(j<5)
                    {
                        var go=ArtPrefab("P_RockClumpClassic"+(cluster%10+1),p,height,parent,true);go.transform.position-=Vector3.up*height*.35f;
                        foreach(var r in go.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>p.y>144?snow:InGorgePilot(p)?wet:dry).ToArray();
                    }
                    else if(terrain.terrainData.GetSteepness((p.x+500)/1000,(p.z+500)/1000)<38)
                        ArtPrefab(j%2==0?"P_Fern2":"P_Grass2",p,Pick(.5f,1.1f),parent,false);
                }
            }
            // Small groves at the edge of rock shelves, with a clear water corridor.
            foreach(var anchor in new[]{new Vector3(-94,0,284),new Vector3(-54,0,335),new Vector3(12,0,341),new Vector3(82,0,363),new Vector3(-162,0,268),new Vector3(-103,0,288)})
            for(int j=0;j<4;j++)
            {
                var p=Ground(anchor+new Vector3(Pick(-5,5),0,Pick(-5,5)));
                if(!ClearForArt(p,3,true))continue;
                // If a shelf is exposed, plant on its upper surface rather than inside the mesh.
                Physics.SyncTransforms();if(Physics.Raycast(p+Vector3.up*35,Vector3.down,out var hit,45,1<<8,QueryTriggerInteraction.Ignore)&&hit.normal.y>.72f){p=hit.point;var go=ArtPrefab(p.y>143?"P_OakTree2_Snow":"P_FirTree4",p,Pick(7,11),parent,true,true);go.transform.position+=Vector3.up*(p.y-Ground(p).y);}
            }
        }
        private static void DressGorgeShelves(Transform parent,Material dry,Material wet,Material snow)
        {
            Physics.SyncTransforms();
            var massifs=parent.Cast<Transform>().Where(t=>t.name.StartsWith("Massif ")).Select(t=>t.GetComponent<MeshRenderer>().bounds).ToArray();
            int plantedTrees=0;
            foreach(var b in massifs)
            {
                for(int j=0;j<12;j++)
                {
                    var p=new Vector3(b.center.x+Pick(-.32f,.32f)*b.size.x,b.max.y+8,b.center.z+Pick(-.28f,.28f)*b.size.z);
                    if(!Physics.Raycast(p,Vector3.down,out var hit,80,1<<8,QueryTriggerInteraction.Ignore)||hit.normal.y<.74f)continue;
                    p=hit.point;bool tree=j==0&&plantedTrees<10;
                    float clearance=ScenicDistance(p,out _,out var road)-road.Width;
                    if(clearance<(tree?7:3)||!ClearRecoveryAccess(p,tree?4:1)||RiverDistance(p)<(tree?13:8))continue;
                    string prefab=tree?(p.y>142?"P_OakTree2_Snow":"P_FirTree4"):(j%3==0?"P_Bush1_Light":j%2==0?"P_Fern2":"P_Grass2");
                    if(!tree&&p.y>146)prefab="P_BushSnowDead02";
                    var plant=ArtPrefab(prefab,p,tree?Pick(6.5f,10):Pick(.55f,1.35f),parent,tree,tree);
                    plant.transform.position+=Vector3.up*(p.y-Ground(p).y-.035f);
                    if(tree)plantedTrees++;
                }
                // Talus collects at the foot of each formation, in unequal groups.
                for(int j=0;j<4;j++)
                {
                    var p=Ground(new Vector3(b.center.x+Pick(-.3f,.3f)*b.size.x,0,b.min.z+Pick(-2,4)));
                    float height=j==0?Pick(3.2f,5.2f):Pick(.8f,2.1f);
                    if(ScenicDistance(p,out _,out var road)-road.Width<height+4||!ClearRecoveryAccess(p,height)||RiverDistance(p)<9+height)continue;
                    var rock=ArtPrefab("P_RockClumpClassic"+(j*2+2),p,height,parent,true);rock.transform.position-=Vector3.up*height*.24f;
                    foreach(var renderer in rock.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>p.y>140?snow:InGorgePilot(p)?wet:dry).ToArray();
                }
            }
            // Reuse the established scene palette for new native foliage.
            foreach(var renderer in parent.GetComponentsInChildren<Renderer>())
            {
                var mats=renderer.sharedMaterials;
                for(int i=0;i<mats.Length;i++)if(mats[i]!=null&&AssetDatabase.GetAssetPath(mats[i]).StartsWith("Assets/SoStylized/"))
                {var graded=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Native "+mats[i].name+".mat");if(graded!=null)mats[i]=graded;}
                renderer.sharedMaterials=mats;
            }
        }
        private static void PaintGorgeGround()
        {
            var data=terrain.terrainData;int n=data.alphamapResolution;var alpha=data.GetAlphamaps(0,0,n,n);
            const int bx=300,bz=690,bw=380,bh=225;string path=VisualRoot+"/Gorge original splats.bytes";
            if(!File.Exists(path)){var initial=data.GetAlphamaps(bx,bz,bw,bh);var bytes=new byte[bw*bh*8*4];Buffer.BlockCopy(initial,0,bytes,0,bytes.Length);File.WriteAllBytes(path,bytes);AssetDatabase.ImportAsset(path);}
            var saved=File.ReadAllBytes(path);var baseline=new float[bh,bw,8];if(saved.Length!=bh*bw*8*4)throw new InvalidOperationException("Gorge splat baseline mismatch");Buffer.BlockCopy(saved,0,baseline,0,saved.Length);
            for(int z=bz;z<bz+bh;z++)for(int x=bx;x<bx+bw;x++)for(int l=0;l<8;l++)alpha[z,x,l]=baseline[z-bz,x-bx,l];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                float u=x/(float)(n-1),v=z/(float)(n-1);var p=new Vector3(u*1000-500,data.GetInterpolatedHeight(u,v)-50,v*1000-500);
                if(!InGorgePilot(p)&&!InMountainPilot(p))continue;
                int ix=Mathf.RoundToInt(u*1024),iz=Mathf.RoundToInt(v*1024);if(roadDistance[iz,ix]<roadWidth[iz,ix]+2)continue;
                float slope=data.GetSteepness(u,v),noise=ArtNoise(p.x,p.z,.07f);
                BlendLayer(alpha,z,x,3,Smooth01(20+noise*8,43,slope)*.92f);
                if(p.y>132)BlendLayer(alpha,z,x,4,Smooth01(134+noise*8,157,p.y)*(1-Smooth01(40,68,slope))*.85f);
                if(InGorgePilot(p)&&RiverDistance(p)<13)BlendLayer(alpha,z,x,6,.28f+noise*.22f);
            }
            data.SetAlphamaps(0,0,alpha);
        }
    }
}

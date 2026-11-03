using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static System.Random artRandom;
        private static float Pick(float a, float b) => Mathf.Lerp(a, b, (float)artRandom.NextDouble());
        private static Transform ReplaceArtGroup(string name)
        {
            var old = environment.Find(name); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            return ScenicGroup(name);
        }
        private static bool ClearForArt(Vector3 p, float radius, bool tree = false)
        {
            if(RiverDistance(p)<(tree?11:7)+radius||!ClearRecoveryAccess(p,radius))return false;
            float d = ScenicDistance(p, out _, out var road);
            if (road != null && d < road.Width + radius + 1.8f) return false;
            foreach (var pad in plan.Nodes.Where(n => n.Id == "g" || n.Id.Length == 1 || n.Id.StartsWith("R")))
            {
                float clearance = pad.Id == "g" ? 40 : pad.Id.Length == 1 ? 20 : tree ? 16 : 10;
                if (Flat(p - pad.Point).magnitude < clearance + radius) return false;
            }
            return p.y > 10.6f && (ShoreDistance(p) > (tree ? 12 : 0)) && terrain.terrainData.GetSteepness((p.x + 500) / 1000, (p.z + 500) / 1000) < (tree ? 32 : 48);
        }
        private static GameObject ArtPrefab(string name, Vector3 p, float height, Transform parent, bool solid, bool tree = false)
        {
            var go = ScenicPrefab(name, p, height, Pick(0, 360), parent, false);
            if (go == null) throw new InvalidOperationException("Missing art prefab: " + name);
            if (solid)
            {
                if (tree)
                {
                    var collider = go.AddComponent<CapsuleCollider>(); var b = ScenicBounds(go);
                    collider.center = go.transform.InverseTransformPoint(new Vector3(p.x, Ground(p).y + height * .32f, p.z));
                    collider.height = height * .64f / go.transform.lossyScale.y;
                    collider.radius = Mathf.Max(.15f, height * .018f) / go.transform.lossyScale.x;
                }
                else
                {
                    var mf = go.GetComponentsInChildren<MeshFilter>().FirstOrDefault(f => f.sharedMesh != null);
                    if (mf != null) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                }
            }
            foreach (var lod in go.GetComponentsInChildren<LODGroup>())
            {
                var levels = lod.GetLODs();
                for (int i = 0; i < levels.Length; i++) levels[i].screenRelativeTransitionHeight = levels.Length == 1 ? .004f : i == 0 ? .18f : i == levels.Length - 1 ? .006f : .045f;
                lod.SetLODs(levels); lod.RecalculateBounds();
            }
            return go;
        }
        [MenuItem("Tow Trouble/Art Integration/2 - Compose Biomes and Recovery Sites")]
        public static void IntegrateBiomeArt()
        {
            VisualContext(); ScenicPaths(); artRandom = new System.Random(417209);
            // Replace only our generated dressing, never the vendor prefabs or source demo scenes.
            foreach (string name in new[] { "Mega - retained environment", "Biome dressing - So Stylized", "Scenic biome details" })
            { var old = environment.Find(name); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject); }
            terrain.terrainData.SetTreeInstances(Array.Empty<TreeInstance>(), false);
            var parent = ReplaceArtGroup("Authored biome groves and rock formations");
            int trees = 0;
            for (float z = -442; z < 450; z += 19) for (float x = -442; x < 450; x += 19)
            {
                var p = Ground(new Vector3(x + Pick(-7, 7), 0, z + Pick(-7, 7)));
                if (!ClearForArt(p, 3.2f, true)) continue;
                float grove = ArtNoise(p.x, p.z, .014f);
                bool desert = DesertAt(p), snow = p.y > 129, coast = ShoreDistance(p) < 42 || p.z < -337;
                if (grove < (desert ? .55f : coast ? .65f : .33f)) continue;
                string prefab;
                float height;
                if (desert) { prefab = artRandom.Next(3) == 0 ? "P_Cactus05" : "P_Cactus03"; height = Pick(2, 4.6f); }
                else if (snow) { prefab = artRandom.Next(2) == 0 ? "P_Pine02_Snowy" : "P_Pine03_Snowy"; height = Pick(8, 14); }
                else { bool fir = p.y > 65 || grove > .60f; prefab = fir ? "P_FirTree4" : artRandom.Next(2) == 0 ? "P_BirchTree1" : "P_BirchTree3"; height = fir ? Pick(12, 20) : Pick(9, 15); }
                ArtPrefab(prefab, p, height, parent, true, !desert); trees++;
            }
            // Group rock masses around selected anchors, with small satellites; no repeated pillars.
            var anchors = new[] { new Vector3(-185,0,257),new Vector3(120,0,287),new Vector3(-70,0,418),new Vector3(230,0,325),new Vector3(-370,0,140),new Vector3(-330,0,-70),new Vector3(-280,0,-184),new Vector3(-138,0,-301),new Vector3(295,0,118),new Vector3(395,0,-40),new Vector3(390,0,-290),new Vector3(252,0,-370) };
            for (int i = 0; i < anchors.Length; i++) for (int j = 0; j < 8; j++)
            {
                var p = Ground(anchors[i] + new Vector3(Pick(-22,22),0,Pick(-19,19)));
                float height = j < 2 ? Pick(8, 15) : Pick(1.2f, 5.5f);
                if (!ClearForArt(p, Mathf.Min(height, 10))) continue;
                var go = ArtPrefab(DesertAt(p) ? (j % 2 == 0 ? "P_RockDesert_Rock03" : "P_RockDesert_Rock08") : "P_BoulderClassic" + (j % 3 + 1), p, height, parent, true);
                go.transform.position -= Vector3.up * height * .18f;
            }
            // Ground cover is concentrated beside routes, in groves, and around the coast.
            var verge = ReplaceArtGroup("Roadside undergrowth and gravel");
            foreach (var path in scenicRoads) for (int i = 5; i < path.Points.Length - 5; i += 9)
            {
                var forward = Flat(path.Points[i+1] - path.Points[i-1]).normalized;
                var side = Vector3.Cross(Vector3.up, forward) * (artRandom.Next(2) == 0 ? -1 : 1);
                var center = path.Points[i] + side * (path.Width + Pick(2.4f, 5.5f));
                for (int j = 0; j < 3; j++)
                {
                    var p = Ground(center + new Vector3(Pick(-2,2),0,Pick(-2,2)));
                    if (!ClearForArt(p, .15f)) continue;
                    bool snow = p.y > 129, desert = DesertAt(p);
                    string prefab = snow ? "P_GrasSnow_Paintable" : desert ? "P_GrassDesert1_Paintable" : j == 0 ? "P_Grass2" : j == 1 ? "P_Fern2_Yellow" : "P_Foxtails1";
                    ArtPrefab(prefab, p, Pick(.28f,.65f), verge, false);
                }
                if (i % 27 == 5)
                {
                    var p = Ground(center + side * 1.6f);
                    if (ClearForArt(p, .7f)) ArtPrefab(DesertAt(p) ? "P_RockDesert_Rock05" : "P_BoulderClassic2", p, Pick(.3f, .9f), verge, true);
                }
            }
            DressShoreline(); DressRecoveryVignettes(); RefreshArtHorizon(); GradeSceneMaterials();
            terrain.detailObjectDensity = .8f; terrain.detailObjectDistance = 95;
            EditorUtility.SetDirty(terrain.terrainData); SaveVisualScene();
            Debug.Log("[TowTrouble] Composed biome groves: " + trees + " trees/cacti; authored 24 recovery surrounds.");
        }
        private static void DressShoreline()
        {
            var parent = ReplaceArtGroup("Lakeshore rocks reeds and driftwood");
            for (float z = -390; z < -130; z += 5.5f) for (float x = -430; x < -35; x += 5.5f)
            {
                var p = Ground(new Vector3(x+Pick(-2,2),0,z+Pick(-2,2))); float shore = ShoreDistance(p);
                if (shore < -2 || shore > 17 || p.y < 10.2f || !ClearForArt(p,.4f)) continue;
                float clump = ArtNoise(p.x,p.z,.055f); if (clump < .43f) continue;
                bool rock = artRandom.Next(4)==0;
                var go = ArtPrefab(rock ? "P_BoulderClassic2" : shore < 6 ? "P_Marshtail03" : "P_Foxtails1",p,rock?Pick(.6f,2.1f):Pick(.45f,1.25f),parent,rock);
                if (rock) go.transform.position -= Vector3.up*.25f;
            }
            foreach (var p0 in new[]{new Vector3(-302,0,-207),new Vector3(-173,0,-327),new Vector3(-379,0,-275)})
            {var p=Ground(p0);if(ClearForArt(p,3))ArtPrefab("P_OakFallen1",p,1.2f,parent,true);}
        }
        private static void DressRecoveryVignettes()
        {
            var parent = ReplaceArtGroup("Recovery situations - 24 authored surrounds");
            foreach(var site in plan.Sites)
            {
                var group=new GameObject(site.Id+" recovery setting").transform;group.SetParent(parent,false);
                var origin=RecoveryPoint(site);
                var edge=plan.Edges.First(e=>e.A==site.Node||e.B==site.Node);
                var forward=Flat(nodes[edge.A==site.Node?edge.B:edge.A].Point-origin).normalized;
                var side=Vector3.Cross(Vector3.up,forward);
                for(int j=0;j<7;j++)
                {
                    var p=Ground(origin-forward*Pick(7,15)+side*(j%2==0?-1:1)*Pick(7,12));
                    if(!ClearForArt(p,.5f))continue;
                    string name=site.Biome==3?"P_SnowDrift03":site.Biome==5?"P_RockDesert_Rock05":site.Biome==1?"P_Marshtail03":site.Biome==4?"P_GrassDesert1_Paintable":"P_BoulderClassic2";
                    ArtPrefab(name,p,site.Biome==3?Pick(.5f,1.2f):site.Biome==1?Pick(.65f,1.2f):Pick(.45f,1.6f),group,site.Biome==0||site.Biome==2||site.Biome==5);
                }
                if(site.Biome==0)
                {
                    var p=Ground(origin-forward*12+side*10);
                    if(ClearForArt(p,2))ArtPrefab("P_FirFallen1",p,1.1f,group,true);
                }
            }
        }
        private static void RefreshArtHorizon()
        {
            var parent=ReplaceArtGroup("County horizon");
            var material=ScenicMaterial("County distant layered mountains",new Color(.38f,.48f,.48f));
            var vertices=new List<Vector3>();var tri=new List<int>();var uv=new List<Vector2>();const int count=256;
            for(int ring=0;ring<5;ring++)for(int i=0;i<count;i++)
            {
                float angle=i/(float)count*Mathf.PI*2;var dir=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                float edge=500/Mathf.Max(Mathf.Abs(dir.x),Mathf.Abs(dir.y));float radius=edge+new[]{0,65,170,380,680}[ring];
                float baseH=Ground(new Vector3(dir.x*edge,0,dir.y*edge)).y;
                float peak=40+85*ArtNoise(dir.x*700,dir.y*700,.009f)+Mathf.Max(0,dir.y)*100;
                float height=ring==0?baseH-.5f:ring==1?baseH+15:ring==2?baseH+peak:ring==3?baseH+peak*.65f:baseH-15;
                vertices.Add(new Vector3(dir.x*radius,height,dir.y*radius));uv.Add(new Vector2(dir.x*radius*.03f,dir.y*radius*.03f));
                if(ring<4){int a=ring*count+i,b=ring*count+(i+1)%count,c=a+count,d=b+count;tri.AddRange(new[]{a,b,c,b,d,c});}
            }
            var mesh=new Mesh{name="Layered surrounding hills"};mesh.SetVertices(vertices);mesh.SetTriangles(tri,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject("Distant ridges - scenery",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=VisualAsset(mesh,"Finished horizon.asset");go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
    }
}

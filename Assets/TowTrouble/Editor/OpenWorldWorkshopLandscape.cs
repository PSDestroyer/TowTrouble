using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        [MenuItem("Tow Trouble/Visuals/Refine Rural Workshop Landscape")]
        public static void RefineRuralWorkshopLandscape()
        {
            VisualContext(); ScenicPaths(); CachePolishRoads();
            var world = UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
            var workshop = environment.Find("Workshop service base");
            if (workshop == null) throw new InvalidOperationException("Workshop is missing.");
            var scene = EditorSceneManager.GetActiveScene(); var data = terrain.terrainData;
            const string backup = "/private/tmp/TowWorkshopLandscape";
            Directory.CreateDirectory(backup);
            if (!File.Exists(backup + "/Before.unity")) EditorSceneManager.SaveScene(scene, backup + "/Before.unity", true);
            if (!File.Exists(backup + "/Terrain.asset")) File.Copy(AssetDatabase.GetAssetPath(data), backup + "/Terrain.asset");
            var mapBefore = EditorJsonUtility.ToJson(world.Map);
            var heights = data.GetHeights(0, 0, data.heightmapResolution, data.heightmapResolution);
            Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Refine rural workshop landscape");
            Undo.RegisterCompleteObjectUndo(data, "Shape workshop ground palette");
            try
            {
                var old = workshop.Find("Rural workshop landscape");
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                var root = PolishGroup(workshop, "Rural workshop landscape");
                Undo.RegisterCreatedObjectUndo(root.gameObject, "Workshop landscape");
                PaintRuralWorkshop();
                // Two generations of circulation marks exist; neither should leave a
                // decorative racetrack through the open yard.
                foreach (var wear in workshop.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Workshop vehicle circulation wear"))
                { Undo.RecordObject(wear.gameObject, "Replace circular yard impressions"); wear.gameObject.SetActive(false); }
                var roads = environment.Find("Road network - 24 recovery spurs").GetComponentsInChildren<MeshCollider>();
                var material = GarageWearMaterial("Rural workshop restrained tracks", .14f);
                GarageWheelPath(root, "Rural workshop entrance wear", material, .24f, roads,
                    new[] { new Vector3(1,0,27),new Vector3(-4,0,15),new Vector3(-10,0,3),new Vector3(-23,0,-8),new Vector3(-36,0,-9) });
                GarageWheelPath(root, "Rural workshop collection wear", material, .22f, roads,
                    new[] {new Vector3(-31,0,-9),new Vector3(-41,0,-15),new Vector3(-47,0,-24)});
                BuildRuralWorkshopApron(root, world.GaragePosition);
                DressRuralWorkshopEdges(root, world);
                BuildRuralWorkshopEntry(root);
                if (mapBefore != EditorJsonUtility.ToJson(world.Map) ||
                    !heights.Cast<float>().SequenceEqual(data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution).Cast<float>()))
                    throw new InvalidOperationException("Workshop landscape must preserve gameplay and terrain heights.");
                // Ground surface classification follows the visual paint without changing vehicle stats.
                RefreshGarageTrackSurfaces(world.GaragePosition);
                SaveVisualScene(); Undo.CollapseUndoOperations(undo);
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
        }

        private static void PaintRuralWorkshop()
        {
            var d = terrain.terrainData; var origin = terrain.transform.position;
            int x0 = Mathf.FloorToInt((-76-origin.x)/d.size.x*(d.alphamapWidth-1));
            int z0 = Mathf.FloorToInt((-58-origin.z)/d.size.z*(d.alphamapHeight-1));
            int x1 = Mathf.CeilToInt((28-origin.x)/d.size.x*(d.alphamapWidth-1));
            int z1 = Mathf.CeilToInt((52-origin.z)/d.size.z*(d.alphamapHeight-1));
            int width=x1-x0+1, height=z1-z0+1;
            var paint=d.GetAlphamaps(x0,z0,width,height);
            const string baseline="Docs/Validation/WorkshopLandscape/PaintBaseline.bin";
            Directory.CreateDirectory(Path.GetDirectoryName(baseline));
            if (!File.Exists(baseline))
            { using(var writer=new BinaryWriter(File.Create(baseline))) {writer.Write(width);writer.Write(height);writer.Write(d.alphamapLayers);foreach(float value in paint)writer.Write(value);} }
            using(var reader=new BinaryReader(File.OpenRead(baseline)))
            {
                if(reader.ReadInt32()!=width||reader.ReadInt32()!=height||reader.ReadInt32()!=d.alphamapLayers)throw new InvalidOperationException("Workshop paint baseline dimensions changed.");
                for(int z=0;z<height;z++)for(int x=0;x<width;x++)for(int l=0;l<d.alphamapLayers;l++)paint[z,x,l]=reader.ReadSingle();
            }
            float Oval(Vector3 p,float cx,float cz,float rx,float rz)=>Mathf.Sqrt(Mathf.Pow((p.x-cx)/rx,2)+Mathf.Pow((p.z-cz)/rz,2));
            for(int z=0;z<height;z++)for(int x=0;x<width;x++)
            {
                var p=origin+new Vector3((x+x0)/(float)(d.alphamapWidth-1)*d.size.x,0,(z+z0)/(float)(d.alphamapHeight-1)*d.size.z);
                float fade=Mathf.SmoothStep(0,1,Mathf.Min(Mathf.Min(x,width-1-x),Mathf.Min(z,height-1-z))/12f);
                float noise=Mathf.PerlinNoise(p.x*.085f+42,p.z*.085f+17);
                float shape=Mathf.Min(Oval(p,-22,0,32,25),Mathf.Min(Oval(p,-28,-27,31,12),Oval(p,-9,29,18,17)));
                float court=1-Smooth01(.91f+noise*.07f,1.12f+noise*.10f,shape);
                float roadDistance=ScenicDistance(p,out _,out var road);
                float access=1-Smooth01(road.Width+.25f,road.Width+3,roadDistance);
                float worked=Mathf.Max(court,access);
                // Existing eight layers only: no ninth terrain pass or new texture set.
                var target=new float[d.alphamapLayers];
                target[0]=1-worked;
                float gravel=court*(.34f+noise*.08f);
                target[3]=worked*gravel; target[6]=worked*Mathf.Lerp(.10f,.38f,court);
                target[2]=worked*.025f;target[1]=worked-target[3]-target[6]-target[2];
                for(int l=0;l<d.alphamapLayers;l++)paint[z,x,l]=Mathf.Lerp(paint[z,x,l],target[l],fade);
            }
            d.SetAlphamaps(x0,z0,paint);d.SetBaseMapDirty();terrain.Flush();EditorUtility.SetDirty(d);
        }

        private static void BuildRuralWorkshopApron(Transform root,Vector3 garage)
        {
            // Flush, thin visual concrete at the actual doors. Driving still uses the existing ground.
            var apron=PolishGroup(root,"Worn concrete door apron");
            for(int i=0;i<3;i++)
            {
                var p=Ground(garage+new Vector3(-5.7f+i*5.7f,0,11.5f))-Vector3.up*.065f;
                var slab=Cube("Concrete service panel",p,new Vector3(5.65f,.022f,5.8f),WorkshopFinish("Foundation"),apron,false);
                slab.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            ConfigureDetailLod(apron.gameObject,180);
        }

        private static void DressRuralWorkshopEdges(Transform root,RecoveryWorld world)
        {
            // Remove isolated soft plants from the central apron. Trees and all existing physics remain.
            foreach(var t in environment.GetComponentsInChildren<Transform>().Where(t=>
                t.name.StartsWith("P_Grass")||t.name.StartsWith("P_Bush")||t.name.StartsWith("P_Fern")).ToArray())
            {
                var p=t.position;
                if(p.x< -40||p.x>7||p.z< -18||p.z>15||t.GetComponentsInChildren<Collider>().Length>0)continue;
                Undo.RecordObject(t.gameObject,"Clear isolated apron planting");t.gameObject.SetActive(false);
            }
            var plants=PolishGroup(root,"Irregular courtyard planted edges");
            var centers=new[]{new Vector2(-57,7),new Vector2(-51,18),new Vector2(-54,-37),new Vector2(-33,-43),new Vector2(-9,-42),new Vector2(10,-14)};
            foreach(var c in centers)for(int j=0;j<7;j++)
            {
                var p=new Vector3(c.x+Mathf.Cos(j*2.4f)*(1+j%3),0,c.y+Mathf.Sin(j*2.4f)*(1+j%2));
                if(PolishRoadNear(p,2)||world.Map.Deliveries.Any(b=>Flat(p-b.Position).magnitude<15)||Flat(p-world.GaragePosition).magnitude<16)continue;
                var go=ForestProp(j%4==0?"P_Bush1_Light":j%2==0?"P_Fern2_Yellow":"P_Grass2",p,j%4==0?1.25f:.6f,j*137,plants,false,false);
                foreach(var lod in go.GetComponentsInChildren<LODGroup>(true))
                {
                    var levels=lod.GetLODs();if(levels.Length>0){var chosen=levels[Mathf.Min(1,levels.Length-1)].renderers;foreach(var r in lod.GetComponentsInChildren<Renderer>(true))r.enabled=chosen.Contains(r);}
                    UnityEngine.Object.DestroyImmediate(lod);
                }
                var visible=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
                var group=go.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.025f,visible)});group.RecalculateBounds();
            }
        }

        private static void BuildRuralWorkshopEntry(Transform root)
        {
            var trees=environment.GetComponentsInChildren<Transform>().Where(t =>
                t.name.StartsWith("P_BirchTree") || t.name.StartsWith("P_FirTree") || t.name.StartsWith("P_OakTree")).ToArray();
            Vector3 p=default; float best=float.MaxValue;
            for(float z=35;z<=80;z+=2)for(float x=-65;x<=45;x+=2)
            {
                var candidate=Ground(new Vector3(x,0,z));
                float distance=ScenicDistance(candidate,out _,out var road);
                float shoulder=distance-road.Width;
                if(shoulder<4.5f||shoulder>18||trees.Any(t=>Flat(t.position-candidate).magnitude<5))continue;
                float score=Mathf.Abs(shoulder-5.5f)+Mathf.Abs(z-52)*.12f+Mathf.Abs(x-15)*.06f;
                if(score<best){best=score;p=candidate;}
            }
            if(best==float.MaxValue)throw new InvalidOperationException("No visible workshop entry shoulder.");
            var sign=PolishGroup(root,"Tow County Garage entrance sign");sign.position=p;sign.rotation=Quaternion.Euler(0,25,0);
            using(var shapes=new GarageFacadeShapes())
            {
                var wood=WorkshopFinish("Timber");var trim=WorkshopFinish("Trim");
                foreach(float x in new[]{-1.4f,1.4f})shapes.Box(wood,new Vector3(x,1.15f,0),new Vector3(.15f,2.3f,.15f));
                shapes.Box(wood,new Vector3(0,2.1f,0),new Vector3(3,.9f,.16f));
                foreach(float y in new[]{1.64f,2.56f})shapes.Box(trim,new Vector3(0,y,0),new Vector3(3.12f,.075f,.20f));
                shapes.Finish(sign,"Rural workshop entrance sign");
            }
            BakeReceivingSignText(sign,"WorkshopBrand","TOW COUNTY\n<size=80%>GARAGE</size>");
            foreach(float side in new[]{-1f,1f})GarageYardFence(root,Ground(p+Vector3.right*side*4.5f),0,WorkshopFinish("Timber"));
            ConfigureDetailLod(sign.gameObject,180);
        }
    }
}

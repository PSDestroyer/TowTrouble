using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private const string CountyDesktopRoot = "Desktop county - layered biome details";
        private const string CountyCottageRoot = "County cottages and yards";
        private const string CountyDesktopFolder = VisualRoot + "/Desktop County";
        private sealed class CountyPart { public Mesh Mesh; public Material Material; public int Submesh; public Matrix4x4 Matrix; }
        private sealed class CountyPrototype { public List<CountyPart> Parts=new List<CountyPart>(); public Bounds Bounds; }
        private sealed class CountyBatch { public Material Material; public Vector3 Origin; public List<CombineInstance> Parts=new List<CombineInstance>(); public bool CastShadows; }
        private sealed class CountyPlacement { public string Pass,Prefab; public int Biome; public float X,Y,Z,Size; }
        private static readonly string[][] CountyPlants = {
            new[]{"P_Grass2","P_Fern2","P_Bush2_Light","P_RockSpire_Rock01"},
            new[]{"P_Marshtail03","P_Bush2_Light","P_Grass2","P_RockSpire_Rock01"},
            new[]{"P_Grass2","P_Bush1_Light","P_RockSpire_Rock01","P_RockSpire_Rock02"},
            new[]{"P_BushSnowDead01","P_Bush1_Snowy","P_SnowDrift03","P_SnowDrift01"},
            new[]{"P_GrassDesert1_Paintable","P_Bush1_Dry","P_RockSpire_Rock01","P_PalmStump01"},
            new[]{"P_GrassDesert1_Paintable","P_Bush2_Dry","P_RockDesert_Rock05","P_CactusPricklyPear01"}
        };

        [MenuItem("Tow Trouble/Visuals/Refine Desktop County Biomes and Borders")]
        public static void RefineDesktopCounty()
        {
            VisualContext(); ScenicPaths(); ScenicRoadField(501);
            var scene=EditorSceneManager.GetActiveScene();
            const string backup="/private/tmp/TowDesktopCounty";
            Directory.CreateDirectory(backup);
            if(!File.Exists(backup+"/Before.unity"))EditorSceneManager.SaveScene(scene,backup+"/Before.unity",true);
            if(!AssetDatabase.IsValidFolder(CountyDesktopFolder))AssetDatabase.CreateFolder(VisualRoot,"Desktop County");
            var map=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>().Map;
            string mapBefore=EditorJsonUtility.ToJson(map);
            var originalColliders=environment.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
            var baseline=new int[6];
            foreach(var t in environment.GetComponentsInChildren<Transform>())
            {
                if(!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)||PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject)!=t.gameObject)continue;
                string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                if(!path.StartsWith("Assets/SoStylized/")||t.name.Contains("Tree")||t.name.StartsWith("Collision"))continue;
                var rs=t.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(rs.Length==0)continue;
                Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                if(b.size.y>2.5f||Mathf.Max(b.size.x,b.size.z)>4)continue;
                baseline[CountyBiome(b.center)]++;
            }
            var added=new int[6];
            var cache=new Dictionary<string,CountyPrototype>();var materials=new Dictionary<string,Material>();
            var batches=new Dictionary<string,CountyBatch>();var placements=new List<CountyPlacement>();
            var waters=environment.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterials.Any(m=>m!=null&&m.shader!=null&&m.shader.name.ToLowerInvariant().Contains("water"))).ToArray();
            CountyIndexWater(waters);
            var old=environment.Find(CountyDesktopRoot);
            var oldCottages = environment.Find(CountyCottageRoot);
            var oldCottageColliders = oldCottages == null ? new HashSet<Collider>() : oldCottages.GetComponentsInChildren<Collider>(true).ToHashSet();
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Desktop county biome compositions");
            if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
            var root=PolishGroup(environment,CountyDesktopRoot);root.gameObject.AddComponent<RecoveryDesktopScenery>();
            Transform cottageRoot = null;
            var random=new System.Random(111141);
            var occupied=new Dictionary<Vector2Int,List<Vector3>>();
            var cottages = new List<Vector3>();
            float Next(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
            Vector3 InPatch(Vector3 center, float radius)
            {
                float angle = Next(0, 2 * Mathf.PI);
                float distance = Mathf.Sqrt(Next(0, 1)) * radius;
                return center + new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
            }
            bool Place(Vector3 point,int biome,string pass,string specific=null,float explicitSize=0,bool border=false)
            {
                if(placements.Count>50000)throw new InvalidOperationException("County composition exceeded review budget");
                float roll=(float)random.NextDouble();int pick=roll<.40f?0:roll<.65f?1:roll<.94f?2:3;
                string prefab=specific??CountyPlants[biome][pick];
                float variation=(float)random.NextDouble();
                float size=explicitSize>0?explicitSize:
                    prefab.Contains("GrassDesert")?Mathf.Lerp(2.0f,2.8f,variation):
                    prefab.Contains("Grass")?Mathf.Lerp(2.4f,3.3f,variation):
                    prefab.Contains("Fern")?Mathf.Lerp(1.4f,2.1f,variation):
                    prefab.Contains("Marshtail")?Mathf.Lerp(1.3f,2.0f,variation):
                    prefab.Contains("BushSnowDead")?Mathf.Lerp(1.2f,1.8f,variation):
                    prefab.Contains("Bush")?Mathf.Lerp(2.6f,4.2f,variation):
                    prefab.Contains("SnowDrift")?Mathf.Lerp(2.4f,3.8f,variation):
                    prefab.Contains("Cactus")?Mathf.Lerp(1.2f,2.0f,variation):Mathf.Lerp(.65f,1.2f,variation);
                point=Ground(point)-Vector3.up*.08f;
                if (!CountyClear(point, size * .55f, map, border) ||
                    cottages.Any(c => Flat(c - point).sqrMagnitude < Mathf.Pow(9.5f + size * .5f, 2))) return false;
                var cell=new Vector2Int(Mathf.FloorToInt(point.x/8),Mathf.FloorToInt(point.z/8));
                if(!border)
                {
                    // Keep shrub crowns from joining into continuous hedges.
                    float spacing = prefab.Contains("Bush") ? size * .62f : prefab.Contains("Tree") ? size * .65f : size * .20f;
                    for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                        if(occupied.TryGetValue(cell+new Vector2Int(dx,dz),out var neighbors))
                            foreach(var other in neighbors)if(Vector2.Distance(new Vector2(point.x,point.z),new Vector2(other.x,other.z))<spacing+other.y)return false;
                    if(!occupied.TryGetValue(cell,out var list)){list=new List<Vector3>();occupied.Add(cell,list);}
                    list.Add(new Vector3(point.x,spacing,point.z));
                }
                // Keep the authored meadow layout stable while retiring the old perimeter rock pass.
                int yaw = random.Next(360);
                if (border) return false;
                if(!cache.TryGetValue(prefab,out var proto)){proto=CountyLoadPrototype(prefab);cache.Add(prefab,proto);}
                float scale=size/Mathf.Max(.01f,Mathf.Max(proto.Bounds.size.x,proto.Bounds.size.z));
                var anchor=new Vector3(proto.Bounds.center.x,proto.Bounds.min.y,proto.Bounds.center.z);
                var normal=terrain.terrainData.GetInterpolatedNormal((point.x+500)/1000,(point.z+500)/1000);
                // Follow shallow banks, never lay bushes horizontally on steep cliffs.
                var tilt=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(Vector3.up,normal),prefab.Contains("Tree") ? .10f : .5f);
                var transform=Matrix4x4.TRS(point - Vector3.up * .025f,tilt*Quaternion.Euler(0,yaw,0),Vector3.one*scale)*Matrix4x4.Translate(-anchor);
                int cx=Mathf.FloorToInt(point.x/48),cz=Mathf.FloorToInt(point.z/48);var origin=new Vector3(cx*48,0,cz*48);
                foreach(var part in proto.Parts)
                {
                    string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(part.Material));string materialKey=biome+"_"+guid;
                    if(!materials.TryGetValue(materialKey,out var material))
                    {
                        string path=CountyDesktopFolder+"/B"+materialKey+".mat";material=AssetDatabase.LoadAssetAtPath<Material>(path);
                        if(material==null){material=new Material(part.Material);AssetDatabase.CreateAsset(material,path);}else EditorUtility.CopySerialized(part.Material,material);
                        bool meadowFoliage = biome <= 2 && (part.Material.name.Contains("Leaves") || part.Material.name.Contains("Grass"));
                        float desaturation = meadowFoliage ? .24f : biome == 3 ? .04f : .12f;
                        var tint = meadowFoliage ? new Color(.89f, .91f, .85f) :
                            biome == 3 ? new Color(.97f, .99f, 1) :
                            biome >= 4 ? new Color(1, .96f, .90f) : new Color(.92f, .96f, .89f);
                        foreach (string property in new[] { "_BaseColor", "_Color", "_Main_Color", "_Second_Color", "_Bottom_Color", "_Tip_Color", "_SSS_Color" })
                        {
                            if (!material.HasProperty(property)) continue;
                            var color = part.Material.GetColor(property);
                            var muted = Color.Lerp(color, new Color(color.grayscale, color.grayscale, color.grayscale, color.a), desaturation);
                            material.SetColor(property, new Color(muted.r * tint.r, muted.g * tint.g, muted.b * tint.b, color.a));
                        }
                        material.enableInstancing=true;EditorUtility.SetDirty(material);materials.Add(materialKey,material);
                    }
                    string key=$"B{biome}_{cx}_{cz}_{guid}";
                    if(!batches.TryGetValue(key,out var batch)){batch=new CountyBatch{Material=material,Origin=origin};batches.Add(key,batch);}
                    batch.CastShadows |= !prefab.Contains("Grass") && !prefab.Contains("Flower");
                    batch.Parts.Add(new CombineInstance{mesh=part.Mesh,subMeshIndex=part.Submesh,transform=Matrix4x4.Translate(-origin)*transform*part.Matrix});
                }
                placements.Add(new CountyPlacement{Pass=pass,Prefab=prefab,Biome=biome,X=point.x,Y=point.y,Z=point.z,Size=size});
                added[biome]++;return true;
            }
            try
            {
                if (oldCottages != null) Undo.DestroyObjectImmediate(oldCottages.gameObject);
                cottageRoot = PolishGroup(environment, CountyCottageRoot);
                Physics.SyncTransforms();
                cottages.Add(PlaceCountyCottage(cottageRoot, map, "Bridge keeper cottage", "Forest depot lodge",
                    new Vector3(-229, 0, -45), new Vector3(-195, 0, -58), .71f, false));
                cottages.Add(PlaceCountyCottage(cottageRoot, map, "Lakeside caretaker cottage", "Lakeside service house",
                    new Vector3(-48, 0, -265), new Vector3(-65, 0, -290), .69f, true));

                var anchors = environment.GetComponentsInChildren<Transform>().Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) &&
                    (t.name.StartsWith("P_BirchTree") || t.name.StartsWith("P_FirTree") ||
                     t.name.StartsWith("P_OakTree") || t.name.StartsWith("P_Boulder")))
                    .OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToArray();
                var canopy = anchors.Where(t => t.name.Contains("Tree")).Select(t => t.position).ToList();
                string Grass()
                {
                    float choice = Next(0, 1);
                    return choice < .52f ? "P_Grass2" : choice < .82f ? "P_Grass1" : "P_Grass3";
                }

                void Meadow(string name, Vector3 center, float radius, int treeCount)
                {
                    string pass = "Meadow - " + name;
                    var patches = new Vector3[6];
                    for (int i = 0; i < patches.Length; i++) patches[i] = InPatch(center, radius * .76f);
                    // New canopy belongs to the back and sides of the clearing, in small groves.
                    int placed = 0;
                    var copse = patches[0];
                    for (int attempt = 0; attempt < 100 && placed < treeCount; attempt++)
                    {
                        if (attempt % 24 == 0) copse = patches[(attempt / 24) % patches.Length];
                        var point = InPatch(copse, radius * .48f);
                        if (Flat(point - center).sqrMagnitude < radius * radius * .17f ||
                            canopy.Any(t => Flat(t - point).sqrMagnitude < 6.7f * 6.7f)) continue;
                        string tree = placed % 5 == 0 ? "P_OakTree2" : placed % 3 == 0 ? "P_FirTree2" : "P_BirchTree2";
                        if (Place(point, 0, pass, tree, Next(3.9f, 6.2f)))
                        {
                            canopy.Add(point);
                            placed++;
                        }
                    }

                    for (int i = 0; i < patches.Length; i++)
                    {
                        var patch = patches[i];
                        float reach = Next(radius * .27f, radius * .49f);
                        // A few shrub specimens and outcrops per patch leave the grass as the dominant layer.
                        if (i % 2 == 0)
                        {
                            Place(patch, 0, pass, "P_RockSpire_RockClump01", Next(1.8f, 3.7f));
                            Place(InPatch(patch, 3.7f), 0, pass, "P_RockSpire_Rock01", Next(.6f, 1.3f));
                            Place(InPatch(patch, 4f), 0, pass, "P_FirStump1", Next(1.1f, 1.9f));
                        }
                        for (int j = 0; j < 3; j++)
                            Place(InPatch(patch, reach), 0, pass, j == 0 ? "P_Bush1" : "P_Bush3_Light", Next(1.2f, 2.65f));

                        for (int j = 0; j < 86; j++)
                        {
                            var point = InPatch(patch, reach);
                            float choice = Next(0, 1);
                            string plant = choice < .82f ? Grass() : choice < .94f ? "P_Fern2" : "P_FlowerCrocus01";
                            float size = choice < .82f ? Next(1.15f, 3.15f) : choice < .94f ? Next(.85f, 1.8f) : Next(.5f, 1.15f);
                            Place(point, 0, pass, plant, size);
                        }
                    }
                }

                Meadow("forest cottages", new Vector3(-217, 0, -54), 37, 8);
                Meadow("bridge meadow", new Vector3(-193, 0, 13), 34, 6);
                Meadow("river bend", new Vector3(-155, 0, -106), 47, 12);
                Meadow("lakeside cottages", new Vector3(-56, 0, -269), 43, 9);
                Meadow("lake approach", new Vector3(-58, 0, -220), 39, 10);
                Meadow("western woodland", new Vector3(-350, 0, 98), 43, 10);
                Meadow("woodland clearing", new Vector3(-240, 0, 62), 44, 12);
                Meadow("western slope", new Vector3(-410, 0, 180), 38, 6);
                Meadow("upper woodland", new Vector3(-281, 0, 159), 37, 8);
                Meadow("east of the workshop", new Vector3(77, 0, -85), 43, 10);
                Meadow("south of the workshop", new Vector3(-42, 0, -108), 38, 9);
                Meadow("south forest edge", new Vector3(-298, 0, -130), 39, 10);
                Meadow("north forest edge", new Vector3(-170, 0, 144), 40, 10);

                void Island(Vector3 center, int biome, string pass)
                {
                    float radius = Next(6.5f, 12.5f);
                    string[] plants = CountyPlants[biome];
                    int count = biome <= 2 ? 31 : 24;
                    for (int j = 0; j < count; j++)
                    {
                        float choice = Next(0, 1);
                        string plant = biome <= 2 && choice < .77f ? Grass() :
                            choice < .77f ? plants[0] : choice < .87f ? plants[1] :
                            choice < .93f ? plants[2] : plants[3];
                        float size = plant.Contains("Bush") ? Next(1.15f, 2.45f) :
                            plant.Contains("Grass") ? Next(1.25f, 2.85f) : 0;
                        Place(InPatch(center, radius), biome, pass, plant, size);
                    }
                }
                var centers=new List<Vector3>();
                bool Reserve(Vector3 center,float spacing)
                {
                    if(centers.Any(p=>Flat(p-center).sqrMagnitude<spacing*spacing))return false;
                    centers.Add(center);return true;
                }
                foreach(var site in map.Sites.Skip(1))
                {
                    var f=Flat(site.ExitDirection).normalized;var r=Vector3.Cross(Vector3.up,f);
                    foreach(float side in new[]{-1f,1f}){var center=site.Position+r*side*13-f*5;if(Reserve(center,17))Island(center,site.Biome,"Recovery banks "+site.Id);}
                }
                // Tie understory to existing mature trees and large rocks.
                foreach(var anchor in anchors)
                {
                    var center=anchor.position+new Vector3(4,0,3);if(!Reserve(center,26))continue;
                    Island(center,CountyBiome(center),"Canopy and rock understory");
                }
                // Wider roadside compositions retain a clear shoulder and bridge approach.
                foreach(var road in scenicRoads)for(int i=25;i<road.Points.Length-5;i+=48)
                {
                    var p=road.Points[i];var forward=Flat(road.Points[i+1]-road.Points[i-1]).normalized;
                    var side=Vector3.Cross(Vector3.up,forward)*(i%96==25?1:-1);
                    var center=p+side*(road.Width+12);if(Reserve(center,25))Island(center,CountyBiome(center),"Roadside groundcover");
                }
                // Coverage pass: fill empty, safe meadow pockets rather than spend a biome quota early.
                for(int z=-430;z<=430;z+=30)for(int x=-430;x<=430;x+=30)
                {
                    var center=new Vector3(x+Mathf.PerlinNoise(x*.03f,z*.03f)*14-7,0,z+Mathf.PerlinNoise(z*.03f,x*.03f)*14-7);
                    center=Ground(center);int biome=CountyBiome(center);
                    if(!CountyClear(center,5,map,false)||!Reserve(center,19))continue;
                    Island(center,biome,"Meadow infill");
                }
                // Consume the retired border pass draws to preserve the existing roadside grass distribution.
                for(int edge=0;edge<4;edge++)for(int i=0;i<96;i++)
                {
                    float along=-465+i*9.8f;float inset=465+(i%3)*6;
                    var p=edge==0?new Vector3(inset,0,along):edge==1?new Vector3(-inset,0,along):edge==2?new Vector3(along,0,inset):new Vector3(along,0,-inset);
                    int b=CountyBiome(Ground(p));string rock=b==5?"P_RockDesert_Clump09":"P_RockSpire_RockClump01";
                    Place(p,b,"Map perimeter",rock,8+(float)random.NextDouble()*10,true);
                }

                // Extend short vegetation into the spaces between meadow patches near the
                // green driving routes. Two noise scales leave irregular open pockets.
                for (int i = 0; i < 47000; i++)
                {
                    var point = new Vector3(Next(-455, 205), 0, Next(-335, 205));
                    int x = Mathf.RoundToInt((point.x + 500) * .5f), z = Mathf.RoundToInt((point.z + 500) * .5f);
                    float verge = roadDistance[z, x] - roadWidth[z, x];
                    if (verge < 5 || verge > 31 || CountyBiome(point) > 2) continue;
                    float coverage = .65f * Mathf.PerlinNoise(point.x * .071f + 52, point.z * .071f + 61)
                        + .35f * Mathf.PerlinNoise(point.x * .024f + 71, point.z * .024f + 92);
                    if (coverage < .34f || Next(0, 1) > coverage + .1f) continue;
                    Place(point, CountyBiome(point), "Roadside meadow grass", Grass(), Next(1.05f, 2.6f));
                }

                int triangles=0;
                AssetDatabase.StartAssetEditing();
                try {
                foreach(var pair in batches)
                {
                    var batch=pair.Value;var mesh=new Mesh{name=pair.Key,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(batch.Parts.ToArray(),true,true,false);mesh.RecalculateBounds();triangles+=(int)mesh.GetIndexCount(0)/3;
                    var asset = VisualAsset(mesh, "Desktop County/" + pair.Key + ".asset");
                    var go=new GameObject(pair.Key,typeof(MeshFilter),typeof(MeshRenderer),typeof(LODGroup));go.transform.SetParent(root,false);go.transform.position=batch.Origin;go.GetComponent<MeshFilter>().sharedMesh=asset;
                    var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=batch.Material;renderer.shadowCastingMode=batch.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;renderer.receiveShadows=true;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
                    var lod = go.GetComponent<LODGroup>();
                    float cutoff = batch.CastShadows
                        ? Mathf.Clamp(asset.bounds.size.magnitude / 850f, .018f, .10f)
                        : Mathf.Clamp(asset.bounds.size.magnitude / 185f, .025f, .36f);
                    lod.SetLODs(new[] { new LOD(cutoff, new Renderer[] { renderer }) });
                    lod.RecalculateBounds();
                }
                } finally { AssetDatabase.StopAssetEditing(); }
                AddDesktopCountyReflections();
                if(mapBefore!=EditorJsonUtility.ToJson(map)||originalColliders.Any(p=>!oldCottageColliders.Contains(p.Key) && (p.Key==null||p.Value!=EditorJsonUtility.ToJson(p.Key))))throw new InvalidOperationException("Gameplay or existing colliders changed.");
                if(root.GetComponentsInChildren<Collider>().Length!=0)throw new InvalidOperationException("Decorative collision added");
                Directory.CreateDirectory("Docs/Validation/PCCounty");
                File.WriteAllText("Docs/Validation/PCCounty/Placements.json",Newtonsoft.Json.JsonConvert.SerializeObject(placements));
                File.WriteAllText("Docs/Validation/PCCounty/Authoring.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{baselineSmallNature=baseline,addedSmallNature=added,ratios=baseline.Select((n,i)=>n==0?0:(n+added[i])/(float)n).ToArray(),perimeter=placements.Count(p=>p.Pass=="Map perimeter"),detailInstances=placements.Count,renderers=batches.Count,triangles,materials=materials.Count,collidersAdded=cottageRoot.GetComponentsInChildren<Collider>().Length,existingCollidersUnchanged=originalColliders.Count-oldCottageColliders.Count,cottages=cottages.Select(p=>new{p.x,p.y,p.z}).ToArray(),mapUnchanged=true,terrainUnchanged=true},Newtonsoft.Json.Formatting.Indented));
                AssetDatabase.SaveAssets();
                foreach (var group in new[] { root, cottageRoot })
                    foreach (var child in group.GetComponentsInChildren<Transform>(true))
                        Undo.RegisterCreatedObjectUndo(child.gameObject, "Desktop county composition");
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undo);
            }
            catch
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root.gameObject);
                if (cottageRoot != null) UnityEngine.Object.DestroyImmediate(cottageRoot.gameObject);
                Undo.RevertAllDownToGroup(undo);
                throw;
            }
        }

        [MenuItem("Tow Trouble/Visuals/Prepare Desktop County Reflections")]
        public static void AddDesktopCountyReflections()
        {
            VisualContext();
            var root=environment.Find(CountyDesktopRoot);
            if(root==null)throw new InvalidOperationException("Author county art first");
            var map=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>().Map;
            foreach(int index in new[]{5,9,13,17,21})
            {
                string name="County biome "+map.Sites[index].Biome+" reflection";
                var existing=root.Find(name);if(existing!=null)continue;
                DesktopProbe(root,name,map.Sites[index].Position+Vector3.up*3,new Vector3(100,36,100));
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private static Vector3 PlaceCountyCottage(Transform parent, RecoveryMap map, string name, string exterior,
            Vector3 reference, Vector3 frontage, float scale, bool lakeside)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SuntailExteriorFolder + "/" + exterior + ".prefab");
            var bounds = SuntailLocalBounds(prefab.GetComponentsInChildren<MeshFilter>(true), prefab.transform);
            var doorway = prefab.transform.Find("Exterior doorway");
            var localFront = Flat(doorway.localPosition - bounds.center).normalized;
            var extents = bounds.extents * scale;
            float radius = new Vector2(extents.x, extents.z).magnitude;
            float best = float.PositiveInfinity;
            Vector3 position = default;
            Quaternion rotation = Quaternion.identity;
            for (int z = -20; z <= 20; z += 2)
            for (int x = -20; x <= 20; x += 2)
            {
                var point = Ground(reference + new Vector3(x, 0, z));
                if (!CountyClear(point, radius + 2, map, false) ||
                    terrain.terrainData.GetSteepness((point.x + 500) / 1000, (point.z + 500) / 1000) > 8) continue;
                var forward = Flat(frontage - point);
                if (forward.sqrMagnitude < 1) continue;
                var yaw = Quaternion.LookRotation(forward) * Quaternion.Inverse(Quaternion.LookRotation(localFront));
                float low = point.y, high = point.y;
                foreach (float dz in new[] { -extents.z, extents.z })
                foreach (float dx in new[] { -extents.x, extents.x })
                {
                    float y = Ground(point + yaw * new Vector3(dx, 0, dz)).y;
                    low = Mathf.Min(low, y);
                    high = Mathf.Max(high, y);
                }
                if (high - low > .55f) continue;
                float cost = x * x + z * z + (high - low) * 60;
                if (cost >= best) continue;
                if (Physics.OverlapBox(point + yaw * (bounds.center * scale), extents + new Vector3(2, 0, 2),
                        yaw, ~0, QueryTriggerInteraction.Ignore).Any(c => !(c is TerrainCollider))) continue;
                best = cost;
                position = new Vector3(point.x, low - .12f, point.z);
                rotation = yaw;
            }
            if (float.IsPositiveInfinity(best)) throw new InvalidOperationException("No clear cottage site for " + name);

            var yard = PolishGroup(parent, name);
            yard.SetPositionAndRotation(position, rotation);
            var house = (GameObject)PrefabUtility.InstantiatePrefab(prefab, yard);
            house.transform.localScale = Vector3.one * scale;
            Physics.SyncTransforms();
            var front = rotation * localFront;
            var right = Vector3.Cross(Vector3.up, front);
            Vector3 At(float side, float depth) => position + right * side + front * depth;

            Material Finish(string role) => AssetDatabase.LoadAssetAtPath<Material>(
                GarageServiceFolder + "/Workshop facade " + role + ".mat");
            var timber = Finish("Timber");
            var metal = Finish("Metal");
            var cutWood = Finish("WallLight");
            void Prop(string propName, Vector3 point, float footprint, Quaternion yaw, Action<GarageFacadeShapes> draw)
            {
                point = Ground(point);
                if (!CountyClear(point, footprint, map, false) ||
                    Physics.OverlapBox(point + Vector3.up * .65f, new Vector3(footprint, .6f, footprint),
                        yaw, ~0, QueryTriggerInteraction.Ignore).Any(c => !(c is TerrainCollider))) return;
                var prop = PolishGroup(yard, propName);
                prop.SetPositionAndRotation(point - Vector3.up * .16f, yaw);
                using (var shapes = new GarageFacadeShapes())
                {
                    draw(shapes);
                    shapes.Finish(prop, name + " - " + propName);
                }
                ConfigureDetailLod(prop.gameObject, 105);
            }

            var frontYaw = Quaternion.LookRotation(front);
            Prop("Supply crate", At(-radius * .66f, radius * .82f), .93f, frontYaw,
                shapes => ReceivingCrate(shapes, Vector3.zero, timber, metal));
            Prop("Firewood", At(-radius - .6f, -.6f), .98f, frontYaw,
                shapes => ReceivingFirewood(shapes, Vector3.zero, timber, cutWood));
            Prop("Rest bench", At(radius * .72f, radius * .95f), 1.35f, frontYaw,
                shapes => ReceivingBench(shapes, Vector3.zero, timber, metal));
            if (lakeside)
                Prop("Rowboat", At(radius + 3.5f, -radius * .25f), 2.6f, frontYaw * Quaternion.Euler(0, 30, 0),
                    shapes => CountyRowboat(shapes, timber, Finish("Door"), Finish("Trim")));
            else
                Prop("Stacked timber", At(radius + 1.6f, -radius * .36f), 1.3f, frontYaw * Quaternion.Euler(0, 20, 0),
                    shapes => ReceivingTimber(shapes, Vector3.zero, timber, metal));
            return position;
        }

        private static void CountyRowboat(GarageFacadeShapes shapes, Material timber, Material hull, Material trim)
        {
            var outline = new[] { new Vector2(.49f, -1.95f), new Vector2(.80f, -.92f),
                new Vector2(.83f, .35f), new Vector2(.61f, 1.28f), new Vector2(0, 2.16f) };
            for (int i = 1; i < outline.Length; i++)
            {
                var a = outline[i - 1];
                var b = outline[i];
                foreach (float side in new[] { -1f, 1f })
                {
                    var from = new Vector3(a.x * side, .36f, a.y);
                    var to = new Vector3(b.x * side, .36f, b.y);
                    var center = (from + to) * .5f;
                    var rotation = Quaternion.LookRotation(to - from);
                    float length = Vector3.Distance(from, to) + .03f;
                    shapes.Box(hull, center, new Vector3(.14f, .53f, length), rotation);
                    shapes.Box(trim, center + Vector3.up * .27f, new Vector3(.18f, .08f, length), rotation);
                }
                float width = (a.x + b.x) * .79f;
                shapes.Box(timber, new Vector3(0, .15f, (a.y + b.y) * .5f), new Vector3(width, .16f, b.y - a.y));
            }
            shapes.Box(hull, new Vector3(0, .37f, -1.95f), new Vector3(1.03f, .55f, .15f));
            foreach (float z in new[] { -1.08f, .02f, .92f })
                shapes.Box(timber, new Vector3(0, .47f, z), new Vector3(1.38f, .1f, .30f));
            shapes.Box(timber, new Vector3(0, .055f, -.95f), new Vector3(1.58f, .13f, .17f));
            shapes.Box(timber, new Vector3(0, .055f, .73f), new Vector3(1.58f, .13f, .17f));
        }

        private static int CountyBiome(Vector3 p)
        {
            float y=terrain.SampleHeight(p)+terrain.transform.position.y;
            if(y>138)return 3;if(p.x>220&&p.z<175)return 5;if(p.z< -335)return 4;
            if(p.z>170||y>55)return 2;if(p.z< -120&&p.x< -100)return 1;return 0;
        }
        private static bool CountyClear(Vector3 p,float radius,RecoveryMap map,bool border)
        {
            if(Mathf.Abs(p.x)>490||Mathf.Abs(p.z)>490||p.y<11.7f)return false;
            if(Flat(p-new Vector3(-9,0,9)).magnitude<66||Flat(p-map.Sites[0].Position).magnitude<25)return false;
            if(map.Deliveries.Any(d=>Flat(d.Position-p).magnitude<16+radius))return false;
            foreach(var s in map.Sites){var f=Flat(s.ExitDirection).normalized;var right=Vector3.Cross(Vector3.up,f);var d=p-s.Position;float side=Mathf.Abs(Vector3.Dot(d,right)),along=Vector3.Dot(d,f);if(side<5+radius&&along> -11-radius&&along<30+radius)return false;}
            int x=Mathf.Clamp(Mathf.RoundToInt((p.x+500)*.5f),0,500),z=Mathf.Clamp(Mathf.RoundToInt((p.z+500)*.5f),0,500);
            if(roadDistance[z,x]<roadWidth[z,x]+radius+3.5f)return false;
            if(!border&&terrain.terrainData.GetSteepness((p.x+500)/1000,(p.z+500)/1000)>39)return false;
            if(CountyOverWater(p,radius))return false;
            foreach(var house in new[]{new Vector3(-198.5f,0,-25),new Vector3(-71,0,-263)})if(Flat(p-house).magnitude<12+radius)return false;
            return true;
        }
        private struct CountyWaterFace { public Vector3 A,B,C; }
        private static readonly Dictionary<Vector2Int,List<CountyWaterFace>> countyWaterCells=new Dictionary<Vector2Int,List<CountyWaterFace>>();
        private static void CountyIndexWater(MeshRenderer[] renderers)
        {
            countyWaterCells.Clear();
            foreach(var renderer in renderers)
            {
                var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
                var vertices=mesh.vertices;var indices=mesh.triangles;
                for(int i=0;i<indices.Length;i+=3)
                {
                    var face=new CountyWaterFace{A=renderer.transform.TransformPoint(vertices[indices[i]]),B=renderer.transform.TransformPoint(vertices[indices[i+1]]),C=renderer.transform.TransformPoint(vertices[indices[i+2]])};
                    int minX=Mathf.FloorToInt(Mathf.Min(face.A.x,Mathf.Min(face.B.x,face.C.x))/16),maxX=Mathf.FloorToInt(Mathf.Max(face.A.x,Mathf.Max(face.B.x,face.C.x))/16);
                    int minZ=Mathf.FloorToInt(Mathf.Min(face.A.z,Mathf.Min(face.B.z,face.C.z))/16),maxZ=Mathf.FloorToInt(Mathf.Max(face.A.z,Mathf.Max(face.B.z,face.C.z))/16);
                    for(int z=minZ;z<=maxZ;z++)for(int x=minX;x<=maxX;x++)
                    {var key=new Vector2Int(x,z);if(!countyWaterCells.TryGetValue(key,out var list)){list=new List<CountyWaterFace>();countyWaterCells.Add(key,list);}list.Add(face);}
                }
            }
        }
        private static bool CountyOverWater(Vector3 p,float radius)
        {
            // Query actual water triangles, not the bounding rectangle of a winding river.
            for(int sample=0;sample<9;sample++)
            {
                var q=sample==0?p:p+new Vector3(Mathf.Cos((sample-1)*Mathf.PI/4),0,Mathf.Sin((sample-1)*Mathf.PI/4))*radius;
                if(!countyWaterCells.TryGetValue(new Vector2Int(Mathf.FloorToInt(q.x/16),Mathf.FloorToInt(q.z/16)),out var faces))continue;
                foreach(var f in faces)
                {
                    float den=(f.B.z-f.C.z)*(f.A.x-f.C.x)+(f.C.x-f.B.x)*(f.A.z-f.C.z);if(Mathf.Abs(den)<.0001f)continue;
                    float a=((f.B.z-f.C.z)*(q.x-f.C.x)+(f.C.x-f.B.x)*(q.z-f.C.z))/den;
                    float b=((f.C.z-f.A.z)*(q.x-f.C.x)+(f.A.x-f.C.x)*(q.z-f.C.z))/den;float c=1-a-b;
                    if(a>=0&&b>=0&&c>=0&&p.y<f.A.y*a+f.B.y*b+f.C.y*c+.35f)return true;
                }
            }
            return false;
        }
        private static CountyPrototype CountyLoadPrototype(string name)
        {
            string path=PrefabPath(name);if(path==null)throw new InvalidOperationException("Missing nature prefab "+name);
            var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));source.hideFlags=HideFlags.HideAndDontSave;source.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            try
            {
                var all=source.GetComponentsInChildren<MeshRenderer>(true);var allowed=new HashSet<Renderer>(all);
                foreach(var lod in source.GetComponentsInChildren<LODGroup>(true))
                {var chosen=lod.GetLODs().Select(l=>l.renderers).FirstOrDefault(r=>r.Any(x=>x!=null));if(chosen!=null)foreach(var r in lod.GetComponentsInChildren<Renderer>(true))if(!chosen.Contains(r))allowed.Remove(r);}
                var result=new CountyPrototype();bool first=true;
                foreach(var r in all.Where(r=>allowed.Contains(r)))
                {
                    var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
                    if(first){result.Bounds=r.bounds;first=false;}else result.Bounds.Encapsulate(r.bounds);
                    for(int sub=0;sub<mesh.subMeshCount&&sub<r.sharedMaterials.Length;sub++)if(r.sharedMaterials[sub]!=null)result.Parts.Add(new CountyPart{Mesh=mesh,Material=r.sharedMaterials[sub],Submesh=sub,Matrix=r.transform.localToWorldMatrix});
                }
                if(result.Parts.Count==0)throw new InvalidOperationException("Empty nature prototype "+name);
                return result;
            }
            finally{UnityEngine.Object.DestroyImmediate(source);}
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TowTrouble.Editor
{
    public static class WinterExpeditionAuthoring
    {
        private const string Folder = "Assets/TowTrouble/OpenWorld/Winter Expedition";
        private const string TerrainPath = Folder + "/Winter County Terrain.asset";
        private const string Nature = "Assets/SoStylized/Environment/";

        [MenuItem("Tow Trouble/Expeditions/Author Winter County")]
        private static void AuthorWinterCounty() => Debug.Log(Apply());

        [MenuItem("Tow Trouble/Expeditions/Refine Winter Roads and Horizon")]
        public static void RefineWinterCounty()
        {
            var scene = SceneManager.GetActiveScene();
            var terrain = Terrain.activeTerrain;
            var session = UnityEngine.Object.FindFirstObjectByType<DemoSession>();
            if (Application.isPlaying || scene.name != "WinterExpeditions" || scene.isDirty || terrain == null ||
                AssetDatabase.GetAssetPath(terrain.terrainData) != TerrainPath || session == null ||
                session.GetComponent<WinterExpedition>() == null)
                throw new InvalidOperationException("Open the saved winter scene in Edit Mode first.");
            var environment = terrain.transform.parent;
            var data = terrain.terrainData;
            var alpha = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            PackRoads(terrain, alpha);
            data.SetAlphamaps(0, 0, alpha);
            SaveContactMap(terrain, alpha);
            var layers = data.terrainLayers;
            var roadLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(Folder + "/Wind-packed snow.terrainlayer");
            if (roadLayer == null)
            {
                roadLayer = UnityEngine.Object.Instantiate(layers[1]);
                roadLayer.name = "Wind-packed snow";
                AssetDatabase.CreateAsset(roadLayer, Folder + "/Wind-packed snow.terrainlayer");
            }
            roadLayer.diffuseRemapMax = new Vector4(.54f, .66f, .77f, 1);
            EditorUtility.SetDirty(roadLayer);
            layers[1] = roadLayer;
            data.terrainLayers = layers;
            var horizon = Surface("Distant winter slopes", new Color(.60f, .72f, .82f));
            var ridges = Surface("Distant winter ridges", new Color(.45f, .56f, .68f));
            var frosted = AssetDatabase.LoadAssetAtPath<Material>(Nature + "Foliage/Materials/MV_GrassSnow.mat");
            foreach (var renderer in environment.GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    string name = materials[i].name;
                    Material replacement = name == "County MV_Mountain" ? ridges
                        : Contains(name, "distant ground") || Contains(name, "horizon terrain") || Contains(name, "foothill soil") ? horizon
                        : Contains(name, "MV_Flower") || Contains(name, "MV_Daisy") || Contains(name, "MV_Sunflower") ? frosted : null;
                    if (replacement == null) continue;
                    materials[i] = replacement;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = materials;
            }
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static string Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "WinterExpeditions" || scene.isDirty || Application.isPlaying)
                throw new InvalidOperationException("Open and save WinterExpeditions in Edit Mode first.");

            var session = UnityEngine.Object.FindFirstObjectByType<DemoSession>();
            var world = session.World;
            var map = UnityEngine.Object.FindFirstObjectByType<RecoveryMap>();
            var terrain = Terrain.activeTerrain;
            if (terrain == null || AssetDatabase.GetAssetPath(terrain.terrainData) != TerrainPath)
                throw new InvalidOperationException("The winter scene must use its own terrain asset.");
            var environment = terrain.transform.parent;
            if (session.GetComponent<WinterExpedition>() != null)
                throw new InvalidOperationException("The winter expedition is already authored in this scene.");

            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/TowTrouble/OpenWorld", "Winter Expedition");

            var snow = Surface("Snow banks", new Color(.79f, .87f, .94f));
            var packed = Surface("Packed road snow", new Color(.56f, .66f, .73f));
            var stone = Surface("Exposed slate", new Color(.33f, .39f, .47f));
            var amber = Surface("Anchor reflector", new Color(1f, .56f, .09f), .22f);
            var paint = Surface("Survey blue", new Color(.13f, .36f, .55f));
            var cableMaterial = Surface("Winch cable", new Color(.06f, .08f, .1f));

            var ground = PaintTerrain(terrain);
            var track = environment.GetComponentInChildren<RecoveryWheelTracks>();
            var trackSettings = new SerializedObject(track);
            trackSettings.FindProperty("surfaceMap").objectReferenceValue = ground;
            trackSettings.ApplyModifiedPropertiesWithoutUndo();
            foreach (var surface in environment.GetComponentsInChildren<RecoveryGroundSurface>(true))
            {
                surface.ConfigureMap(ground);
                EditorUtility.SetDirty(surface);
            }
            foreach (var surface in environment.GetComponentsInChildren<RecoveryTrackSurface>(true))
            {
                if (surface.Kind == RecoveryTrackKind.Wood || surface.Kind == RecoveryTrackKind.Hard) continue;
                surface.Configure(RecoveryTrackKind.Snow);
                EditorUtility.SetDirty(surface);
            }
            foreach (var mud in environment.GetComponentsInChildren<RecoveryMudTerrain>(true))
            {
                if (!mud.TryGetComponent<RecoveryGroundSurface>(out var surface)) surface = mud.gameObject.AddComponent<RecoveryGroundSurface>();
                surface.Configure(RecoveryGroundKind.Snow);
                if (mud.TryGetComponent<RecoveryMudSurface>(out var reactive)) UnityEngine.Object.DestroyImmediate(reactive);
                if (mud.TryGetComponent<Dinopunch.LivingMud.DynamicMudSurface>(out var visual)) UnityEngine.Object.DestroyImmediate(visual);
                if (mud.TryGetComponent<MeshRenderer>(out var renderer)) renderer.sharedMaterial = snow;
                UnityEngine.Object.DestroyImmediate(mud);
            }

            WinterizeTrees(terrain.terrainData);
            int changed = WinterizeScenery(environment, snow, packed, stone);
            var root = new GameObject("Winter expedition sites");
            root.transform.SetParent(environment, false);
            var surveys = new[]
            {
                Survey(map, "B", "Pinewood weather station", "Sheltered forest roads and deep snow on the shoulders.", 850),
                Survey(map, "E", "Frozen lakeshore survey", "Follow the shoreline trail and stop by the landing.", 1050),
                Survey(map, "D", "High cabin snow survey", "Climb the switchbacks. Yellow posts mark winch anchors.", 1500)
            };
            foreach (var survey in surveys) BuildSurveyMarker(root.transform, survey, paint, snow);
            var anchors = PlaceAnchors(map, world.GaragePosition, surveys, root.transform, stone, amber);

            var cableObject = new GameObject("Expedition winch cable");
            cableObject.transform.SetParent(root.transform, false);
            var line = cableObject.AddComponent<LineRenderer>();
            line.sharedMaterial = cableMaterial;
            line.positionCount = 3;
            line.startWidth = line.endWidth = .026f;
            line.numCapVertices = 3;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.enabled = false;
            var expedition = session.gameObject.AddComponent<WinterExpedition>();
            expedition.Configure(surveys, anchors.ToArray(), line);

            EditorUtility.SetDirty(terrain.terrainData);
            EditorUtility.SetDirty(expedition);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the winter scene.");
            return $"Winter county saved. {changed:N0} renderer palettes changed, {anchors.Count} anchors, {surveys.Length} surveys.";
        }

        private static Material Surface(string name, Color color, float emission = 0)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            material.SetFloat("_Smoothness", .14f);
            if (emission > 0)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static RecoveryGroundMap PaintTerrain(Terrain terrain)
        {
            var data = terrain.terrainData;
            int n = data.alphamapResolution;
            var original = data.GetAlphamaps(0, 0, n, n);
            var previousLayers = data.terrainLayers;
            data.terrainLayers = new[] { previousLayers[4], previousLayers[7], previousLayers[3] };

            var winter = new float[n, n, 3];
            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)(n - 1), v = z / (float)(n - 1);
                float lane = Mathf.Clamp01((original[z, x, 1] + original[z, x, 7] * .95f) * 1.1f);
                float exposed = Mathf.SmoothStep(0, .87f, Mathf.InverseLerp(29, 54, data.GetSteepness(u, v))) * (1 - lane);
                winter[z, x, 0] = (1 - lane - exposed);
                winter[z, x, 1] = lane;
                winter[z, x, 2] = exposed;
            }
            PackRoads(terrain, winter);
            data.SetAlphamaps(0, 0, winter);
            return SaveContactMap(terrain, winter);
        }

        private static void PackRoads(Terrain terrain, float[,,] alpha)
        {
            var roads = terrain.transform.parent.Find("Road network - 24 recovery spurs");
            int n = alpha.GetLength(0);
            var origin = terrain.transform.position;
            float sx = (n - 1) / terrain.terrainData.size.x, sz = (n - 1) / terrain.terrainData.size.z;
            foreach (Transform road in roads)
            {
                if (!road.gameObject.activeSelf || !road.TryGetComponent<MeshCollider>(out var collider)) continue;
                var mesh = collider.sharedMesh;
                if (mesh == null || !mesh.isReadable || mesh.vertexCount % 2 != 0) continue;
                var vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i += 2)
                {
                    var center = road.TransformPoint((vertices[i] + vertices[i + 1]) * .5f) - origin;
                    float radius = Mathf.Clamp(Vector3.Distance(vertices[i], vertices[i + 1]) * .5f, 1.5f, 7f);
                    int left = Mathf.Max(0, Mathf.FloorToInt((center.x - radius) * sx));
                    int right = Mathf.Min(n - 1, Mathf.CeilToInt((center.x + radius) * sx));
                    int bottom = Mathf.Max(0, Mathf.FloorToInt((center.z - radius) * sz));
                    int top = Mathf.Min(n - 1, Mathf.CeilToInt((center.z + radius) * sz));
                    for (int z = bottom; z <= top; z++)
                    for (int x = left; x <= right; x++)
                    {
                        float dx = x / sx - center.x, dz = z / sz - center.z;
                        float distance = Mathf.Sqrt(dx * dx + dz * dz);
                        float lane = .86f * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(radius - 1f, radius, distance)));
                        float current = alpha[z, x, 1];
                        if (lane <= current) continue;
                        float scale = (1 - lane) / Mathf.Max(.001f, 1 - current);
                        alpha[z, x, 0] *= scale;
                        alpha[z, x, 1] = lane;
                        alpha[z, x, 2] *= scale;
                    }
                }
            }
        }

        private static RecoveryGroundMap SaveContactMap(Terrain terrain, float[,,] winter)
        {
            int n = winter.GetLength(0);
            const int resolution = 513;
            var kinds = new byte[resolution * resolution];
            var tracks = new byte[kinds.Length];
            var impressions = new byte[kinds.Length];
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                int row = Mathf.RoundToInt(z * (n - 1f) / (resolution - 1)), column = Mathf.RoundToInt(x * (n - 1f) / (resolution - 1));
                int cell = z * resolution + x;
                kinds[cell] = (byte)(winter[row, column, 1] > .46f ? RecoveryGroundKind.Rock : RecoveryGroundKind.Snow);
                tracks[cell] = (byte)RecoveryTrackKind.Snow;
                impressions[cell] = 1;
            }

            const string path = Folder + "/Winter wheel contact map.asset";
            var ground = AssetDatabase.LoadAssetAtPath<RecoveryGroundMap>(path);
            bool create = ground == null;
            if (create) ground = ScriptableObject.CreateInstance<RecoveryGroundMap>();
            var position = terrain.transform.position;
            var size = terrain.terrainData.size;
            ground.Configure(new Vector2(position.x, position.z), new Vector2(size.x, size.z), resolution, kinds);
            ground.ConfigureTrackKinds(tracks);
            ground.ConfigureWheelImpressions(impressions);
            if (create) AssetDatabase.CreateAsset(ground, path);
            else EditorUtility.SetDirty(ground);
            return ground;
        }

        private static void WinterizeTrees(TerrainData data)
        {
            var pines = Enumerable.Range(1, 3).Select(i => AssetDatabase.LoadAssetAtPath<GameObject>(
                Nature + $"Trees/Pine/Prefabs/P_Pine{i:00}_Snowy.prefab")).ToArray();
            var oaks = Enumerable.Range(1, 4).Select(i => AssetDatabase.LoadAssetAtPath<GameObject>(
                Nature + $"Trees/Oak/Prefabs/P_OakTree{i}_Snow.prefab")).ToArray();
            var bushes = Enumerable.Range(1, 3).Select(i => AssetDatabase.LoadAssetAtPath<GameObject>(
                Nature + $"Foliage/Prefabs/P_Bush{i}_Snowy.prefab")).ToArray();
            var original = data.treePrototypes;
            var instances = data.treeInstances;
            var palette = new List<TreePrototype>();
            var replacements = new int[original.Length];
            for (int i = 0; i < original.Length; i++)
            {
                var prefab = original[i].prefab;
                string name = prefab.name;
                if (!Contains(name, "Snow") && !Contains(name, "Rock"))
                {
                    var choices = Contains(name, "Bush") || Contains(name, "Fern") || Contains(name, "Flower") ||
                        Contains(name, "Foxtail") || Contains(name, "Rice") || Contains(name, "Ears") ? bushes
                        : Contains(name, "Oak") || Contains(name, "Birch") || Contains(name, "Maple") ? oaks : pines;
                    prefab = choices[i % choices.Length] ?? prefab;
                }
                int index = palette.FindIndex(item => item.prefab == prefab);
                if (index < 0) { index = palette.Count; palette.Add(new TreePrototype { prefab = prefab, bendFactor = original[i].bendFactor }); }
                replacements[i] = index;
            }
            for (int i = 0; i < instances.Length; i++)
            {
                var instance = instances[i];
                instance.prototypeIndex = replacements[instance.prototypeIndex];
                instance.color = Color.white;
                instances[i] = instance;
            }
            data.treeInstances = Array.Empty<TreeInstance>();
            data.treePrototypes = palette.ToArray();
            data.SetTreeInstances(instances, false);
        }

        private static int WinterizeScenery(Transform root, Material snow, Material packed, Material stone)
        {
            var snowyPine = Nature + "Trees/Materials/MV_PineLeaves_Snow.mat";
            var snowyPineLod = Nature + "Trees/Materials/LODs/MV_PineLeaves_Snow_SingleMat.mat";
            var snowyOak = Nature + "Trees/Materials/MV_OakLeaves_Snowy.mat";
            var snowyOakLod = Nature + "Trees/Materials/LODs/MV_OakLeaves_Snowy_SingleMat.mat";
            var snowyShrub = Nature + "Foliage/Materials/M_BushLeaves_Snow.mat";
            var snowyGrass = Nature + "Foliage/Materials/MV_GrassSnow.mat";
            var snowyRock = Nature + "Rocks/Materials/Classic/MV_RockClassic_Rocks_Snowy.mat";
            var snowyCliff = Nature + "Rocks/Materials/Classic/MV_RockClassic_Cliff_Snow.mat";
            var cache = new Dictionary<Material, Material>();
            int count = 0;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials = renderer.sharedMaterials;
                if (materials.Length == 0) continue;
                bool changed = false;
                bool hide = materials.All(m => m != null &&
                    (Contains(m.name, "Cactus") || Contains(m.name, "Palm") || Contains(m.name, "Bamboo")));
                if (hide)
                {
                    renderer.enabled = false;
                    foreach (var collider in renderer.GetComponents<Collider>()) collider.enabled = false;
                    continue;
                }
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null) continue;
                    if (!cache.TryGetValue(source, out var replacement))
                    {
                        string name = source.name;
                        string path = null;
                        if (!Contains(name, "snow") && !Contains(name, "winter") && !Contains(name, "water") && !Contains(name, "foam"))
                        {
                            bool lod = Contains(name, "lod") || Contains(name, "singlemat");
                            if (Contains(name, "grass") && (Contains(name, "MV_") || Contains(name, "ground cover"))) path = snowyGrass;
                            else if (Contains(name, "firleaves") || Contains(name, "pineleaves") || Contains(name, "chinapine")) path = lod ? snowyPineLod : snowyPine;
                            else if (Contains(name, "leaves") && (Contains(name, "oak") || Contains(name, "birch") || Contains(name, "maple") || Contains(name, "knotwood"))) path = lod ? snowyOakLod : snowyOak;
                            else if (Contains(name, "bush") || Contains(name, "MV_Fern") || Contains(name, "foxtail") || Contains(name, "marshtail")) path = snowyShrub;
                            else if (Contains(name, "RockClassic") || Contains(name, "RockSpire") || Contains(name, "RockDesert")) path = Contains(name, "Cliff") ? snowyCliff : snowyRock;
                            else if (Contains(name, "Mud") || Contains(name, "Soft soil") || Contains(name, "Sand bed")) replacement = snow;
                            else if (Contains(name, "compacted circulation") || Contains(name, "workshop forecourt") || Contains(name, "road aggregate")) replacement = packed;
                            else if (Contains(name, "sandstone") || Contains(name, "warm alpine rock")) replacement = stone;
                        }
                        if (path != null) replacement = AssetDatabase.LoadAssetAtPath<Material>(path);
                        cache[source] = replacement;
                    }
                    if (replacement == null) continue;
                    materials[i] = replacement;
                    changed = true;
                }
                if (changed)
                {
                    renderer.sharedMaterials = materials;
                    count++;
                }
            }
            return count;
        }

        private static bool Contains(string value, string part) => value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

        private static WinterExpedition.Survey Survey(RecoveryMap map, string id, string name, string brief, int reward)
            => new WinterExpedition.Survey { Name = name, Brief = brief, Point = map.Deliveries.Single(bay => bay.Id == id).Position, Reward = reward };

        private static void BuildSurveyMarker(Transform root, WinterExpedition.Survey survey, Material paint, Material snow)
        {
            // Keep the usable parking area unobstructed.
            Vector3 position = survey.Point + Vector3.forward * 7;
            if (Ground(position, out var hit)) position.y = hit.point.y;
            var site = new GameObject(survey.Name).transform;
            site.SetParent(root, false);
            site.position = position;
            Prop(site, "Survey mast", PrimitiveType.Cylinder, new Vector3(0, .82f, 0), new Vector3(.09f, .82f, .09f), paint);
            Prop(site, "Instrument case", PrimitiveType.Cube, new Vector3(0, .27f, 0), new Vector3(.68f, .54f, .49f), paint);
            Prop(site, "Weather shield", PrimitiveType.Cube, new Vector3(0, 1.64f, 0), new Vector3(.72f, .13f, .65f), snow);
            Prop(site, "Survey reflector", PrimitiveType.Cube, new Vector3(0, 1.44f, 0), new Vector3(.43f, .22f, .44f), snow);
        }

        private static List<Transform> PlaceAnchors(RecoveryMap map, Vector3 start, WinterExpedition.Survey[] surveys,
            Transform root, Material metal, Material amber)
        {
            var anchors = new List<Transform>();
            var route = new List<Vector3>();
            foreach (var survey in surveys)
            {
                if (!map.TryGetRoadRoute(start, survey.Point, route)) continue;
                float spacing = 0;
                for (int i = 2; i < route.Count - 2; i++)
                {
                    spacing += Vector3.Distance(route[i - 1], route[i]);
                    if (spacing < 24 && i != 3) continue;
                    if (anchors.Any(a => (a.position - route[i]).sqrMagnitude < 17f * 17f)) continue;
                    var side = Vector3.Cross(Vector3.up, route[i + 1] - route[i - 1]).normalized;
                    if (!Ground(route[i] + side * 5, out var hit) || Mathf.Abs(hit.point.y - route[i].y) > 2.2f)
                        if (!Ground(route[i] - side * 5, out hit) || Mathf.Abs(hit.point.y - route[i].y) > 2.2f) continue;

                    var post = new GameObject("Winch anchor " + (anchors.Count + 1)).transform;
                    post.SetParent(root, false);
                    post.position = hit.point + Vector3.up * .84f;
                    Prop(post, "Steel post", PrimitiveType.Cylinder, Vector3.down * .24f, new Vector3(.13f, .62f, .13f), metal);
                    Prop(post, "Yellow reflector", PrimitiveType.Cylinder, Vector3.up * .21f, new Vector3(.18f, .11f, .18f), amber);
                    Prop(post, "Winch eye", PrimitiveType.Sphere, Vector3.zero, Vector3.one * .25f, amber);
                    anchors.Add(post);
                    spacing = 0;
                }
            }
            return anchors;
        }

        private static bool Ground(Vector3 point, out RaycastHit hit)
            => Physics.Raycast(point + Vector3.up * 8, Vector3.down, out hit, 18, 1 << 8, QueryTriggerInteraction.Ignore);

        private static void Prop(Transform parent, string name, PrimitiveType kind, Vector3 point, Vector3 scale, Material material)
        {
            var child = GameObject.CreatePrimitive(kind);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = point;
            child.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(child.GetComponent<Collider>());
            child.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}

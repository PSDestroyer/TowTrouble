using System;
using System.Collections.Generic;
using System.Linq;
using Dinopunch.LivingMud;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static Material gravel,snow,sand,wood,ink,amber;
        private static string PrefabPath(string name)=>AssetDatabase.FindAssets(name+" t:Prefab",new[]{"Assets/SoStylized/Environment"}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>System.IO.Path.GetFileNameWithoutExtension(p)==name);
        private static bool BlocksRoad(Bounds b,float clearance=7)
        {b.Expand(new Vector3(clearance*2,0,clearance*2));foreach(var e in plan.Edges){var a=nodes[e.A].Point;var c=nodes[e.B].Point;int n=Mathf.CeilToInt(Vector3.Distance(a,c)/5);for(int i=0;i<=n;i++){var p=Vector3.Lerp(a,c,i/(float)n);if(p.x>b.min.x&&p.x<b.max.x&&p.z>b.min.z&&p.z<b.max.z)return true;}}return false;}
        private static void Colliders(GameObject go)
        {foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=8;foreach(var filter in go.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null&&filter.GetComponent<Collider>()==null)filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;}
        private static void CopyMegaDressing(Scene source,Terrain original)
        {
            var parent=new GameObject("Mega - retained environment").transform;parent.SetParent(environment,false);
            foreach(var go in source.GetRootGameObjects())
            {
                if(!go.name.StartsWith("P_")||go.name.Contains("Water")||go.name.Contains("Sky")||go.GetComponentInChildren<Camera>(true)!=null||go.GetComponentInChildren<Light>(true)!=null)continue;
                var renderers=go.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)continue;var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                var p=go.transform.position;float u=p.x/900+.5f,v=.5f-p.z/900;float ground=Ground(p).y;
                if(u<.02f||u>.97f||v<.02f||v>.98f||u>.69f&&v>.32f||LakeDistance(new Vector2(u,v))<12||BlocksRoad(bounds)||Mathf.Max(bounds.size.x,bounds.size.z)>140)continue;
                bool tree=go.name.Contains("Tree")||go.name.Contains("Pine")||go.name.Contains("Fir")||go.name.Contains("Bush")||go.name.Contains("Grass")||go.name.Contains("Flower");if(tree&&ground>125&&!go.name.Contains("Snow"))continue;
                var copy=UnityEngine.Object.Instantiate(go,parent);copy.name=go.name;float old=original.SampleHeight(p)+original.transform.position.y;copy.transform.position=new Vector3(p.x,ground+Mathf.Clamp(p.y-old,-4,3),p.z);Colliders(copy);
            }
        }
        private static void CreateRoads()
        {
            gravel=Mat("Warm gravel",new Color(.68f,.48f,.27f));gravel.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SoStylized/Environment/Landscape/Textures/T_Dirt_BC.png"));gravel.SetTextureScale("_BaseMap",Vector2.one*.16f);
            snow=Mat("Packed snow",new Color(.87f,.94f,.98f));sand=Mat("Dune tracks",new Color(.86f,.63f,.32f));wood=Mat("Weathered timber",new Color(.32f,.20f,.10f));ink=Mat("Workshop charcoal",new Color(.075f,.10f,.11f));amber=Mat("Dispatch gold",new Color(1,.62f,.10f));
            var roads=new GameObject("Road network - 24 recovery spurs").transform;roads.SetParent(environment,false);int index=0;
            foreach(var edge in plan.Edges)
            {
                var a=nodes[edge.A].Point;var b=nodes[edge.B].Point;var middle=(a+b)*.5f;float u=middle.x/900+.5f,v=.5f-middle.z/900;var material=middle.y>124?snow:u>.70f||v>.85f?sand:gravel;
                var delta=b-a;delta.y=0;var right=new Vector3(delta.z,0,-delta.x).normalized;int n=Mathf.Max(2,Mathf.CeilToInt(delta.magnitude/2));float halfWidth=edge.Trail?3.2f:4.8f;var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
                for(int i=0;i<=n;i++)
                {var center=Vector3.Lerp(a,b,i/(float)n);for(int side=-1;side<=1;side+=2){var p=Ground(center+right*side*halfWidth);p.y+=.035f;vertices.Add(p);uv.Add(new Vector2(side<0?0:1,i*delta.magnitude/n*.1f));}if(i<n){int k=i*2;triangles.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});}}
                var mesh=new Mesh{name="Conforming road"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var go=MeshObject("Road-"+(index++).ToString("00")+"-"+edge.A+"-"+edge.B,mesh,material,roads);
                if(material==snow||material==sand)go.AddComponent<RecoveryGroundSurface>().Configure(material==snow?RecoveryGroundKind.Snow:RecoveryGroundKind.Sand);
            }
            // Native Broken Vector geometry at the workshop approach, fitted to its flat platform.
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BrokenVector/LowPolyRoadPack/Prefabs/Road Straight 1.prefab");
            if(prefab==null)throw new InvalidOperationException("Low Poly Road Pack missing.");
            for(int i=0;i<2;i++){var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,roads);go.name="Broken Vector - workshop approach";var rs=go.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);go.transform.localScale=new Vector3(10/Mathf.Max(.1f,bounds.size.x),1,10/Mathf.Max(.1f,bounds.size.z));var p=nodes["g"].Point+new Vector3(0,0,-i*10);go.transform.position=Ground(p)+Vector3.up*(.1f-bounds.min.y);foreach(var r in rs)r.sharedMaterial=gravel;Colliders(go);}
        }
        private static void CreateWater()
        {
            var waterPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("P_Water"));var renderer=waterPrefab==null?null:waterPrefab.GetComponentInChildren<Renderer>();var material=renderer==null?Mat("Lake turquoise",new Color(.12f,.57f,.63f)):renderer.sharedMaterial;
            var vertices=new List<Vector3>();var center=Vector2.zero;foreach(var p in lake)center+=p;center/=lake.Length;vertices.Add(new Vector3((center.x-.5f)*900,10.4f,(.5f-center.y)*900));foreach(var p in lake)vertices.Add(new Vector3((p.x-.5f)*900,10.4f,(.5f-p.y)*900));var triangles=new List<int>();for(int i=0;i<lake.Length;i++)triangles.AddRange(new[]{0,(i+1)%lake.Length+1,i+1});
            var mesh=new Mesh{name="Lake shore polygon"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,vertices.Select(p=>new Vector2(p.x*.02f,p.z*.02f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();MeshObject("Lake - native So Stylized water",mesh,material,environment,false);
        }
        private static void DressBiomes()
        {
            var parent=new GameObject("Biome dressing - So Stylized").transform;parent.SetParent(environment,false);var random=new System.Random(6731);
            var desertNames=new[]{"P_Cactus03","P_Cactus05","P_Cactus08","P_DesertBush01","P_RockDesert_Clump09","P_RockDesert_Layered01"};var forestNames=new[]{"P_BirchTree1","P_BirchTree2","P_FirTree2","P_FirTree4"};var snowyNames=new[]{"P_OakTree1_Snow","P_OakTree2_Snow","P_Bush2_Snowy"};
            var lookup=desertNames.Concat(forestNames).Concat(snowyNames).Distinct().ToDictionary(n=>n,n=>{var path=PrefabPath(n);return path==null?null:AssetDatabase.LoadAssetAtPath<GameObject>(path);});
            for(int i=0;i<850;i++)
            {
                float u=.06f+(float)random.NextDouble()*.88f,v=.04f+(float)random.NextDouble()*.90f;var p=new Vector3((u-.5f)*900,0,(.5f-v)*900);p=Ground(p);
                if(Corridor(p,out _)<17||LakeDistance(new Vector2(u,v))<15||Vector3.Distance(p,nodes["g"].Point)<40)continue;
                bool desert=u>.70f&&v>.32f,snowy=p.y>125&&v<.3f;var names=desert?desertNames:snowy?snowyNames:forestNames;if(!desert&&!snowy&&u>.65f)continue;
                var prefab=lookup[names[random.Next(names.Length)]];if(prefab==null)continue;var copy=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);copy.transform.position=p;copy.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);copy.transform.localScale*=.65f+(float)random.NextDouble()*.65f;
                if(desert&&copy.name.Contains("Rock")){var rs=copy.GetComponentsInChildren<Renderer>();if(rs.Length>0){var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);copy.transform.localScale*=Mathf.Min(1,12/Mathf.Max(b.size.x,b.size.z));}}
                var renderers=copy.GetComponentsInChildren<Renderer>();if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);if(BlocksRoad(bounds,5)){UnityEngine.Object.DestroyImmediate(copy);continue;}}Colliders(copy);
            }
        }
        private static void CreateWorkshop()
        {
            var parent=new GameObject("Central garage - Tow County").transform;parent.SetParent(environment,false);var p=Ground(nodes["g"].Point);var red=Mat("Workshop warm red",new Color(.43f,.17f,.10f));
            // Open front and a parking yard: the camera and player do not start inside a solid box.
            var building=p+new Vector3(0,0,22);Cube("Workshop rear",building+new Vector3(0,3,7),new Vector3(18,6,.4f),red,parent);Cube("Workshop left",building+new Vector3(-9,3,0),new Vector3(.4f,6,14),red,parent);Cube("Workshop right",building+new Vector3(9,3,0),new Vector3(.4f,6,14),red,parent);Cube("Roof",building+new Vector3(0,6.2f,0),new Vector3(20,.5f,16),ink,parent);Cube("Workshop header",building+new Vector3(0,5.2f,-7),new Vector3(18,1.6f,.5f),red,parent);Text("TOW TROUBLE\nATELIER",building+new Vector3(0,5.2f,-7.3f),0,2.2f,parent);
            for(int i=0;i<4;i++)Cube("Tool chest",building+new Vector3(-6+i*4,1,5),new Vector3(1.4f,2,1),wood,parent);
            foreach(float x in new[]{-7f,7f})Cube("Garage parking edge",p+new Vector3(x,.08f,0),new Vector3(.14f,.1f,12),amber,parent,false);
            Text("GARAJ  [G]",p+new Vector3(0,.13f,-5),90,1.4f,parent);Text("PĂDURE  ←    MUNTE  ↑    DEȘERT  →",p+new Vector3(0,3,-17),180,1.3f,parent);
        }
        private static void CreateLocations()
        {
            var parent=new GameObject("24 recoveries and 8 delivery bays").transform;parent.SetParent(environment,false);
            foreach(var bay in plan.Deliveries)
            {
                var p=Ground(nodes[bay.Node].Point);var go=new GameObject("Dropoff "+bay.Id+" - "+bay.Name);go.transform.SetParent(parent,false);go.transform.position=p;
                foreach(float x in new[]{-7f,7f})Cube("Bay side",p+new Vector3(x,.1f,0),new Vector3(.18f,.12f,14),amber,go.transform,false);foreach(float z in new[]{-7f,7f})Cube("Bay edge",p+new Vector3(0,.1f,z),new Vector3(14,.12f,.18f),amber,go.transform,false);
                Cube("Destination sign",p+new Vector3(0,3,9),new Vector3(13,2,.3f),ink,go.transform);Text(bay.Name,p+new Vector3(0,3,8.8f),0,1.35f,go.transform,BayEnglish(bay.Id));Text("PREDARE",p+new Vector3(0,.16f,0),90,2,go.transform);
                FitDestinationCaption(DestinationCaption(go.transform), go.transform.Find("Destination sign"));
            }
            foreach(var site in plan.Sites)
            {
                var p=Ground(RecoveryPoint(site));var go=new GameObject(site.Id+" - "+site.Name);go.transform.SetParent(parent,false);go.transform.position=p;
            }
        }
        // Keep authored object names stable while printing English on the signs.
        private const string DropoffMark = "DROP-OFF";
        private static string WorldSignCaption(string text)
        {
            switch (text)
            {
                case "PREDARE": return DropoffMark;
                case "GARAJ  [G]": return "GARAGE  [G]";
                case "TOW TROUBLE\nATELIER": return "TOW TROUBLE\nWORKSHOP";
                case "PĂDURE  ←    MUNTE  ↑    DEȘERT  →": return "FOREST  ←    MOUNTAIN  ↑    DESERT  →";
                case "POSTUL ALPIN": return "ALPINE POST";
                case "REFUGIUL MONTAN": return "MOUNTAIN REFUGE";
                default: return text;
            }
        }
        private static bool IsDropoffMark(string text) =>
            text.StartsWith("PREDARE", StringComparison.Ordinal) || text.StartsWith(DropoffMark, StringComparison.Ordinal);

        // The destination displays have separate ground paint and a freestanding name board.
        private static TextMeshPro DestinationCaption(Transform bay)
        {
            return bay.GetComponentsInChildren<TextMeshPro>(true)
                .Single(label => !IsDropoffMark(label.text));
        }

        private static bool FitDestinationCaption(TextMeshPro label, Transform board)
        {
            var size = new Vector2(board.localScale.x - .3f, board.localScale.y * .88f);
            float max = board.localScale.y * 4.5f;
            if (label.enableAutoSizing && label.rectTransform.sizeDelta == size &&
                Mathf.Approximately(label.fontSizeMin, .5f) && Mathf.Approximately(label.fontSizeMax, max) &&
                label.textWrappingMode == TextWrappingModes.NoWrap) return false;

            Undo.RecordObjects(new UnityEngine.Object[] {label, label.rectTransform}, "Fit destination name to sign");
            label.rectTransform.sizeDelta = size;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = .5f;
            label.fontSizeMax = max;
            return true;
        }

        // Migration for the authored scene. Keep site anchors used by dressing tools and delivery geometry.
        public static int NaturalizeRecoverySigns()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
                throw new InvalidOperationException("Open OpenWorldRecovery in Edit Mode.");
            var roots = scene.GetRootGameObjects();
            var map = roots.SelectMany(root => root.GetComponentsInChildren<RecoveryMap>(true)).Single();
            var locations = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Single(t => t.name == "24 recoveries and 8 delivery bays");
            var siteAnchors = map.Sites.Select(site => new {
                site.Id, Anchor = locations.Cast<Transform>().Single(t => t.name.StartsWith(site.Id + " - "))
            }).ToArray();
            var surroundings = locations.parent.Find("Recovery situations - 24 authored surrounds");

            int changed = 0;
            foreach (var site in siteAnchors)
            {
                foreach (Transform child in site.Anchor.Cast<Transform>().ToArray())
                {
                    var text = child.GetComponent<TextMeshPro>();
                    if (child.name != "Recovery sign" && child.name != "Site marker post" &&
                        child.name != "Recovery sign reverse label" &&
                        !(text != null && text.text.StartsWith(site.Id + " ") && text.text.Contains("→"))) continue;
                    Undo.DestroyObjectImmediate(child.gameObject);
                    changed++;
                }
                var setting = surroundings != null ? surroundings.Find(site.Id + " recovery setting") : null;
                if (setting == null) continue;
                foreach (Transform child in setting.Cast<Transform>().ToArray())
                {
                    if (child.name != "Recovery boundary stake" && child.name != "Reflective warning cap") continue;
                    Undo.DestroyObjectImmediate(child.gameObject);
                    changed++;
                }
            }

            foreach (var label in roots.SelectMany(root => root.GetComponentsInChildren<TextMeshPro>(true)))
            {
                string old = label.text;
                string replacement = WorldSignCaption(old);
                foreach (var bay in map.Deliveries)
                {
                    string prefix = bay.Id + " · ";
                    if (old == bay.Name) replacement = bay.English;
                    else if (old == "PREDARE " + bay.Id || old == "PREDARE · " + bay.Id ||
                             old == DropoffMark + " " + bay.Id || old == DropoffMark + " · " + bay.Id) replacement = DropoffMark;
                    else if (old.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        string name = old.Substring(prefix.Length);
                        replacement = name == bay.Name || IsDropoffMark(name) ? bay.English : name;
                    }
                    else continue;
                    break;
                }
                if (replacement == old) continue;
                Undo.RecordObject(label, "Update recovery sign caption");
                label.text = replacement;
                changed++;
            }
            foreach (var bay in map.Deliveries)
            {
                var group = locations.Cast<Transform>().Single(t => t.name.StartsWith("Dropoff " + bay.Id + " -"));
                if (FitDestinationCaption(DestinationCaption(group), group.Find("Destination sign"))) changed++;
            }
            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save recovery signage.");
            }
            return changed;
        }

        private static void CreateMud(GameObject template,RecoveryWorld world)
        {
            var parent=new GameObject("Living Mud - forest and lake recoveries").transform;parent.SetParent(environment,false);
            foreach(var site in plan.Sites.Where(s=>s.Biome==0&&(s.Id=="R02"||s.Id=="R03")||s.Biome==1))
            {
                var center=RecoveryPoint(site);var patch=UnityEngine.Object.Instantiate(template,parent);patch.name=site.Id+" - Living Mud";patch.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);patch.transform.localScale=Vector3.one;
                var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();const int n=12;for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){float u=x/(float)n,v=z/(float)n;var p=Ground(center+new Vector3((u-.5f)*16,0,(v-.5f)*18));p.y+=.07f;vertices.Add(p);uv.Add(new Vector2(u,v));if(x<n&&z<n){int k=z*(n+1)+x;triangles.AddRange(new[]{k,k+n+1,k+1,k+1,k+n+1,k+n+2});}}
                var mesh=new Mesh{name=patch.name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Root+"/Meshes/"+site.Id+"-Mud.asset");patch.GetComponent<MeshFilter>().sharedMesh=mesh;patch.GetComponent<MeshCollider>().sharedMesh=mesh;
                patch.GetComponent<RecoveryMudSurface>().Configure(world,world.Drivers.Concat(new[]{world.Casualty}).ToArray(),patch.GetComponent<DynamicMudSurface>(),AssetDatabase.LoadAssetAtPath<Shader>("Assets/TowTrouble/Integrations/LivingMud/LivingMudWheelStamps.shader"),AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Living Mud/Prefabs/Stamp Prefabs/Tire Tread Stamp/Tire Tread Stamp Normal.png"));var serialized=new SerializedObject(patch.GetComponent<RecoveryMudSurface>());serialized.FindProperty("resolution").intValue=512;serialized.ApplyModifiedPropertiesWithoutUndo();patch.SetActive(true);patch.layer=8;
            }
        }
    }
}

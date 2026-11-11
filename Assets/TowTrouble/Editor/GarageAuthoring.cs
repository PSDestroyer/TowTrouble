using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowTrouble.Editor
{
    public static class GarageAuthoring
    {
        public const string Root="Assets/TowTrouble/Garage";
        public const string CatalogPath=Root+"/GarageCatalog.asset";
        private static readonly string[] Packs={"Assets/Hot_Rod_Oldtimers","Assets/Hot_Rod_OffRoad"};
        private static readonly string[] Stock={"Historic_Taxi","Historic_Villager","Historic_Bulldog","Goblin_Offroad"};
        private static readonly string[] WheelJoints={"Wheel_Front_Left","Wheel_Front_Right","Wheel_Rear_Left","Wheel_Rear_Right"};
        private static readonly List<string> audit=new List<string>();
        [MenuItem("Tow Trouble/Integrate Garage and Contract Board")]
        public static void ApplyMenu(){Apply();}
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play Mode before authoring the garage.");
            var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(DemoBuilder.ScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(DemoBuilder.ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try{return BuildAndWire();}finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }
        private static string BuildAndWire()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play Mode before authoring the garage.");
            Directory.CreateDirectory(Root+"/Assemblies");Directory.CreateDirectory(Root+"/Parts");Directory.CreateDirectory(Root+"/Materials");
            var entries=new List<GaragePart>();audit.Clear();audit.Add("source,classification,reason");
            foreach(GarageSlot slot in Enum.GetValues(typeof(GarageSlot)))entries.Add(new GaragePart{Id="stock_"+slot,Name="Original",EnglishName="Stock",Slot=slot,Price=0,PaintIndex=slot==GarageSlot.Paint?0:-1,Description="Echipamentul inițial.",EnglishDescription="Original equipment."});
            var all=AssetDatabase.FindAssets("t:Prefab",Packs).Select(AssetDatabase.GUIDToAssetPath).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            var stock=new GameObject[4];
            for(int i=0;i<4;i++){string path=all.Single(x=>Path.GetFileNameWithoutExtension(x)==Stock[i]);stock[i]=Assembly(path);}
            foreach(var path in all)
            {
                string n=Path.GetFileNameWithoutExtension(path);
                if(path.Contains("/Demo_Vehicles/")&&!path.Contains("No_Interior")&&!n.StartsWith("Battle")&&!n.StartsWith("SciFi")&&!n.Contains("Razor")&&!n.Contains("Toy")&&!n.Contains("Ruin"))
                {
                    if(Stock.Contains(n)){Audit(path,"stock assembly","Included with the existing four vehicle archetypes");continue;}
                    try{var assembly=Assembly(path);entries.Add(Part(path,GarageSlot.Assembly,1200,new[]{assembly},null,null));Audit(path,"body assembly","Complete compatible body/chassis sockets; wheel anchors authored");}catch(InvalidOperationException){Audit(path,"excluded","No complete four-wheel assembly");}continue;
                }
                if(!path.Contains("/Parts/")||n.Contains("Weapon")||n.Contains("Turret")||n.StartsWith("Props_")||path.Contains("SciFi")||n.StartsWith("Hoverpad")||n.Contains("Razor")||n.Contains("Axe")||n.Contains("Savage")){Audit(path,"excluded","Combat, sci-fi, character, projectile or non-part content");continue;}
                if(path.Contains("/Body")||path.Contains("No_Rig")||path.Contains("NoRig")||n.Contains("Adapter")){Audit(path,"assembly dependency","Bodies and rig variants are represented by complete body assemblies; adapters require a verified combination");continue;}
                GarageSlot slot;string socket;string[] families=null;int price=450;
                if(n.StartsWith("Wheel_"))
                {
                    if(n.Contains("Rim")||n.Contains("None")||n.Contains("NoWheel")||n.Contains("Hub")||n.Contains("Steamroller")){Audit(path,"excluded","Rim-only, empty or unsuitable wheel geometry");continue;}
                    slot=GarageSlot.Wheels;socket="SOCKET_Wheel_";
                }
                else if(n.StartsWith("Bonnet_")){slot=GarageSlot.Bonnet;socket="SOCKET_Bonnet";families=FitFamilies(n.Substring(7).Split('_')[0]);price=600;}
                else if(n.StartsWith("Fender_")){Audit(path,"kit dependency","Grouped into matching front/rear/step kits");continue;}
                else if(n.StartsWith("Chassis_")){Audit(path,"kit dependency","Grouped into matching front/rear chassis kits");continue;}
                else if(n.StartsWith("Bumper_"))
                {
                    if(n.Contains("Side_Left")||n.Contains("Side_Right")){Audit(path,"kit dependency","Included with matching bumper");continue;}
                    bool rear=n.Contains("Rear");slot=rear?GarageSlot.RearBumper:GarageSlot.FrontBumper;socket=rear?"SOCKET_Bumper_Rear":"SOCKET_Bumper_Front";price=400;
                }
                else if(n.StartsWith("Attachment_Top_")){slot=GarageSlot.Roof;socket="SOCKET_Attachment_Top";price=350;}
                else if(n.Contains("Motor_Exhaust_")){slot=GarageSlot.Exhaust;socket="SOCKET_Motor_Exhaust";price=300;}
                else if(n.StartsWith("Motor_")||n.StartsWith("A_Motor_")){slot=GarageSlot.Intake;socket=n.Contains("Carb_")?"SOCKET_Motor_Carb":"SOCKET_Motor";price=400;}
                else if(n=="Attachment_Flatbed_Winch"){slot=GarageSlot.Roof;socket="SOCKET_Attachment_Top";families=new[]{"Villager","Lorry","Pickupster"};price=600;}
                else{Audit(path,"excluded","No validated noncombat mounting slot");continue;}
                if(slot==GarageSlot.Wheels)
                {
                    var visual=Visual(path);var part=Part(path,slot,500,new[]{visual,visual,visual,visual},WheelJoints.Select(x=>"SOCKET_"+x).ToArray(),null);entries.Add(part);
                }
                else
                {
                    var paths=new List<string>{path};
                    if(slot==GarageSlot.FrontBumper){string stem=n.Replace("_Front","");paths.AddRange(all.Where(x=>Path.GetFileNameWithoutExtension(x).StartsWith(stem+"_Front_Side_")));}
                    var sockets=paths.Select(_=>socket).ToArray();entries.Add(Part(path,slot,price,paths.Select(Visual).ToArray(),sockets,families));
                }
                Audit(path,"garage part","Mounting socket and family filter; final visual fit requires inspection");
            }
            AddKits(entries,all,"Fender_",GarageSlot.Fenders);
            AddKits(entries,all,"Chassis_",GarageSlot.Chassis);
            Functional(entries);ConfigureCableProgression(entries);ConfigureWinchPrototype(entries);GarageHandlingAuthoring.Apply(entries);
            var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>(CatalogPath);if(catalog==null){catalog=ScriptableObject.CreateInstance<RecoveryGarageCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
            foreach(var entry in entries)if(entry.Slot==GarageSlot.Paint&&entry.Price==0)entry.PaintIndex=0;
            catalog.Configure(entries.ToArray(),stock);catalog.TireShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/TowTrouble/Art/Mud/Stylized Tire Coating.shadergraph");
            var mud=AssetDatabase.LoadAssetAtPath<Material>("Assets/Living Mud/URP Materials/Dynamic Mud Surface Material/DMS Mud03 Material.mat");catalog.MudTexture=mud.GetTexture("_Albedo");EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            var scene=SceneManager.GetSceneByPath(DemoBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(DemoBuilder.ScenePath,OpenSceneMode.Additive);
            var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryWorld>(true)).Single();
            foreach(var car in world.Drivers){var custom=car.GetComponent<RecoveryCustomization>();if(custom==null)custom=Undo.AddComponent<RecoveryCustomization>(car.gameObject);custom.Configure(car,catalog);EditorUtility.SetDirty(custom);}
            var garage=new Vector3(16,0,92);if(Physics.Raycast(garage+Vector3.up*20,Vector3.down,out var ground,40,1<<8,QueryTriggerInteraction.Ignore))garage.y=ground.point.y;world.ConfigureGarage(garage);EditorUtility.SetDirty(world);CreateGarageMarker(scene,world.GaragePosition);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Docs/Design");File.WriteAllLines("Docs/Design/HotRod-AssetAudit.csv",audit);
            var report=entries.GroupBy(x=>x.Slot).Select(g=>g.Key+": "+g.Count());File.WriteAllText("Docs/Design/HotRod-Catalog.md","# Catalogul Hot Rod pentru garaj\n\nGenerat din cele două pachete locale. ID-urile folosesc GUID-urile sursă. Prefaburile derivate sunt sub Assets/TowTrouble/Garage; sursele rămân intacte.\n\n"+string.Join("\n",report)+"\n\nAuditul complet, inclusiv excluderile și dependențele, este în HotRod-AssetAudit.csv. Compatibilitatea automată verifică familia și socketul; intersecțiile vizuale se verifică în Editor. Armele, proiectilele și hovercrafturile nu fac parte din joc.\n");
            return "Garage catalog: "+entries.Count+" entries; source audit: "+all.Length+" prefabs; tutorial wired; Mega/Desert untouched.";
        }
        private static void Audit(string path,string kind,string reason){audit.Add("\""+path+"\",\""+kind+"\",\""+reason+"\"");}
        [MenuItem("Tow Trouble/Garage/Refresh Winch Prototype")]
        public static void RefreshWinchPrototype()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode only.");
            var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>(CatalogPath);
            if(catalog==null)throw new InvalidOperationException("Missing garage catalog.");
            Undo.RecordObject(catalog,"Configure winch prototype");
            var entries=new List<GaragePart>(catalog.Parts);ConfigureWinchPrototype(entries);
            var serialized=new SerializedObject(catalog);var saved=serialized.FindProperty("stockAssemblies");
            var assemblies=new GameObject[saved.arraySize];for(int i=0;i<assemblies.Length;i++)assemblies[i]=(GameObject)saved.GetArrayElementAtIndex(i).objectReferenceValue;
            catalog.Configure(entries.ToArray(),assemblies);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
        }
        private static void ConfigureWinchPrototype(List<GaragePart> entries)
        {
            var stock=entries.Single(p=>p.Id=="stock_Winch");
            stock.Name="Troliu stock · 8 kN";stock.EnglishName="Stock winch · 8 kN";
            stock.WinchForce=RecoveryWinch.StockForce;stock.WinchSpeed=RecoveryWinch.StockSpeed;
            stock.Description="8 kN; până la 1,0 m/s. Încetinește sub sarcină și se oprește la limită.";
            stock.EnglishDescription="8 kN; up to 1.0 m/s. Slows under load and stalls at its limit.";
            var improved=entries.Single(p=>p.Id=="winch_2");
            improved.Name="Troliu îmbunătățit · 14 kN";improved.EnglishName="Improved winch · 14 kN";
            improved.WinchForce=14000;improved.WinchSpeed=1.3f;
            improved.Description="14 kN; până la 1,3 m/s. Un cablu mai slab se poate rupe. Aderența contează.";
            improved.EnglishDescription="14 kN; up to 1.3 m/s. A weaker cable can break. Grip matters.";
        }
        [MenuItem("Tow Trouble/Garage/Refresh Cable Progression")]
        public static void RefreshCableProgression()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode only.");
            var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>(CatalogPath);
            if(catalog==null)throw new InvalidOperationException("Missing garage catalog.");
            Undo.RecordObject(catalog,"Add starter cable progression");
            var entries=new List<GaragePart>(catalog.Parts);
            ConfigureCableProgression(entries);
            var serialized=new SerializedObject(catalog);var saved=serialized.FindProperty("stockAssemblies");
            var assemblies=new GameObject[saved.arraySize];for(int i=0;i<assemblies.Length;i++)assemblies[i]=(GameObject)saved.GetArrayElementAtIndex(i).objectReferenceValue;
            catalog.Configure(entries.ToArray(),assemblies);
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
        }
        private static void ConfigureCableProgression(List<GaragePart> entries)
        {
            // Keep the free stock ID and all purchased upgrade IDs stable in saved builds.
            var stock=entries.Single(p=>p.Id=="stock_Cable");
            stock.Name="Cablu stock · 11 kN";stock.EnglishName="Stock cable · 11 kN";
            stock.Price=0;stock.CableStrength=RecoveryRules.BreakForce;
            stock.Description="Prag de rupere 11 kN. Trage progresiv și evită smuciturile.";
            stock.EnglishDescription="Break threshold 11 kN. Build the pull gently and avoid sudden jerks.";
            var light=entries.FirstOrDefault(p=>p.Id=="cable_14");
            if(light==null){light=new GaragePart{Id="cable_14",Slot=GarageSlot.Cable};entries.Insert(entries.IndexOf(stock)+1,light);}
            light.Name="Cablu ușor · 14 kN";light.EnglishName="Light cable · 14 kN";
            light.Price=200;light.CableStrength=14000;
            light.Description="Primul upgrade de cablu. Prag de rupere 14 kN.";
            light.EnglishDescription="First cable upgrade. Break threshold 14 kN.";
            var standard=entries.FirstOrDefault(p=>p.Id=="cable_18");
            if(standard==null){standard=new GaragePart{Id="cable_18",Slot=GarageSlot.Cable};entries.Insert(entries.IndexOf(light)+1,standard);}
            standard.Name="Cablu standard · 18 kN";standard.EnglishName="Standard cable · 18 kN";
            standard.Price=700;standard.CableStrength=18000;
            standard.Description="Prag de rupere 18 kN. Mai multă rezervă pentru sarcini și smucituri.";
            standard.EnglishDescription="Break threshold 18 kN. More headroom for load and sudden jerks.";
        }
        private static string[] FitFamilies(string family)=>family=="Geep"?new[]{"Geep","Goblin"}:family=="Pickupster"?new[]{"Pickupster","Goblin"}:new[]{family};
        private static GaragePart Part(string path,GarageSlot slot,int price,GameObject[] prefabs,string[] sockets,string[] families)
        {
            string n=Path.GetFileNameWithoutExtension(path).Replace('_',' ');return new GaragePart{Id="part_"+AssetDatabase.AssetPathToGUID(path),Name=n,EnglishName=n,Slot=slot,Price=price,Prefabs=prefabs,Sockets=sockets,Families=families,AddedMass=slot==GarageSlot.Assembly?30:slot==GarageSlot.Chassis?40:0,Description="Piesă Hot Rod modulară. Previzionează potrivirea înainte de cumpărare.",EnglishDescription="Modular Hot Rod part. Preview its fit before buying."};
        }
        private static void AddKits(List<GaragePart> entries,string[] all,string prefix,GarageSlot slot)
        {
            var paths=all.Where(p=>Path.GetFileNameWithoutExtension(p).StartsWith(prefix)&&!p.Contains("No_Rig")&&!p.Contains("NoRig")&&!p.Contains("SciFi")&&!p.Contains("Scifi")).ToArray();
            foreach(var group in paths.GroupBy(p=>{string n=Path.GetFileNameWithoutExtension(p);foreach(var suffix in new[]{"_Front_Left","_Front_Right","_Rear_Left","_Rear_Right","_Left","_Right","_Front","_Rear"})if(n.EndsWith(suffix))return n.Substring(0,n.Length-suffix.Length);return n;}))
            {
                var groupPaths=group.OrderBy(x=>x).ToArray();if(groupPaths.Length<2)continue;
                string[] sockets=groupPaths.Select(p=>{string n=Path.GetFileNameWithoutExtension(p);return slot==GarageSlot.Chassis?(n.EndsWith("Front")?"SOCKET_Chassis_Front":"SOCKET_Chassis_Rear"):n.Contains("Front")?"SOCKET_Fenders_Front":n.Contains("Rear")?"SOCKET_Fenders_Rear":"SOCKET_Fenders_Step";}).ToArray();
                var family=group.Key.Substring(prefix.Length).Split('_')[0];var p=Part(groupPaths[0],slot,slot==GarageSlot.Chassis?2200:600,groupPaths.Select(Visual).ToArray(),sockets,slot==GarageSlot.Chassis?null:FitFamilies(family));p.Name=p.EnglishName=group.Key.Replace('_',' ');if(slot==GarageSlot.Chassis)p.Track=group.Key.Contains("Widest")?1.1f:1.03f;entries.Add(p);
            }
        }
        private static void Functional(List<GaragePart> parts)
        {
            GaragePart Add(string id,string ro,string en,GarageSlot slot,int price,string description,string english)
            {var p=new GaragePart{Id=id,Name=ro,EnglishName=en,Slot=slot,Price=price,Description=description,EnglishDescription=english};parts.Add(p);return p;}
            GameObject Wheel(string n)=>Visual(AssetDatabase.FindAssets(n+" t:Prefab",Packs).Select(AssetDatabase.GUIDToAssetPath).First(p=>Path.GetFileNameWithoutExtension(p)==n));
            var sockets=WheelJoints.Select(x=>"SOCKET_"+x).ToArray();
            var tires=Add("tires_allterrain","Universale îmbunătățite","All-terrain",GarageSlot.Wheels,1200,"Noroi: aderență +10%. Set de patru roți.","Mud grip +10%. Four-wheel set.");tires.MudGrip=1.1f;tires.Prefabs=Enumerable.Repeat(Wheel("Wheel_Truk_Offroad"),4).ToArray();tires.Sockets=sockets;
            tires=Add("tires_mud","Anvelope de noroi","Mud tires",GarageSlot.Wheels,1200,"Aderență în noroi +30%; rezistență în noroi -15%.","Mud grip +30%; mud resistance -15%.");tires.MudGrip=1.3f;tires.MudResistance=.85f;tires.Prefabs=Enumerable.Repeat(Wheel("Wheel_Buggy_Mud"),4).ToArray();tires.Sockets=sockets;
            tires=Add("tires_sand","Anvelope late de nisip","Wide sand tires",GarageSlot.Wheels,1200,"Set lat. Specializarea pe nisip se aplică odată cu biomul; noroi -10% aderență.","Wide set. Sand specialization arrives with the biome; mud grip -10%.");tires.MudGrip=.9f;tires.Prefabs=Enumerable.Repeat(Wheel("Wheel_HotRod_Fat_Wider"),4).ToArray();tires.Sockets=sockets;
            tires=Add("tires_winter","Anvelope de iarnă cu crampoane","Studded winter tires",GarageSlot.Wheels,1600,"Roți cu crampoane. Efectul pe zăpadă se aplică odată cu biomul; noroi -15% aderență.","Studded wheels. Snow effect arrives with the biome; mud grip -15%.");tires.MudGrip=.85f;tires.Prefabs=Enumerable.Repeat(Wheel("Wheel_Studs_Wide"),4).ToArray();tires.Sockets=sockets;
            Add("engine_2","Motor nivel 2","Engine level 2",GarageSlot.Engine,800,"Forță nominală +20%; aderența limitează forța utilă.","Nominal drive +20%; usable force remains grip-limited.").DriveMultiplier=1.2f;
            Add("engine_3","Motor nivel 3","Engine level 3",GarageSlot.Engine,2600,"Forță nominală +35%. Preț total de la motorul inițial.","Nominal drive +35%. Full price from stock engine.").DriveMultiplier=1.35f;
            var reduction=Add("low_range","Reductor","Low range",GarageSlot.Transmission,1000,"Forță +25%; viteză maximă -20%.","Drive +25%; top speed -20%.");reduction.DriveMultiplier=1.25f;reduction.SpeedMultiplier=.8f;
            var suspension=Add("suspension_offroad","Suspensie de teren","Off-road suspension",GarageSlot.Suspension,1000,"Gardă la sol +15 cm; susținere îmbunătățită pe denivelări.","Ride height +15 cm; improved support on uneven ground.");suspension.Lift=.15f;suspension.AddedMass=20;
            Add("cable_26","Cablu întărit · 26 kN","Reinforced cable · 26 kN",GarageSlot.Cable,1400,"Prag de rupere 26 kN.","Break threshold 26 kN.").CableStrength=26000;
            Add("cable_36","Cablu profesional · 36 kN","Professional cable · 36 kN",GarageSlot.Cable,3400,"Prag de rupere 36 kN.","Break threshold 36 kN.").CableStrength=36000;
            Add("winch_2","Troliu îmbunătățit","Improved winch",GarageSlot.Winch,1600,"",""); // Configured by ConfigureWinchPrototype.
            ConfigureSpecialistRecoveryEquipment(parts);
            for(int i=1;i<=3;i++){var p=Add("paint_"+i,new[]{"","Crem","Măsliniu","Muștar"}[i],new[]{"","Cream","Olive","Mustard"}[i],GarageSlot.Paint,100,"Finisaj pentru caroserie.","Body paint finish.");p.PaintIndex=i;}
        }
        [MenuItem("Tow Trouble/Garage/Refresh Specialist Recovery Equipment")]
        public static void RefreshSpecialistRecoveryEquipment()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode only");
            var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>(CatalogPath);
            Undo.RecordObject(catalog,"Expert recovery equipment");var entries=new List<GaragePart>(catalog.Parts);ConfigureSpecialistRecoveryEquipment(entries);GarageHandlingAuthoring.Apply(entries);
            var serialized=new SerializedObject(catalog);var saved=serialized.FindProperty("stockAssemblies");
            var assemblies=new GameObject[saved.arraySize];for(int i=0;i<assemblies.Length;i++)assemblies[i]=(GameObject)saved.GetArrayElementAtIndex(i).objectReferenceValue;
            catalog.Configure(entries.ToArray(),assemblies);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
        }
        private static void ConfigureSpecialistRecoveryEquipment(List<GaragePart> entries)
        {
            var source=entries.Single(p=>p.Id=="tires_winter");
            var alpine=entries.FirstOrDefault(p=>p.Id=="tires_winter_pro");
            if(alpine==null){alpine=new GaragePart{Id="tires_winter_pro",Slot=GarageSlot.Wheels};entries.Add(alpine);}
            alpine.Name="Anvelope alpine ranforsate";alpine.EnglishName="Heavy alpine tires";alpine.Price=1800;
            alpine.Description="Zăpadă: aderență +135%; +35 kg. Noroi: aderență -20%. Pentru recuperări grele pe zăpadă.";
            alpine.EnglishDescription="Snow grip +135%; +35 kg. Mud grip -20%. For heavy recoveries on snow.";
            alpine.Prefabs=source.Prefabs;alpine.Sockets=source.Sockets;alpine.Families=source.Families;alpine.MudGrip=.80f;alpine.AddedMass=35;
            var winch=entries.FirstOrDefault(p=>p.Id=="winch_3");
            if(winch==null){winch=new GaragePart{Id="winch_3",Slot=GarageSlot.Winch};entries.Add(winch);}
            winch.Name="Troliu profesional · 22 kN";winch.EnglishName="Professional winch · 22 kN";winch.Price=4200;winch.WinchForce=22000;winch.WinchSpeed=.85f;
            winch.Description="22 kN; până la 0,85 m/s. Recomandat cablu de 26 kN și teren cu aderență bună.";
            winch.EnglishDescription="22 kN; up to 0.85 m/s. Recommended: 26 kN cable and firm footing.";
        }
        private static string InferFamily(string name)
        {foreach(string family in new[]{"Taxi","Villager","Bulldog","Bucket","Electron","Lorry","Martian","Midget","Midjet","Prince","Rumpler","Spartan","Speedster","Geep","Goblin","Pickupster"})if(name.Contains(family))return family;return name.Split('_')[0];}
        private static GameObject Assembly(string path)
        {
            string target=Root+"/Assemblies/"+Path.GetFileName(path);var saved=AssetDatabase.LoadAssetAtPath<GameObject>(target);
            if(saved!=null)
            {
                var data=saved.GetComponent<RecoveryAssembly>();if(data.Family.Contains("_")||data.Radii==null||data.Radii.Length!=4)
                {
                    var edit=PrefabUtility.LoadPrefabContents(target);
                    try{var binding=edit.GetComponent<RecoveryAssembly>();if(binding.Family.Contains("_"))binding.Family=InferFamily(Path.GetFileNameWithoutExtension(path));binding.Radii=binding.Wheels.Select(w=>w.GetComponentsInChildren<Renderer>().First().bounds.size.y*.5f).ToArray();PrefabUtility.SaveAsPrefabAsset(edit,target);}finally{PrefabUtility.UnloadPrefabContents(edit);}
                }return saved;
            }
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);var go=UnityEngine.Object.Instantiate(source);go.name=source.name;go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);go.transform.localScale=Vector3.one*1.25f;
            try
            {
                Sanitize(go);var data=go.AddComponent<RecoveryAssembly>();var first=go.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>r.name.StartsWith("Body_"));data.Family=first==null?InferFamily(source.name):first.name.Substring(5).Split('_')[0];
                data.Wheels=new Transform[4];data.Anchors=new Vector3[4];data.Radii=new float[4];
                for(int i=0;i<4;i++)
                {
                    var joint=go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==WheelJoints[i]);if(joint==null)throw new InvalidOperationException("Incomplete wheel rig");
                    var rs=joint.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new InvalidOperationException("Missing wheel renderer");
                    data.Radius=rs[0].bounds.size.y*.5f;data.Radii[i]=data.Radius;var wrapper=new GameObject("Suspended "+WheelJoints[i]).transform;wrapper.SetParent(go.transform,false);wrapper.position=joint.position;wrapper.rotation=Quaternion.identity;joint.SetParent(wrapper,true);data.Wheels[i]=wrapper;data.Anchors[i]=wrapper.position+Vector3.up*.12f;
                }
                Bounds b=BoundsOf(go);data.ColliderCenter=b.center;data.ColliderSize=new Vector3(Mathf.Clamp(b.size.x*.74f,1.2f,3),Mathf.Clamp(b.size.y*.65f,1,2.6f),Mathf.Clamp(b.size.z*.8f,2.4f,5.7f));data.FrontHitch=new Vector3(0,.66f,b.max.z+.08f);data.RearHitch=new Vector3(0,.66f,b.min.z-.08f);
                return PrefabUtility.SaveAsPrefabAsset(go,target);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        private static GameObject Visual(string path)
        {
            string target=Root+"/Parts/"+Path.GetFileName(path);var saved=AssetDatabase.LoadAssetAtPath<GameObject>(target);if(saved!=null)return saved;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);var go=UnityEngine.Object.Instantiate(source);go.name=source.name;
            try{Sanitize(go);return PrefabUtility.SaveAsPrefabAsset(go,target);}finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        private static void Sanitize(GameObject go)
        {
            foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);foreach(var c in go.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(c);
            foreach(var t in go.GetComponentsInChildren<Transform>(true))if(t!=null&&(t.name.Contains("Weapon")||t.name.Contains("Turret")||t.name.Contains("Top_Sign")))t.gameObject.SetActive(false);
            foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var values=r.sharedMaterials;for(int i=0;i<values.Length;i++)
                {
                    var original=values[i];if(original==null||original.shader.name.StartsWith("Universal Render Pipeline"))continue;
                    string path=Root+"/Materials/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original))+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));if(original.HasProperty("_MainTex"))material.SetTexture("_BaseMap",original.GetTexture("_MainTex"));if(original.HasProperty("_Color"))material.SetColor("_BaseColor",original.GetColor("_Color"));AssetDatabase.CreateAsset(material,path);}values[i]=material;
                }r.sharedMaterials=values;
            }
        }
        private static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        private static void CreateGarageMarker(Scene scene,Vector3 center)
        {
            var existing=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Garage service area");if(existing!=null)
            {
                existing.transform.position=Vector3.up*center.y;
                foreach(var edge in existing.GetComponentsInChildren<Transform>().Where(t=>t.name=="Service bay edge")){var p=edge.position;if(Physics.Raycast(p+Vector3.up*20,Vector3.down,out var hit,40,1<<8,QueryTriggerInteraction.Ignore))edge.position=new Vector3(p.x,hit.point.y+.07f,p.z);}
                return;
            }
            var root=new GameObject("Garage service area");SceneManager.MoveGameObjectToScene(root,scene);root.transform.position=Vector3.up*center.y;Undo.RegisterCreatedObjectUndo(root,"Add garage service marker");
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(1,.65f,.13f));string path=Root+"/Garage marker.mat";var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old==null)AssetDatabase.CreateAsset(material,path);else{UnityEngine.Object.DestroyImmediate(material);material=old;}
            foreach(float x in new[]{-5f,5f}){var line=GameObject.CreatePrimitive(PrimitiveType.Cube);line.name="Service bay edge";line.transform.SetParent(root.transform,false);line.transform.position=center+new Vector3(x,.06f,0);line.transform.localScale=new Vector3(.13f,.05f,9);UnityEngine.Object.DestroyImmediate(line.GetComponent<Collider>());line.GetComponent<Renderer>().sharedMaterial=material;}
            var sign=new GameObject("Garage prompt",typeof(TMPro.TextMeshPro));sign.transform.SetParent(root.transform,false);sign.transform.position=center+new Vector3(0,3,4);var text=sign.GetComponent<TMPro.TextMeshPro>();text.text="GARAGE";text.fontSize=7;text.alignment=TMPro.TextAlignmentOptions.Center;text.color=new Color(1,.85f,.5f);text.rectTransform.sizeDelta=new Vector2(12,2);
            AssetDatabase.SaveAssets();
        }
    }
}

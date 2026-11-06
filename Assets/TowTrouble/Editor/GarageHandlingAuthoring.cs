using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace TowTrouble.Editor
{
    public static class GarageHandlingAuthoring
    {
        [MenuItem("Tow Trouble/Garage/Refresh Wheel and Body Characteristics")]
        public static void Refresh()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode only");
            var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>(GarageAuthoring.CatalogPath);
            Undo.RecordObject(catalog,"Wheel and body handling profiles");Apply(catalog.Parts);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
            Directory.CreateDirectory("Docs/Validation/GarageHandling");
            File.WriteAllText("Docs/Validation/GarageHandling/Profiles.json",Newtonsoft.Json.JsonConvert.SerializeObject(catalog.Parts.Where(p=>p.Slot==GarageSlot.Wheels||p.Slot==GarageSlot.Assembly).Select(p=>new{p.Id,p.Name,slot=p.Slot.ToString(),tire=p.TireKind.ToString(),p.Price,p.DryGrip,p.MudGrip,p.MudResistance,p.SandGrip,p.SnowGrip,p.AddedMass,p.SpeedMultiplier,p.BodyMass,p.BodyDrive,p.BodySpeed,p.Description,p.EnglishDescription}),Newtonsoft.Json.Formatting.Indented));
        }
        public static void Apply(IEnumerable<GaragePart> parts)
        {
            foreach(var p in parts)
            {
                if(p.Slot==GarageSlot.Wheels)Wheel(p);
                else if(p.Slot==GarageSlot.Assembly)Body(p);
                p.Price=RecoveryCareerBalance.Price(p);
            }
        }
        private static void Tire(GaragePart p,GarageTireKind role,float dry,float mud,float sand,float snow,float resistance=1,float mass=0,float speed=1)
        {p.TireKind=role;p.DryGrip=dry;p.MudGrip=mud;p.SandGrip=sand;p.SnowGrip=snow;p.MudResistance=resistance;p.AddedMass=mass;p.SpeedMultiplier=speed;p.DriveMultiplier=1;}
        private static void Wheel(GaragePart p)
        {
            var name=p.Prefabs!=null&&p.Prefabs.Length>0&&p.Prefabs[0]!=null?p.Prefabs[0].name.ToLowerInvariant():p.Name.ToLowerInvariant();
            switch(p.Id)
            {
                case "stock_Wheels":Tire(p,GarageTireKind.Universal,1,1,1,1);break;
                case "tires_allterrain":Tire(p,GarageTireKind.AllTerrain,1,1.1f,1,1);break;
                case "tires_mud":Tire(p,GarageTireKind.Mud,1,1.3f,1,1,.85f);break;
                case "tires_sand":Tire(p,GarageTireKind.Sand,1,.9f,1.45f,1);break;
                case "tires_winter":Tire(p,GarageTireKind.Winter,1,.85f,1,1.65f);break;
                case "tires_winter_pro":Tire(p,GarageTireKind.Alpine,1,.80f,1,2.35f,1,35);break;
                default:
                    if(name.Contains("stud")||name.Contains("spike"))Tire(p,GarageTireKind.Winter,.97f,.90f,.90f,1.4f,1,20);
                    else if(name.Contains("monster"))Tire(p,GarageTireKind.AllTerrain,.96f,1.16f,1.12f,1.12f,.92f,55,.94f);
                    else if(name.Contains("mud")||name.Contains("tractor"))Tire(p,GarageTireKind.Mud,.94f,1.18f,.95f,1.05f,.94f,24,.98f);
                    else if(name.Contains("gt")||name.Contains("lemans")||name.Contains("lowrider")||name.Contains("futura")||name.Contains("modern"))Tire(p,GarageTireKind.Road,1.08f,.88f,.90f,.80f,1.08f,-12,1.02f);
                    else if(name.Contains("fat")||name.Contains("wide"))Tire(p,GarageTireKind.Sand,.98f,.96f,1.20f,.92f,1,18);
                    else if(name.Contains("bug")||name.Contains("offroad")||name.Contains("geep"))Tire(p,GarageTireKind.AllTerrain,1,1.08f,1.05f,1.08f,.98f,12);
                    else if(name.Contains("lorry")||name.Contains("armor"))Tire(p,GarageTireKind.AllTerrain,1,1.05f,1,1,1,35,.98f);
                    else Tire(p,GarageTireKind.Universal,1,1,1,1);
                    break;
            }
            string[] ro={"","Universale","De stradă","Mixte / off-road","De noroi","Late de nisip","Cu crampoane","Alpine ranforsate"};
            string[] en={"","All-round","Road","All-terrain","Mud","Wide sand","Studded winter","Heavy alpine"};
            // All four surfaces and full-set mass are explicit; paint/rim variants may share a profile.
            p.Description=ro[(int)p.TireKind]+" · set de 4. Drum "+Pct(p.DryGrip)+", noroi "+Pct(p.MudGrip)+", nisip "+Pct(p.SandGrip)+", zăpadă "+Pct(p.SnowGrip)+". Masă "+p.AddedMass.ToString("+0;-0;0")+" kg.";
            p.EnglishDescription=en[(int)p.TireKind]+" · four-wheel set. Road "+Pct(p.DryGrip)+", mud "+Pct(p.MudGrip)+", sand "+Pct(p.SandGrip)+", snow "+Pct(p.SnowGrip)+". Mass "+p.AddedMass.ToString("+0;-0;0")+" kg.";
            if(!Mathf.Approximately(p.MudResistance,1)){p.Description+=" Rezistență noroi "+Pct(p.MudResistance)+".";p.EnglishDescription+=" Mud resistance "+Pct(p.MudResistance)+".";}
            if(!Mathf.Approximately(p.SpeedMultiplier,1)){p.Description+=" Viteză "+Pct(p.SpeedMultiplier)+".";p.EnglishDescription+=" Speed "+Pct(p.SpeedMultiplier)+".";}

        }
        private static string Pct(float value)=>((value-1)*100).ToString("+0;-0;0")+"%";
        public static void ApplyBodies(IEnumerable<GaragePart> parts)
        { foreach(var p in parts)if(p.Slot==GarageSlot.Assembly)Body(p); }
        [MenuItem("Tow Trouble/Garage/Rebalance Body Prices and Speeds")]
        public static void RefreshBodies()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode only");
            var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>(GarageAuthoring.CatalogPath);
            Undo.RecordObject(catalog,"Body economy and speed balance");
            ApplyBodies(catalog.Parts);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
        }
        private static void Body(GaragePart p)
        {
            p.AddedMass=0;p.DriveMultiplier=1;p.SpeedMultiplier=1;
            if(p.Id=="stock_Assembly"){p.Price=0;p.BodyMass=p.BodyDrive=p.BodySpeed=0;p.Description="Șasiul original al mașinii tale. Masa și forța depind de modelul ales.";p.EnglishDescription="Your vehicle's original chassis. Mass and pulling force follow the selected model.";return;}
            string n=(p.Prefabs!=null&&p.Prefabs.Length>0&&p.Prefabs[0]!=null?p.Prefabs[0].name:p.Name).ToLowerInvariant();
            float mass,drive,speed;string typeRO,typeEN;
            if(n.Contains("pickupster")){mass=1840;drive=4000;speed=58/3.6f;typeRO="Utilitară";typeEN="Utility";}
            else if(n.Contains("lorry")){mass=1980;drive=4100;speed=54/3.6f;typeRO="Grea";typeEN="Heavy";}
            else if(n.Contains("villager")){mass=1750;drive=3900;speed=60/3.6f;typeRO="Utilitară";typeEN="Utility";}
            else if(n.Contains("goblin")){mass=1550;drive=3500;speed=66/3.6f;typeRO="Off-road";typeEN="Off-road";}
            else if(n.Contains("geep")){mass=1450;drive=3350;speed=64/3.6f;typeRO="Off-road";typeEN="Off-road";}
            else if(n.Contains("bucket")){mass=1020;drive=2600;speed=76/3.6f;typeRO="Ușoară";typeEN="Light";}
            else if(n.Contains("bulldog")){mass=1150;drive=2800;speed=72/3.6f;typeRO="Compactă";typeEN="Compact";}
            else if(n.Contains("speedster")){mass=1100;drive=2850;speed=78/3.6f;typeRO="Sport";typeEN="Sport";}
            else if(n.Contains("electron")){mass=1210;drive=3000;speed=76/3.6f;typeRO="Ușoară";typeEN="Light";}
            else if(n.Contains("prince")){mass=1440;drive=3300;speed=70/3.6f;typeRO="Echilibrată";typeEN="Balanced";}
            else if(n.Contains("rumpler")){mass=1380;drive=3150;speed=68/3.6f;typeRO="Echilibrată";typeEN="Balanced";}
            else if(n.Contains("spartan")){mass=1620;drive=3600;speed=60/3.6f;typeRO="Robustă";typeEN="Heavy-duty";}
            else if(n.Contains("martian")){mass=1260;drive=3000;speed=76/3.6f;typeRO="Sport";typeEN="Sport";}
            else if(n.Contains("midjet")||n.Contains("midget")){mass=980;drive=2500;speed=74/3.6f;typeRO="Ușoară";typeEN="Light";}
            else if(n.Contains("taxi")){mass=1350;drive=3100;speed=68/3.6f;typeRO="Echilibrată";typeEN="Balanced";}
            else throw new InvalidOperationException("Review unknown body profile: "+p.Id+" / "+n);
            if(n.Contains("bigfoot")){mass+=130;drive*=1.02f;speed*=.90f;}
            if(n.Contains("buggy")){mass-=120;drive*=.96f;speed*=1.02f;}
            if(n.Contains("high")){mass+=55;speed*=.97f;}
            if(n.Contains("roadster")||n.Contains("cabrio")){mass-=70;drive*=.97f;speed*=1.06f;}
            if(n.Contains("low")){mass-=65;speed*=1.04f;}
            if(n.Contains("offroad")){mass+=90;drive*=1.04f;speed*=.94f;}
            if(n.Contains("tow")){mass+=200;drive*=1.06f;speed*=.90f;}
            if(n.Contains("lemans")||n.Contains("_gt")){mass+=40;drive*=1.06f;speed*=1.09f;}
            if(n.Contains("aero")){mass-=35;speed*=1.05f;}
            speed=Mathf.Min(Mathf.Round(speed*3.6f)/3.6f,RecoveryBodyBalance.MaximumSpeed);
            p.BodyMass=Mathf.Round(mass/10)*10;p.BodyDrive=Mathf.Round(drive/10)*10;p.BodySpeed=speed;
            p.Price=RecoveryBodyBalance.Price(p.BodyMass,p.BodyDrive,speed);
            p.Description=typeRO+": "+p.BodyMass.ToString("0")+" kg · "+(p.BodyDrive*4/1000).ToString("0.0")+" kN · "+(speed*3.6f).ToString("0")+" km/h. "+(mass>=1600?"Sprijin mai bun; mai grea la urcare.":mass<=1250?"Agilă; sprijin mai redus la troliu.":"Compromis între masă și tragere.");
            p.EnglishDescription=typeEN+": "+p.BodyMass.ToString("0")+" kg · "+(p.BodyDrive*4/1000).ToString("0.0")+" kN · "+(speed*3.6f).ToString("0")+" km/h. "+(mass>=1600?"More footing; heavier uphill.":mass<=1250?"Agile; less winch anchoring weight.":"Balances mass and pulling force.");
        }
    }
}

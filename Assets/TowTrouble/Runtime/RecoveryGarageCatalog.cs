using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    public enum GarageSlot { Assembly, Wheels, Bonnet, FrontBumper, RearBumper, Fenders, Roof, Intake, Exhaust, Chassis, Engine, Transmission, Suspension, Cable, Winch, Paint }
    public enum GarageTireKind { Unspecified, Universal, Road, AllTerrain, Mud, Sand, Winter, Alpine }
    [Serializable] public sealed class GaragePart
    {
        public string Id, Name, EnglishName, Description, EnglishDescription;
        public GarageSlot Slot;
        public int Price;
        public Texture2D Thumbnail;
        public Rect ThumbnailUV=new Rect(0,0,1,1);
        public GameObject[] Prefabs;
        public string[] Sockets, Families;
        public float DriveMultiplier=1, SpeedMultiplier=1, MudGrip=1, MudResistance=1, AddedMass, Lift, Track=1, CableStrength=RecoveryRules.BreakForce, WinchSpeed=RecoveryWinch.StockSpeed, WinchForce=RecoveryWinch.StockForce;
        public GarageTireKind TireKind;
        public float DryGrip=1, SandGrip=1, SnowGrip=1;
        public float BodyMass, BodyDrive, BodySpeed;
        public int PaintIndex=-1;
        public string Label(RecoveryProfile profile)=>profile.L(Name,EnglishName);
        public bool Fits(string family)=>Families==null||Families.Length==0||Array.IndexOf(Families,family)>=0;
    }
    [CreateAssetMenu(menuName="Tow Trouble/Garage catalog")]
    public sealed class RecoveryGarageCatalog : ScriptableObject
    {
        [SerializeField] private GaragePart[] parts=Array.Empty<GaragePart>();
        [SerializeField] private GameObject[] stockAssemblies;
        public Shader TireShader;
        public Texture MudTexture;
        private Dictionary<string,GaragePart> lookup;
        private readonly Dictionary<GameObject,HashSet<string>> socketCache=new Dictionary<GameObject,HashSet<string>>();
        public GaragePart[] Parts=>parts;
        public void Configure(GaragePart[] entries,GameObject[] stock)
        {
            foreach(var entry in entries)if(entry.Thumbnail==null)
            {var previous=Array.Find(parts,p=>p.Id==entry.Id);if(previous!=null){entry.Thumbnail=previous.Thumbnail;entry.ThumbnailUV=previous.ThumbnailUV;}}
            parts=entries;stockAssemblies=stock;lookup=null;socketCache.Clear();
        }
        public GaragePart Find(string id)
        {
            if(string.IsNullOrEmpty(id))return null;
            if(lookup==null){lookup=new Dictionary<string,GaragePart>(StringComparer.Ordinal);foreach(var p in parts)lookup[p.Id]=p;}
            return lookup.TryGetValue(id,out var value)?value:null;
        }
        public GaragePart Stock(GarageSlot slot){foreach(var p in parts)if(p.Slot==slot&&p.Price==0)return p;return null;}
        public GameObject Assembly(RecoveryBuild build)
        {
            var selected=Find(build.Item(GarageSlot.Assembly,this));
            return selected!=null&&selected.Prefabs!=null&&selected.Prefabs.Length>0?selected.Prefabs[0]:stockAssemblies[Mathf.Clamp(build.Model,0,3)];
        }
        public string Family(RecoveryBuild build){var a=Assembly(build);return a==null?"":a.GetComponent<RecoveryAssembly>().Family;}
        public bool Compatible(GaragePart part,RecoveryBuild build)
        {
            if(part==null||!part.Fits(Family(build)))return false;
            if(part.Sockets==null||part.Sockets.Length==0)return true;
            var assembly=Assembly(build);if(!socketCache.TryGetValue(assembly,out var sockets)){sockets=new HashSet<string>();foreach(var t in assembly.GetComponentsInChildren<Transform>(true))sockets.Add(t.name);socketCache[assembly]=sockets;}
            foreach(var socket in part.Sockets)if(!sockets.Contains(socket))return false;return true;
        }
    }
    [Serializable] public sealed class RecoveryBuild
    {
        public int Model;
        public string[] Items=Array.Empty<string>();
        public RecoveryBuild Copy()=>new RecoveryBuild{Model=Model,Items=Items==null?Array.Empty<string>():(string[])Items.Clone()};
        public string Item(GarageSlot slot,RecoveryGarageCatalog catalog)
        {if(Items!=null)foreach(var id in Items){var p=catalog.Find(id);if(p!=null&&p.Slot==slot)return id;}return catalog.Stock(slot)?.Id;}
        public void Set(GaragePart part,RecoveryGarageCatalog catalog)
        {
            var ids=new List<string>();if(Items!=null)foreach(var id in Items){var p=catalog.Find(id);if(p!=null&&p.Slot!=part.Slot)ids.Add(id);}ids.Add(part.Id);Items=ids.ToArray();
        }
        public void Normalize(RecoveryGarageCatalog catalog,Predicate<string> owned=null)
        {
            Model=Mathf.Clamp(Model,0,3);var selected=new Dictionary<GarageSlot,GaragePart>();
            if(Items!=null)foreach(var id in Items){var p=catalog.Find(id);if(p!=null&&(p.Price==0||owned==null||owned(id)))selected[p.Slot]=p;}
            Items=Array.Empty<string>();
            if(selected.TryGetValue(GarageSlot.Assembly,out var assembly))Set(assembly,catalog);
            foreach(GarageSlot slot in Enum.GetValues(typeof(GarageSlot)))
            {
                if(slot==GarageSlot.Assembly)continue;
                var part=selected.TryGetValue(slot,out var p)&&catalog.Compatible(p,this)?p:catalog.Stock(slot);
                if(part!=null)Set(part,catalog);
            }
        }
    }
    [Serializable] public sealed class GaragePreset { public string Name; public RecoveryBuild Build; public string Label; }
    public struct GaragePerformance
    {
        public float Mass, Drive, Speed, DryGrip, MudGrip, MudResistance, SandGrip, SnowGrip, Lift, Track, Cable, Winch, WinchForce;
        public static GaragePerformance Calculate(RecoveryBuild build,RecoveryGarageCatalog catalog)
        {
            int model=Mathf.Clamp(build.Model,0,3);
            var value=new GaragePerformance{Mass=RecoveryVehicle.Masses[model],Drive=RecoveryVehicle.PullForces[model],Speed=RecoveryBodyBalance.StockSpeed(model),DryGrip=1,MudGrip=1,MudResistance=1,SandGrip=1,SnowGrip=1,Track=1,Cable=RecoveryRules.BreakForce,Winch=RecoveryWinch.StockSpeed,WinchForce=RecoveryWinch.StockForce};
            var body=catalog.Find(build.Item(GarageSlot.Assembly,catalog));
            if(body!=null&&body.BodyMass>0){value.Mass=body.BodyMass;value.Drive=body.BodyDrive>0?body.BodyDrive:value.Drive;value.Speed=body.BodySpeed>0?body.BodySpeed:value.Speed;}
            if(build.Items!=null)foreach(var id in build.Items)
            {
                var p=catalog.Find(id);if(p==null)continue;
                // Older catalogues with no authored tire profile retain their specialist response.
                value.DryGrip*=p.DryGrip;
                value.SandGrip*=p.TireKind==GarageTireKind.Unspecified&&id=="tires_sand"?1.45f:p.SandGrip;
                value.SnowGrip*=p.TireKind==GarageTireKind.Unspecified&&id=="tires_winter_pro"?2.35f:p.TireKind==GarageTireKind.Unspecified&&id=="tires_winter"?1.65f:p.SnowGrip;
                value.Mass+=p.AddedMass;value.Drive*=p.DriveMultiplier;value.Speed*=p.SpeedMultiplier;
                value.MudGrip*=p.MudGrip;value.MudResistance*=p.MudResistance;value.Lift+=p.Lift;value.Track*=p.Track;
                if(p.Slot==GarageSlot.Cable)value.Cable=p.CableStrength;if(p.Slot==GarageSlot.Winch){value.Winch=p.WinchSpeed;value.WinchForce=p.WinchForce;}
            }
            value.Speed=Mathf.Min(value.Speed,RecoveryBodyBalance.MaximumSpeed);
            value.Mass=Mathf.Clamp(value.Mass,900,2400);value.Track=Mathf.Clamp(value.Track,.9f,1.15f);value.Lift=Mathf.Clamp(value.Lift,0,.22f);return value;
        }
    }
}

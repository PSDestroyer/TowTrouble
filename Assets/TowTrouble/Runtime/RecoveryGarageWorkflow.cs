using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowTrouble
{
    public enum GarageItemState { Available, Owned, Equipped }

    // Pure preparation rules; profile persistence and network commit stay in DemoSession.
    public static class RecoveryGarageWorkflow
    {
        public static readonly string[] SetupNames = { "General", "Mud", "Snow", "Sand", "Forest", "Mountain", "Road" };
        public static GarageSlot[] Slots(int section) => section == 0
            ? new[] { GarageSlot.Bonnet, GarageSlot.FrontBumper, GarageSlot.RearBumper, GarageSlot.Fenders, GarageSlot.Roof, GarageSlot.Intake, GarageSlot.Exhaust, GarageSlot.Paint }
            : section == 1 ? new[] { GarageSlot.Assembly, GarageSlot.Wheels, GarageSlot.Engine, GarageSlot.Transmission, GarageSlot.Suspension, GarageSlot.Chassis }
            : new[] { GarageSlot.Cable, GarageSlot.Winch };
        public static int Section(GarageSlot slot) => slot == GarageSlot.Cable || slot == GarageSlot.Winch ? 2
            : slot == GarageSlot.Assembly || slot == GarageSlot.Wheels || slot == GarageSlot.Engine || slot == GarageSlot.Transmission || slot == GarageSlot.Suspension || slot == GarageSlot.Chassis ? 1 : 0;
        public static int GridIndex(int current,int count,int x,int y)
        {
            if(current<0||count<=0)return -1;
            if(y<0)return current>=3?current-3:current;
            if(y>0)return current/3<(count-1)/3?Mathf.Min(current+3,count-1):current;
            if(x<0&&current%3>0)return current-1;
            if(x>0&&current%3<2&&current+1<count)return current+1;
            return current;
        }
        public static GarageItemState State(RecoveryProfile profile, GaragePart part, RecoveryGarageCatalog catalog)
        {
            if (profile.Build(profile.vehicle).Item(part.Slot, catalog) == part.Id) return GarageItemState.Equipped;
            return part.Price == 0 || profile.Owns(part.Id) ? GarageItemState.Owned : GarageItemState.Available;
        }
        public static RecoveryBuild Preview(RecoveryBuild fitted, GaragePart part, RecoveryGarageCatalog catalog)
        {
            var build = fitted.Copy();
            if (part != null) build.Set(part, catalog);
            build.Normalize(catalog);
            return build;
        }
        public static List<GaragePart> ReplacedParts(RecoveryBuild fitted, RecoveryBuild preview, GaragePart selected, RecoveryGarageCatalog catalog)
        {
            var replaced = new List<GaragePart>();
            foreach (GarageSlot slot in Enum.GetValues(typeof(GarageSlot)))
            {
                if (selected != null && slot == selected.Slot) continue;
                string old = fitted.Item(slot, catalog), next = preview.Item(slot, catalog);
                var p = catalog.Find(old);
                if (old != next && p != null && p.Price > 0) replaced.Add(p);
            }
            return replaced;
        }
        public static bool TryPrepareFit(RecoveryProfile profile, GaragePart selected, RecoveryGarageCatalog catalog, out RecoveryBuild fitted)
        {
            fitted = null;
            if (selected == null || State(profile, selected, catalog) == GarageItemState.Equipped) return false;
            var current = profile.Build(profile.vehicle);
            if (!catalog.Compatible(selected, current)) return false;
            var next = Preview(current, selected, catalog);
            if (selected.Price > 0 && !profile.Owns(selected.Id) && !RecoveryEconomy.Buy(profile, selected)) return false;
            next.Normalize(catalog, profile.Owns);
            fitted = next;
            return true;
        }
        public static bool SameBuild(RecoveryBuild a, RecoveryBuild b, RecoveryGarageCatalog catalog)
        {
            if (a == null || b == null || a.Model != b.Model) return false;
            foreach (GarageSlot slot in Enum.GetValues(typeof(GarageSlot)))
                if (a.Item(slot, catalog) != b.Item(slot, catalog)) return false;
            return true;
        }
        public static bool AppearanceOnly(GaragePart part)
        {
            if (part == null || part.Slot == GarageSlot.Assembly || part.Slot == GarageSlot.Wheels || part.Slot == GarageSlot.Engine || part.Slot == GarageSlot.Transmission || part.Slot == GarageSlot.Suspension || part.Slot == GarageSlot.Cable || part.Slot == GarageSlot.Winch) return false;
            return Mathf.Approximately(part.DriveMultiplier,1) && Mathf.Approximately(part.SpeedMultiplier,1) && Mathf.Abs(part.AddedMass)<.01f && Mathf.Abs(part.Lift)<.001f && Mathf.Approximately(part.Track,1)
                && Mathf.Approximately(part.DryGrip,1) && Mathf.Approximately(part.MudGrip,1) && Mathf.Approximately(part.SnowGrip,1) && Mathf.Approximately(part.SandGrip,1) && Mathf.Approximately(part.MudResistance,1);
        }
        public static string DisplayName(GaragePart part, RecoveryProfile profile, RecoveryGarageCatalog catalog)
        {
            string name = RecoveryGarageOrdering.DisplayName(part, profile);
            if (part.Slot != GarageSlot.Assembly || !catalog.Parts.Any(p => p.Id != part.Id && p.Slot == part.Slot && RecoveryGarageOrdering.DisplayName(p, profile) == name)) return name;
            string original = part.EnglishName ?? part.Name ?? "";
            return name + (original.StartsWith("Historic ",StringComparison.Ordinal) ? " Classic" : original.StartsWith("Rod ",StringComparison.Ordinal) ? " Hot Rod" : "");
        }
        public static bool TireMatches(GaragePart part, int filter)
        {
            switch(filter)
            {
                case 1:return part.TireKind == GarageTireKind.Road;
                case 2:return part.TireKind == GarageTireKind.Universal || part.TireKind == GarageTireKind.AllTerrain || part.TireKind == GarageTireKind.Unspecified;
                case 3:return part.TireKind == GarageTireKind.Mud;
                case 4:return part.TireKind == GarageTireKind.Winter || part.TireKind == GarageTireKind.Alpine;
                case 5:return part.TireKind == GarageTireKind.Sand;
                default:return true;
            }
        }
        public static GaragePart Recommend(RecoveryContract contract, GaragePerformance fitted, RecoveryBuild build, RecoveryGarageCatalog catalog, RecoveryProfile profile=null)
        {
            if(contract == null)return null;
            Func<GaragePart,int> cost=p=>profile==null?p.Price:p.Price==0||profile.Owns(p.Id)?0:RecoveryEconomy.Price(profile,p);
            var candidates=catalog.Parts.Where(p=>p.Id!=build.Item(p.Slot,catalog)&&catalog.Compatible(p,build));
            var material=contract.Load?.Material ?? RecoveryLoadMaterial.Firm;
            float snowTarget=contract.Tier>=3?2.2f:1.25f;
            Func<GaragePart,bool> tire=null;
            if(material==RecoveryLoadMaterial.Snow && fitted.SnowGrip<snowTarget)tire=p=>p.SnowGrip>=snowTarget;
            else if((material==RecoveryLoadMaterial.Mud||material==RecoveryLoadMaterial.Silt)&&fitted.MudGrip<1.15f)tire=p=>p.MudGrip>=1.15f;
            else if(material==RecoveryLoadMaterial.Sand&&fitted.SandGrip<1.15f)tire=p=>p.SandGrip>=1.15f;
            if(tire!=null)return candidates.Where(p=>p.Slot==GarageSlot.Wheels&&tire(p)).OrderBy(cost).ThenBy(p=>p.Price).ThenBy(p=>p.Id,StringComparer.Ordinal).FirstOrDefault();
            float demand=Mathf.Max(contract.BreakoutResistance,contract.ExtractionProfile?.BreakawayForce??0);
            if(fitted.Cable<demand)return candidates.Where(p=>p.Slot==GarageSlot.Cable&&p.CableStrength>=demand*1.1f).OrderBy(cost).ThenBy(p=>p.Price).FirstOrDefault();
            if(fitted.Drive*4<demand)
            {
                Func<GaragePart,bool> improves=p=>GaragePerformance.Calculate(Preview(build,p,catalog),catalog).Drive>fitted.Drive+1;
                var upgrade=candidates.Where(p=>(p.Slot==GarageSlot.Engine||p.Slot==GarageSlot.Transmission)&&improves(p)).OrderBy(cost).ThenBy(p=>p.Price).FirstOrDefault();
                return upgrade??candidates.Where(p=>p.Slot==GarageSlot.Assembly&&improves(p)).OrderBy(cost).ThenBy(p=>p.Price).FirstOrDefault();
            }
            return null;
        }
        public static string SetupKey(int model,int index)=>"setup_"+model+"_"+index;
        public static string SetupLabel(GaragePreset preset,int index)=>!string.IsNullOrEmpty(preset?.Label)?preset.Label:new[]{"Forest","Mountain","Sand","Snow"}[Mathf.Clamp(index,0,3)];
        public static string InUseReason(RecoveryProfile profile, string id)
        {
            profile.EnsureGarage();
            for(int i=0;i<profile.builds.Length;i++)if(Array.IndexOf(profile.builds[i].Items??Array.Empty<string>(),id)>=0)return i==profile.vehicle?"Equipped on this vehicle":"Equipped on "+RecoveryVehicle.ModelNames[i];
            foreach(var preset in profile.presets)if(Array.IndexOf(preset.Build?.Items??Array.Empty<string>(),id)>=0)
            {
                string name=preset.Label;
                if(string.IsNullOrEmpty(name)){int index;var tail=preset.Name?.Substring(preset.Name.LastIndexOf('_')+1);name=int.TryParse(tail,out index)?SetupLabel(preset,index):"saved setup";}
                return "Used in "+name+" setup";
            }
            return null;
        }
    }
}

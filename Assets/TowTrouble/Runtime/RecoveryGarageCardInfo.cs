using UnityEngine;
namespace TowTrouble
{
    // Only real gameplay modifiers are advertised; cosmetic parts return no performance claim.
    public static class RecoveryGarageCardInfo
    {
        public static string Performance(GaragePart part,bool english,int model=0)
        {
            if(part==null)return "";
            if(part.Slot==GarageSlot.Assembly)
            {
                int index=Mathf.Clamp(model,0,3);float mass=part.BodyMass>0?part.BodyMass:RecoveryVehicle.Masses[index];float drive=part.BodyDrive>0?part.BodyDrive:RecoveryVehicle.PullForces[index];
                float speed=part.BodySpeed>0?part.BodySpeed:RecoveryBodyBalance.StockSpeed(index);
                return mass.ToString("0")+" kg · "+(drive*4/1000).ToString("0.0")+" kN\n"+(english?"Top speed: ":"Viteză: ")+(speed*3.6f).ToString("0")+" km/h";
            }
            if(part.Slot==GarageSlot.Wheels&&part.TireKind!=GarageTireKind.Unspecified)
            {
                switch(part.TireKind)
                {
                    case GarageTireKind.Road:return (english?"Road ":"Drum ")+Percent(part.DryGrip);
                    case GarageTireKind.AllTerrain:return (english?"Mud ":"Noroi ")+Percent(part.MudGrip);
                    case GarageTireKind.Mud:return (english?"Mud ":"Noroi ")+Percent(part.MudGrip);
                    case GarageTireKind.Sand:return (english?"Sand ":"Nisip ")+Percent(part.SandGrip);
                    case GarageTireKind.Winter:case GarageTireKind.Alpine:return (english?"Snow ":"Zăpadă ")+Percent(part.SnowGrip);
                    default:return english?"All-round · 1.00×":"Universale · 1.00×";
                }
            }
            if(part.Slot==GarageSlot.Cable)return (part.CableStrength/1000).ToString("0")+" kN";
            if(part.Slot==GarageSlot.Winch)return (part.WinchForce/1000).ToString("0")+" kN · "+part.WinchSpeed.ToString("0.##")+" m/s";
            if(part.Id=="tires_winter_pro")return english?"Snow grip +135%":"Zăpadă +135%";
            if(part.Id=="tires_winter")return english?"Snow grip +65%":"Zăpadă +65%";
            if(part.Id=="tires_sand")return english?"Sand grip +45%":"Nisip +45%";
            if(!Mathf.Approximately(part.DriveMultiplier,1))return Percent(part.DriveMultiplier)+(english?" pull":" forță");
            if(!Mathf.Approximately(part.MudGrip,1))return (english?"Mud grip ":"Noroi ")+Percent(part.MudGrip);
            if(part.Lift>0)return "+"+(part.Lift*100).ToString("0")+(english?" cm clearance":" cm gardă");
            if(!Mathf.Approximately(part.Track,1))return Percent(part.Track)+(english?" track width":" ecartament");
            if(Mathf.Abs(part.AddedMass)>.1f)return part.AddedMass.ToString("+0;-0")+" kg";
            return "";
        }
        private static string Percent(float multiplier)=>((multiplier-1)*100).ToString("+0;-0;0")+"%";
    }
}

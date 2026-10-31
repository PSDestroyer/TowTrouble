using UnityEngine;

namespace TowTrouble
{
    public sealed class RecoveryContract
    {
        public readonly int Id, Zone;
        public readonly string Title, Description, Terrain, Tip;
        public readonly Vector3 Target, Delivery;
        public readonly float Extraction, MudDrag;
        public int Reward=>RecoveryEconomy.Rewards[Mathf.Clamp(Id-1,0,4)];
        public string Difficulty(RecoveryProfile p)=>p.L(new[]{"Local","Standard","Specializat"}[Id-1],new[]{"Local","Standard","Specialist"}[Id-1]);
        public RecoveryContract(int id, int zone, string title, string description, string terrain, string tip, Vector3 target, Vector3 delivery, float extraction, float drag)
        { Id=id; Zone=zone; Title=title; Description=description; Terrain=terrain; Tip=tip; Target=target; Delivery=delivery; Extraction=extraction; MudDrag=drag; }

        // MD section 8: the first playable release contains contracts 01–03.
        public static readonly RecoveryContract[] All = {
            new RecoveryContract(1,0,"Primul cârlig","Mută sedanul de pe iarbă în zona sigură.","Iarbă / pietriș","Apropie cârligul din spate, conectează și accelerează lin.",new Vector3(2,.35f,18),new Vector3(0,0,43),26,0),
            new RecoveryContract(2,0,"La marginea drumului","Ridică sedanul din șanț și livrează-l pe drum.","Șanț / pietriș","Trage spre centrul drumului. Schimbă unghiul înainte să forțezi.",new Vector3(-5.5f,-.1f,18),new Vector3(0,0,75),29,350),
            new RecoveryContract(3,0,"Roți în noroi","Scoate mașina din făgașe și livreaz-o pe pietriș.","Noroi / pietriș","Accelerează treptat. Scurtează cablul pentru o tragere continuă.",new Vector3(-1,.12f,18),new Vector3(0,0,108),29,1700)
        };
        public string TitleFor(RecoveryProfile p)=>p.L(Title,new[]{"First hook","By the roadside","Wheels in the mud"}[Id-1]);
        public string DescriptionFor(RecoveryProfile p)=>p.L(Description,new[]{"Move the sedan from the grass into the safe zone.","Pull the sedan out of the ditch and bring it onto the road.","Recover the car from the ruts and deliver it onto gravel."}[Id-1]);
        public string TerrainFor(RecoveryProfile p)=>p.L(Terrain,new[]{"Grass / gravel","Ditch / gravel","Mud / gravel"}[Id-1]);
        public static RecoveryContract Get(int index) => All[Mathf.Clamp(index,0,All.Length-1)];
        public static readonly string[] Zones={"Valea Verde","Lacul Albastru","Canionul Roșu"};
    }
}

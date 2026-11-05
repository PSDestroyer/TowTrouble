using UnityEngine;

namespace TowTrouble
{
    public sealed class RecoveryContract
    {
        public readonly int Id, Zone;
        public readonly string Title, Description, Terrain, Tip;
        public readonly Vector3 Target, Delivery;
        public readonly float Extraction, MudDrag;
        public bool IsOpenWorld;
        public int Tier,ProgressId=-1;
        public int TargetModel,TargetPaint;
        public RecoveryLoadSettings Load;
        public float Heading,Pitch,Roll,BreakoutResistance;public Vector3 ExitDirection;
        public string EnglishTitle,EnglishDescription,DeliveryName,DeliveryEnglish;
        public RecoveryExtractionSettings ExtractionProfile;
        public RecoverySituationKind SituationKind;
        public bool IsExtracted(Vector3 position)=>IsOpenWorld?(Vector2.Distance(new Vector2(position.x,position.z),new Vector2(Target.x,Target.z))>=Extraction):position.z>=Extraction;
        public bool ContainsDelivery(Vector3 position)=>Vector2.Distance(new Vector2(position.x,position.z),new Vector2(Delivery.x,Delivery.z))<7&&Mathf.Abs(position.y-Delivery.y)<4;
        public int CompletionId=>ProgressId>=0?ProgressId:Id-1;
        public int Reward=>RecoveryEconomy.Rewards[Mathf.Clamp(IsOpenWorld?Tier:Id-1,0,4)];
        public string Difficulty(RecoveryProfile p)=>p.L(new[]{"Local","Standard","Specializat","Greu","Expert"}[Mathf.Clamp(IsOpenWorld?Tier:Id-1,0,4)],new[]{"Local","Standard","Specialist","Hard","Expert"}[Mathf.Clamp(IsOpenWorld?Tier:Id-1,0,4)]);
        public RecoveryContract(int id, int zone, string title, string description, string terrain, string tip, Vector3 target, Vector3 delivery, float extraction, float drag)
        { Id=id; Zone=zone; Title=title; Description=description; Terrain=terrain; Tip=tip; Target=target; Delivery=delivery; Extraction=extraction; MudDrag=drag; }

        // MD section 8: the first playable release contains contracts 01–03.
        public static readonly RecoveryContract[] All = {
            new RecoveryContract(1,0,"Primul cârlig","Mută sedanul de pe iarbă în zona sigură.","Iarbă / pietriș","Apropie cârligul din spate, conectează și accelerează lin.",new Vector3(2,.35f,18),new Vector3(0,0,43),26,0),
            new RecoveryContract(2,0,"La marginea drumului","Ridică sedanul din șanț și livrează-l pe drum.","Șanț / pietriș","Trage spre centrul drumului. Schimbă unghiul înainte să forțezi.",new Vector3(-5.5f,-.1f,18),new Vector3(0,0,75),29,350),
            new RecoveryContract(3,0,"Roți în noroi","Scoate mașina din făgașe și livreaz-o pe pietriș.","Noroi / pietriș","Accelerează treptat. Scurtează cablul pentru o tragere continuă.",new Vector3(-1,.12f,18),new Vector3(0,0,108),29,1700)
        };
        public string TitleFor(RecoveryProfile p)=>p.L(Title,IsOpenWorld?EnglishTitle:new[]{"First hook","By the roadside","Wheels in the mud"}[Id-1]);
        public string DescriptionFor(RecoveryProfile p)=>p.L(Description,IsOpenWorld?EnglishDescription:new[]{"Move the sedan from the grass into the safe zone.","Pull the sedan out of the ditch and bring it onto the road.","Recover the car from the ruts and deliver it onto gravel."}[Id-1]);
        public string TerrainFor(RecoveryProfile p)=>p.L(Terrain,IsOpenWorld?RecoveryMap.BiomeEnglish(Zone):new[]{"Grass / gravel","Ditch / gravel","Mud / gravel"}[Id-1]);
        public static RecoveryContract Get(int index) => All[Mathf.Clamp(index,0,All.Length-1)];
        public static readonly string[] Zones={"Valea Verde","Lacul Albastru","Canionul Roșu"};
    }
}

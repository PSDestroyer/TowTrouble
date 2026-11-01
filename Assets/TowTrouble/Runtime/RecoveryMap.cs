using System;
using UnityEngine;
namespace TowTrouble
{
    [Serializable] public sealed class RecoverySite
    {
        public string Id,Name,English,DeliveryName,DeliveryEnglish;
        public int Biome,Tier;
        public Vector3 Position,Delivery;
        public float Heading;
        public bool CrossRegion;
        public float Depth,Pitch,Roll,Resistance;public Vector3 ExitDirection;
        public RecoveryExtractionSettings Extraction = new RecoveryExtractionSettings();
        public string Situation,EnglishSituation;
    }
    [Serializable] public sealed class RecoveryDropoff
    {public string Id,Name,English;public Vector3 Position;}
    // Scene-owned locations. Tutorial IDs 0–2 remain unchanged; open-world progress uses 3–26.
    public sealed class RecoveryMap : MonoBehaviour
    {
        [SerializeField] private RecoverySite[] sites=Array.Empty<RecoverySite>();
        [SerializeField] private RecoveryDropoff[] deliveries=Array.Empty<RecoveryDropoff>();
        [SerializeField] private Vector3[] roadCheckpoints=Array.Empty<Vector3>();
        [SerializeField] private Texture2D mapImage;
        private RecoveryContract[] contracts;
        public RecoverySite[] Sites=>sites;
        public RecoveryDropoff[] Deliveries=>deliveries;
        public Texture2D Image=>mapImage;
        public RecoveryContract[] Contracts
        {
            get
            {
                if(contracts==null){contracts=new RecoveryContract[sites.Length];for(int i=0;i<sites.Length;i++)
                {var s=sites[i];contracts[i]=new RecoveryContract(i+1,s.Biome,s.Name,(s.Situation??"")+" Recuperează mașina și livreaz-o la "+s.DeliveryName+".",BiomeName(s.Biome),"Alege un unghi bun și trage progresiv.",s.Position-Vector3.up*s.Depth,s.Delivery,8+s.Tier,0)
                {IsOpenWorld=true,ProgressId=3+i,Tier=s.Tier,EnglishTitle=s.English,EnglishDescription=(s.EnglishSituation??"")+" Recover the car and deliver it to "+s.DeliveryEnglish+".",DeliveryName=s.DeliveryName,DeliveryEnglish=s.DeliveryEnglish,Heading=s.Heading,Pitch=s.Pitch,Roll=s.Roll,BreakoutResistance=s.Resistance,ExitDirection=s.ExitDirection,ExtractionProfile=s.Extraction};}}
                return contracts;
            }
        }
        public void Configure(RecoverySite[] values,RecoveryDropoff[] bays,Vector3[] roads){sites=values;deliveries=bays;roadCheckpoints=roads;contracts=null;}
        public void SetImage(Texture2D value){mapImage=value;}
        public Vector3 NearestRoad(Vector3 position)
        {var best=position;float distance=float.MaxValue;foreach(var p in roadCheckpoints){float d=(p-position).sqrMagnitude;if(d<distance){distance=d;best=p;}}return best;}
        public static string BiomeName(int id)=>new[]{"Pădure / noroi","Lac / mâl","Munte / stâncă","Zăpadă","Plajă / nisip","Deșert / canion"}[Mathf.Clamp(id,0,5)];
        public static string BiomeEnglish(int id)=>new[]{"Forest / mud","Lake / silt","Mountain / rock","Snow","Beach / sand","Desert / canyon"}[Mathf.Clamp(id,0,5)];
        public static string Preparation(int id,bool english)=>english?new[]{"Mud tires and ground clearance.","Grip, winch and firm ground.","Low range, suspension and a strong cable.","Winter tires, low range and stability.","Wide sand tires and moderate weight.","Sand tires, suspension and a strong cable."}[Mathf.Clamp(id,0,5)]:new[]{"Anvelope de noroi și gardă la sol.","Tracțiune, troliu și sprijin pe teren tare.","Reductor, suspensie și cablu rezistent.","Anvelope de iarnă, reductor și stabilitate.","Anvelope late și masă moderată.","Anvelope de nisip, suspensie și cablu rezistent."}[Mathf.Clamp(id,0,5)];
    }
}

using System.Collections.Generic;
using DistantLands.Cozy;
using DistantLands.Cozy.Data;
using UnityEngine;

namespace TowTrouble
{
    [DefaultExecutionOrder(-100)]
    public sealed class RecoveryCozyEnvironment : MonoBehaviour
    {
        [SerializeField] private RecoveryWorld world;
        [SerializeField] private DemoSession session;
        [SerializeField] private CozyWeather cozy;
        [SerializeField] private RecoveryGroundMap ground;
        [SerializeField] private WeatherProfile[] profiles;
        [SerializeField] private ParticleFX rainParticles,snowParticles;
        [SerializeField] private AudioFX rainAudio;
        [SerializeField] private Light skyFill;
        private readonly List<WeatherRelation> weather=new List<WeatherRelation>();
        private RecoveryVehicleEffects[] vehicles;
        private SatelliteProfile[] runtimeSatellites;
        private float snowWeight,nextRoofCheck,roofVisibility=1;
        private bool focused=true,appPaused;
        public float RainAmount {get;private set;}
        public float SnowAmount {get;private set;}
        public float NightAmount {get;private set;}
        public float DayPercentage {get;private set;}
        public float WetAmount {get;private set;}
        public float SnowRegionAmount=>snowWeight;
        public void Configure(RecoveryWorld owner,DemoSession local,CozyWeather sphere,RecoveryGroundMap map,WeatherProfile[] sky,ParticleFX rain,ParticleFX snow,AudioFX audio,Light fill)
        {world=owner;session=local;cozy=sphere;ground=map;profiles=sky;rainParticles=rain;snowParticles=snow;rainAudio=audio;skyFill=fill;}
        private void Awake()
        {
            var satellites=cozy.GetComponentInChildren<CozySatelliteModule>(true);
            if(satellites!=null)
            {
                runtimeSatellites=new SatelliteProfile[satellites.satellites.Length];
                for(int i=0;i<runtimeSatellites.Length;i++)
                {var p=Instantiate(satellites.satellites[i]);p.orbitRef=null;p.moonRef=null;p.lightRef=null;runtimeSatellites[i]=p;}
                satellites.satellites=runtimeSatellites;
            }
            vehicles=new[]{world.Drivers[0].GetComponent<RecoveryVehicleEffects>(),world.Drivers[1].GetComponent<RecoveryVehicleEffects>(),world.Casualty.GetComponent<RecoveryVehicleEffects>()};
            foreach(var p in profiles)weather.Add(new WeatherRelation{profile=p,weight=0,transitioning=false});
            weather[1].weight=1;
            var ecosystem=cozy.weatherModule.ecosystem;
            ecosystem.weatherSelectionMode=CozyEcosystem.EcosystemStyle.manual;ecosystem.removeProfilesWithNoWeight=false;
            ecosystem.weightedWeatherProfiles=weather;ecosystem.currentWeather=ecosystem.weatherChangeCheck=profiles[1];
        }
        private void Start(){rainParticles.InitializeEffect(cozy);snowParticles.InitializeEffect(cozy);rainAudio.InitializeEffect(cozy);}
        private void Update()
        {
            if(cozy==null||cozy.timeModule==null||cozy.weatherModule==null)return;
            bool live=session.Connected&&world.Phase!=RecoveryPhase.Lobby;
            float seconds=live?world.EnvironmentSeconds:0;
            DayPercentage=RecoveryWeatherRules.Day(seconds);NightAmount=RecoveryWeatherRules.Night(DayPercentage);
            cozy.timeModule.currentTime=DayPercentage;
            RecoveryWeatherRules.Weather(seconds,world.EnvironmentSeed,out int from,out int to,out float blend);
            // COZY3.6 expects a strongest profile strictly above 0.5. Keep the midpoint safe.
            if(Mathf.Abs(blend-.5f)<.0001f)blend=.5001f;
            for(int i=0;i<weather.Count;i++)weather[i].weight=0;
            weather[from].weight+=1-blend;weather[to].weight+=blend;
            cozy.weatherModule.ecosystem.currentWeather=cozy.weatherModule.ecosystem.weatherChangeCheck=profiles[blend<.5f?from:to];
            var local=world.Drivers[Mathf.Clamp(session.LocalSlot,0,1)];var position=local.transform.position;
            bool onSnow=ground!=null&&ground.Sample(position)==RecoveryGroundKind.Snow;
            float alpine=Mathf.SmoothStep(0,1,Mathf.InverseLerp(115,150,position.y))*Mathf.InverseLerp(180,240,position.z);
            float target=live?Mathf.Max(onSnow?1:0,alpine):0;
            snowWeight=Mathf.MoveTowards(snowWeight,target,Time.deltaTime/7);
            WetAmount=RecoveryWeatherRules.Wet(from,to,blend);
            RecoveryWeatherRules.Precipitation(WetAmount,snowWeight,out float rain,out float snow);
            if(Time.time>=nextRoofCheck)
            {
                nextRoofCheck=Time.time+.25f;
                // No precipitation under the workshop canopy; one inexpensive probe per quarter second.
                roofVisibility=Physics.Raycast(position+Vector3.up*3.5f,Vector3.up,12,~0,QueryTriggerInteraction.Ignore)?0:1;
            }
            RainAmount=live?rain*roofVisibility:0;SnowAmount=live?snow*roofVisibility:0;
            foreach(var effects in vehicles)if(effects!=null)effects.SetNightLighting(NightAmount);
            if(session.Audio!=null)session.Audio.SetEnvironment(NightAmount,RainAmount);
        }
        private void LateUpdate()
        {
            if(cozy==null)return;
            rainParticles.PlayEffect(RainAmount);snowParticles.PlayEffect(SnowAmount);
            float gain=focused&&!appPaused&&session.Profile!=null?session.Profile.ambience:0;
            rainAudio.PlayEffect(RainAmount*gain);
        }
        public void ApplyVisibility()
        {
            if(cozy==null)return;
            // Preserve the chill game's visibility while keeping headlights meaningful.
            RenderSettings.ambientSkyColor=Floor(RenderSettings.ambientSkyColor*.75f,Color.Lerp(new Color(.08f,.10f,.14f),new Color(.32f,.38f,.50f),NightAmount));
            RenderSettings.ambientEquatorColor=Floor(RenderSettings.ambientEquatorColor*.75f,Color.Lerp(new Color(.05f,.06f,.085f),new Color(.24f,.29f,.39f),NightAmount));
            RenderSettings.ambientGroundColor=Floor(RenderSettings.ambientGroundColor*.75f,Color.Lerp(new Color(.025f,.03f,.045f),new Color(.12f,.16f,.24f),NightAmount));
            RenderSettings.fogDensity=Mathf.Min(RenderSettings.fogDensity,.0012f);
            if(skyFill!=null){skyFill.intensity=Mathf.Lerp(.20f,.14f,NightAmount);skyFill.color=Color.Lerp(new Color(1,.96f,.88f),new Color(.5f,.65f,1),NightAmount);}
        }
        private static Color Floor(Color value,Color minimum)=>new Color(Mathf.Max(value.r,minimum.r),Mathf.Max(value.g,minimum.g),Mathf.Max(value.b,minimum.b),1);
        private void OnApplicationFocus(bool value){focused=value;}
        private void OnApplicationPause(bool value){appPaused=value;}
        private void OnDisable()
        {
            if(!Application.isPlaying)return;
            rainParticles?.PlayEffect(0);snowParticles?.PlayEffect(0);rainAudio?.PlayEffect(0);
            if(vehicles!=null)foreach(var effect in vehicles)if(effect!=null)effect.SetNightLighting(0);
            if(session!=null&&session.Audio!=null)session.Audio.SetEnvironment(0,0);
        }
        private void OnDestroy()
        {if(runtimeSatellites!=null)foreach(var p in runtimeSatellites)if(p!=null){if(Application.isPlaying)Destroy(p);else DestroyImmediate(p);}}
    }
}

using INab.BetterFog.Core;
using INab.BetterFog.URP;
using DistantLands.Cozy;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TowTrouble
{
    // Uses the vendor's predefined assembly, without introducing a dependency
    // from the gameplay assembly onto third-party rendering code.
    [DefaultExecutionOrder(220),DisallowMultipleComponent]
    public sealed class RecoveryAtmospherePresentation : MonoBehaviour
    {
        [SerializeField] private RecoveryCozyEnvironment environment;
        [SerializeField] private CozyWeather sky;
        [SerializeField] private DemoSession session;
        [SerializeField] private Volume volume;
        [SerializeField] private Light fill;
        [Header("Color grading")]
        [Tooltip("Off: Saturation and Contrast are controlled directly in the scene Global Volume. On: use the automatic day/night grading instead.")]
        [SerializeField] private bool automaticColorGrading;
        [Header("Distance haze — keep the foreground clear")]
        [SerializeField,Range(.0001f,.004f)] private float clearDensity=.00105f;
        [SerializeField,Range(.0001f,.004f)] private float rainDensity=.00145f;
        [SerializeField,Min(0)] private float clearDistance=110;
        private BetterFogVolumeComponent fog;
        private ColorAdjustments grade;
        private Bloom bloom;
        private Vignette vignette;
        private bool previousFog,bound,switchFogApplied;
        private FogMode previousFogMode;
        private Color previousFogColor;
        private float previousFogDensity;
        private float previousSunIntensity,previousFillIntensity;
        private Color previousFillColor;
        private bool ownsColorGrading;
        private float manualSaturation,manualContrast;
        private readonly RecoverySwitchColorGrade switchColorGrade=new RecoverySwitchColorGrade();
        public void Configure(RecoveryCozyEnvironment weather,CozyWeather sphere,DemoSession owner,Volume global,Light skyFill)
        {environment=weather;sky=sphere;session=owner;volume=global;fill=skyFill;}
        private void OnEnable()
        {
            if(volume==null)return;
            var profile=volume.profile;
            profile.TryGet(out fog);profile.TryGet(out grade);profile.TryGet(out bloom);profile.TryGet(out vignette);
            previousFog=RenderSettings.fog;
            previousFogMode=RenderSettings.fogMode;previousFogColor=RenderSettings.fogColor;previousFogDensity=RenderSettings.fogDensity;
            previousSunIntensity=sky.sunLight.intensity;
            if(fill!=null){previousFillIntensity=fill.intensity;previousFillColor=fill.color;}
            bound=true;
        }
        private void LateUpdate()
        {
            if(!bound||fog==null)return;
            float night=environment.NightAmount,wet=environment.WetAmount,snow=environment.SnowRegionAmount;
            float hour=environment.DayPercentage*24;
            float morning=Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.5f,6,hour))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(7,9,hour)));
            float daylight=Mathf.Min(1-night,Mathf.Clamp01(sky.sunlightColor.maxColorComponent));
            bool performance=session.Profile!=null&&session.Profile.quality==0;
            // COZY owns sky/weather; Switch uses native fog and PC keeps Better Fog.
            bool switchRendering=RecoveryPlatform.UsesSwitchRendering;
            RenderSettings.fog=switchRendering;
            fog._FogDensity.value=Mathf.Lerp(Mathf.Lerp(clearDensity,.0008f,night),rainDensity,wet)+snow*.00012f+morning*.00016f;
            fog._DistanceFogOffset.value=-Mathf.Lerp(clearDistance,70,night);
            fog._FogIntensity.value=Mathf.Lerp(.68f,.80f,wet);
            var dayFog=Color.Lerp(new Color(.48f,.68f,.86f),new Color(.40f,.53f,.67f),wet);
            fog._FogColor.value=Color.Lerp(new Color(.035f,.060f,.10f),dayFog,daylight);
            fog._Height.value=Mathf.Lerp(session.World.GaragePosition.y+3,session.World.Drivers[Mathf.Clamp(session.LocalSlot,0,1)].transform.position.y+2,snow);
            fog._HeightDensity.value=.05f+wet*.02f+morning*.035f;
            fog._UseNoise.value=!performance&&!switchRendering;
            fog._NoiseIntensity.value=.12f+wet*.10f;
            fog._SunIntensity.value=.10f*daylight*(1-wet);
            if(switchRendering)
            {
                // Native vertex/fragment fog uses the scene's existing Exp2 variants; no fullscreen noise/composite passes.
                switchFogApplied=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
                RenderSettings.fogDensity=Mathf.Lerp(.004f,.0055f,Mathf.Max(wet,snow));
                RenderSettings.fogColor=fog._FogColor.value;fog._FogIntensity.value=0;
            }
            switchColorGrade.Dispose();
            UpdateColorGrading(night);
            switchColorGrade.Apply(grade,night,wet);
            if(bloom!=null)bloom.intensity.value=switchRendering?0:Mathf.Lerp(.08f,.16f,night);
            if(vignette!=null)vignette.intensity.value=switchRendering?0:Mathf.Lerp(.065f,.09f,night);
            // Lift contact shadows rather than raising exposure and bleaching highlights.
            RenderSettings.ambientEquatorColor=Floor(RenderSettings.ambientEquatorColor,Color.Lerp(new Color(.30f,.33f,.38f),new Color(.28f,.32f,.41f),night));
            RenderSettings.ambientGroundColor=Floor(RenderSettings.ambientGroundColor,Color.Lerp(new Color(.15f,.16f,.18f),new Color(.14f,.16f,.21f),night));
            // Menu lighting owns these lights on both front-end screens.
            if(session.View!="Title"&&session.View!="Main")
            {
                sky.sunLight.intensity=Mathf.Lerp(.84f,.95f,night);
                if(fill!=null){fill.intensity=Mathf.Lerp(.21f,.18f,night);fill.color=Color.Lerp(new Color(1,.97f,.94f),new Color(.56f,.68f,1),night);}
            }
        }
        private static Color Floor(Color a,Color b)=>new Color(Mathf.Max(a.r,b.r),Mathf.Max(a.g,b.g),Mathf.Max(a.b,b.b),1);
        private void UpdateColorGrading(float night)
        {
            if(!automaticColorGrading){ReleaseColorGrading();return;}
            if(grade==null)return;
            if(!ownsColorGrading)
            {
                manualSaturation=grade.saturation.value;manualContrast=grade.contrast.value;
                ownsColorGrading=true;
            }
            grade.saturation.value=Mathf.Lerp(3,5,night);grade.contrast.value=Mathf.Lerp(7,2,night);
        }
        private void ReleaseColorGrading()
        {
            if(!ownsColorGrading)return;
            if(grade!=null){grade.saturation.value=manualSaturation;grade.contrast.value=manualContrast;}
            ownsColorGrading=false;
        }
        private void OnDisable()
        {
            if(!bound)return;bound=false;RenderSettings.fog=previousFog;
            if(switchFogApplied){RenderSettings.fogMode=previousFogMode;RenderSettings.fogColor=previousFogColor;RenderSettings.fogDensity=previousFogDensity;switchFogApplied=false;}
            switchColorGrade.Dispose();
            ReleaseColorGrading();
            var shared=volume!=null?volume.sharedProfile:null;
            if(shared!=null)
            {
                if(fog!=null&&shared.TryGet<BetterFogVolumeComponent>(out var original))
                    for(int i=0;i<fog.parameters.Count;i++)fog.parameters[i].SetValue(original.parameters[i]);
                if(bloom!=null&&shared.TryGet<Bloom>(out var originalBloom))bloom.intensity.value=originalBloom.intensity.value;
                if(vignette!=null&&shared.TryGet<Vignette>(out var originalVignette))vignette.intensity.value=originalVignette.intensity.value;
            }
            if(sky!=null&&sky.sunLight!=null)sky.sunLight.intensity=previousSunIntensity;
            if(fill!=null){fill.intensity=previousFillIntensity;fill.color=previousFillColor;}
        }
    }
}

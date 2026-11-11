using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryAudioDirector
    {
        private readonly AudioSource[] ignitionSources=new AudioSource[2];
        private readonly int[] ignitionModels={-1,-1};
        private readonly double[] ignitionAt={-100,-100};
        private readonly float[] ignitionDuration=new float[2],ignitionGains=new float[2];
        private float IgnitionEnvelope(int slot)
        {
            float elapsed=(float)(Time.realtimeSinceStartupAsDouble-ignitionAt[slot]);
            return Mathf.SmoothStep(0,1,Mathf.InverseLerp(Mathf.Max(0,ignitionDuration[slot]-.75f),Mathf.Max(.1f,ignitionDuration[slot]-.2f),elapsed));
        }
        private void EngineIgnition(RecoveryVehicle car,int slot,bool ignition,bool connected)
        {
            bool changed=ignitionModels[slot]>=0&&ignitionModels[slot]!=car.ModelIndex;
            bool audible=connected&&car.gameObject.activeInHierarchy&&Time.timeScale>.01f&&focused&&!appPaused;
            int action=RecoveryIgnitionPolicy.Edge(wasEngine[slot],ignition,changed,audible);
            wasEngine[slot]=ignition;ignitionModels[slot]=car.ModelIndex;
            var source=ignitionSources[slot];
            if(source!=null)
            {
                source.transform.position=car.transform.position+Vector3.up*.5f;
                source.volume=audible?BusGain(RecoveryAudioBus.Vehicles)*ignitionGains[slot]:0;
                if(changed||!connected||!car.gameObject.activeInHierarchy)source.Stop();
            }
            if(changed||!ignition)ignitionAt[slot]=-100;
            if(action==0)return;
            var profile=bank.IgnitionFor(car.ModelIndex);
            var clip=profile==null?null:action>0?profile.Start:profile.Stop;
            if(source!=null)source.Stop(); // Rapid changes never overlap starter and shutdown.
            if(clip==null){if(action>0)Play(RecoverySound.EngineStart,car.transform.position,slot,.7f);return;}
            if(BusGain(RecoveryAudioBus.Vehicles)<=0)return; // Do not replay on unmute.
            if(source==null)ignitionSources[slot]=source=Source("Driver "+slot+" ignition",RecoveryAudioBus.Vehicles,false,true);
            source.transform.position=car.transform.position+Vector3.up*.5f;source.clip=clip;source.pitch=1;source.priority=70;
            ignitionGains[slot]=RecoveryAudioPolicy.Gain(action>0?profile.StartGain:profile.StopGain);
            source.volume=BusGain(RecoveryAudioBus.Vehicles)*ignitionGains[slot];source.Play();
            if(action>0){ignitionAt[slot]=Time.realtimeSinceStartupAsDouble;ignitionDuration[slot]=clip.length;}
        }
    }
    public static class RecoveryIgnitionPolicy
    {
        // Focus/pause is presentation only; callers still record actual ignition state.
        public static int Edge(bool previous,bool current,bool modelChanged,bool audible)
            =>!audible?0:current&&(!previous||modelChanged)?1:previous&&!current&&!modelChanged?-1:0;
    }
}

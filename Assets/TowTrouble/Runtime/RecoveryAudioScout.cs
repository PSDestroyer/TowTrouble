using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryAudioDirector
    {
        private readonly int[] engineModels={-1,-1};
        private readonly float[] muscleDrive=new float[2],muscleEffort=new float[2];
        private bool VehicleEngine(RecoveryVehicle car,int slot,bool running)
        {
            bool scout=car.ModelIndex==0&&bank.HasScoutMuscle;
            var profile=scout?null:bank.EngineFor(car.ModelIndex);
            bool selected=scout||profile!=null;
            int model=selected?car.ModelIndex:-1;
            if(model!=engineModels[slot])
            {
                engineModels[slot]=model;muscleDrive[slot]=muscleEffort[slot]=0;
                for(int layer=0;layer<3;layer++)
                {
                    var source=engines[slot,layer];source.Stop();source.volume=0;source.pitch=1;
                    source.clip=scout?(layer==0?bank.ScoutMuscleIdle:layer==1?bank.ScoutMuscleLow:bank.ScoutMuscleHigh):profile!=null?profile.Clip(layer):
                        (layer==0?bank.EngineIdle:layer==1?bank.EngineDrive:bank.EngineLoad);
                }
            }
            if(!selected)return false;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            float target=running?RecoveryScoutEngineMix.Drive(car.Speed,car.ControlInput.y):0;
            muscleDrive[slot]=RecoveryScoutEngineMix.Smooth(muscleDrive[slot],target,dt,target>muscleDrive[slot]?.4f:.65f);
            float effort=running?RecoveryAudioPolicy.Gain(car.EngineEffort)*Mathf.Abs(car.ControlInput.y):0;
            muscleEffort[slot]=RecoveryScoutEngineMix.Smooth(muscleEffort[slot],effort,dt,.5f);
            var weights=RecoveryScoutEngineMix.Weights(muscleDrive[slot],muscleEffort[slot]);
            float gain=running?BusGain(RecoveryAudioBus.Vehicles)*RecoveryAudioPolicy.Gain(scout?bank.ScoutEngineGain:profile.Gain)*IgnitionEnvelope(slot):0;
            for(int layer=0;layer<3;layer++)
            {
                var source=engines[slot,layer];source.transform.position=car.transform.position+Vector3.up*.5f;
                // A steady idle and narrow pitch range retain the recorded engine character.
                float pitch=layer==0?1:layer==1?Mathf.Lerp(.92f,1.12f,muscleDrive[slot]):Mathf.Lerp(.9f,1.04f,muscleDrive[slot]);
                Loop(source,gain*weights[layer],pitch,.7f);
            }
            return true;
        }
    }
    public static class RecoveryScoutEngineMix
    {
        public static float Drive(float speed,float throttle)=>Mathf.Clamp01(Mathf.Abs(throttle)*.6f+Mathf.Clamp01(Mathf.Abs(speed)/22f)*.4f);
        public static float Smooth(float current,float target,float dt,float seconds)=>Mathf.Lerp(current,target,1-Mathf.Exp(-Mathf.Max(0,dt)/Mathf.Max(.01f,seconds)));
        public static Vector3 Weights(float drive,float effort)
        {
            drive=RecoveryAudioPolicy.Gain(drive);effort=RecoveryAudioPolicy.Gain(effort);
            float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(drive/.65f));
            float high=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,1,drive))*.16f;
            float volume=Mathf.Lerp(.27f,.37f,blend)*(1+.18f*effort);
            return new Vector3(1-blend,blend*(1-high),blend*high)*volume;
        }
    }
}

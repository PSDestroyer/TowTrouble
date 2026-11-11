using System;
using UnityEngine;
using UnityEngine.Audio;

namespace TowTrouble
{
    public enum RecoverySound { Navigate, Confirm, Back, Open, Close, Reject, Fit, Purchase, Accept, Complete, Attach, Detach, Snap, Release, Impact, Obstacle, Signal, Creak, WinchStart, WinchStop, EngineStart }
    public enum RecoveryAudioBus { Vehicles, Recovery, UI, Ambience, Music }
    public enum RecoveryAudioSurface { Dirt, Grass, Wood, Mud, Snow, Sand }
    [Serializable] public sealed class RecoverySoundSet
    {
        public RecoverySound Sound;
        public RecoveryAudioBus Bus;
        public AudioClip[] Clips=Array.Empty<AudioClip>();
        [Range(0,1)] public float Gain=.5f;
        public int Priority=100;
        public float Cooldown=.08f;
    }
    [Serializable] public sealed class RecoverySurfaceSounds
    { public RecoveryAudioSurface Surface; public AudioClip[] Clips=Array.Empty<AudioClip>(); [Range(0,1)] public float Gain=.2f; }
    [Serializable] public sealed class RecoveryIgnitionSounds
    {
        public int Model;
        public AudioClip Start,Stop;
        [Range(0,1)] public float StartGain=.16f,StopGain=.14f;
    }
    [Serializable] public sealed class RecoveryVehicleEngineSounds
    {
        public int Model;
        public AudioClip Idle,Low,High;
        [Range(0,1)] public float Gain=.68f;
        public bool Ready=>Idle!=null&&Low!=null&&High!=null;
        public AudioClip Clip(int layer)=>layer==0?Idle:layer==1?Low:High;
    }
    [CreateAssetMenu(menuName="Tow Trouble/Audio Bank")]
    public sealed class RecoveryAudioBank : ScriptableObject
    {
        public AudioMixerGroup Vehicles,Recovery,UI,Ambience,Music;
        public AudioClip EngineIdle,EngineDrive,EngineLoad,WinchMotor,Wind,MountainWind,Birds,Insects,Water,Garage;
        [Header("Scout classic engine (optional; other vehicles use the original bank)")]
        public AudioClip ScoutMuscleIdle,ScoutMuscleLow,ScoutMuscleHigh;
        [Tooltip("Independent level for the prepared Muscle clips; legacy engine boost does not apply.")]
        [Range(0,1)] public float ScoutEngineGain=.68f;
        public bool HasScoutMuscle=>ScoutMuscleIdle!=null&&ScoutMuscleLow!=null&&ScoutMuscleHigh!=null;
        public RecoveryIgnitionSounds[] Ignitions=Array.Empty<RecoveryIgnitionSounds>();
        public RecoveryIgnitionSounds IgnitionFor(int model)
        {if(Ignitions!=null)foreach(var sound in Ignitions)if(sound!=null&&sound.Model==model)return sound;return null;}
        public RecoveryVehicleEngineSounds[] VehicleEngines=Array.Empty<RecoveryVehicleEngineSounds>();
        public RecoveryVehicleEngineSounds EngineFor(int model)
        {
            if(VehicleEngines!=null)foreach(var engine in VehicleEngines)
                if(engine!=null&&engine.Model==model&&engine.Ready)return engine;
            return null;
        }
        [Header("Music and mix")]
        [Range(0,1)] public float MusicGain=.36f;
        [Range(0,1)] public float EngineGain=.85f;
        [Tooltip("Gain applied only to the three engine loops, in decibels.")]
        [Range(0,9)] public float EngineBoostDb=6f;
        [Min(.2f)] public float MusicCrossfade=4;
        [Min(0)] public float MenuMusicGap=3,ExploreMusicGap=16;
        public AudioClip MenuMusic;
        public AudioClip[] ExploreMusic=Array.Empty<AudioClip>();
        public RecoverySoundSet[] Sounds=Array.Empty<RecoverySoundSet>();
        public RecoverySurfaceSounds[] Surfaces=Array.Empty<RecoverySurfaceSounds>();
        public Vector3[] Waterfalls=Array.Empty<Vector3>();
        public AudioMixerGroup Group(RecoveryAudioBus bus)=>bus==RecoveryAudioBus.Vehicles?Vehicles:bus==RecoveryAudioBus.Recovery?Recovery:bus==RecoveryAudioBus.UI?UI:bus==RecoveryAudioBus.Ambience?Ambience:Music;
        public RecoverySoundSet Find(RecoverySound sound)=>Array.Find(Sounds,x=>x.Sound==sound);
        public RecoverySurfaceSounds Surface(RecoveryAudioSurface surface)=>Array.Find(Surfaces,x=>x.Surface==surface);
    }
    // Shared presentation rules: audio never influences traction, cable strength, rewards or gameplay RNG.
    public static class RecoveryAudioPolicy
    {
        public static float Gain(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Clamp01(value);
        public static bool MotorActive(WinchState state)=>state==WinchState.TakingUp||state==WinchState.Pulling||state==WinchState.Stalled||state==WinchState.PayingOut;
        public static float EnginePitch(float speed,float throttle,float slip,float mass)
            =>Mathf.Clamp((.78f+Mathf.Repeat(Mathf.Max(0,speed),7)/7*.48f+Gain(Mathf.Abs(throttle))*.22f+Gain(slip)*.25f)*Mathf.Lerp(1.1f,.8f,Mathf.InverseLerp(950,2200,mass)),.65f,1.65f);
        public static float EngineBoost(float decibels)=>Mathf.Pow(10,Mathf.Clamp(decibels,0,9)/20f);
        // x: idle, y: revs/drive, z: loaded combustion. Effort already includes
        // cable reaction, mud and tire slip from the vehicle simulation.
        public static Vector3 EngineLayers(float speed,float throttle,float effort)
        {
            throttle=Gain(Mathf.Abs(throttle));effort=Gain(effort);
            float load=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.2f,.85f,effort));
            return new Vector3(.18f*(1-.82f*throttle),
                .2f*Mathf.Max(throttle,.35f*Gain(speed/25)),.14f*load*throttle);
        }
        public static bool Changed(uint current,uint previous)=>unchecked((int)(current-previous))>0;
        public static float MusicDuck(bool menu,float tension,bool stalled)=>Mathf.Min(menu?.65f:1,Mathf.Lerp(1,.32f,Mathf.Max(Mathf.InverseLerp(.65f,.95f,tension),stalled?.8f:0)));
        public static float MusicEnvelope(float start,bool incoming,float blend)
        {float t=Mathf.Clamp01(blend);return incoming?Mathf.Sqrt(Mathf.Lerp(start*start,1,Mathf.Sin(t*Mathf.PI*.5f)*Mathf.Sin(t*Mathf.PI*.5f))):Mathf.Max(0,start*Mathf.Cos(t*Mathf.PI*.5f));}
    }
}

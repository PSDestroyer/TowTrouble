using System;
using UnityEngine;

namespace TowTrouble
{
    // One local presentation rig per session. Physics/network events are read, never authored here.
    [DefaultExecutionOrder(200)]
    public sealed partial class RecoveryAudioDirector : MonoBehaviour
    {
        private const int VoiceCount=12;
        private DemoSession session;
        private RecoveryAudioBank bank;
        private readonly AudioSource[] voices=new AudioSource[VoiceCount];
        private readonly RecoveryAudioBus[] voiceBuses=new RecoveryAudioBus[VoiceCount];
        private readonly float[] voiceGains=new float[VoiceCount];
        private readonly double[] cooldowns=new double[72];
        private readonly AudioSource[,] engines=new AudioSource[2,3];
        private readonly AudioSource[] motors=new AudioSource[2],music=new AudioSource[2];
        private readonly AudioSource[] legacyEngines=new AudioSource[2];
        private readonly bool[] legacyEnabled=new bool[2];
        private readonly RecoveryHapticEvents[] history=new RecoveryHapticEvents[2];
        private readonly bool[] historyReady=new bool[2],wasEngine=new bool[2];
        private readonly uint[] signalHistory=new uint[2];
        private readonly WinchState[] motorStates=new WinchState[2];
        private readonly float[] grainClock=new float[3];
        private readonly RecoveryAudioSurface[] surfaces=new RecoveryAudioSurface[3];
        private AudioSource wind,birds,insects,water,waterfall,garage;
        private AudioSource legacyCasualty;
        private bool casualtyEnabled;
        private AudioListener listener;
        private AudioListener[] oldListeners;
        private bool[] oldListenerEnabled;
        private string run,view;
        private RecoveryPhase phase;
        private bool seeded,obstacle,focused=true,appPaused,hadConnection;
        private int localSlot=-1,musicIndex;
        private readonly double[] surfaceAt=new double[3];
        private readonly bool[] grounded=new bool[3];
        private readonly double[] nextCreakAt=new double[2];
        private double nextWorkshopAt;
        private bool musicMenu,trackStarted;
        private int uiFrame=-1,uiPriority=999;
        private float musicBlend,musicDuck=1;
        private float environmentNight,environmentRain;
        public void SetEnvironment(float night,float rain){environmentNight=Mathf.Clamp01(night);environmentRain=Mathf.Clamp01(rain);}
        public int PlayingOneShots { get { int n=0;foreach(var s in voices)if(s!=null&&s.isPlaying)n++;return n; } }
        public RecoveryAudioBank Bank=>bank;
        public void Configure(DemoSession owner,RecoveryAudioBank value)
        {
            session=owner;bank=value;focused=Application.isFocused;
            if(bank==null){enabled=false;return;}
            for(int i=0;i<VoiceCount;i++)voices[i]=Source("One shot "+i,RecoveryAudioBus.Recovery,false,false);
            for(int i=0;i<2;i++)
            {
                var car=owner.World.Drivers[i];car.ExternalAudio=true;
                legacyEngines[i]=car.GetComponent<AudioSource>();
                if(legacyEngines[i]!=null){legacyEnabled[i]=legacyEngines[i].enabled;legacyEngines[i].Stop();legacyEngines[i].enabled=false;}
                for(int j=0;j<3;j++)engines[i,j]=Source("Driver "+i+" engine "+j,RecoveryAudioBus.Vehicles,true,true,j==0?bank.EngineIdle:j==1?bank.EngineDrive:bank.EngineLoad);
                motors[i]=Source("Driver "+i+" winch",RecoveryAudioBus.Recovery,true,true,bank.WinchMotor);
                music[i]=Source("Music "+i,RecoveryAudioBus.Music,false,false);music[i].priority=160;
            }
            legacyCasualty=owner.World.Casualty.GetComponent<AudioSource>();
            if(legacyCasualty!=null){casualtyEnabled=legacyCasualty.enabled;legacyCasualty.Stop();legacyCasualty.enabled=false;}
            owner.World.ExternalAudio=true;
            wind=Source("Wind",RecoveryAudioBus.Ambience,true,false,bank.Wind);
            birds=Source("Birds",RecoveryAudioBus.Ambience,true,false,bank.Birds);
            insects=Source("Insects",RecoveryAudioBus.Ambience,true,false,bank.Insects);
            water=Source("Shore",RecoveryAudioBus.Ambience,true,false,bank.Water);
            waterfall=Source("Waterfall",RecoveryAudioBus.Ambience,true,true,bank.Water);waterfall.maxDistance=100;waterfall.minDistance=14;
            garage=Source("Workshop",RecoveryAudioBus.Ambience,false,true,bank.Garage);garage.maxDistance=32;garage.minDistance=4;
            oldListeners=FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            oldListenerEnabled=new bool[oldListeners.Length];
            for(int i=0;i<oldListeners.Length;i++){oldListenerEnabled[i]=oldListeners[i].enabled;oldListeners[i].enabled=false;}
            var ear=new GameObject("Local vehicle audio listener");ear.transform.SetParent(transform,false);listener=ear.AddComponent<AudioListener>();
        }
        private AudioSource Source(string label,RecoveryAudioBus bus,bool loop,bool spatial,AudioClip clip=null)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);var s=go.AddComponent<AudioSource>();
            s.playOnAwake=false;s.loop=loop;s.clip=clip;s.volume=0;s.outputAudioMixerGroup=bank.Group(bus);
            s.spatialBlend=spatial?1:0;s.dopplerLevel=0;s.minDistance=4;s.maxDistance=55;s.rolloffMode=AudioRolloffMode.Logarithmic;s.priority=bus==RecoveryAudioBus.Ambience?200:90;
            return s;
        }
        private float BusGain(RecoveryAudioBus bus)
        {
            if(session?.Profile==null||!focused||appPaused)return 0;
            var p=session.Profile;
            return RecoveryAudioPolicy.Gain(bus==RecoveryAudioBus.Music?p.music:bus==RecoveryAudioBus.Ambience?p.ambience:bus==RecoveryAudioBus.UI?p.ui:p.effects);
        }
        public void Play(RecoverySound sound,Vector3 position=default,int slot=-1,float strength=1)
        {
            if(!isActiveAndEnabled||bank==null)return;
            var set=bank.Find(sound);if(set==null||set.Clips.Length==0)return;
            if(set.Bus==RecoveryAudioBus.UI)
            {
                if(uiFrame==Time.frameCount&&uiPriority<=set.Priority)return;
                if(uiFrame==Time.frameCount)for(int i=0;i<VoiceCount;i++)if(voiceBuses[i]==RecoveryAudioBus.UI&&voices[i].isPlaying)voices[i].Stop();
                uiFrame=Time.frameCount;uiPriority=set.Priority;
            }
            int key=(int)sound*3+Mathf.Clamp(slot,0,2);double now=Time.realtimeSinceStartupAsDouble;
            if(now<cooldowns[key])return;cooldowns[key]=now+set.Cooldown;
            var clip=set.Clips[(Time.frameCount+(slot+1)*7)%set.Clips.Length];
            Shot(clip,set.Bus,set.Gain*RecoveryAudioPolicy.Gain(strength),set.Priority,position,set.Bus!=RecoveryAudioBus.UI,1);
        }
        private void Shot(AudioClip clip,RecoveryAudioBus bus,float gain,int priority,Vector3 position,bool spatial,float pitch)
        {
            if(clip==null||gain<=0||BusGain(bus)<=0)return;
            int best=-1;for(int i=0;i<VoiceCount;i++)
            {
                if(!voices[i].isPlaying){best=i;break;}
                if(voices[i].priority>priority&&(best<0||voices[i].priority>voices[best].priority))best=i;
            }
            if(best<0)return;
            var s=voices[best];s.Stop();s.transform.position=position;s.clip=clip;s.outputAudioMixerGroup=bank.Group(bus);s.priority=priority;
            s.pitch=pitch;s.spatialBlend=spatial?1:0;s.ignoreListenerPause=bus==RecoveryAudioBus.UI;
            voiceBuses[best]=bus;voiceGains[best]=gain;s.volume=gain*BusGain(bus);s.Play();
        }
        private void Loop(AudioSource s,float target,float pitch=1,float fade=5)
        {
            if(s==null||s.clip==null)return;
            var bus=s.outputAudioMixerGroup==bank.Ambience?RecoveryAudioBus.Ambience:s.outputAudioMixerGroup==bank.Vehicles?RecoveryAudioBus.Vehicles:RecoveryAudioBus.Recovery;
            if(BusGain(bus)<=0){s.volume=0;if(s.isPlaying)s.Stop();return;}
            s.pitch=Mathf.MoveTowards(s.pitch,pitch,Time.unscaledDeltaTime*2);
            s.volume=Mathf.MoveTowards(s.volume,Mathf.Clamp01(target),Time.unscaledDeltaTime*fade);
            if(target>.001f&&s.volume>.001f&&!s.isPlaying)s.Play();
            else if(target<=0&&s.volume<=.001f&&s.isPlaying)s.Stop();
        }
        private void LateUpdate()
        {
            if(session==null||bank==null||session.World==null||session.Profile==null)return;
            var world=session.World;int slot=Mathf.Clamp(session.LocalSlot,0,1);var local=world.Drivers[slot];
            if(listener!=null){listener.transform.position=local.transform.position+Vector3.up;var camera=Camera.main;if(camera!=null)listener.transform.rotation=camera.transform.rotation;}
            for(int i=0;i<VoiceCount;i++)if(voices[i].isPlaying)voices[i].volume=voiceGains[i]*BusGain(voiceBuses[i])*(voiceBuses[i]!=RecoveryAudioBus.UI&&Time.timeScale==0?0:1);
            bool online=session.Connected&&session.LocalSlot>=0;
            bool driving=online&&world.Phase!=RecoveryPhase.Lobby&&world.Phase!=RecoveryPhase.Complete&&Time.timeScale>.01f&&focused&&!appPaused;
            bool baseline=!seeded||hadConnection!=online||localSlot!=slot||run!=world.RunId;
            ObserveEvents(world,online,driving,baseline);
            hadConnection=online;localSlot=slot;seeded=true;run=world.RunId;phase=world.Phase;obstacle=world.ClearingObstacle;
            for(int i=0;i<2;i++)Vehicle(world.Drivers[i],i,driving,world);
            if(driving&&world.Casualty!=null&&world.Casualty.gameObject.activeInHierarchy)Surface(world.Casualty,2);
            Ambience(world,local.transform.position,online);
            Music(world,online,slot);
        }
        private void ObserveEvents(RecoveryWorld world,bool online,bool driving,bool baseline)
        {
            bool ready=online&&world.HapticTelemetryReady;
            for(int i=0;i<2;i++)
            {
                var e=ready?world.HapticEvents(i):default;uint signal=ready?world.SignalSerial(i):0;var p=world.Drivers[i].RearHitch;
                if(ready&&!baseline&&historyReady[i])
                {
                    var old=history[i];
                    // Read both drivers' counters locally; no extra audio RPCs and no late-join replay.
                    if(driving)
                    {
                        if(RecoveryAudioPolicy.Changed(e.Broken,old.Broken))Play(RecoverySound.Snap,p,i);
                        else if(RecoveryAudioPolicy.Changed(e.Detached,old.Detached))Play(RecoverySound.Detach,p,i);
                        if(RecoveryAudioPolicy.Changed(e.Attached,old.Attached))Play(RecoverySound.Attach,p,i);
                        if(RecoveryAudioPolicy.Changed(e.Impact,old.Impact))Play(RecoverySound.Impact,p,i,Mathf.Lerp(.3f,1,e.ImpactStrength));
                        if(RecoveryAudioPolicy.Changed(signal,signalHistory[i])&&i!=session.LocalSlot)Play(RecoverySound.Signal,p,i);
                    }
                }
                history[i]=e;signalHistory[i]=signal;historyReady[i]=ready;
            }
            if(!baseline&&online)
            {
                if(phase==RecoveryPhase.Recover&&world.Phase==RecoveryPhase.Deliver)Play(RecoverySound.Release,world.Casualty.transform.position);
                if(phase!=RecoveryPhase.Complete&&world.Phase==RecoveryPhase.Complete)Play(RecoverySound.Complete);
                if(obstacle&&!world.ClearingObstacle&&world.HasContract)Play(RecoverySound.Obstacle,world.Casualty.transform.position);
            }
            if(view!=session.View)
            {
                if(view!=null&&session.View!="Gameplay"&&session.View!="Results")Play(RecoverySound.Open);
                else if(view!=null&&session.View=="Gameplay")Play(RecoverySound.Close);
                view=session.View;
            }
        }
        private void Vehicle(RecoveryVehicle car,int slot,bool driving,RecoveryWorld world)
        {
            bool ignition=session.Connected&&car.gameObject.activeInHierarchy&&car.EngineRunning&&world.Phase!=RecoveryPhase.Lobby&&world.Phase!=RecoveryPhase.Complete;
            bool running=driving&&ignition;
            if(running&&!wasEngine[slot])Play(RecoverySound.EngineStart,car.transform.position,slot,.7f);
            wasEngine[slot]=ignition;
            float throttle=Mathf.Abs(car.ControlInput.y),speed=car.Speed;
            var slip=car.WheelSlip;float average=(slip.x+slip.y+slip.z+slip.w)*.25f;
            // A hidden lobby preview may not have run Awake yet. Its audio is muted,
            // but pitch evaluation must still tolerate the uninitialized physics cache.
            float mass=car.Body!=null?car.Body.mass:RecoveryBodyBalance.StockMass(car.ModelIndex);
            float pitch=RecoveryAudioPolicy.EnginePitch(speed,throttle,average,mass);
            float gain=running?BusGain(RecoveryAudioBus.Vehicles)*bank.EngineGain*RecoveryAudioPolicy.EngineBoost(bank.EngineBoostDb):0;
            var layers=RecoveryAudioPolicy.EngineLayers(speed,throttle,car.EngineEffort);
            for(int layer=0;layer<3;layer++)
            {
                var source=engines[slot,layer];source.transform.position=car.transform.position+Vector3.up*.5f;
                float layerPitch=layer==0?Mathf.Lerp(.65f,.85f,Mathf.InverseLerp(2200,950,mass)):
                    layer==2?pitch*Mathf.Lerp(.82f,.70f,RecoveryAudioPolicy.Gain(car.EngineEffort)):pitch;
                Loop(source,gain*layers[layer],layerPitch,1.2f);
            }
            var frame=world.Winch(slot);bool motor=running&&world.Attached(slot)&&RecoveryAudioPolicy.MotorActive(frame.State);
            if(motor&& !RecoveryAudioPolicy.MotorActive(motorStates[slot]))Play(RecoverySound.WinchStart,car.RearHitch,slot,.65f);
            if(!motor&&RecoveryAudioPolicy.MotorActive(motorStates[slot])&&driving)Play(RecoverySound.WinchStop,car.RearHitch,slot,.65f);
            motorStates[slot]=motor?frame.State:WinchState.Disconnected;
            motors[slot].transform.position=car.RearHitch;
            float rate=frame.State==WinchState.Stalled?.55f:.6f+.5f*Mathf.Clamp01(Mathf.Abs(frame.Speed)/Mathf.Max(.1f,car.WinchSpeed));
            Loop(motors[slot],motor?BusGain(RecoveryAudioBus.Recovery)*(.15f+.07f*Mathf.Clamp01(frame.Load)):0,rate);
            if(running)Surface(car,slot);
            double now=Time.realtimeSinceStartupAsDouble;
            if(running&&world.Attached(slot)&&world.Tension(slot)>.72f&&now>=nextCreakAt[slot])
            {Play(RecoverySound.Creak,car.RearHitch,slot,Mathf.InverseLerp(.65f,1,world.Tension(slot)));nextCreakAt[slot]=now+Mathf.Lerp(1.6f,.48f,world.Tension(slot));}
        }
        private void Surface(RecoveryVehicle car,int index)
        {
            // Low-frequency contact queries, cached between grains; no allocations or per-wheel audio emitters.
            double now=Time.realtimeSinceStartupAsDouble;
            if(now>=surfaceAt[index])
            {
                surfaceAt[index]=now+.1;grounded[index]=car.TryGetWheelContact(0,out var hit);
                if(!grounded[index])return;
                var collider=hit.collider;var mud=collider.GetComponent<RecoveryMudTerrain>();var surface=collider.GetComponent<RecoveryGroundSurface>();
                surfaces[index]=mud!=null||car.MudFraction>.1f?RecoveryAudioSurface.Mud:surface!=null&&surface.KindAt(hit.point)==RecoveryGroundKind.Snow?RecoveryAudioSurface.Snow:
                    surface!=null&&surface.KindAt(hit.point)==RecoveryGroundKind.Sand?RecoveryAudioSurface.Sand:
                    collider.name.StartsWith("Bridge",StringComparison.Ordinal)?RecoveryAudioSurface.Wood:RecoveryAudioSurface.Dirt;
                if(surfaces[index]==RecoveryAudioSurface.Dirt&&session.World.Map!=null&&(session.World.Map.NearestRoad(hit.point)-hit.point).sqrMagnitude>36)surfaces[index]=RecoveryAudioSurface.Grass;
            }
            if(!grounded[index])return;
            float slip=car.WheelSlip.magnitude*.5f;float motion=Mathf.Max(car.Speed,slip*3);
            if(motion<.6f)return;
            grainClock[index]-=Time.unscaledDeltaTime;if(grainClock[index]>0)return;
            var set=bank.Surface(surfaces[index]);if(set==null||set.Clips.Length==0)return;
            grainClock[index]=Mathf.Lerp(.46f,.13f,Mathf.Clamp01(motion/15));
            var clip=set.Clips[(Time.frameCount+index*13)%set.Clips.Length];
            Shot(clip,RecoveryAudioBus.Vehicles,set.Gain*Mathf.Clamp01(motion/5),175,car.transform.position,true,.75f+Mathf.Clamp01(motion/20)*.35f);
        }
        private void Ambience(RecoveryWorld world,Vector3 p,bool online)
        {
            float mountain=0,lake=0,desert=0,forest=1;float closest=float.MaxValue;int biome=0;
            if(world.Map!=null)foreach(var site in world.Map.Sites)
            {float d=(site.Position-p).sqrMagnitude;if(d<closest){closest=d;biome=site.Biome;}}
            mountain=Mathf.InverseLerp(60,145,p.y);desert=biome>=4?1:0;forest=(1-mountain)*(1-desert);
            if(world.Map!=null)foreach(var site in world.Map.Sites)if(site.Biome==1||site.Biome==4)lake=Mathf.Max(lake,1-Mathf.Clamp01(Vector3.Distance(site.Position,p)/90));
            float gain=BusGain(RecoveryAudioBus.Ambience)*(session.MenuOpen?.5f:1);
            if(!focused||appPaused)gain=0;
            // Two wind recordings crossfade by altitude through source selection only at zero gain.
            var wanted=mountain>.55f?bank.MountainWind:bank.Wind;
            if(wind.clip!=wanted){Loop(wind,0,1,1);if(wind.volume<.002f){wind.Stop();wind.clip=wanted;}}
            else Loop(wind,gain*(.10f+.13f*mountain+.05f*desert),1,.12f);
            Loop(birds,gain*.15f*forest*Mathf.Lerp(1,.08f,environmentNight)*(1-environmentRain*.65f),1,.12f);
            Loop(insects,gain*.055f*forest*Mathf.Lerp(.25f,1,environmentNight)*(1-environmentRain*.7f),1,.12f);Loop(water,gain*.15f*lake,1,.12f);
            float best=float.MaxValue;Vector3 fall=Vector3.zero;foreach(var point in bank.Waterfalls){float distance=(point-p).sqrMagnitude;if(distance<best){best=distance;fall=point;}}
            waterfall.transform.position=fall;Loop(waterfall,best<10000?gain*.25f:0,1,.18f);
            garage.transform.position=world.GaragePosition;garage.volume=gain*.13f;
            if(gain>0&&Vector3.Distance(p,world.GaragePosition)<32&&Time.realtimeSinceStartupAsDouble>=nextWorkshopAt){garage.Play();nextWorkshopAt=Time.realtimeSinceStartupAsDouble+13;}
        }
        private void Silence(){foreach(var s in GetComponentsInChildren<AudioSource>())s.Stop();}
        private void OnApplicationFocus(bool value){focused=value;UpdateSuspension();}
        private void OnApplicationPause(bool value){appPaused=value;UpdateSuspension();}
        private void UpdateSuspension(){foreach(var s in GetComponentsInChildren<AudioSource>()){if(focused&&!appPaused)s.UnPause();else s.Pause();}}
        private void OnDisable(){Silence();if(listener!=null)listener.enabled=false;RestoreListeners();}
        private void OnEnable(){if(listener==null)return;if(oldListeners!=null)foreach(var previous in oldListeners)if(previous!=null)previous.enabled=false;listener.enabled=true;}
        private void RestoreListeners(){if(oldListeners!=null)for(int i=0;i<oldListeners.Length;i++)if(oldListeners[i]!=null)oldListeners[i].enabled=oldListenerEnabled[i];}
        private void OnDestroy()
        {
            if(session!=null&&session.World!=null){session.World.ExternalAudio=false;foreach(var car in session.World.Drivers)if(car!=null)car.ExternalAudio=false;}
            for(int i=0;i<2;i++)if(legacyEngines[i]!=null)legacyEngines[i].enabled=legacyEnabled[i];
            if(legacyCasualty!=null)legacyCasualty.enabled=casualtyEnabled;RestoreListeners();
        }
    }
}

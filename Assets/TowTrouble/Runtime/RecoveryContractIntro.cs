using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    // A local reenactment. Only copied mesh transforms move; the networked target and its physics never do.
    public sealed class RecoveryContractIntro : IDisposable
    {
        public const float ReturnAt = 5.5f, Duration = 5.85f;
        private readonly RecoveryContract contract;
        private readonly GameObject actor;
        private readonly Renderer[] originals;
        private readonly bool[] hidden;
        private readonly Transform[] wheels = new Transform[4];
        private readonly Vector3[] wheelAnchors;
        private readonly float wheelRadius, suspensionTravel, spinDirection;
        private readonly Vector3[] path = new Vector3[9], pathNormals = new Vector3[9];
        private readonly Quaternion finalRotation;
        private readonly Vector3 cameraOffset;
        private readonly float focusHeight, shotDistance;
        private readonly bool reducedMotion;
        private readonly ParticleSystem smoke;
        private ParticleSystem wheelSpray;
        private readonly Vector3[] sprayPoints=new Vector3[4],sprayNormals=new Vector3[4];
        private readonly bool[] sprayGrounded=new bool[4];
        private readonly float[] sprayCredit=new float[4];
        private readonly System.Random sprayRandom=new System.Random(71);
        private Color sprayColor;
        private float spraySize,sprayGravity,sprayLifetime;
        private bool sprayWater,spraySwitch;
        private bool AuthoredWheelRut=>contract.ExtractionProfile!=null&&contract.ExtractionProfile.Enabled;
        private bool restored, disposed;
        private AudioSource engine, engineLow;
        private readonly int modelIndex;
        private float audioGain;
        private bool audioAudible=true;
        public float Elapsed { get; private set; }
        public bool ShowingIncident => Elapsed >= .25f && Elapsed < ReturnAt;
        public bool Returning => Elapsed >= ReturnAt;
        public bool Finished => Elapsed >= Duration;
        public Vector3 ActorPosition => actor.transform.position;
        public float Fade => FadeAt(Elapsed);
        public bool CanSkip => Elapsed >= .7f && Elapsed < 5.2f;
        public static float FadeAt(float time)
        {
            if (time < .25f) return Mathf.Clamp01(time / .25f);
            if (time < .6f) return 1 - Mathf.Clamp01((time - .25f) / .35f);
            if (time < 5.2f) return 0;
            if (time < ReturnAt) return Mathf.Clamp01((time - 5.2f) / .3f);
            return 1 - Mathf.Clamp01((time - ReturnAt) / (Duration - ReturnAt));
        }
        public RecoveryContractIntro(RecoveryVehicle source, RecoveryContract incident, Transform parent, bool reduceMotion)
        {
            contract = incident; reducedMotion = reduceMotion; modelIndex=source.ModelIndex;
            finalRotation = Quaternion.Euler(contract.Pitch, contract.Heading, contract.Roll);
            actor = new GameObject("Recovery incident reenactment");
            actor.SetActive(false); actor.transform.SetParent(parent, false);
            originals = source.GetComponentsInChildren<Renderer>(true);
            hidden = new bool[originals.Length];
            for (int i = 0; i < originals.Length; i++) hidden[i] = originals[i].forceRenderingOff;
            try
            {
                var model = source.CurrentModel;
                var copied = new Dictionary<Transform, Transform>();
                Copy(model.Root.transform, actor.transform, copied, true);
                for (int i = 0; i < 4; i++)
                    copied.TryGetValue(model.Wheels[i], out wheels[i]);
                wheelAnchors=(Vector3[])model.Anchors.Clone();wheelRadius=model.Radius;
                var collider=source.GetComponentInChildren<WheelCollider>(true);suspensionTravel=collider!=null?collider.suspensionDistance:.3f;
                // Approach from the authored escape side: firm ground leads into the actual trap.
                var forward = Quaternion.Euler(0, contract.Heading, 0) * Vector3.forward;
                bool failedEngine=contract.SituationKind == RecoverySituationKind.EngineFailure;
                var entry=failedEngine || contract.ExitDirection.sqrMagnitude<.01f ? -forward : Vector3.ProjectOnPlane(contract.ExitDirection,Vector3.up).normalized;
                spinDirection=Vector3.Dot(-entry,forward)>=0?1:-1;
                float distance = failedEngine ? 4 : 5.5f;
                float bottom = float.MaxValue;
                foreach (var anchor in model.Anchors) bottom = Mathf.Min(bottom, anchor.y - model.Radius);
                for (int i = 0; i < path.Length; i++)
                {
                    float t = i / (float)(path.Length - 1);
                    var point = contract.Target + entry * distance * (1 - t);
                    pathNormals[i]=Vector3.up;
                    if(Physics.Raycast(point+Vector3.up*50,Vector3.down,out var surface,100,1<<8,QueryTriggerInteraction.Ignore))
                    {point.y=surface.point.y-bottom+.06f;pathNormals[i]=surface.normal;}
                    else point.y=contract.Target.y;
                    path[i] = point;
                }
                // Fit the actual vehicle rather than using the same distance for every body.
                var bounds=new Bounds(source.transform.position,Vector3.zero);bool found=false;
                foreach(var renderer in model.Root.GetComponentsInChildren<MeshRenderer>(true))
                {if(!renderer.enabled)continue;if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);}
                focusHeight=found?Mathf.Clamp(bounds.center.y-source.transform.position.y,.85f,1.6f):1.05f;
                shotDistance=found?Mathf.Clamp(bounds.extents.magnitude*2.1f,6.5f,9.5f):8.5f;
                cameraOffset = ChooseCamera();
                if(contract.SituationKind==RecoverySituationKind.EngineFailure && source.TryGetComponent<RecoveryDistressVisuals>(out var distress))
                    smoke=distress.CreateIncidentSmoke(actor.transform);
                CreateWheelSpray(source);
                Pose();
            }
            catch { Dispose(); throw; }
        }
        private static Transform Copy(Transform source, Transform parent, Dictionary<Transform, Transform> copied, bool root = false)
        {
            var go = new GameObject(source.name); var target = go.transform; target.SetParent(parent, false);
            target.localPosition = source.localPosition; target.localRotation = source.localRotation; target.localScale = source.localScale;
            go.layer = source.gameObject.layer; go.SetActive(root || source.gameObject.activeSelf); copied.Add(source, target);
            if (source.TryGetComponent<MeshFilter>(out var filter) && source.TryGetComponent<MeshRenderer>(out var renderer) && renderer.enabled)
            {
                go.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var mesh = go.AddComponent<MeshRenderer>(); mesh.sharedMaterials = renderer.sharedMaterials;
                mesh.shadowCastingMode = renderer.shadowCastingMode; mesh.receiveShadows = renderer.receiveShadows;
                mesh.lightProbeUsage = renderer.lightProbeUsage; mesh.reflectionProbeUsage = renderer.reflectionProbeUsage;
            }
            foreach (Transform child in source) Copy(child, target, copied);
            return target;
        }
        public void Advance(float delta,bool audible=true)
        {
            if (disposed) return;
            audioAudible=audible;
            Elapsed = Mathf.Min(Duration, Elapsed + Mathf.Max(0, delta));
            if (Returning) RestoreTarget();
            else
            {
                if(ShowingIncident&&!actor.activeSelf)
                {actor.SetActive(true);foreach(var renderer in originals)if(renderer!=null)renderer.forceRenderingOff=true;}
                Pose();
                AdvanceWheelSpray(audible?Mathf.Min(Mathf.Max(0,delta),.1f):0);
            }
        }
        public void Skip() { if (CanSkip) Elapsed = 5.2f; }
        private Vector3 Travel(float progress)
        {
            float index = Mathf.Clamp01(progress) * (path.Length - 1);
            int low = Mathf.Min(path.Length - 2, Mathf.FloorToInt(index));
            return Vector3.Lerp(path[low], path[low + 1], index - low);
        }
        private void Pose()
        {
            float time = Mathf.Max(0, Elapsed - .25f);
            if(smoke!=null && time>=2.3f && Application.isPlaying && !smoke.isPlaying)smoke.Play(true);
            float approach = Mathf.Clamp01(time / 2.2f);
            float travel = 1 - (1 - approach) * (1 - approach);
            bool soft=SoftSurface;
            float settle = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(soft?.8f:1.1f, 2.9f, time));
            var position = Vector3.Lerp(Travel(travel), contract.Target, settle);
            float sample=travel*(path.Length-1);int low=Mathf.Min(path.Length-2,Mathf.FloorToInt(sample));
            var normal=Vector3.Slerp(pathNormals[low],pathNormals[low+1],sample-low);
            var forward=Quaternion.Euler(0,contract.Heading,0)*Vector3.forward;
            var heading = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward,normal),normal);
            var rotation = Quaternion.Slerp(heading, finalRotation, settle);
            float struggle = AttemptAt(time);
            bool failure = contract.SituationKind == RecoverySituationKind.EngineFailure;
            if (!reducedMotion)
            {
                // One loaded suspension movement, then a damped release; no continuous shaking.
                float rock=Mathf.Sin(Mathf.Clamp01((time-2.7f)/1.8f)*Mathf.PI*2)*struggle;
                float impact=contract.SituationKind==RecoverySituationKind.Rocks||contract.SituationKind==RecoverySituationKind.Ditch
                    ? Mathf.Sin(Mathf.Clamp01((time-1.5f)/1.2f)*Mathf.PI*2)*(1-Mathf.Clamp01((time-1.5f)/1.2f))*.9f:0;
                rotation *= Quaternion.Euler((failure?.15f:soft?.45f:1.15f)*rock+impact,0,(soft?.15f:.4f)*rock);
            }
            actor.transform.SetPositionAndRotation(position, rotation);
            float spin = spinDirection*(travel * (failure?4:5.5f) / wheelRadius * Mathf.Rad2Deg + (failure ? 0 : AttemptTravel(time) * (soft?320:contract.SituationKind==RecoverySituationKind.Water?190:100)));
            for (int i = 0; i < wheels.Length; i++) if (wheels[i] != null)
            {
                var anchor=actor.transform.TransformPoint(wheelAnchors[i]);var up=actor.transform.up;float offset=-.12f;sprayGrounded[i]=false;
                if(Physics.Raycast(anchor+up*RecoveryWheelPose.ProbeLift,-up,out var contact,wheelRadius+suspensionTravel+RecoveryWheelPose.ProbeLift,1<<8,QueryTriggerInteraction.Ignore))
                    {
                    offset=RecoveryWheelPose.SuspensionOffset(contact.distance,wheelRadius,suspensionTravel);
                    sprayGrounded[i]=true;sprayNormals[i]=contact.normal;
                    var cover=contact.collider.GetComponent<RecoveryTrackSurface>();
                    sprayPoints[i]=(cover!=null?cover.VisualPoint(contact.point):contact.point)+contact.normal*.045f;
                }
                wheels[i].SetPositionAndRotation(RecoveryWheelPose.Position(anchor,up,offset),RecoveryWheelPose.Rotation(actor.transform.rotation,0,spin));
            }
            if(engine!=null)
            {
                float envelope=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.5f,time))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.3f,4.9f,time)));
                if(failure)envelope*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.7f,2.4f,time));
                // Restrained approach, settle to idle, then one gentle attempt to get free.
                // No rapid pitch wobble: preserve the recorded classic engine character.
                float demand=.42f*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.6f,2.2f,time)))+(failure?0:(soft?.32f:.24f)*struggle);
                var weights=RecoveryScoutEngineMix.Weights(demand,0);
                float gain=audioAudible&&Application.isFocused?audioGain*envelope:0;
                SetEngineLayer(engine,gain*weights.x,1);
                SetEngineLayer(engineLow,gain*weights.y,Mathf.Lerp(.92f,1.04f,demand));
            }
        }
        private void SetEngineLayer(AudioSource source,float volume,float pitch)
        {
            if(source==null)return;
            source.volume=volume;source.pitch=pitch;
            if(Application.isPlaying&&ShowingIncident&&!source.isPlaying&&volume>.001f)source.Play();
        }
        public void ConfigureAudio(RecoveryAudioBank bank,float effects)
        {
            if(bank==null||engine!=null)return;
            bool scout=modelIndex==0&&bank.HasScoutMuscle;
            var profile=scout?null:bank.EngineFor(modelIndex);
            var idle=scout?bank.ScoutMuscleIdle:profile!=null?profile.Idle:bank.EngineIdle;
            var low=scout?bank.ScoutMuscleLow:profile!=null?profile.Low:bank.EngineDrive;
            if(idle==null)idle=low;
            if(idle==null)return;
            // A close cinematic mix, quieter than driving, respects the player's effects level.
            audioGain=RecoveryAudioPolicy.Gain(effects)*RecoveryAudioPolicy.Gain(scout?bank.ScoutEngineGain:profile!=null?profile.Gain:bank.EngineGain)*.8f;
            engine=CreateEngineLayer(bank,idle);
            if(low!=null)engineLow=CreateEngineLayer(bank,low);
        }
        private AudioSource CreateEngineLayer(RecoveryAudioBank bank,AudioClip clip)
        {
            var source=actor.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=true;source.spatialBlend=0;
            source.clip=clip;source.outputAudioMixerGroup=bank.Vehicles;source.volume=0;source.priority=110;source.dopplerLevel=0;
            return source;
        }
        private void CreateWheelSpray(RecoveryVehicle source)
        {
            var kind=contract.SituationKind;
            if(kind!=RecoverySituationKind.Mud&&kind!=RecoverySituationKind.Sand&&kind!=RecoverySituationKind.Snow&&kind!=RecoverySituationKind.Water&&kind!=RecoverySituationKind.DryWash&&!AuthoredWheelRut)return;
            Material material=source.GetComponent<RecoveryVehicleEffects>()?.VaporMaterial;
            if(material==null)foreach(var ps in source.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var candidate=ps.GetComponent<ParticleSystemRenderer>()?.sharedMaterial;
                    if(candidate!=null&&candidate.shader.name=="TowTrouble/Exhaust Vapor"){material=candidate;break;}
                    if(ps.name=="Road dust")material=candidate;
                }
            if(material==null)return;
            sprayWater=kind==RecoverySituationKind.Water;spraySwitch=RecoveryPlatform.UsesSwitchRendering;
            bool snow=kind==RecoverySituationKind.Snow,mud=kind==RecoverySituationKind.Mud||AuthoredWheelRut;
            sprayColor=sprayWater?new Color(.65f,.88f,.95f,.75f):snow?new Color(.92f,.96f,1,.85f):mud?new Color(.26f,.16f,.08f,1):new Color(.76f,.56f,.28f,.8f);
            spraySize=sprayWater?.14f:snow?.22f:mud?.17f:.24f;
            sprayGravity=snow?.4f:sprayWater?.85f:mud?1:.6f;sprayLifetime=snow?.8f:.6f;
            var go=new GameObject("Incident wheel spray");go.transform.SetParent(actor.transform,false);
            wheelSpray=go.AddComponent<ParticleSystem>();wheelSpray.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=wheelSpray.main;main.playOnAwake=false;main.loop=false;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.useUnscaledTime=true;main.maxParticles=spraySwitch?48:96;main.startSpeed=0;main.gravityModifier=sprayGravity;
            // Simulated explicitly by the intro clock so solo pause/focus/skip remain deterministic.
            main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=wheelSpray.emission;emission.enabled=false;var shape=wheelSpray.shape;shape.enabled=false;
            var color=wheelSpray.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(.8f,.55f),new GradientAlphaKey(0,1)});color.color=gradient;
            var size=wheelSpray.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.45f));
            var renderer=wheelSpray.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
            var tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",Color.white);tint.SetColor("_Color",Color.white);renderer.SetPropertyBlock(tint);
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;
            renderer.renderMode=sprayWater?ParticleSystemRenderMode.Stretch:ParticleSystemRenderMode.Billboard;
            if(sprayWater){renderer.lengthScale=1.4f;renderer.velocityScale=.12f;}
        }
        private void AdvanceWheelSpray(float delta)
        {
            if(wheelSpray==null||delta<=0||!ShowingIncident)return;
            wheelSpray.Simulate(delta,false,false,false);
            if(Elapsed>=5.2f)return;
            float time=Mathf.Max(0,Elapsed-.25f),attempt=AttemptAt(time);
            float approach=.22f*(1-Mathf.Clamp01(time/2.2f));
            float proximity=1;
            if(sprayWater||contract.SituationKind==RecoverySituationKind.Mud)
                proximity=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,3,Vector3.Distance(actor.transform.position,contract.Target)));
            float strength=(approach+attempt)*proximity;
            if(strength<=0)return;
            float rate=(spraySwitch?10:18)*strength*RecoveryVehicle.VisualEffectsGain;
            for(int i=0;i<4;i++)
            {
                if(!sprayGrounded[i]||(AuthoredWheelRut&&i!=contract.ExtractionProfile.BlockedWheel)){sprayCredit[i]=0;continue;}
                sprayCredit[i]+=rate*delta;int count=Mathf.Min(3,Mathf.FloorToInt(sprayCredit[i]));sprayCredit[i]-=count;
                for(int j=0;j<count;j++)
                {
                    float jitter=(float)sprayRandom.NextDouble(),lift=(float)sprayRandom.NextDouble();
                    var normal=sprayNormals[i];var back=Vector3.ProjectOnPlane(-actor.transform.forward*spinDirection,normal).normalized;
                    var side=Vector3.Cross(normal,back);
                    var outward=Vector3.ProjectOnPlane(actor.transform.right,normal).normalized*Mathf.Sign(wheelAnchors[i].x);
                    var particle=new ParticleSystem.EmitParams{
                        position=sprayPoints[i]+outward*.1f+side*((jitter-.5f)*.12f),
                        velocity=back*Mathf.Lerp(.8f,2.6f,attempt)+normal*Mathf.Lerp(2,3.2f,lift)+outward*Mathf.Lerp(.9f,1.5f,jitter)+side*((jitter-.5f)*.35f),
                        startColor=sprayColor,startSize=spraySize*Mathf.Lerp(.65f,1.25f,jitter),
                        startLifetime=sprayLifetime*Mathf.Lerp(.75f,1, lift),rotation=jitter*360};
                    wheelSpray.Emit(particle,1);
                }
            }
            // Emit may start the system; prevent a second automatic simulation tick.
            wheelSpray.Pause(false);
        }
        private bool SoftSurface => contract.SituationKind==RecoverySituationKind.Mud||contract.SituationKind==RecoverySituationKind.Sand||contract.SituationKind==RecoverySituationKind.Snow||contract.SituationKind==RecoverySituationKind.DryWash;
        private static float AttemptAt(float time)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.7f,3.2f,time))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.6f,4.5f,time)));
        // Integral of the same acceleration envelope: wheel speed settles smoothly with the motor.
        private static float RampIntegral(float time,float start,float duration)
        {
            float u=Mathf.Clamp01((time-start)/duration);
            return duration*(u*u*u-.5f*u*u*u*u)+Mathf.Max(0,time-start-duration);
        }
        private static float AttemptTravel(float time)=>RampIntegral(time,2.7f,.5f)-RampIntegral(time,3.6f,.9f);
        private void ShotPose(Vector3 actorPosition,float time,Vector3 offset,out Vector3 position,out Vector3 look)
        {
            float establish=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.2f,2.2f,time));
            float reveal=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.8f,4.5f,time));
            // Early look-ahead shows where the car is heading. Finish on the trapped vehicle.
            look=actorPosition+Vector3.up*focusHeight+(reducedMotion?Vector3.zero:(contract.Target-actorPosition)*(.16f*establish));
            float orbit=reducedMotion?0:(SoftSurface?7:contract.SituationKind==RecoverySituationKind.EngineFailure?-5:4)*reveal;
            float scale=reducedMotion?1:Mathf.Lerp(1.12f,.9f,reveal);
            position=look+Quaternion.AngleAxis(orbit,Vector3.up)*offset*scale;
        }
        public void CameraPose(out Vector3 position, out Vector3 look)
        {
            ShotPose(actor.transform.position,Mathf.Max(0,Elapsed-.25f),cameraOffset,out position,out look);
            position.y = Mathf.Max(position.y, Ground(position, position.y - 2) + 1.15f);
            if (Physics.Linecast(look, position, out var hit, 1 << 8, QueryTriggerInteraction.Ignore)) position = hit.point + hit.normal * .45f;
            position.y = Mathf.Max(position.y, Ground(position, position.y - 2) + 1.15f);
        }
        private Vector3 ChooseCamera()
        {
            float best = float.MaxValue; Vector3 chosen = new Vector3(7, 5, 7);
            float preferred=AuthoredWheelRut?48:contract.SituationKind==RecoverySituationKind.EngineFailure?42:SoftSurface?68:55;
            float lowHeight=AuthoredWheelRut?2.6f:contract.SituationKind==RecoverySituationKind.Water?4.2f:3.2f;
            foreach (float yaw in new[] { -preferred, preferred, -85, 85, -130, 130, 180 })
            foreach (float height in new[] { lowHeight, 6f })
            {
                var offset = Quaternion.Euler(0, contract.Heading + yaw, 0) * new Vector3(0, height, shotDistance);
                float score = height * .08f + Mathf.Abs(Mathf.Abs(yaw) - preferred) * .002f;
                // Check the entire planned push-in/orbit, not just the starting view.
                for (int i = 0; i < 7; i++)
                {
                    float time=i*.75f,approach=Mathf.Clamp01(time/2.2f),travel=1-(1-approach)*(1-approach);
                    float settle=Mathf.SmoothStep(0,1,Mathf.InverseLerp(SoftSurface?.8f:1.1f,2.9f,time));
                    var point=Vector3.Lerp(Travel(travel),contract.Target,settle);
                    ShotPose(point,time,offset,out var eye,out var focus);
                    eye.y = Mathf.Max(eye.y, Ground(eye, eye.y - 2) + 1.15f);
                    if (Physics.Linecast(focus, eye, 1 << 8, QueryTriggerInteraction.Ignore)) score += 100;
                    if (Physics.CheckSphere(eye,.35f,1<<8,QueryTriggerInteraction.Ignore))score+=100;
                }
                if (score < best) { best = score; chosen = offset; }
            }
            return chosen;
        }
        private static float Ground(Vector3 point, float fallback)
            => Physics.Raycast(point + Vector3.up * 50, Vector3.down, out var hit, 100, 1 << 8, QueryTriggerInteraction.Ignore) ? hit.point.y : fallback;
        private void RestoreTarget()
        {
            if (restored) return; restored = true; actor.SetActive(false);
            for (int i = 0; i < originals.Length; i++) if (originals[i] != null) originals[i].forceRenderingOff = hidden[i];
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true; RestoreTarget();
            if (Application.isPlaying) UnityEngine.Object.Destroy(actor); else UnityEngine.Object.DestroyImmediate(actor);
        }
    }
}

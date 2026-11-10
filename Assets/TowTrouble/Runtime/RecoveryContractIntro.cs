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
        private readonly bool reducedMotion;
        private readonly ParticleSystem smoke;
        private bool restored, disposed;
        private AudioSource engine;
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
            contract = incident; reducedMotion = reduceMotion;
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
                cameraOffset = ChooseCamera();
                if(contract.SituationKind==RecoverySituationKind.EngineFailure && source.TryGetComponent<RecoveryDistressVisuals>(out var distress))
                    smoke=distress.CreateIncidentSmoke(actor.transform);
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
            float settle = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.1f, 2.9f, time));
            var position = Vector3.Lerp(Travel(travel), contract.Target, settle);
            float sample=travel*(path.Length-1);int low=Mathf.Min(path.Length-2,Mathf.FloorToInt(sample));
            var normal=Vector3.Slerp(pathNormals[low],pathNormals[low+1],sample-low);
            var forward=Quaternion.Euler(0,contract.Heading,0)*Vector3.forward;
            var heading = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward,normal),normal);
            var rotation = Quaternion.Slerp(heading, finalRotation, settle);
            float struggle = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(2.8f, 3.1f, time)) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(4, 4.6f, time)));
            bool failure = contract.SituationKind == RecoverySituationKind.EngineFailure;
            if (!reducedMotion) rotation *= Quaternion.Euler(Mathf.Sin(time * 17) * (failure ? .3f : .85f) * struggle, 0, Mathf.Sin(time * 11) * .3f * struggle);
            actor.transform.SetPositionAndRotation(position, rotation);
            float spin = spinDirection*(travel * (failure?4:5.5f) / wheelRadius * Mathf.Rad2Deg + (failure ? 0 : Mathf.Max(0, Mathf.Min(time, 4.3f) - 2.7f) * 340));
            for (int i = 0; i < wheels.Length; i++) if (wheels[i] != null)
            {
                var anchor=actor.transform.TransformPoint(wheelAnchors[i]);var up=actor.transform.up;float offset=-.12f;
                if(Physics.Raycast(anchor+up*RecoveryWheelPose.ProbeLift,-up,out var contact,wheelRadius+suspensionTravel+RecoveryWheelPose.ProbeLift,1<<8,QueryTriggerInteraction.Ignore))
                    offset=RecoveryWheelPose.SuspensionOffset(contact.distance,wheelRadius,suspensionTravel);
                wheels[i].SetPositionAndRotation(RecoveryWheelPose.Position(anchor,up,offset),RecoveryWheelPose.Rotation(actor.transform.rotation,0,spin));
            }
            if(engine!=null)
            {
                float envelope=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.5f,time))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.3f,4.9f,time)));
                if(failure)envelope*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.7f,2.4f,time));
                engine.volume=audioAudible&&Application.isFocused?audioGain*envelope:0;
                engine.pitch=Mathf.Lerp(1,.78f,settle)+(failure?0:Mathf.Sin(time*9)*.07f*struggle);
                if(Application.isPlaying&&ShowingIncident&&!engine.isPlaying&&engine.volume>.001f)engine.Play();
            }
        }
        public void ConfigureAudio(RecoveryAudioBank bank,float effects)
        {
            if(bank==null||bank.EngineDrive==null)return;
            audioGain=RecoveryAudioPolicy.Gain(effects)*bank.EngineGain*.34f;
            engine=actor.AddComponent<AudioSource>();engine.playOnAwake=false;engine.loop=true;engine.spatialBlend=0;
            engine.clip=bank.EngineDrive;engine.outputAudioMixerGroup=bank.Vehicles;engine.volume=0;engine.priority=110;engine.dopplerLevel=0;
        }
        public void CameraPose(out Vector3 position, out Vector3 look)
        {
            look = actor.transform.position + Vector3.up * 1.05f;
            float drift = reducedMotion ? 0 : Mathf.SmoothStep(0, 1, Mathf.Clamp01((Elapsed - .25f) / 4.8f));
            position = look + Quaternion.AngleAxis(drift * 4, Vector3.up) * cameraOffset * (1 - drift * .06f);
            position.y = Mathf.Max(position.y, Ground(position, position.y - 2) + 1.15f);
            if (Physics.Linecast(look, position, out var hit, 1 << 8, QueryTriggerInteraction.Ignore)) position = hit.point + hit.normal * .45f;
            // A steep bank's hit normal can point down. Protect the shortened arm as well as its destination.
            position.y = Mathf.Max(position.y, Ground(position, position.y - 2) + 1.15f);
        }
        private Vector3 ChooseCamera()
        {
            float best = float.MaxValue; Vector3 chosen = new Vector3(7, 5, 7);
            foreach (float yaw in new[] { -52f, 52, -85, 85, -130, 130, 180 })
            foreach (float height in new[] { 3.8f, 6f })
            {
                var offset = Quaternion.Euler(0, contract.Heading + yaw, 0) * new Vector3(0, height, 8.5f);
                float score = height * .08f + Mathf.Abs(Mathf.Abs(yaw) - 52) * .002f;
                for (int i = 0; i < 5; i++)
                {
                    var focus = Vector3.Lerp(Travel(i / 4f), contract.Target, i / 4f) + Vector3.up * 1.05f;
                    var eye = focus + offset;
                    eye.y = Mathf.Max(eye.y, Ground(eye, eye.y - 2) + 1.15f);
                    if (Physics.Linecast(focus, eye, 1 << 8, QueryTriggerInteraction.Ignore)) score += 100;
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

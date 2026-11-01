using System;
using UnityEngine;

namespace TowTrouble
{
    public enum ExtractionStage : byte { Blocked, TakingLoad, Moving, Releasing, Free }

    [Serializable]
    public sealed class RecoveryExtractionSettings
    {
        public bool Enabled;
        public bool TerrainAware;
        [Range(0, 3)] public int BlockedWheel;
        [Min(.5f)] public float Travel = 2.2f;
        [Min(0)] public float BreakawayForce = 5600f;
        [Min(0)] public float MovingForce = 3600f;
        [Min(0)] public float MovingDrag = 4200f;
        [Min(.1f)] public float ReleaseDuration = .4f;
        [Min(0)] public float ClearHold = .2f;
    }

    // Host-owned, displacement-driven rules. No Transform, timer-based progress,
    // Rigidbody simulation, profile writes or presentation dependencies.
    public sealed class RecoveryExtraction
    {
        private readonly RecoveryExtractionSettings settings;
        private readonly Vector3 origin, wheelOrigin, exit;
        private bool moving, loaded;
        private float clearHold, releaseTime, releaseResistance;

        public ExtractionStage Stage { get; private set; }
        public float Progress { get; private set; }
        public float Alignment { get; private set; } = 1;
        public float StalledSeconds { get; private set; }
        public float ReleasedSeconds { get; private set; }
        public bool IsFree => Stage == ExtractionStage.Free;

        public RecoveryExtraction(RecoveryExtractionSettings config, Vector3 bodyPosition, Vector3 wheelPosition, Vector3 exitDirection)
        {
            settings = config;
            origin = bodyPosition;
            wheelOrigin = wheelPosition;
            exit = Flat(exitDirection).sqrMagnitude > .001f ? Flat(exitDirection).normalized : Vector3.forward;
        }

        public Vector3 Step(Vector3 bodyPosition, Vector3 wheelPosition, Vector3 velocity, Vector3 pull,
            float mass, bool supported, bool canRecover, float deltaTime, Vector3 groundNormal = default)
        {
            if (deltaTime <= 0) return Vector3.zero;
            if (IsFree) { ReleasedSeconds += deltaTime; return Vector3.zero; }

            var planarPull = Flat(pull);
            var planarVelocity = Flat(velocity);
            float previousProgress = Progress;
            float travel = Mathf.Min(Vector3.Dot(bodyPosition - origin, exit), Vector3.Dot(wheelPosition - wheelOrigin, exit));
            Progress = Mathf.Clamp01(travel / Mathf.Max(.5f, settings.Travel));
            float effort = planarPull.magnitude;
            Alignment = effort > 100 ? Vector3.Dot(planarPull / effort, exit) : 1;
            loaded |= canRecover && effort > 100;

            // A sideways detour or a jump cannot satisfy the exit plane/contact test.
            bool clear = canRecover && loaded && Progress >= 1 && supported;
            clearHold = clear ? clearHold + deltaTime : 0;
            // Deep pockets retain their load until the last part of the actual exit.
            float positionResistance = settings.TerrainAware
                ? Mathf.Lerp(1f,.82f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,Progress)))
                : Mathf.Lerp(1f, .6f, Mathf.SmoothStep(0, 1, Progress));
            float angleResistance = Mathf.Lerp(1f, 1.9f, Mathf.InverseLerp(.9f, 0, Alignment));
            float breakaway = Mathf.Max(0, settings.BreakawayForce) * positionResistance * angleResistance;
            float sliding = Mathf.Min(Mathf.Max(0, settings.MovingForce), Mathf.Max(0, settings.BreakawayForce)) * positionResistance * angleResistance;
            float movingCapacity = sliding + planarVelocity.magnitude * Mathf.Max(0, settings.MovingDrag) * positionResistance;

            if (Stage != ExtractionStage.Releasing)
            {
                float forwardSpeed = Vector3.Dot(planarVelocity, exit);
                if (canRecover && effort > 100 && (settings.TerrainAware ? forwardSpeed > .18f : effort > breakaway || forwardSpeed > .18f)) moving = true;
                else if (planarVelocity.magnitude < .04f) moving = false;
                Stage = !canRecover || effort <= 100 ? ExtractionStage.Blocked
                    : forwardSpeed > .05f || Progress - previousProgress > .01f * deltaTime
                        ? ExtractionStage.Moving : ExtractionStage.TakingLoad;
                StalledSeconds = canRecover && effort > 100 && forwardSpeed < .05f
                    ? StalledSeconds + deltaTime : 0;
                if (clear && clearHold >= Mathf.Max(0, settings.ClearHold))
                {
                    Stage = ExtractionStage.Releasing;
                    releaseResistance = moving ? movingCapacity : breakaway;
                    releaseTime = 0;
                    StalledSeconds = 0;
                }
            }

            float capacity = moving ? movingCapacity : breakaway;
            if (Stage == ExtractionStage.Releasing)
            {
                releaseTime += deltaTime;
                float t = Mathf.Clamp01(releaseTime / Mathf.Max(.1f, settings.ReleaseDuration));
                capacity = releaseResistance * (1 - Mathf.SmoothStep(0, 1, t));
                if (t >= 1) { Stage = ExtractionStage.Free; Progress = 1; return Vector3.zero; }
            }

            // Ground resistance never acts in the air or invents a launch impulse.
            // Cancel at most this step's pull + momentum; never reverse it at rest.
            if (!supported) return Vector3.zero;
            if(settings.TerrainAware)
            {
                var normal=groundNormal.sqrMagnitude>.1f?groundNormal.normalized:Vector3.up;
                var tangentPull=Vector3.ProjectOnPlane(pull+Physics.gravity*Mathf.Max(1,mass),normal);
                var tangentVelocity=Vector3.ProjectOnPlane(velocity,normal);
                // The same ground contact supports the static weight and the cable.
                // No resistance is invented while airborne; slope gravity is not added twice.
                capacity*=Mathf.Clamp(Vector3.Dot(normal,Vector3.up),.25f,1);
                return -Vector3.ClampMagnitude(tangentPull+tangentVelocity*Mathf.Max(1,mass)/deltaTime,capacity);
            }
            return -Vector3.ClampMagnitude(planarPull + planarVelocity * Mathf.Max(1, mass) / deltaTime, capacity);
        }

        public static float LoadedCableForce(float previous, float requested, float strength, float deltaTime)
        {
            // Taking up load is gradual; slack and releasing the throttle remain immediate.
            return requested <= previous ? Mathf.Max(0, requested)
                : Mathf.MoveTowards(previous, requested, Mathf.Max(1, strength) / .45f * Mathf.Max(0, deltaTime));
        }

        private static Vector3 Flat(Vector3 value) => new Vector3(value.x, 0, value.z);
    }
}

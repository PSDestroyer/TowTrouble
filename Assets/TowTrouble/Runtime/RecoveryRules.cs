using UnityEngine;

namespace TowTrouble
{
    public enum RecoveryPhase { Lobby, HookUp, Recover, Deliver, Complete, Explore }

    public static class RecoveryRules
    {
        public const float AttachRange = 14f;
        public const float BreakForce = 11000f;
        public const float MaxCableForce = 42000f;
        public const float CableStiffness = 11500f, CableDamping = 1500f;
        public const float ExtractionZ = 29f;
        public static readonly Vector3 Delivery = new Vector3(0f, 0f, 108f);

        public static float CableForce(float distance, float length, float separatingSpeed)
        { return Mathf.Min(CableDemand(distance, length, separatingSpeed), MaxCableForce); }

        public static float CableDemand(float distance, float length, float separatingSpeed)
        {
            if (distance <= length) return 0f;
            return Mathf.Max(0f, (distance - length) * CableStiffness + separatingSpeed * CableDamping);
        }

        public static float CableStressAfter(float stress, float demand, float strength, float deltaTime)
        {
            stress = Mathf.Clamp01(stress);
            if (deltaTime <= 0) return stress;
            float ratio = Mathf.Max(0, demand) / Mathf.Max(1, strength);
            if (ratio <= 1) return Mathf.Max(0, stress - deltaTime * .5f);
            // A major impact fails in this physics step, before it can pull the target.
            if (ratio >= 1.65f) return 1;
            // A small excess has brief tolerance; bigger excess consumes it faster.
            // Below capacity, stress relaxes over two seconds rather than every pulse
            // receiving a fresh grace period. This is not permanent item wear.
            float failureSeconds = .18f / (1 + 8 * (ratio - 1));
            return Mathf.Min(1, stress + deltaTime / failureSeconds);
        }

        public static bool Delivered(Vector3 position, float speed)
        {
            position.y = 0f;
            return Vector3.Distance(position, Delivery) < 7f && speed < 2f;
        }
    }
}

using UnityEngine;

namespace TowTrouble
{
    public enum RecoveryPhase { Lobby, HookUp, Recover, Deliver, Complete, Explore }

    public static class RecoveryRules
    {
        public const float AttachRange = 14f;
        public const float BreakForce = 31000f;
        public const float ExtractionZ = 29f;
        public static readonly Vector3 Delivery = new Vector3(0f, 0f, 108f);

        public static float CableForce(float distance, float length, float separatingSpeed)
        {
            if (distance <= length) return 0f;
            return Mathf.Clamp((distance - length) * 11500f + separatingSpeed * 1500f, 0f, 42000f);
        }

        public static bool Delivered(Vector3 position, float speed)
        {
            position.y = 0f;
            return Vector3.Distance(position, Delivery) < 7f && speed < 2f;
        }
    }
}

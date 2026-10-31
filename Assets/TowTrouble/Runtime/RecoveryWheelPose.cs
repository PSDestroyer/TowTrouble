using UnityEngine;

namespace TowTrouble
{
    // Presentation only. Contact forces and wheel angular velocities remain server-owned.
    public static class RecoveryWheelPose
    {
        public const float ProbeLift = .3f;

        public static float SuspensionOffset(float hitDistance, float radius, float travel)
        {
            // A suspension moves along the chassis up axis. Using hit.normal here
            // pushes the hub sideways whenever adjacent terrain triangles have different normals.
            return Mathf.Clamp(ProbeLift + radius - hitDistance, -travel, 0f);
        }

        public static float InterpolationAlpha(double renderTime, double fixedTime, float fixedDelta)
        {
            return fixedDelta > 0f ? Mathf.Clamp01((float)((renderTime - fixedTime) / fixedDelta)) : 1f;
        }

        public static Vector3 Position(Vector3 anchor, Vector3 up, float offset)
        {
            return anchor + up * offset;
        }

        public static Quaternion Rotation(Quaternion chassis, float steer, float spin)
        {
            // Steer the axle first, then roll around that axle; spinning never changes its direction.
            return chassis * Quaternion.AngleAxis(steer, Vector3.up) * Quaternion.AngleAxis(spin, Vector3.right);
        }
    }
}

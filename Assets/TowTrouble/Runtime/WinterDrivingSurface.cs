using UnityEngine;

namespace TowTrouble
{
    public static class WinterDrivingSurface
    {
        public static bool IsSnow(RaycastHit hit, out bool packed)
        {
            var collider = hit.collider;
            packed = false;
            if (collider == null) return false;
            if (collider.TryGetComponent<RecoveryTrackSurface>(out var track) && track.Kind == RecoveryTrackKind.Wood) return false;
            if (collider.TryGetComponent<RecoveryGroundSurface>(out var surface))
            {
                packed = collider.name.StartsWith("Road-", System.StringComparison.Ordinal) ||
                    (collider is TerrainCollider && surface.KindAt(hit.point) == RecoveryGroundKind.Rock);
                return true;
            }
            return collider is TerrainCollider;
        }

        public static float Penetration(Vector3 point, bool packed, float slip, float loadRatio)
        {
            if (packed) return .018f + .017f * slip;

            float snowPack = Mathf.PerlinNoise(point.x * .067f + 42, point.z * .067f + 19);
            float freshDepth = Mathf.Lerp(.095f, .225f, snowPack);
            float pressure = Mathf.Lerp(.77f, 1.23f, Mathf.InverseLerp(.25f, 1.65f, loadRatio));
            return Mathf.Clamp(freshDepth * pressure + .045f * slip, .065f, .27f);
        }

        public static float Grip(bool packed, Vector3 normal, float winterTires)
        {
            // Packed snow rolls easily on level roads and gets slick on steep grades.
            // In powder, the tire keeps its bite on moderate climbs while the snow resists rolling.
            float slopeLoss = packed ? Mathf.InverseLerp(.995f, .90f, normal.y) :
                Mathf.InverseLerp(.968f, .865f, normal.y);
            float stock = packed ? Mathf.SmoothStep(.72f, .32f, slopeLoss) :
                Mathf.SmoothStep(.49f, .26f, slopeLoss);

            // The shared garage rating gives winter tires their largest gain in loose snow.
            float tireGain = winterTires <= 1 ? winterTires :
                1 + (winterTires - 1) * (packed ? .26f : .48f);
            return Mathf.Min(packed ? .91f : .82f, stock * tireGain);
        }
    }
}

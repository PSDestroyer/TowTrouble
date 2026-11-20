using UnityEngine;

namespace TowTrouble
{
    public enum BodyCoatingSurface { Air, Road, Snow, PackedSnow, Mud, Dirt, Sand }

    public static class RecoveryBodyCoatingRules
    {
        // XYZ = snow, mud, sand. Convex mixing keeps the total below one and lets old deposits linger.
        public static Vector3 Step(Vector3 coating, BodyCoatingSurface surface, float speed, float slip, float seconds)
        {
            if (seconds <= 0 || surface == BodyCoatingSurface.Air) return coating;
            float motion = Mathf.InverseLerp(.25f, 6.5f, Mathf.Abs(speed));
            if (surface == BodyCoatingSurface.Road)
            {
                coating.x *= Mathf.Exp(-.045f * motion * seconds);
                coating.y *= Mathf.Exp(-.025f * motion * seconds);
                coating.z *= Mathf.Exp(-.060f * motion * seconds);
                return coating;
            }
            float activity = Mathf.Max(motion, Mathf.InverseLerp(.12f, .75f, slip) * .85f);
            float rate = surface == BodyCoatingSurface.Mud ? .11f : surface == BodyCoatingSurface.Dirt ? .045f :
                surface == BodyCoatingSurface.PackedSnow ? .025f : surface == BodyCoatingSurface.Sand ? .065f : .09f;
            var target = surface == BodyCoatingSurface.Snow || surface == BodyCoatingSurface.PackedSnow ? Vector3.right :
                surface == BodyCoatingSurface.Sand ? Vector3.forward : Vector3.up;
            return Vector3.Lerp(coating, target, 1 - Mathf.Exp(-rate * activity * seconds));
        }

        public static BodyCoatingSurface Classify(RaycastHit hit, bool winter)
        {
            var collider = hit.collider;
            if (collider == null) return BodyCoatingSurface.Air;
            if (winter && WinterDrivingSurface.IsSnow(hit, out bool packed))
                return packed ? BodyCoatingSurface.PackedSnow : BodyCoatingSurface.Snow;
            if (collider.TryGetComponent<RecoveryMudTerrain>(out _)) return BodyCoatingSurface.Mud;
            if (collider.TryGetComponent<RecoveryTrackSurface>(out var track))
            {
                if (track.Kind == RecoveryTrackKind.Wood || track.Kind == RecoveryTrackKind.Hard) return BodyCoatingSurface.Road;
                if (track.Kind == RecoveryTrackKind.Snow) return BodyCoatingSurface.Snow;
                if (track.Kind == RecoveryTrackKind.Sand) return BodyCoatingSurface.Sand;
                if (track.Kind == RecoveryTrackKind.Dirt && !collider.name.StartsWith("Road-", System.StringComparison.Ordinal))
                    return BodyCoatingSurface.Dirt;
            }
            if (collider.TryGetComponent<RecoveryGroundSurface>(out var ground))
            {
                var kind = ground.KindAt(hit.point);
                if (kind == RecoveryGroundKind.Snow)
                    return collider.name.StartsWith("Road-", System.StringComparison.Ordinal) ? BodyCoatingSurface.PackedSnow : BodyCoatingSurface.Snow;
                if (kind == RecoveryGroundKind.Sand && !collider.name.StartsWith("Road-", System.StringComparison.Ordinal))
                    return BodyCoatingSurface.Sand;
            }
            return BodyCoatingSurface.Road;
        }
    }
}

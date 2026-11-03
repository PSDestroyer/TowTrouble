using UnityEngine;

namespace TowTrouble
{
    public static class RecoveryWaypointProjection
    {
        // All 2D coordinates are in canvas units measured from the bottom left.
        public static bool Place(Vector3 viewport, Vector3 cameraPoint, Vector2 canvasSize,
            Rect safeCenters, out Vector2 position, out float angle)
        {
            var center = canvasSize * .5f;
            position = new Vector2(viewport.x * canvasSize.x, viewport.y * canvasSize.y);
            angle = 0;
            if (viewport.z > .001f && safeCenters.Contains(position)) return false;

            // The perspective projection mirrors targets behind the camera.
            // Use their camera-space side and fold backward distance toward the bottom edge.
            Vector2 direction = viewport.z > .001f
                ? position - center
                : new Vector2(cameraPoint.x, cameraPoint.y + Mathf.Min(0, cameraPoint.z));
            if (direction.sqrMagnitude < .000001f) direction = Vector2.down;
            direction.Normalize();
            float horizontal = EdgeDistance(direction.x, center.x, safeCenters.xMin, safeCenters.xMax);
            float vertical = EdgeDistance(direction.y, center.y, safeCenters.yMin, safeCenters.yMax);
            position = center + direction * Mathf.Min(horizontal, vertical);
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return true;
        }

        private static float EdgeDistance(float direction, float center, float min, float max)
        {
            if (Mathf.Abs(direction) < .000001f) return float.PositiveInfinity;
            return ((direction > 0 ? max : min) - center) / direction;
        }
    }
}

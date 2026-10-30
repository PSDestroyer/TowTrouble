using UnityEngine;

namespace TowTrouble
{
    // Physics data lives beside the collider; it does not depend on a visual asset.
    [DisallowMultipleComponent]
    public sealed class RecoveryMudTerrain : MonoBehaviour
    {
        [SerializeField,Range(.1f,1.5f)] private float longitudinalGrip=.78f;
        [SerializeField,Range(.1f,1.5f)] private float lateralGrip=.60f;
        [SerializeField,Range(0,1000)] private float resistancePerWheel=375f;
        [SerializeField,Range(0,.8f)] private float aggressiveThrottleLoss=.42f;
        public float LongitudinalGrip=>longitudinalGrip;
        public float LateralGrip=>lateralGrip;
        public float ResistancePerWheel=>resistancePerWheel;
        public float AggressiveThrottleLoss=>aggressiveThrottleLoss;
    }
}

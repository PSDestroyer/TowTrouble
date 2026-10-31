using UnityEngine;
namespace TowTrouble
{
    // Authored, sanitized Hot Rod assembly. Wheel transforms are separated from visual sockets.
    public sealed class RecoveryAssembly : MonoBehaviour
    {
        public string Family;
        public Transform[] Wheels;
        public Vector3[] Anchors;
        public float Radius;
        public float[] Radii;
        public Vector3 ColliderCenter,ColliderSize;
        public Vector3 FrontHitch,RearHitch;
    }
}

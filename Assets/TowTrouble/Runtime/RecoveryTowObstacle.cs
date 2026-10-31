using UnityEngine;
namespace TowTrouble
{
    // Scene-owned obstacle, simulated by the server and included in recovery snapshots.
    public sealed class RecoveryTowObstacle : MonoBehaviour
    {
        public int SiteIndex;
        public Rigidbody Body;
        public Vector3 Home,Side;
        public Quaternion HomeRotation;
        public Vector3 Hitch=>Body.worldCenterOfMass+Vector3.up*.15f;
        public static bool IsClear(Vector3 displacement,Vector3 side)
            =>Mathf.Abs(Vector3.Dot(displacement,side))>=4.5f&&displacement.sqrMagnitude>=25;
        public bool Cleared=>IsClear(transform.position-Home,Side);
        public void ResetObstacle()
        {Body.isKinematic=true;Body.position=Home;Body.rotation=HomeRotation;transform.SetPositionAndRotation(Home,HomeRotation);}
    }
}

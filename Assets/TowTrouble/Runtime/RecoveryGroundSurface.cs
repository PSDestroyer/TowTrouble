using UnityEngine;
namespace TowTrouble
{
    public enum RecoveryGroundKind { Rock, Sand, Snow }
    // Local contact data on the actual road collider, independent of render materials.
    public sealed class RecoveryGroundSurface : MonoBehaviour
    {
        [SerializeField] private RecoveryGroundKind kind;
        [SerializeField] private bool terrain;
        [SerializeField,Range(0,1)] private float softness;
        public float Softness=>softness;
        public void ConfigureSoftness(float value){softness=Mathf.Clamp01(value);}
        [SerializeField] private RecoveryGroundMap surfaceMap;
        public RecoveryGroundKind Kind=>kind;
        public void ConfigureTerrain(){terrain=true;}
        public void ConfigureMap(RecoveryGroundMap map){surfaceMap=map;}
        public RecoveryGroundKind KindAt(Vector3 p){if(surfaceMap!=null)return surfaceMap.Sample(p);if(!terrain)return kind;float u=p.x/900+.5f,v=.5f-p.z/900;return p.y>124&&v<.3f?RecoveryGroundKind.Snow:u>.70f||v>.85f?RecoveryGroundKind.Sand:RecoveryGroundKind.Rock;}
        public float Grip=>kind==RecoveryGroundKind.Snow?.52f:kind==RecoveryGroundKind.Sand?.68f:1.1f;
        public void Configure(RecoveryGroundKind value){kind=value;}
    }
}

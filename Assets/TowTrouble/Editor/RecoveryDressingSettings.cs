using UnityEngine;
namespace TowTrouble.Editor
{
    // Editor-only recipe; the scene contains static prefab instances and native LODGroups.
    public sealed class RecoveryDressingSettings : ScriptableObject
    {
        public int Seed=611021;
        [Range(.25f,2)] public float Density=1;
        [Range(100,3000)] public int MaximumProps=1400;
        [Range(40,200)] public float FoliageDistance=110;
        [Range(60,300)] public float StoneDistance=160;
        public bool RoadsideClusters=true,LandscapeClusters=true,StoryClusters=true;
        [Range(.15f,.5f)] public float WheelBankHeight=.26f;
    }
}

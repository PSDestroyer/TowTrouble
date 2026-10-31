using UnityEngine;

namespace TowTrouble
{
    // Visual response only; independent of the traction/physics surface enum.
    public enum RecoveryTrackKind { Dirt, Sand, Snow, Grass, Hard, Wood }

    [DisallowMultipleComponent]
    public sealed class RecoveryTrackSurface : MonoBehaviour
    {
        [SerializeField] private RecoveryTrackKind kind = RecoveryTrackKind.Hard;
        public RecoveryTrackKind Kind => kind;
        public void Configure(RecoveryTrackKind value) { kind = value; }

        public static RecoveryTrackKind FromPaint(float grass, float dirt, float sand, float rock, float snow)
        {
            float best = grass; var result = RecoveryTrackKind.Grass;
            if(dirt > best) { best=dirt; result=RecoveryTrackKind.Dirt; }
            if(sand > best) { best=sand; result=RecoveryTrackKind.Sand; }
            if(rock > best) { best=rock; result=RecoveryTrackKind.Hard; }
            if(snow > best) result=RecoveryTrackKind.Snow;
            return result;
        }

        // Raised shoulders imply a shallow depression without excavating shared TerrainData.
        public static float Relief(RecoveryTrackKind value)
        {
            switch(value)
            {
                case RecoveryTrackKind.Dirt: return .018f;
                case RecoveryTrackKind.Sand: return .035f;
                case RecoveryTrackKind.Snow: return .048f;
                case RecoveryTrackKind.Grass: return .004f;
                default: return 0;
            }
        }
    }
}

using System;
using UnityEngine;

namespace TowTrouble
{
    // Authored from the terrain paint. Shared by terrain and road colliders so
    // visible sand/snow and tire grip agree, without reading textures per wheel.
    public sealed class RecoveryGroundMap : ScriptableObject
    {
        [SerializeField] private Vector2 origin;
        [SerializeField] private Vector2 size;
        [SerializeField] private int resolution;
        [SerializeField] private byte[] cells = Array.Empty<byte>();
        [SerializeField] private byte[] wheelImpressions = Array.Empty<byte>();

        public void Configure(Vector2 worldOrigin, Vector2 worldSize, int gridSize, byte[] values)
        {
            if (gridSize < 2 || worldSize.x <= 0 || worldSize.y <= 0 || values == null || values.Length != gridSize * gridSize)
                throw new ArgumentException("Invalid ground map dimensions.");
            origin = worldOrigin; size = worldSize; resolution = gridSize; cells = (byte[])values.Clone();wheelImpressions=Array.Empty<byte>();
        }
        public void ConfigureWheelImpressions(byte[] values)
        {
            if(values==null||values.Length!=resolution*resolution)throw new ArgumentException("Invalid wheel impression coverage.");
            wheelImpressions=(byte[])values.Clone();
        }
        public bool CanLeaveWheelImpression(Vector3 point)
        {
            if(resolution<2||wheelImpressions==null||wheelImpressions.Length!=resolution*resolution)return false;
            float u=(point.x-origin.x)/size.x,v=(point.z-origin.y)/size.y;
            if(u<0||v<0||u>1||v>1)return false;
            int x=Mathf.Clamp(Mathf.RoundToInt(u*(resolution-1)),0,resolution-1),z=Mathf.Clamp(Mathf.RoundToInt(v*(resolution-1)),0,resolution-1);
            return wheelImpressions[z*resolution+x]!=0;
        }
        public RecoveryGroundKind Sample(Vector3 point)
        {
            if (resolution < 2 || cells == null || cells.Length != resolution * resolution) return RecoveryGroundKind.Rock;
            float u = (point.x - origin.x) / size.x, v = (point.z - origin.y) / size.y;
            int x = Mathf.Clamp(Mathf.RoundToInt(u * (resolution - 1)), 0, resolution - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt(v * (resolution - 1)), 0, resolution - 1);
            return (RecoveryGroundKind)cells[z * resolution + x];
        }
    }
}

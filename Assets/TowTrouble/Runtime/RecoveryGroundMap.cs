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

        [SerializeField] private byte[] trackKinds = Array.Empty<byte>();
        public int Resolution => resolution;

        public void ConfigureTrackKinds(byte[] values)
        {
            if(values==null||values.Length!=resolution*resolution||Array.Exists(values,v=>v>(byte)RecoveryTrackKind.Wood))
                throw new ArgumentException("Invalid wheel track surface map.");
            trackKinds=(byte[])values.Clone();
        }
        public bool TryTrackKind(Vector3 point,out RecoveryTrackKind kind)
        {
            kind=RecoveryTrackKind.Hard;
            if(resolution<2||trackKinds==null||trackKinds.Length!=resolution*resolution)return false;
            float u=(point.x-origin.x)/size.x,v=(point.z-origin.y)/size.y;
            if(u<0||v<0||u>1||v>1)return false;
            int x=Mathf.Clamp(Mathf.RoundToInt(u*(resolution-1)),0,resolution-1),z=Mathf.Clamp(Mathf.RoundToInt(v*(resolution-1)),0,resolution-1);
            kind=(RecoveryTrackKind)trackKinds[z*resolution+x];return true;
        }

        // Smooth visual coverage only: traction still samples the original discrete cells.
        public bool TryTrackBlend(Vector3 point, RecoveryTrackKind kind, out float weight, out int mask)
        {
            weight=0;mask=0;
            if(resolution<2||trackKinds==null||trackKinds.Length!=resolution*resolution)return false;
            float u=(point.x-origin.x)/size.x,v=(point.z-origin.y)/size.y;
            if(u<0||v<0||u>1||v>1)return false;
            float x=u*(resolution-1),z=v*(resolution-1);int x0=Mathf.FloorToInt(x),z0=Mathf.FloorToInt(z);
            int x1=Mathf.Min(x0+1,resolution-1),z1=Mathf.Min(z0+1,resolution-1);
            float tx=Mathf.SmoothStep(0,1,x-x0),tz=Mathf.SmoothStep(0,1,z-z0);
            int a=trackKinds[z0*resolution+x0],b=trackKinds[z0*resolution+x1],c=trackKinds[z1*resolution+x0],d=trackKinds[z1*resolution+x1];
            mask=(1<<a)|(1<<b)|(1<<c)|(1<<d);int wanted=(int)kind;
            weight=Mathf.Lerp(Mathf.Lerp(a==wanted?1:0,b==wanted?1:0,tx),Mathf.Lerp(c==wanted?1:0,d==wanted?1:0,tx),tz);
            return true;
        }

        public void Configure(Vector2 worldOrigin, Vector2 worldSize, int gridSize, byte[] values)
        {
            if (gridSize < 2 || worldSize.x <= 0 || worldSize.y <= 0 || values == null || values.Length != gridSize * gridSize)
                throw new ArgumentException("Invalid ground map dimensions.");
            origin = worldOrigin; size = worldSize; resolution = gridSize; cells = (byte[])values.Clone();wheelImpressions=Array.Empty<byte>();trackKinds=Array.Empty<byte>();
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

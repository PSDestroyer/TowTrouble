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
        [SerializeField,HideInInspector] private int coverResolution;
        [SerializeField,HideInInspector] private Vector4 coverBounds;
        [SerializeField,HideInInspector] private float[] coverHeights;
        // Soft-ground render skins deliberately sit above their support collider.
        // Cache their regular authored grid once, rather than reading Mesh.vertices per wheel.
        public void ConfigureVisualCover(Mesh visual,Mesh support)
        {
            coverResolution=0;coverHeights=null;
            if(visual==null||support==null||visual==support)return;
            int n=Mathf.RoundToInt(Mathf.Sqrt(visual.vertexCount));
            if(n*n!=visual.vertexCount||support.vertexCount!=visual.vertexCount)return;
            var a=visual.vertices;var b=support.vertices;
            if(Mathf.Abs(a[n-1].x-a[0].x)<1||Mathf.Abs(a[n*(n-1)].z-a[0].z)<1)return;
            coverResolution=n;coverBounds=new Vector4(a[0].x,a[0].z,a[n-1].x,a[n*(n-1)].z);
            coverHeights=new float[a.Length];for(int i=0;i<a.Length;i++)coverHeights[i]=a[i].y-b[i].y;
        }
        public Vector3 VisualPoint(Vector3 supportPoint)
        {
            if(coverResolution<2||coverHeights==null||coverHeights.Length!=coverResolution*coverResolution)return supportPoint;
            var p=transform.InverseTransformPoint(supportPoint);
            float u=Mathf.InverseLerp(coverBounds.x,coverBounds.z,p.x)*(coverResolution-1),v=Mathf.InverseLerp(coverBounds.y,coverBounds.w,p.z)*(coverResolution-1);
            int x=Mathf.Min(coverResolution-2,Mathf.FloorToInt(u)),z=Mathf.Min(coverResolution-2,Mathf.FloorToInt(v));float tx=u-x,tz=v-z;
            p.y+=Mathf.Lerp(Mathf.Lerp(coverHeights[z*coverResolution+x],coverHeights[z*coverResolution+x+1],tx),Mathf.Lerp(coverHeights[(z+1)*coverResolution+x],coverHeights[(z+1)*coverResolution+x+1],tx),tz);
            return transform.TransformPoint(p);
        }
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

        // Visible loose-material banks; the shader adds the recessed bed and tread normals.
        public static float Relief(RecoveryTrackKind value)
        {
            switch(value)
            {
                case RecoveryTrackKind.Dirt: return .060f;
                case RecoveryTrackKind.Sand: return .085f;
                case RecoveryTrackKind.Snow: return .115f;
                case RecoveryTrackKind.Grass: return .004f;
                default: return 0;
            }
        }
    }
}

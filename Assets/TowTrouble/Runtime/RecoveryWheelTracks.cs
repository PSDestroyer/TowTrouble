using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace TowTrouble
{
    /// <summary>Local, bounded wheel impressions. Does not alter shared TerrainData or network physics.</summary>
    [DefaultExecutionOrder(120)]
    public sealed class RecoveryWheelTracks : MonoBehaviour
    {
        [SerializeField] private RecoveryWorld world;
        [SerializeField] private Terrain ground;
        [SerializeField] private RecoveryGroundMap surfaceMap;
        [SerializeField] private Collider[] roadSurfaces;
        [SerializeField] private Material[] materials;
        [SerializeField] private Bounds[] bridgeExclusions;
        private const int SegmentsPerChunk=128,MaxChunks=36,Columns=7;
        private readonly List<Chunk> chunks=new List<Chunk>();
        private readonly Chunk[] active=new Chunk[3];
        private readonly Vector3[] previous=new Vector3[12];
        private readonly int[] previousKind=new int[12],versions=new int[3];
        private readonly bool[] connected=new bool[12];
        private readonly float[] travel=new float[12];
        private HashSet<Collider> roads;
        private RecoveryVehicle[] vehicles;
        private int recycle;
        private bool wasLobby;
        private static readonly float[] Profile={0,.95f,.12f,0,.12f,.95f,0};
        public void Configure(RecoveryWorld value,Terrain terrain,RecoveryGroundMap map,Collider[] colliders,Material[] skins,Bounds[] bridges)
        {world=value;ground=terrain;surfaceMap=map;roadSurfaces=colliders;materials=skins;bridgeExclusions=bridges;}
        private void Awake()
        {
            if(world==null||ground==null||materials==null||materials.Length!=3){enabled=false;return;}
            roads=new HashSet<Collider>(roadSurfaces??System.Array.Empty<Collider>());
            vehicles=new[]{world.Drivers[0],world.Drivers[1],world.Casualty};
        }
        // Teleports, wheel changes and surface gaps must never connect unrelated impressions.
        public static bool CanConnect(Vector3 a,Vector3 b,int previousSurface,int surface)
        {float d=(a-b).sqrMagnitude;return previousSurface==surface&&d>=.09f&&d<=6.25f&&Mathf.Abs(a.y-b.y)<1.2f;}
        private void LateUpdate()
        {
            if(!Application.isPlaying||vehicles==null)return;
            bool lobby=world.Phase==RecoveryPhase.Lobby;
            if(lobby){if(!wasLobby)Release();wasLobby=true;return;}wasLobby=false;
            for(int v=0;v<vehicles.Length;v++)
            {
                var car=vehicles[v];bool ready=car!=null&&car.isActiveAndEnabled;
                if(ready&&versions[v]!=car.GeometryVersion){versions[v]=car.GeometryVersion;for(int w=0;w<4;w++)connected[v*4+w]=false;}
                for(int w=0;w<4;w++)
                {
                    int key=v*4+w;
                    if(!ready||!car.TryGetWheelContact(w,out var hit)||!TrySurface(hit,out int kind)) {connected[key]=false;continue;}
                    var point=hit.point;point.y=ground.SampleHeight(point)+ground.transform.position.y;
                    if(connected[key]&&(point-previous[key]).sqrMagnitude<.09f)continue;
                    if(connected[key]&&CanConnect(previous[key],point,previousKind[key],kind))
                    {float distance=Vector3.Distance(previous[key],point);GetChunk(kind).Append(previous[key],point,ground,kind,travel[key],travel[key]+distance);travel[key]+=distance;}
                    else travel[key]=0;
                    previous[key]=point;previousKind[key]=kind;connected[key]=true;
                }
            }
            foreach(var chunk in chunks)if(chunk.Dirty)chunk.Upload();
        }
        private bool TrySurface(RaycastHit hit,out int kind)
        {
            kind=0;
            if(bridgeExclusions!=null)foreach(var b in bridgeExclusions)if(b.Contains(hit.point))return false;
            var surface=surfaceMap!=null?surfaceMap.Sample(hit.point):RecoveryGroundKind.Rock;
            kind=surface==RecoveryGroundKind.Snow?2:surface==RecoveryGroundKind.Sand?1:0;
            if(roads.Contains(hit.collider))return true;
            // Living Mud owns its own footprints. Do not stamp buildings, vehicles, rocks or water.
            return hit.collider is TerrainCollider&&(kind!=0||surfaceMap!=null&&surfaceMap.CanLeaveWheelImpression(hit.point));
        }
        private Chunk GetChunk(int kind)
        {
            var chunk=active[kind];if(chunk!=null&&chunk.Count<SegmentsPerChunk)return chunk;
            if(chunks.Count<MaxChunks){chunk=new Chunk(transform,materials[kind]);chunks.Add(chunk);}
            else
            {
                chunk=chunks[recycle++%MaxChunks];for(int i=0;i<active.Length;i++)if(active[i]==chunk)active[i]=null;
                chunk.Reset(materials[kind]);
            }
            active[kind]=chunk;return chunk;
        }
        private void OnDisable(){System.Array.Clear(connected,0,connected.Length);}
        private void OnDestroy(){Release();}
        private void Release()
        {foreach(var c in chunks)c.Dispose();chunks.Clear();System.Array.Clear(active,0,active.Length);System.Array.Clear(connected,0,connected.Length);recycle=0;}
        private sealed class Chunk
        {
            public int Count;public bool Dirty;
            private readonly GameObject go;private readonly Mesh mesh;private readonly MeshRenderer renderer;
            private readonly Vector3[] vertices=new Vector3[SegmentsPerChunk*Columns*2],normals=new Vector3[SegmentsPerChunk*Columns*2];
            private readonly Vector2[] uv=new Vector2[SegmentsPerChunk*Columns*2],rut=new Vector2[SegmentsPerChunk*Columns*2];
            private readonly Color[] colors=new Color[SegmentsPerChunk*Columns*2];
            private readonly int[] triangles=new int[SegmentsPerChunk*(Columns-1)*6];
            private Bounds bounds;
            public Chunk(Transform parent,Material material)
            {
                go=new GameObject("Wheel impressions (pooled)",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
                mesh=new Mesh{name="Bounded wheel rut strip"};mesh.MarkDynamic();go.GetComponent<MeshFilter>().sharedMesh=mesh;
                renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
                for(int segment=0;segment<SegmentsPerChunk;segment++)for(int c=0;c<Columns-1;c++)
                {int a=segment*Columns*2+c,t=(segment*(Columns-1)+c)*6;triangles[t]=a;triangles[t+1]=a+Columns;triangles[t+2]=a+1;triangles[t+3]=a+1;triangles[t+4]=a+Columns;triangles[t+5]=a+Columns+1;}
            }
            public void Reset(Material material){Count=0;mesh.Clear();renderer.sharedMaterial=material;}
            public void Append(Vector3 a,Vector3 b,Terrain terrain,int kind,float arcA,float arcB)
            {
                var direction=(b-a).normalized;var right=Vector3.Cross(Vector3.up,direction).normalized;
                float halfWidth=kind==2?.30f:.27f,depth=kind==2?.048f:kind==1?.035f:.024f;
                for(int row=0;row<2;row++)for(int c=0;c<Columns;c++)
                {
                    var p=(row==0?a:b)+right*((c/6f*2-1)*halfWidth);var n=terrain.terrainData.GetInterpolatedNormal((p.x-terrain.transform.position.x)/terrain.terrainData.size.x,(p.z-terrain.transform.position.z)/terrain.terrainData.size.z);
                    p.y=terrain.SampleHeight(p)+terrain.transform.position.y+.012f+Profile[c]*depth;
                    int index=Count*Columns*2+row*Columns+c;vertices[index]=go.transform.InverseTransformPoint(p);
                    float derivative=(Profile[Mathf.Min(c+1,6)]-Profile[Mathf.Max(c-1,0)])*depth/(halfWidth*2/3);
                    normals[index]=(n-right*derivative).normalized;uv[index]=new Vector2(p.x,p.z)*(kind==0?.2f:.125f);rut[index]=new Vector2(c/6f,(row==0?arcA:arcB));colors[index]=new Color(1,1,1,c==0||c==6?0:1);
                    if(Count==0&&row==0&&c==0)bounds=new Bounds(vertices[index],Vector3.one*.1f);else bounds.Encapsulate(vertices[index]);
                }
                Count++;Dirty=true;
            }
            public void Upload()
            {mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.uv2=rut;mesh.colors=colors;mesh.SetTriangles(triangles,0,Count*(Columns-1)*6,0,false);mesh.bounds=bounds;Dirty=false;}
            public void Dispose(){Object.Destroy(go);Object.Destroy(mesh);}
        }
    }
}

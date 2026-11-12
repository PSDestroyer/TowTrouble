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
        private const int SegmentsPerChunk=128,MaxChunks=36,Columns=7,SurfaceCount=6;
        private const float TrackSampleSpacing = .42f;
        private readonly List<Chunk> chunks=new List<Chunk>();
        private readonly Chunk[] active=new Chunk[SurfaceCount];
        private readonly RaycastHit[] previous=new RaycastHit[12];
        private readonly int[] previousKind=new int[12],versions=new int[3];
        private readonly bool[] connected=new bool[12];
        private readonly float[] travel=new float[12];
        private readonly Tail[,] tails=new Tail[12,SurfaceCount];
        private struct Tail { public Chunk Chunk;public int Segment,Generation; }
        private HashSet<Collider> roads;
        private RecoveryVehicle[] vehicles;
        private int recycle;
        private bool wasLobby;
        private static readonly float[] Profile={0,.95f,.12f,0,.12f,.95f,0};
        public void Configure(RecoveryWorld value,Terrain terrain,RecoveryGroundMap map,Collider[] colliders,Material[] skins)
        {world=value;ground=terrain;surfaceMap=map;roadSurfaces=colliders;materials=skins;roads=new HashSet<Collider>(roadSurfaces??System.Array.Empty<Collider>());}
        private void Awake()
        {
            if(world==null||materials==null||materials.Length!=SurfaceCount){enabled=false;return;}
            roads=new HashSet<Collider>(roadSurfaces??System.Array.Empty<Collider>());
            vehicles=new[]{world.Drivers[0],world.Drivers[1],world.Casualty};
        }
        // Teleports, wheel changes and surface gaps must never connect unrelated impressions.
        public static bool CanConnect(Vector3 a,Vector3 b,int previousSurface,int surface)
        {float d=(a-b).sqrMagnitude;return previousSurface>=0&&previousSurface<SurfaceCount&&surface>=0&&surface<SurfaceCount&&d>=.09f&&d<=6.25f&&Mathf.Abs(a.y-b.y)<1.2f;}
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
                    float strength=kind>=(int)RecoveryTrackKind.Hard?Mathf.Lerp(.45f,1,car.SlipAtWheel(w)):1;
                    RecordContact(key,hit,kind,strength);
                }
            }
            foreach(var chunk in chunks)if(chunk.Dirty)chunk.Upload();
        }
        private RecoveryGroundMap PaintMap(RaycastHit hit)
        {
            var c=hit.collider;
            return c!=null&&!c.TryGetComponent<RecoveryTrackSurface>(out _)&&
                (c is TerrainCollider||roads!=null&&roads.Contains(c))?surfaceMap:null;
        }
        private static int SurfaceMask(RecoveryGroundMap map,Vector3 point,int fallback)
        {return map!=null&&map.TryTrackBlend(point,(RecoveryTrackKind)fallback,out _,out int mask)?mask:1<<fallback;}
        // Shared by runtime sampling and disposable Editor contact previews.
        private void RecordContact(int key,RaycastHit hit,int kind,float strength)
        {
            if(connected[key]&&(hit.point-previous[key].point).sqrMagnitude<.09f)return;
            if(connected[key]&&CanConnect(previous[key].point,hit.point,previousKind[key],kind))
            {
                var start = previous[key];
                var a = start;
                int kindA = previousKind[key];
                int steps = Mathf.CeilToInt(Vector3.Distance(start.point, hit.point) / TrackSampleSpacing);
                bool hasStart = true;
                for (int i = 1; i <= steps; i++)
                {
                    var b = hit;
                    int kindB = kind;
                    if (i < steps)
                    {
                        float t = i / (float)steps;
                        var hint = Vector3.Lerp(start.point, hit.point, t);
                        var normal = Vector3.Lerp(start.normal, hit.normal, t).normalized;
                        if ((!RaycastContact(start.collider, hint, normal, .35f, out b) &&
                             !RaycastContact(hit.collider, hint, normal, .35f, out b)) || !TrySurface(b, out kindB))
                        {
                            BreakStrip(key);
                            hasStart = false;
                            continue;
                        }
                    }
                    if (hasStart && (a.point - b.point).sqrMagnitude < .75f * .75f)
                        AppendSpan(key, a, b, kindA, kindB, strength);
                    else BreakStrip(key);
                    a = b;
                    kindA = kindB;
                    hasStart = true;
                }
            }
            else BreakStrip(key);
            previous[key]=hit;previousKind[key]=kind;connected[key]=true;
        }
        private void BreakStrip(int key)
        {
            travel[key] = 0;
            for (int skin = 0; skin < SurfaceCount; skin++) tails[key, skin] = default;
        }
        private void AppendSpan(int key, RaycastHit a, RaycastHit b, int kindA, int kindB, float strength)
        {
            float distance = Vector3.Distance(a.point, b.point);
            var mapA = PaintMap(a);
            var mapB = PaintMap(b);
            var side = Vector3.Cross((a.normal + b.normal).normalized, (b.point - a.point).normalized) * .30f;
            int mask = SurfaceMask(mapA, a.point, kindA) | SurfaceMask(mapB, b.point, kindB) |
                       SurfaceMask(mapA, a.point - side, kindA) | SurfaceMask(mapA, a.point + side, kindA) |
                       SurfaceMask(mapB, b.point - side, kindB) | SurfaceMask(mapB, b.point + side, kindB);
            for (int skin = 0; skin < SurfaceCount; skin++)
            {
                if ((mask & (1 << skin)) == 0) { tails[key, skin] = default; continue; }
                var chunk = GetChunk(skin);
                if (!chunk.AppendBlended(a, b, skin, travel[key], travel[key] + distance, strength, mapA, mapB, kindA, kindB, true))
                { tails[key, skin] = default; continue; }
                var tail = tails[key, skin];
                if (tail.Chunk != null) tail.Chunk.OpenEnd(tail.Segment, tail.Generation);
                tails[key, skin] = new Tail { Chunk = chunk, Segment = chunk.Count - 1, Generation = chunk.Generation };
            }
            travel[key] += distance;
        }
        public bool TrySurface(RaycastHit hit,out int kind)
        {
            kind=(int)RecoveryTrackKind.Hard;
            var collider=hit.collider;
            if(collider==null||collider.isTrigger||collider.GetComponentInParent<RecoveryVehicle>()!=null)return false;
            // Living Mud owns this collider's stamps/coating. Never double-stamp it.
            if(collider.TryGetComponent<RecoveryMudTerrain>(out var mud)&&mud.OwnsTracks)return false;
            if(collider.TryGetComponent<RecoveryTrackSurface>(out var marked)){kind=(int)marked.Kind;return true;}
            bool terrain=collider is TerrainCollider;
            if((terrain||roads!=null&&roads.Contains(collider))&&surfaceMap!=null&&surfaceMap.TryTrackKind(hit.point,out var painted))
            {kind=(int)painted;return true;}
            if(collider.TryGetComponent<RecoveryGroundSurface>(out var surface))
            {
                var value=surface.KindAt(hit.point);
                kind=(int)(value==RecoveryGroundKind.Snow?RecoveryTrackKind.Snow:value==RecoveryGroundKind.Sand?RecoveryTrackKind.Sand:terrain?RecoveryTrackKind.Grass:RecoveryTrackKind.Hard);
            }
            else if(terrain)kind=(int)RecoveryTrackKind.Grass;
            return true;
        }
        // Project onto the actual contact collider, not the terrain below a deck or rock.
        public static bool ProjectContact(Collider surface,Vector3 hint,Vector3 normal,out Vector3 point,out Vector3 up)
        {
            point=hint;up=normal;
            if (!RaycastContact(surface, hint, normal, .65f, out var hit)) return false;
            point = hit.point;
            up = hit.normal;
            if (surface.TryGetComponent<RecoveryTrackSurface>(out var skin)) point = skin.VisualPoint(point);
            return true;
        }
        private static bool RaycastContact(Collider surface, Vector3 hint, Vector3 normal, float range, out RaycastHit hit)
        {
            hit = default;
            if (surface == null) return false;
            var axis = surface is TerrainCollider ? Vector3.up : normal;
            return surface.Raycast(new Ray(hint + axis * range, -axis), out hit, range * 2) &&
                   Vector3.Dot(hit.normal, normal) > .35f;
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
        {foreach(var c in chunks)c.Dispose();chunks.Clear();System.Array.Clear(active,0,active.Length);System.Array.Clear(connected,0,connected.Length);System.Array.Clear(tails,0,tails.Length);recycle=0;}
        private sealed class Chunk
        {
            public int Count,Generation;public bool Dirty;
            private readonly GameObject go;private readonly Mesh mesh;private readonly MeshRenderer renderer;
            private readonly Vector3[] vertices=new Vector3[SegmentsPerChunk*Columns*2],normals=new Vector3[SegmentsPerChunk*Columns*2];
            private readonly Vector2[] uv=new Vector2[SegmentsPerChunk*Columns*2],rut=new Vector2[SegmentsPerChunk*Columns*2];
            private readonly Vector2[] ends=new Vector2[SegmentsPerChunk*Columns*2];
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
            public void Reset(Material material){Generation++;Count=0;Dirty=false;mesh.Clear();renderer.sharedMaterial=material;}
            public bool Append(RaycastHit a,RaycastHit b,int kind,float arcA,float arcB,float strength)
            {return AppendBlended(a,b,kind,arcA,arcB,strength,null,null,kind,kind,false);}
            public void OpenEnd(int segment,int generation)
            {
                if(generation!=Generation||segment<0||segment>=Count)return;
                for(int i=segment*Columns*2;i<(segment+1)*Columns*2;i++)ends[i]=Vector2.one;
                Dirty=true;
            }
            public bool AppendBlended(RaycastHit a,RaycastHit b,int kind,float arcA,float arcB,float strength,
                RecoveryGroundMap mapA,RecoveryGroundMap mapB,int kindA,int kindB,bool capEnd)
            {
                var normal=(a.normal+b.normal).normalized;var direction=(b.point-a.point).normalized;var right=Vector3.Cross(normal,direction).normalized;
                if(right.sqrMagnitude<.1f)return false;
                var midpoint=(a.point+b.point)*.5f;
                if ((!RaycastContact(a.collider, midpoint, normal, .11f, out _) &&
                     !RaycastContact(b.collider, midpoint, normal, .11f, out _))) return false;
                float halfWidth=kind==2?.30f:.27f,depth=RecoveryTrackSurface.Relief((RecoveryTrackKind)kind);
                for(int row=0;row<2;row++)for(int c=0;c<Columns;c++)
                {
                    var contact=row==0?a:b;var hint=contact.point+right*((c/6f*2-1)*halfWidth);
                    bool supported=ProjectContact(contact.collider,hint,contact.normal,out var p,out var n);
                    p+=n*(.008f+Profile[c]*depth);
                    int index=Count*Columns*2+row*Columns+c;vertices[index]=go.transform.InverseTransformPoint(p);
                    float derivative=(Profile[Mathf.Min(c+1,6)]-Profile[Mathf.Max(c-1,0)])*depth/(halfWidth*2/3);
                    normals[index]=go.transform.InverseTransformDirection((n-right*derivative).normalized);uv[index]=new Vector2(p.x,p.z)*(kind==0?.2f:.125f);rut[index]=new Vector2(c/6f,(row==0?arcA:arcB));
                    var map=row==0?mapA:mapB;float weight=(row==0?kindA:kindB)==kind?1:0;
                    if(map!=null&&map.TryTrackBlend(hint,(RecoveryTrackKind)kind,out float painted,out _))weight=painted;
                    colors[index]=new Color(1,1,1,!supported||c==0||c==6?0:strength*weight);
                    ends[index]=capEnd?new Vector2(row==0?Mathf.Max(1,(arcB-arcA)/.4f):0,1):Vector2.one;
                    if(Count==0&&row==0&&c==0)bounds=new Bounds(vertices[index],Vector3.one*.1f);else bounds.Encapsulate(vertices[index]);
                }
                Count++;Dirty=true;return true;
            }
            public void Upload()
            {mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.uv2=rut;mesh.uv3=ends;mesh.colors=colors;mesh.SetTriangles(triangles,0,Count*(Columns-1)*6,0,false);mesh.bounds=bounds;Dirty=false;}
            public void Dispose(){if(Application.isPlaying){Object.Destroy(go);Object.Destroy(mesh);}else{Object.DestroyImmediate(go);Object.DestroyImmediate(mesh);}}
        }
    }
}

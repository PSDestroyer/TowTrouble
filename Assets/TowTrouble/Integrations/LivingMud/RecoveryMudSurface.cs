using Dinopunch.LivingMud;
using UnityEngine;

namespace TowTrouble
{
    // Living Mud's surface shader and tread texture, with one batched GPU update for
    // all 12 wheels. Visual marks derive from the existing host/client car poses.
    [DefaultExecutionOrder(100)]
    public sealed class RecoveryMudSurface : MonoBehaviour
    {
        [SerializeField] private RecoveryWorld world;
        [SerializeField] private RecoveryVehicle[] vehicles;
        [SerializeField] private DynamicMudSurface surface;
        [SerializeField] private Shader batchStampShader;
        [SerializeField] private Texture2D tread;
        [SerializeField] private int resolution=1024;
        private Renderer mudRenderer;
        private Material authoredMaterial,liveMaterial,stampMaterial;
        private CustomRenderTexture marks;
        private RenderTexture scratch;
        private readonly Vector4[] stamps=new Vector4[12];
        private readonly Vector3[] lastPosition=new Vector3[12];
        private readonly bool[] touched=new bool[12];
        private float lastElapsed;
        private RecoveryPhase previousPhase;
        private bool initialized;
        private RecoveryWheelMud[] wheelCoating;
        private static readonly int StampTexture=Shader.PropertyToID("_Stamp_Render_Texture_1");
        private static readonly int StampCount=Shader.PropertyToID("_StampCount");
        private static readonly int StampData=Shader.PropertyToID("_Stamps");

        public void Configure(RecoveryWorld owner,RecoveryVehicle[] cars,DynamicMudSurface mud,Shader shader,Texture2D tireTexture)
        {world=owner;vehicles=cars;surface=mud;batchStampShader=shader;tread=tireTexture;}
        private void Awake()
        {
            mudRenderer=GetComponent<Renderer>();authoredMaterial=mudRenderer.sharedMaterial;
            liveMaterial=new Material(authoredMaterial){name="Living Mud - session surface"};
            stampMaterial=new Material(batchStampShader){name="Living Mud - multi-wheel stamps"};stampMaterial.SetTexture("_Tread",tread);
            marks=new CustomRenderTexture(resolution,resolution,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
            {name="Living Mud - session wheel tracks",doubleBuffered=true,updateMode=CustomRenderTextureUpdateMode.OnDemand,initializationMode=CustomRenderTextureUpdateMode.OnDemand,initializationSource=CustomRenderTextureInitializationSource.TextureAndColor,initializationColor=Color.clear,material=stampMaterial,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            marks.Create();scratch=new RenderTexture(resolution,resolution,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){name="Living Mud - stamp scratch",wrapMode=TextureWrapMode.Clamp};scratch.Create();liveMaterial.SetTexture(StampTexture,marks);mudRenderer.sharedMaterial=liveMaterial;
            liveMaterial.SetFloat("_Flip_UV",1);surface.RefreshReferences();wheelCoating=new RecoveryWheelMud[vehicles.Length];for(int i=0;i<vehicles.Length;i++)wheelCoating[i]=vehicles[i].GetComponent<RecoveryWheelMud>();initialized=true;Clear();
        }
        private void OnEnable(){if(initialized)Clear();}
        private void OnDisable(){if(wheelCoating!=null)foreach(var coating in wheelCoating)if(coating!=null)coating.ClearMud();}
        private void Clear(){var previous=RenderTexture.active;RenderTexture.active=marks;GL.Clear(false,true,Color.clear);RenderTexture.active=previous;System.Array.Clear(touched,0,touched.Length);if(wheelCoating!=null)foreach(var coating in wheelCoating)if(coating!=null)coating.ClearMud();lastElapsed=0;previousPhase=RecoveryPhase.Lobby;}
        private void LateUpdate()
        {
            if(!initialized||world==null)return;
            if(world.Elapsed+.05f<lastElapsed||(world.Phase==RecoveryPhase.Lobby&&previousPhase!=RecoveryPhase.Lobby))Clear();
            lastElapsed=world.Elapsed;previousPhase=world.Phase;
            if(world.Phase==RecoveryPhase.Lobby||world.Phase==RecoveryPhase.Complete)return;
            var bounds=mudRenderer.bounds;float extent=Mathf.Max(bounds.size.x,bounds.size.z);int count=0;
            for(int car=0;car<vehicles.Length;car++)
            {
                var vehicle=vehicles[car];
                for(int wheel=0;wheel<4;wheel++)
                {
                    int index=car*4+wheel;
                    if(!vehicle.gameObject.activeInHierarchy||!vehicle.TryGetWheelContact(wheel,out var hit)||hit.collider.gameObject!=gameObject)
                    {touched[index]=false;continue;}
                    var position=hit.point;float travelled=touched[index]?Vector3.Distance(position,lastPosition[index]):0;
                    if(wheelCoating[car]!=null)wheelCoating[car].TouchMud(wheel,position,travelled);
                    if(touched[index]&&travelled<.12f)continue;
                    // Resets/teleports start a fresh mark, never draw a line across the map.
                    var forward=transform.InverseTransformDirection(travelled>.12f&&travelled<3f?position-lastPosition[index]:vehicle.transform.forward);float angle=Mathf.Atan2(forward.x,forward.z);
                    // Use the collider triangle UV, matching the DMS shader with Flip UV enabled.
                    var uv=hit.textureCoord;
                    stamps[count++]=new Vector4(uv.x,uv.y,.8f/extent,angle);
                    touched[index]=true;lastPosition[index]=position;
                }
            }
            if(count==0)return;stampMaterial.SetInt(StampCount,count);stampMaterial.SetVectorArray(StampData,stamps);Graphics.Blit(marks,scratch,stampMaterial);Graphics.Blit(scratch,marks);
        }
        private void OnDestroy()
        {
            if(mudRenderer!=null&&authoredMaterial!=null)mudRenderer.sharedMaterial=authoredMaterial;
            if(scratch!=null){scratch.Release();Destroy(scratch);}if(marks!=null){marks.Release();Destroy(marks);}if(liveMaterial!=null)Destroy(liveMaterial);if(stampMaterial!=null)Destroy(stampMaterial);
        }
    }
}

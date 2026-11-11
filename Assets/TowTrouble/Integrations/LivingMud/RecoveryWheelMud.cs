using UnityEngine;

namespace TowTrouble
{
    // Coverage belongs to each wheel; source materials and body paint stay untouched.
    [DefaultExecutionOrder(150)]
    public sealed partial class RecoveryWheelMud : MonoBehaviour
    {
        [SerializeField] private RecoveryVehicle vehicle;
        [SerializeField] private ParticleSystem[] splashes;
        [SerializeField] private RecoveryWorld world;
        [SerializeField] private Texture mudTexture,snowTexture,sandTexture;
        private readonly RecoveryGroundKind[] kinds=new RecoveryGroundKind[4];
        private Vector3 lastPosition;
        private float lastElapsed;
        public void ConfigureGround(RecoveryWorld owner,Texture mud,Texture snow,Texture sand)
        {world=owner;mudTexture=mud;snowTexture=snow;sandTexture=sand;}
        public static Color CoatingColor(RecoveryGroundKind kind)=>kind==RecoveryGroundKind.Snow?new Color(.90f,.95f,1):kind==RecoveryGroundKind.Sand?new Color(.88f,.77f,.57f):new Color(.68f,.53f,.35f);
        private readonly float[] coverage=new float[4];
        private readonly bool[] touching=new bool[4];
        private readonly Renderer[][] renderers=new Renderer[4][];
        private MaterialPropertyBlock properties;
        private readonly System.Collections.Generic.Dictionary<Renderer,int[]> coatingSlots=new System.Collections.Generic.Dictionary<Renderer,int[]>();
        private int model=-1;
        private static readonly int CoatingContrast=Shader.PropertyToID("_CoatingContrast"),CoatingBias=Shader.PropertyToID("_CoatingBias");
        private static readonly int PatternScale=Shader.PropertyToID("_CoatingScale"),Smoothness=Shader.PropertyToID("_Smoothness");
        private static readonly int MudAmount=Shader.PropertyToID("_Mud_Amount");
        private static readonly int MudTint=Shader.PropertyToID("_Mud_Tint"),MudTexture=Shader.PropertyToID("_Mud_Texture");
        public void Configure(RecoveryVehicle owner){vehicle=owner;}
        public void ConfigureSplashes(ParticleSystem[] particles){splashes=particles;}
        public void TouchMud(int wheel,Vector3 point,float distance)
        {TouchSurface(wheel,point,distance,RecoveryGroundKind.Rock);}
        private void TouchSurface(int wheel,Vector3 point,float distance,RecoveryGroundKind kind)
        {
            kinds[wheel]=kind;
            float slip=vehicle.SlipAtWheel(wheel);
            AddMud(wheel,Time.deltaTime*(.25f+slip*.8f)+Mathf.Min(distance,1)*.3f);
        }
        private void Awake(){properties=new MaterialPropertyBlock();BindModel();}
        private void BindModel()
        {
            ClearMud();coatingSlots.Clear();model=vehicle.GeometryVersion;
            for(int wheel=0;wheel<4;wheel++)
            {
                renderers[wheel]=vehicle.WheelVisual(wheel).GetComponentsInChildren<Renderer>(true);
                foreach(var renderer in renderers[wheel])
                {
                    var slots=new System.Collections.Generic.List<int>();var materials=renderer.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)if(materials[i]!=null&&materials[i].HasProperty(MudAmount))slots.Add(i);
                    coatingSlots[renderer]=slots.ToArray();
                }
            }
        }
        public void AddMud(int wheel,float amount)
        {
            if(model!=vehicle.GeometryVersion)BindModel();
            touching[wheel]=true;coverage[wheel]=Mathf.Clamp01(Mathf.Max(.24f,coverage[wheel]+amount));
        }
        public void ClearMud()
        {
            ClearDebris();
            if(splashes!=null)foreach(var spray in splashes)if(spray!=null)spray.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            System.Array.Clear(coverage,0,coverage.Length);System.Array.Clear(touching,0,touching.Length);
            if(properties==null)properties=new MaterialPropertyBlock();
            for(int wheel=0;wheel<4;wheel++)Apply(wheel,0);
        }
        private void Apply(int wheel,float amount)
        {
            if(renderers[wheel]==null)return;
            var kind=kinds[wheel];bool snow=kind==RecoveryGroundKind.Snow,sand=kind==RecoveryGroundKind.Sand;
            // Keep the existing mud path unchanged. Dry dust remains translucent;
            // snow forms larger islands, with rubber visible between deposits.
            float displayAmount=amount*(snow?.65f:sand?.50f:1);
            float scale=snow?65:sand?145:100;
            float smoothness=Mathf.Lerp(.38f,snow?.20f:sand?.08f:.38f,amount);
            var texture=snow?snowTexture:sand?sandTexture:mudTexture;
            foreach(var renderer in renderers[wheel])if(renderer!=null&&coatingSlots.TryGetValue(renderer,out var slots))
                foreach(int slot in slots)
                {
                    renderer.GetPropertyBlock(properties,slot);properties.SetFloat(MudAmount,displayAmount);
                    properties.SetColor(MudTint,CoatingColor(kind));properties.SetFloat(PatternScale,scale);properties.SetFloat(CoatingContrast,snow?Mathf.Lerp(1,5f,amount):1);properties.SetFloat(CoatingBias,snow?-2.2f*amount:0);properties.SetFloat(Smoothness,smoothness);
                    if(texture!=null)properties.SetTexture(MudTexture,texture);renderer.SetPropertyBlock(properties,slot);
                }
        }
        private void LateUpdate()
        {
            if(!Application.isPlaying||Time.timeScale<=.01f)return;
            if(model!=vehicle.GeometryVersion)BindModel();
            if(world!=null&&(world.Phase==RecoveryPhase.Lobby||world.Phase==RecoveryPhase.Complete)){ClearMud();lastPosition=transform.position;return;}
            if((transform.position-lastPosition).sqrMagnitude>100||world!=null&&world.Elapsed+.05f<lastElapsed)ClearMud();
            var movement=transform.position-lastPosition;
            debrisVelocity=Time.deltaTime>.0001f&&movement.sqrMagnitude<=100?movement/Time.deltaTime:Vector3.zero;
            float travel=Mathf.Min(movement.magnitude,1);lastPosition=transform.position;if(world!=null)lastElapsed=world.Elapsed;
            debrisGain=debrisCamera==null?1:RecoveryWheelDebrisRules.DistanceGain(Vector3.Distance(transform.position,debrisCamera.transform.position),RecoveryPlatform.UsesSwitchRendering);
            for(int wheel=0;wheel<4;wheel++)
            {
                if(vehicle.TryGetWheelContact(wheel,out var hit))
                {
                    if(hit.collider.TryGetComponent<RecoveryMudTerrain>(out _))TouchSurface(wheel,hit.point,travel,RecoveryGroundKind.Rock);
                    else if(hit.collider.TryGetComponent<RecoveryGroundSurface>(out var surface))
                    {var kind=surface.KindAt(hit.point);if(kind!=RecoveryGroundKind.Rock)TouchSurface(wheel,hit.point,travel,kind);}
                    ThrowDebris(wheel,hit);
                }
                else ResetDebrisBudget(wheel);
                if(!touching[wheel])coverage[wheel]=RecoveryTraction.CleanCoverage(coverage[wheel],kinds[wheel],vehicle.Speed,Time.deltaTime);
                Apply(wheel,coverage[wheel]);touching[wheel]=false;
            }
        }
        private void OnEnable()
        {
            if(Application.isPlaying)
            {
                debrisCamera=Camera.main;
                bool limited=RecoveryPlatform.UsesSwitchRendering;
                if(powders!=null)foreach(var powder in powders)if(powder!=null)RecoveryWheelDebrisRules.ConfigurePowder(powder,limited);
                if(limited&&splashes!=null)foreach(var spray in splashes)if(spray!=null){var main=spray.main;main.maxParticles=16;}
            }
            if(vehicle!=null)vehicle.ExternalWheelEffects=HasWheelDebris;
        }
        private void OnDisable(){ClearMud();if(vehicle!=null)vehicle.ExternalWheelEffects=false;}
    }
}

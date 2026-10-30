using UnityEngine;

namespace TowTrouble
{
    // Coverage belongs to each wheel; source materials and body paint stay untouched.
    [DefaultExecutionOrder(150)]
    public sealed class RecoveryWheelMud : MonoBehaviour
    {
        [SerializeField] private RecoveryVehicle vehicle;
        [SerializeField] private ParticleSystem[] splashes;
        private readonly float[] splashBudget=new float[4];
        private readonly float[] coverage=new float[4];
        private readonly bool[] touching=new bool[4];
        private readonly Renderer[][] renderers=new Renderer[4][];
        private MaterialPropertyBlock properties;
        private int model=-1;
        private static readonly int MudAmount=Shader.PropertyToID("_Mud_Amount");
        public void Configure(RecoveryVehicle owner){vehicle=owner;}
        public void ConfigureSplashes(ParticleSystem[] particles){splashes=particles;}
        public void TouchMud(int wheel,Vector3 point,float distance)
        {
            float slip=vehicle.SlipAtWheel(wheel);
            AddMud(wheel,Time.deltaTime*(.25f+slip*.8f)+Mathf.Min(distance,1)*.3f);
            if(splashes==null||wheel>=splashes.Length||splashes[wheel]==null)return;
            float rate=vehicle.Speed>.2f||slip>.1f?Mathf.Min(32,vehicle.Speed*3+slip*22):0;
            splashBudget[wheel]=Mathf.Min(3,splashBudget[wheel]+rate*Time.deltaTime);
            int count=(int)splashBudget[wheel];if(count==0)return;splashBudget[wheel]-=count;
            var spray=splashes[wheel];if(!spray.isPlaying)spray.Play();
            float sign=vehicle.ControlInput.y<-.05f?-1:1;
            var particles=new ParticleSystem.EmitParams{position=point+Vector3.up*.06f,velocity=-vehicle.transform.forward*sign*(.5f+slip*2+vehicle.Speed*.15f)+Vector3.up*(.8f+slip)+Random.insideUnitSphere*.25f};
            spray.Emit(particles,count);
        }
        private void Awake(){properties=new MaterialPropertyBlock();BindModel();}
        private void BindModel()
        {
            ClearMud();model=vehicle.ModelIndex;
            for(int wheel=0;wheel<4;wheel++)renderers[wheel]=vehicle.WheelVisual(wheel).GetComponentsInChildren<Renderer>(true);
        }
        public void AddMud(int wheel,float amount)
        {
            if(model!=vehicle.ModelIndex)BindModel();
            touching[wheel]=true;coverage[wheel]=Mathf.Clamp01(Mathf.Max(.24f,coverage[wheel]+amount));
        }
        public void ClearMud()
        {
            System.Array.Clear(splashBudget,0,splashBudget.Length);
            if(splashes!=null)foreach(var spray in splashes)if(spray!=null)spray.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            System.Array.Clear(coverage,0,coverage.Length);System.Array.Clear(touching,0,touching.Length);
            if(properties==null)properties=new MaterialPropertyBlock();
            for(int wheel=0;wheel<4;wheel++)Apply(wheel,0);
        }
        private void Apply(int wheel,float amount)
        {
            if(renderers[wheel]==null)return;
            foreach(var renderer in renderers[wheel])if(renderer!=null){renderer.GetPropertyBlock(properties);properties.SetFloat(MudAmount,amount);renderer.SetPropertyBlock(properties);}
        }
        private void LateUpdate()
        {
            if(model!=vehicle.ModelIndex)BindModel();
            for(int wheel=0;wheel<4;wheel++)
            {
                if(!touching[wheel])coverage[wheel]=Mathf.MoveTowards(coverage[wheel],0,Time.deltaTime*.008f);
                Apply(wheel,coverage[wheel]);touching[wheel]=false;
            }
        }
        private void OnDisable(){ClearMud();}
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace TowTrouble
{
    [DefaultExecutionOrder(140),DisallowMultipleComponent]
    public sealed class RecoveryVehicleEffects : MonoBehaviour
    {
        [SerializeField] private RecoveryVehicle vehicle;
        [SerializeField] private RecoveryVehicleEffectsProfile profile;
        private readonly List<Lamp> lamps=new List<Lamp>();
        private readonly List<ParticleSystem> exhausts=new List<ParticleSystem>();
        private MaterialPropertyBlock block;
        private int geometry=-1;
        private static readonly int EmissionMap=Shader.PropertyToID("_EmissionMap"),EmissionColor=Shader.PropertyToID("_EmissionColor");
        private sealed class Lamp
        {
            public Renderer Renderer;public int Slot;public VehicleEmissionAtlas Atlas;
            public Material Original,Owned;public MaterialPropertyBlock Previous;
        }
        public void Configure(RecoveryVehicle owner,RecoveryVehicleEffectsProfile settings){vehicle=owner;profile=settings;geometry=-1;}
        public static bool ShouldBrake(float throttle,bool brake,float signedSpeed)
        {return brake||(Mathf.Abs(throttle)>.1f&&Mathf.Abs(signedSpeed)>.3f&&throttle*signedSpeed<0);}
        public static float SmokeRate(bool engine,float throttle,float effort,int outlets)
        {return !engine||outlets<=0?0:Mathf.Lerp(4,18,Mathf.Clamp01(Mathf.Max(Mathf.Abs(throttle),effort)))/outlets;}
        public static bool VisiblePart(Transform part,Transform vehicleRoot)
        {for(var t=part;t!=null&&t!=vehicleRoot;t=t.parent)if(!t.gameObject.activeSelf)return false;return true;}
        private void LateUpdate()
        {
            if(vehicle==null||profile==null)return;
            if(geometry!=vehicle.GeometryVersion)Rebind();
            ApplyState(vehicle.BrakeLights,vehicle.EngineRunning,vehicle.ControlInput.y,vehicle.EngineEffort);
        }
        // Also usable by static Editor previews. Never starts particles outside Play Mode.
        public void ApplyState(bool braking,bool running,float throttle,float effort)
        {
            foreach(var lamp in lamps)
            {
                if(lamp.Renderer==null)continue;
                lamp.Renderer.GetPropertyBlock(block,lamp.Slot);
                block.SetTexture(EmissionMap,braking?lamp.Atlas.Braking:lamp.Atlas.Running);
                block.SetColor(EmissionColor,Color.white);lamp.Renderer.SetPropertyBlock(block,lamp.Slot);
            }
            float rate=SmokeRate(running,throttle,effort,exhausts.Count);
            foreach(var ps in exhausts)
            {
                if(ps==null)continue;
                var emission=ps.emission;emission.rateOverTime=rate;
                var main=ps.main;main.startSpeed=Mathf.Lerp(.35f,1.1f,Mathf.Clamp01(Mathf.Abs(throttle)));
                if(Application.isPlaying)
                {
                    if(running&&!ps.isPlaying)ps.Play(false);
                    else if(!running&&ps.isPlaying)ps.Stop(false,ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }
        public void Rebind()
        {
            Clear();if(vehicle==null||profile==null)return;
            if(block==null)block=new MaterialPropertyBlock();
            geometry=vehicle.GeometryVersion;
            foreach(var renderer in vehicle.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!renderer.enabled||!VisiblePart(renderer.transform,vehicle.transform))continue;
                var materials=renderer.sharedMaterials;bool replaced=false;
                for(int slot=0;slot<materials.Length;slot++)
                {
                    var material=materials[slot];if(material==null||!material.HasProperty(EmissionMap))continue;
                    var texture=material.GetTexture(EmissionMap);VehicleEmissionAtlas atlas=null;
                    foreach(var item in profile.Atlases)if(item.Source==texture&&texture!=null){atlas=item;break;}
                    if(atlas==null)continue;
                    var previous=new MaterialPropertyBlock();renderer.GetPropertyBlock(previous,slot);
                    var lamp=new Lamp{Renderer=renderer,Slot=slot,Atlas=atlas,Original=material,Previous=previous};
                    if(!material.IsKeywordEnabled("_EMISSION"))
                    {lamp.Owned=new Material(material){name=material.name+" - vehicle lamps"};lamp.Owned.EnableKeyword("_EMISSION");materials[slot]=lamp.Owned;replaced=true;}
                    lamps.Add(lamp);
                }
                if(replaced)renderer.sharedMaterials=materials;
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null)continue;
                foreach(var ports in profile.Exhausts)
                {
                    if(ports.Mesh!=filter.sharedMesh)continue;
                    for(int i=0;i<ports.Positions.Length&&exhausts.Count<8;i++)CreateExhaust(renderer.transform,ports.Positions[i],ports.Directions[i]);
                    break;
                }
            }
            // Factory bodies without a separate pipe mesh need a stock rear outlet.
            // Measured ports on a fitted exhaust always take precedence.
            if(exhausts.Count==0)
            {
                var custom=vehicle.GetComponent<RecoveryCustomization>();
                Bounds body;
                if(custom!=null)custom.GarageBounds(GarageSlot.Assembly,null,out body,out _);
                else body=new Bounds(new Vector3(0,1,0),new Vector3(2.2f,2,4.5f));
                float rearHeight=vehicle.transform.InverseTransformPoint(vehicle.RearHitch).y;
                float height=Mathf.Max(body.min.y+.35f,rearHeight-.18f);
                var outlet=new Vector3(body.center.x-body.extents.x*.75f,height,body.min.z+.06f);
                CreateExhaust(vehicle.transform,outlet,new Vector3(-.35f,.12f,-1));
            }
            ApplyState(false,false,0,0);
        }
        private void CreateExhaust(Transform parent,Vector3 point,Vector3 direction)
        {
            var go=new GameObject("Exhaust vapor");go.SetActive(false);go.transform.SetParent(parent,false);
            go.transform.localPosition=point+direction.normalized*.012f;go.transform.localRotation=Quaternion.LookRotation(direction);
            var ps=go.AddComponent<ParticleSystem>();var main=ps.main;
            main.playOnAwake=false;main.loop=true;main.duration=1;main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;main.maxParticles=20;
            main.startLifetime=new ParticleSystem.MinMaxCurve(1.1f,1.7f);main.startSize=new ParticleSystem.MinMaxCurve(.17f,.28f);main.startSpeed=.35f;main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            main.startColor=new Color(.42f,.44f,.45f,.50f);main.gravityModifier=-.015f;
            // Gentle world-up drift lets vapor clear the bumper instead of dying
            // inside the car silhouette at normal gameplay camera distance.
            var drift=ps.velocityOverLifetime;drift.enabled=true;drift.space=ParticleSystemSimulationSpace.World;
            drift.x=.06f;drift.y=.35f;drift.z=-.03f;
            var emission=ps.emission;emission.rateOverTime=0;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.025f;
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});color.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,2.5f));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=profile.Smoke;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;
            ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);go.SetActive(true);exhausts.Add(ps);
        }
        private void OnDisable(){Clear();geometry=-1;}
        private void OnDestroy(){Clear();}
        private void Clear()
        {
            foreach(var lamp in lamps)
            {
                if(lamp.Renderer!=null)
                {
                    lamp.Renderer.SetPropertyBlock(lamp.Previous,lamp.Slot);
                    if(lamp.Owned!=null){var values=lamp.Renderer.sharedMaterials;if(lamp.Slot<values.Length&&values[lamp.Slot]==lamp.Owned){values[lamp.Slot]=lamp.Original;lamp.Renderer.sharedMaterials=values;}}
                }
                if(lamp.Owned!=null)Dispose(lamp.Owned);
            }
            lamps.Clear();foreach(var ps in exhausts)if(ps!=null){ps.gameObject.SetActive(false);Dispose(ps.gameObject);}exhausts.Clear();
        }
        private static void Dispose(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}

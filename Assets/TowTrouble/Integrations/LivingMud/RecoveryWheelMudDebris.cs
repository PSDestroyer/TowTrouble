using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryWheelMud
    {
        [SerializeField] private RecoveryWheelTracks trackSurfaces;
        [SerializeField] private ParticleSystem[] powders;
        private readonly float[] splashBudget=new float[4],powderBudget=new float[4];
        private readonly RecoveryWheelDebrisKind[] debrisKinds=new RecoveryWheelDebrisKind[4];
        private Vector3 debrisVelocity;
        private Camera debrisCamera;
        private float debrisGain=1;
        private bool HasWheelDebris=>splashes!=null&&splashes.Length==4&&powders!=null&&powders.Length==4;
        public void ConfigureDebris(RecoveryWheelTracks tracks,ParticleSystem[] fineParticles)
        {trackSurfaces=tracks;powders=fineParticles;}
        private void ResetDebrisBudget(int wheel){splashBudget[wheel]=powderBudget[wheel]=0;}
        private void ClearDebris()
        {
            for(int i=0;i<4;i++)ResetDebrisBudget(i);
            if(powders!=null)foreach(var ps in powders)if(ps!=null)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private RecoveryWheelDebrisKind DebrisSurface(RaycastHit hit,out Vector3 point)
        {
            point=hit.point;
            if(hit.collider.TryGetComponent<RecoveryTrackSurface>(out var skin))point=skin.VisualPoint(point);
            if(hit.collider.TryGetComponent<RecoveryMudTerrain>(out _))return RecoveryWheelDebrisKind.Mud;
            if(trackSurfaces!=null&&trackSurfaces.TrySurface(hit,out int kind))return RecoveryWheelDebrisRules.FromTrack((RecoveryTrackKind)kind);
            if(skin!=null)return RecoveryWheelDebrisRules.FromTrack(skin.Kind);
            if(hit.collider.TryGetComponent<RecoveryGroundSurface>(out var ground))
            {
                var value=ground.KindAt(point);
                if(value==RecoveryGroundKind.Snow)return RecoveryWheelDebrisKind.Snow;
                if(value==RecoveryGroundKind.Sand)return RecoveryWheelDebrisKind.Sand;
            }
            return hit.collider is TerrainCollider?RecoveryWheelDebrisKind.Grass:RecoveryWheelDebrisKind.Stone;
        }
        private void ThrowDebris(int wheel,RaycastHit hit)
        {
            if(debrisGain<=0){ResetDebrisBudget(wheel);return;}
            if(splashes==null||wheel>=splashes.Length||splashes[wheel]==null)return;
            var kind=DebrisSurface(hit,out var point);
            bool packed = false;
            bool winterSnow = vehicle.WinterDriving && WinterDrivingSurface.IsSnow(hit, out packed);
            if (winterSnow) kind = RecoveryWheelDebrisKind.Snow;
            if(debrisKinds[wheel]!=kind){ResetDebrisBudget(wheel);debrisKinds[wheel]=kind;}
            float angular=vehicle.WheelAngularVelocity[wheel],slip=vehicle.SlipAtWheel(wheel),speed=vehicle.Speed;
            float rate=RecoveryWheelDebrisRules.Rate(kind,speed,slip,angular,true)*Mathf.Clamp01(RecoveryVehicle.VisualEffectsGain)*debrisGain;
            float looseSnow = winterSnow ? (packed ? .38f : 1f) : 0f;
            rate *= 1 + looseSnow * (1.35f + .45f * slip);
            float dt=Mathf.Min(Time.deltaTime,.05f);
            splashBudget[wheel]=Mathf.Min(winterSnow?5:3,splashBudget[wheel]+rate*dt);
            int count=(int)splashBudget[wheel];splashBudget[wheel]-=count;
            var normal=hit.normal;var forward=Vector3.Cross(vehicle.WheelVisual(wheel).right,normal).normalized;
            if(forward.sqrMagnitude<.5f)forward=Vector3.ProjectOnPlane(vehicle.transform.forward,normal).normalized;
            // Wheel rates/speed are already replicated. No VFX messages or per-frame allocations.
            var velocity=vehicle.Body!=null&&!vehicle.Body.isKinematic?vehicle.Body.linearVelocity:debrisVelocity;
            var back=RecoveryWheelDebrisRules.ThrowDirection(forward,velocity,angular,vehicle.ControlInput.y);
            var side=Vector3.Cross(normal,forward)*(wheel%2==0?-1:1);
            var color=RecoveryWheelDebrisRules.Color(kind);
            float lift=kind==RecoveryWheelDebrisKind.Snow?1.5f:kind==RecoveryWheelDebrisKind.Grass?.7f:1.1f;
            float size=kind==RecoveryWheelDebrisKind.Mud?.10f:kind==RecoveryWheelDebrisKind.Snow?.09f:kind==RecoveryWheelDebrisKind.Sand?.045f:.05f;
            lift += .55f * looseSnow;
            size *= 1 + .46f * looseSnow;
            var spray=splashes[wheel];
            if(count>0)
            {
                if(!spray.isPlaying)spray.Play(false);
                for(int i=0;i<count;i++)
                {
                    var particle=new ParticleSystem.EmitParams{position=point+normal*.07f+back*(.13f*looseSnow)+Random.insideUnitSphere*.06f,
                        velocity=back*(.7f+speed*.18f+slip*2.2f+looseSnow*Random.Range(.55f,1.5f))+normal*(lift+slip*1.5f)
                            +side*(.25f+slip*.7f+looseSnow*Random.Range(.65f,1.5f))+Random.insideUnitSphere*.35f,
                        startColor=color*Random.Range(.85f,1.12f),startSize=size*Random.Range(.65f-.05f*looseSnow,1.35f+.07f*looseSnow),
                        startLifetime=Random.Range(.30f,.58f)+looseSnow*.10f};
                    if(kind==RecoveryWheelDebrisKind.Grass)particle.startSize3D=new Vector3(size*.35f,size*1.8f,size*.4f);
                    spray.Emit(particle,1);
                }
            }
            if(powders==null||wheel>=powders.Length||powders[wheel]==null)return;
            bool limited=RecoveryPlatform.UsesSwitchRendering;
            if(!RecoveryWheelDebrisRules.PowderWheel(wheel,vehicle.WheelSlip,limited)){powderBudget[wheel]=0;return;}
            float powderRate=RecoveryWheelDebrisRules.PowderRate(kind,speed,slip,angular,true,limited)*Mathf.Clamp01(RecoveryVehicle.VisualEffectsGain)*debrisGain;
            powderRate *= 1 + looseSnow * (limited ? .5f : 1.15f);
            powderBudget[wheel]=powderRate>0?Mathf.Min(winterSnow?4:2,powderBudget[wheel]+powderRate*dt):0;
            int fine=(int)powderBudget[wheel];powderBudget[wheel]-=fine;
            if(fine==0)return;
            var powder=powders[wheel];if(!powder.isPlaying)powder.Play(false);
            var powderColor = RecoveryWheelDebrisRules.PowderColor(kind);
            powderColor.a += .13f * looseSnow;
            for(int i=0;i<fine;i++)
            {
                var puff=new ParticleSystem.EmitParams{position=point+normal*.18f+back*Random.Range(.05f,.18f+looseSnow*.14f)+side*Random.Range(-.08f,.16f),
                    velocity=back*(.45f+speed*.12f+looseSnow*(.64f+slip))+normal*(.40f+slip*.55f)
                        +side*Random.Range(.08f,.30f+looseSnow*.68f),
                    startColor=powderColor,startSize=RecoveryWheelDebrisRules.PowderSize(kind,speed,slip,limited)*Random.Range(.85f,1.15f)*(1+.27f*looseSnow),
                    startLifetime=RecoveryWheelDebrisRules.PowderLifetime(kind,limited)*Random.Range(.9f,1.1f)};
                powder.Emit(puff,1);
            }
        }
    }
}

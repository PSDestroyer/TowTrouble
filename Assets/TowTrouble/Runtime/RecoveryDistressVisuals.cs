using UnityEngine;

namespace TowTrouble
{
    // Presentation follows the active contract on every peer; it never drives gameplay.
    public sealed class RecoveryDistressVisuals : MonoBehaviour
    {
        [SerializeField] private ParticleSystem engineSmoke;
        public void Configure(ParticleSystem smoke) { engineSmoke=smoke; }
        public static float SmokeRate(RecoverySituationKind kind,bool active,float gain)
        { return active&&kind==RecoverySituationKind.EngineFailure?8*Mathf.Clamp01(gain):0; }
        public void Present(RecoverySituationKind kind,bool active)
        {
            if(engineSmoke==null)return;
            float rate=SmokeRate(kind,active,RecoveryVehicle.VisualEffectsGain);
            var emission=engineSmoke.emission;emission.rateOverTime=rate;
            if(!Application.isPlaying)return;
            if(rate>0&&Time.timeScale>.01f&&engineSmoke.gameObject.activeInHierarchy){if(!engineSmoke.isPlaying)engineSmoke.Play(false);}
            else if(engineSmoke.isPlaying)engineSmoke.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public ParticleSystem CreateIncidentSmoke(Transform parent)
        {
            if(engineSmoke==null)return null;
            var copy=Instantiate(engineSmoke,parent,false);
            copy.transform.localPosition=transform.InverseTransformPoint(engineSmoke.transform.position);
            copy.gameObject.SetActive(true);
            var main=copy.main;main.useUnscaledTime=true;main.playOnAwake=false;main.maxParticles=48;main.stopAction=ParticleSystemStopAction.None;
            copy.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            return copy;
        }
        private void OnDisable()
        { if(engineSmoke!=null)engineSmoke.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear); }
    }
}

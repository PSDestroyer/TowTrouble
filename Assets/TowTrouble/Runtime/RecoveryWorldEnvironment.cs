using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryWorld
    {
        private float environmentSeconds;
        private uint environmentSeed=2791;
        private bool environmentReceived;
        public float EnvironmentSeconds=>environmentSeconds;
        public uint EnvironmentSeed=>environmentSeed;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal bool ValidationEnvironment;
        internal float ValidationEnvironmentSeconds;
#endif
        private void ResetEnvironment()
        {environmentSeconds=0;environmentSeed=unchecked((uint)System.Environment.TickCount);environmentReceived=false;}
        private void TickEnvironment()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(ValidationEnvironment){environmentSeconds=Mathf.Max(0,ValidationEnvironmentSeconds);return;}
#endif
            if((IsServerInitialized||IsClientInitialized&&environmentReceived)&&FreeRoam&&Phase!=RecoveryPhase.Lobby&&Phase!=RecoveryPhase.Complete)
                environmentSeconds+=Time.deltaTime;
        }
        private void ReceiveEnvironment(float seconds,uint seed)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)return;
            environmentSeconds=seconds;environmentSeed=seed;environmentReceived=true;
        }
    }
}

namespace TowTrouble
{
    public sealed partial class RecoveryWorld
    {
        private readonly RecoveryHapticEvents[] hapticEvents=new RecoveryHapticEvents[2];
        private uint hapticSequence,receivedHapticSequence;
        private bool receivedHaptics;
        public bool HapticTelemetryReady=>IsServerInitialized||receivedHaptics;
        public override void OnStartClient()
        { base.OnStartClient();if(!IsServerInitialized){receivedHaptics=false;receivedHapticSequence=0;} }
        public override void OnStopClient(){receivedHaptics=false;base.OnStopClient();}
        public RecoveryHapticEvents HapticEvents(int slot)
        {
            if(slot<0||slot>=2)return default;
            var value=hapticEvents[slot];
            if(IsServerInitialized){value.Impact=drivers[slot].HapticImpactSerial;value.ImpactStrength=drivers[slot].HapticImpactStrength;}
            return value;
        }
        private void ReceiveHaptics(uint sequence,RecoveryHapticEvents first,RecoveryHapticEvents second)
        {
            if(receivedHaptics&&!RecoveryHaptics.Newer(sequence,receivedHapticSequence))return;
            receivedHaptics=true;receivedHapticSequence=sequence;hapticEvents[0]=first;hapticEvents[1]=second;
        }
    }
}

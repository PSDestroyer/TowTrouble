using UnityEngine;

namespace TowTrouble
{
    // Cosmetic counters travel in the existing world snapshot, never in a player save or a client-authoritative RPC.
    public struct RecoveryHapticEvents
    {
        public uint Attached, Detached, Broken, Impact;
        public float ImpactStrength;
    }
    public struct RecoveryHapticFrame
    {
        public bool Active, Attached, Recovering, Delivered;
        public int Slot, Resets;
        public string Run;
        public float Tension;
        public WinchFrame Winch;
        public RecoveryHapticEvents Events;
    }
    public enum RecoveryHapticPulse { None, Connect, Disconnect, Impact, Release, Snap }

    // Deterministic signal design; hardware, input ownership and lifecycle are handled by the session/output adapter.
    public sealed class RecoveryHaptics
    {
        public const float MaximumLow=.35f, MaximumHigh=.25f;
        private RecoveryHapticFrame previous;
        private bool seeded;
        private float tension, clock, pulseAge, pulseDuration;
        private Vector2 pulsePeak;
        public RecoveryHapticPulse Pulse { get; private set; }
        public void Reset() { seeded=false;tension=clock=pulseAge=pulseDuration=0;pulsePeak=Vector2.zero;Pulse=RecoveryHapticPulse.None; }
        public static bool Newer(uint incoming,uint previous) => unchecked((int)(incoming-previous))>0;
        private static float Unit(float value) => float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Clamp01(value);
        public static float ImpactStrength(float closingSpeed,float velocityChange)
        {
            if(closingSpeed<2.5f||velocityChange<.75f)return 0;
            return Unit(Mathf.InverseLerp(2.5f,10,closingSpeed)*Mathf.InverseLerp(.75f,3,velocityChange));
        }
        public Vector2 Step(RecoveryHapticFrame frame,float deltaTime,float intensity)
        {
            float dt=float.IsNaN(deltaTime)||float.IsInfinity(deltaTime)?0:Mathf.Clamp(deltaTime,0,5);
            float gain=Unit(intensity);
            if(!frame.Active||gain<=0)
            { Reset();previous=frame;seeded=true;return Vector2.zero; }
            pulseAge+=dt;if(Pulse!=RecoveryHapticPulse.None&&pulseAge>=pulseDuration)Pulse=RecoveryHapticPulse.None;
            bool baseline=!seeded||!previous.Active||previous.Run!=frame.Run||previous.Slot!=frame.Slot||previous.Resets!=frame.Resets;
            if(baseline) { tension=0;Pulse=RecoveryHapticPulse.None;pulseDuration=0; }
            else
            {
                // Priority keeps a collision or detach tick from masking a cable failure.
                if(frame.Events.Broken!=previous.Events.Broken) Trigger(RecoveryHapticPulse.Snap,new Vector2(.33f,.23f),.19f);
                else
                {
                    if(frame.Delivered&&previous.Recovering&&(frame.Attached||previous.Attached)) Trigger(RecoveryHapticPulse.Release,new Vector2(.22f,.12f),.25f);
                    if(frame.Events.Impact!=previous.Events.Impact) Trigger(RecoveryHapticPulse.Impact,new Vector2(.29f,.17f)*Unit(frame.Events.ImpactStrength),.13f);
                    if(frame.Events.Attached!=previous.Events.Attached) Trigger(RecoveryHapticPulse.Connect,new Vector2(.075f,.12f),.075f);
                    else if(frame.Events.Detached!=previous.Events.Detached) Trigger(RecoveryHapticPulse.Disconnect,new Vector2(.045f,.065f),.055f);
                }
            }
            previous=frame;seeded=true;clock+=dt;
            float target=frame.Attached?Unit(frame.Tension):0;
            tension=Mathf.MoveTowards(tension,target,dt*(target>tension?3:6));
            float load=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,1,tension));
            var signal=new Vector2(.21f*load,.025f*load);
            if(frame.Attached)
            {
                float motorLoad=Unit(frame.Winch.Load);
                if(frame.Winch.State==WinchState.Pulling||frame.Winch.State==WinchState.TakingUp)
                    signal=Vector2.Max(signal,new Vector2(.025f+.055f*motorLoad,.02f+.025f*motorLoad));
                else if(frame.Winch.State==WinchState.PayingOut) signal=Vector2.Max(signal,new Vector2(.015f,.025f));
                else if(frame.Winch.State==WinchState.Stalled)
                {
                    float beat=.5f+.5f*Mathf.Sin(clock*4*2*Mathf.PI);
                    signal=Vector2.Max(signal,new Vector2(.055f+.04f*beat,.13f*beat));
                }
                if(tension>.84f)
                {
                    float beat=.5f+.5f*Mathf.Sin(clock*5*2*Mathf.PI);
                    signal.y=Mathf.Max(signal.y,.055f+.14f*beat*Mathf.InverseLerp(.84f,1,tension));
                }
            }
            if(Pulse!=RecoveryHapticPulse.None)
            {
                float envelope=1-Mathf.SmoothStep(0,1,pulseAge/pulseDuration);
                if(Pulse==RecoveryHapticPulse.Release)
                    envelope=pulseAge<.09f?1-Mathf.SmoothStep(0,.09f,pulseAge):pulseAge<.14f?0:1-Mathf.SmoothStep(.14f,.25f,pulseAge);
                signal=Vector2.Max(signal,pulsePeak*envelope);
            }
            return new Vector2(Mathf.Min(MaximumLow,signal.x)*gain,Mathf.Min(MaximumHigh,signal.y)*gain);
        }
        private void Trigger(RecoveryHapticPulse kind,Vector2 peak,float duration)
        {
            if(peak.sqrMagnitude<=0||Pulse>kind)return;
            Pulse=kind;pulsePeak=peak;pulseDuration=duration;pulseAge=0;
        }
    }
}

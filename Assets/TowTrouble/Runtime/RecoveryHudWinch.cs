using TMPro;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private TMP_Text winchText;
        private void RefreshWinchFeedback(RecoveryWorld world,int slot,bool attached)
        {
            bool visible=world.HasContract&&attached;
            winchText.gameObject.SetActive(visible);
            if(!visible)return;
            var frame=world.Winch(slot);
            float capacity=world.Drivers[slot].WinchForce;
            string label=WinchLabel(frame.State,P.language=="en");
            string hint=frame.State==WinchState.Stalled?L("Schimbă poziția sau folosește un troliu mai puternic.","Reposition or use a stronger winch.")
                :frame.State==WinchState.Minimum?L("Aproape de țintă · continuă prin tractare.","Close to target · continue by towing.")
                :frame.State==WinchState.Maximum?L("Lungime maximă atinsă.","Maximum cable length reached.")
                :frame.State==WinchState.PayingOut?L("Dai cablu pentru a reduce tensiunea.","Paying out cable to ease tension.")
                :frame.State==WinchState.Holding?L("Lungimea rămâne fixă · dă cablu pentru a relaxa tensiunea.","Length stays fixed · pay out to ease tension.")
                :(session.UsingGamepad?"B":P.brakeKey)+L(" · Ține frâna pentru sprijin; aderența contează."," · Hold brake for support; grip matters.");
            if(world.Tension(slot)>.82f&&frame.State!=WinchState.PayingOut)
                hint=L("CABLU APROAPE DE LIMITĂ · Oprește tragerea sau dă cablu.","CABLE NEAR LIMIT · Stop pulling or pay out.");
            winchText.text=L("TROLIU · ","WINCH · ")+label+"  |  "+(frame.Load*capacity/1000).ToString("0.0")+" / "+(capacity/1000).ToString("0")+" kN  |  "+frame.Length.ToString("0.0")+" m\n"+hint;
            winchText.color=frame.State==WinchState.Stalled?Orange:Paper;
        }

        public static string WinchLabel(WinchState state,bool english)
        {
            switch(state)
            {
                case WinchState.TakingUp:return english?"TAKING UP SLACK":"PREIA CABLUL";
                case WinchState.Pulling:return english?"PULLING":"TRAGE";
                case WinchState.Stalled:return english?"AT WINCH LIMIT":"LIMITA TROLIULUI";
                case WinchState.PayingOut:return english?"PAYING OUT":"DĂ CABLU";
                case WinchState.Minimum:return english?"MINIMUM LENGTH":"LUNGIME MINIMĂ";
                case WinchState.Maximum:return english?"MAXIMUM LENGTH":"LUNGIME MAXIMĂ";
                default:return english?"HOLDING LENGTH":"ȚINE LUNGIMEA";
            }
        }
    }
}

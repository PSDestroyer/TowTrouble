using UnityEngine;
using UnityEngine.InputSystem;
namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private void BuildTutorialOffer()
        {
            Header("YOUR FIRST RECOVERY STARTS HERE","A short practice session in the workshop yard. No pressure, no purchases.");
            var card=Panel(pageRoot,300,245,1000,410);Icon(card,RecoveryIcon.Hook,36,32,54,Amber);
            Text(card,"LEARN THE ROPES",112,31,852,58,36,Paper,true);
            Text(card,"Drive your starter, connect a cable, use the winch and bring a practice car home.\n\nYour career money and equipment stay safe. You can skip this and replay it later from How to Play.",36,130,928,204,24,Muted);
            Button(pageRoot,"START TRAINING",1004,790,520,StartMenuTraining,true);
            Button(pageRoot,"SKIP · START EXPLORING",76,790,600,SkipTutorialOffer);BuildMenuFooter();
        }
        private void SkipTutorialOffer(){P.tutorialOffered=true;P.Save();StartMenuSession(true);}
        private void BuildHelp()
        {
            Header("HOW TO PLAY","Take your time. A good position makes a heavy recovery easier.");
            string[] tabs={"Driving","Recovery","Workshop"};
            for(int i=0;i<3;i++){int index=i;menuTabs.Add(Button(pageRoot,tabs[i],76+i*486,196,466,()=>{helpTab=index;Show("Help");},helpTab==i));}
            string[][] titles={new[]{"DRIVE AT YOUR PACE","LOOK AROUND","RESET SAFELY"},new[]{"LINE UP THE HITCH","PULL STEADILY","BRING THEM HOME"},new[]{"PREVIEW FIRST","MATCH YOUR EQUIPMENT","FIT AND SAVE"}};
            var family=RecoveryInputPrompts.Family(RecoveryInputDevices.Pad);bool pad=session.UsingGamepad;
            string[][] text={new[]{(pad?RecoveryInputPrompts.TriggerLabels(family):P.forwardKey+" / "+P.reverseKey)+" to drive and reverse. "+(pad?"Left stick":P.leftKey+" / "+P.rightKey)+" to steer. Brake before tight turns.",(pad?"Right stick · Look around. ":"Hold RMB · Look around. ")+(pad?RecoveryInputPrompts.NorthLabel(family):"C")+" resets the camera.",(pad?RecoveryInputPrompts.SelectLabel(family):P.resetKey)+" returns your vehicle to a safe point. Resets affect the recovery bonus."},new[]{"Bring your rear hitch close to the target's tow point. "+(pad?RecoveryInputPrompts.ConfirmLabel(family):P.hookKey)+" attaches or detaches the cable.","Use "+(pad?RecoveryInputPrompts.PreviousTabLabel(family):P.reelKey)+" to reel in and "+(pad?RecoveryInputPrompts.NextTabLabel(family):P.extendKey)+" to pay out. Ease off when the cable is at risk. Firm ground improves grip.","Once the car is free, tow it to the marked drop-off and stop inside. If the cable detaches, return and reconnect."},new[]{"Stop near the workshop and detach your cable. "+(pad?RecoveryInputPrompts.WestLabel(family):P.garageKey)+" opens the garage. Browsing previews a part; it does not buy it.","Bodies and engines add pull. Tires help apply it to the ground. Match cable strength to your pull and winch. Weight helps anchoring but adds load uphill.","Buy & Fit purchases the highlighted part. Owned parts refit for free. Saved setups store fitted equipment, and previews revert when you leave."}};
            RecoveryIcon[][] icons={new[]{RecoveryIcon.Body,RecoveryIcon.DirectionArrow,RecoveryIcon.Back},new[]{RecoveryIcon.Hook,RecoveryIcon.Winch,RecoveryIcon.FinishFlag},new[]{RecoveryIcon.Garage,RecoveryIcon.Wheel,RecoveryIcon.Save}};
            for(int i=0;i<3;i++){
                var card=Panel(pageRoot,76+i*486,280,466,440);Icon(card,icons[helpTab][i],32,30,64,Amber);
                Text(card,(i+1)+" · "+titles[helpTab][i],30,124,406,76,26,Paper,true);
                ControlText(Text(card,text[helpTab][i],30,216,406,190,22,Muted));
            }
            var training=Button(pageRoot,P.tutorialCompleted?"REPLAY TRAINING":"START TRAINING",1004,790,520,StartMenuTraining,true);training.interactable=!session.Connected&&!session.Connecting;
            if(session.Connected)Text(pageRoot,"Training starts from the main menu. Your current recovery stays active.",440,765,1084,30,15,Muted);
            Footer();
            MenuHint(pageRoot,"previous","Previous tab",440,800,270);MenuHint(pageRoot,"next","Next tab",720,800,250);
        }
        private void BuildTutorialDone()
        {
            bool finished=page=="TrainingComplete";
            var card=Panel(pageRoot,380,240,840,420);Icon(card,RecoveryIcon.Check,30,28,44,Amber);
            Text(card,finished?"YOU'RE READY TO ROLL":"PRACTICE CAR DELIVERED",92,24,718,62,33,Paper,true);
            Text(card,finished?"Training complete. Your career equipment is back in place.\n\nChoose a recovery when you're ready. You can revisit the guide or training from the main menu.":"Good work. One last stop: the workshop.\n\nWe'll park you in the yard. Open the garage, preview some equipment, then choose Finish Training. Practice purchases do not affect your career.",32,126,776,180,24,Muted);
            Button(card,finished?"START EXPLORING":"VISIT THE WORKSHOP",32,330,776,ContinueTutorial,true);BuildMenuFooter();
        }
        private void ContinueTutorial()
        {
            session.ConsumeMenuInput();
            if(session.Training&&session.World.Phase==RecoveryPhase.Complete)session.ContinueTrainingAfterDelivery();
            else session.CloseOverlay();
            Show("Gameplay");
        }
        private void RefreshTrainingHud()
        {
            var world=session.World;int slot=session.LocalSlot;int step=session.TrainingStep;
            string camera=session.UsingGamepad?"Right stick · Look around":"Hold RMB · Look around";
            string[] instructions={session.UsingGamepad?RecoveryInputPrompts.TriggerLabels(RecoveryInputPrompts.Family(RecoveryInputDevices.Pad))+" · Drive    Left stick · Steer":"Drive a little using "+P.forwardKey+" / "+P.reverseKey+" and "+P.leftKey+" / "+P.rightKey,camera,session.UsingGamepad?RecoveryInputPrompts.ConfirmLabel(RecoveryInputPrompts.Family(RecoveryInputDevices.Pad))+" · Attach your cable":P.hookKey+" · Attach your cable","Hold "+(session.UsingGamepad?RecoveryInputPrompts.PreviousTabLabel(RecoveryInputPrompts.Family(RecoveryInputDevices.Pad)):P.reelKey)+" to reel in, then pull the car free","Tow the practice car to the marked bay","Practice car delivered",(session.UsingGamepad?RecoveryInputPrompts.WestLabel(RecoveryInputPrompts.Family(RecoveryInputDevices.Pad)):P.garageKey)+" · Open the workshop","Preview some parts, then finish training"};
            SetMissionStatus("TRAINING · "+(Mathf.Min(step,6)+1)+" / 7",instructions[Mathf.Clamp(step,0,7)]);
            objective.text="";marker.gameObject.SetActive(false);crew.text="";foreach(var tag in hudPlates)tag.gameObject.SetActive(false);
            if(world.HasContract){bool working=UpdateApproach(world,slot);RefreshUrgentHint(world,slot);RefreshRecoveryGuidance(world,slot,Camera.main,working);}
            else {ClearRecoveryGuidance();if(step>=6){RefreshGarageGuidance(world,slot,Camera.main);RefreshExploreHint(world,slot);}else exploreHintWanted=false;}
        }
    }
}

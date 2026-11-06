using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private int vehiclePreview;
        private bool vehicleOwnsNavigation,vehiclePreviousNavigation,vehicleSubmitRelease;
        private EventSystem vehicleEventSystem;
        private int vehicleMoveX,vehicleMoveY;
        private float vehicleNextMove;
        private TMP_Text vehicleControlsHint;
        private void SetVehicleNavigation(bool active)
        {
            if(active&&!vehicleOwnsNavigation&&EventSystem.current!=null){vehicleEventSystem=EventSystem.current;vehiclePreviousNavigation=vehicleEventSystem.sendNavigationEvents;vehicleEventSystem.sendNavigationEvents=false;vehicleOwnsNavigation=true;}
            else if(!active&&vehicleOwnsNavigation){if(vehicleEventSystem!=null)vehicleEventSystem.sendNavigationEvents=vehiclePreviousNavigation;vehicleOwnsNavigation=false;vehicleEventSystem=null;}
        }
        private bool HandleVehicleControls()
        {
            bool active=page=="Vehicles"||page=="Confirm"&&confirmationReturn=="Vehicles"||page=="Settings"&&firstSetup;
            if(!active){SetVehicleNavigation(false);return false;}SetVehicleNavigation(true);
            var keyboard=Keyboard.current;var pad=Gamepad.current;var family=RecoveryInputPrompts.Family(pad);
            if(vehicleControlsHint!=null)vehicleControlsHint.text=session.UsingGamepad?"D-pad / LS · Select     "+RecoveryInputPrompts.ConfirmLabel(family)+" · Confirm     "+RecoveryInputPrompts.CancelLabel(family)+" · Back":"Arrows / WASD · Select     Enter · Confirm     Esc · Back";
            if(keyboard?.escapeKey.wasPressedThisFrame==true||RecoveryInputPrompts.Cancel(pad)?.wasPressedThisFrame==true){session.ConsumeMenuInput();Show(page=="Confirm"?"Vehicles":page=="Settings"||!P.starterChosen?"Title":returnPage);return true;}
            GarageDirection(keyboard,pad,out int x,out int y);
            if(x==0&&y==0){vehicleMoveX=vehicleMoveY=0;}
            else if(x!=vehicleMoveX||y!=vehicleMoveY||Time.unscaledTime>=vehicleNextMove)
            {
                bool first=x!=vehicleMoveX||y!=vehicleMoveY;vehicleMoveX=x;vehicleMoveY=y;vehicleNextMove=Time.unscaledTime+(first?.35f:.12f);
                var selected=EventSystem.current?.currentSelectedGameObject?.GetComponent<UnityEngine.UI.Selectable>();
                if(page=="Settings"&&selected is UnityEngine.UI.Slider slider&&x!=0)slider.value+=x*.025f;
                else{var next=selected==null?null:y<0?selected.FindSelectableOnUp():y>0?selected.FindSelectableOnDown():x<0?selected.FindSelectableOnLeft():selected.FindSelectableOnRight();if(next!=null)EventSystem.current.SetSelectedGameObject(next.gameObject);}
            }
            bool held=keyboard!=null&&(keyboard.enterKey.isPressed||keyboard.numpadEnterKey.isPressed)||RecoveryInputPrompts.Confirm(pad)?.isPressed==true;
            bool pressed=keyboard!=null&&(keyboard.enterKey.wasPressedThisFrame||keyboard.numpadEnterKey.wasPressedThisFrame)||RecoveryInputPrompts.Confirm(pad)?.wasPressedThisFrame==true;
            if(vehicleSubmitRelease){if(!held)vehicleSubmitRelease=false;}
            else if(pressed){vehicleSubmitRelease=true;session.ConsumeMenuInput();var button=EventSystem.current?.currentSelectedGameObject?.GetComponent<UnityEngine.UI.Button>();if(button!=null&&button.interactable)button.onClick.Invoke();}
            return true;
        }
        private void PreviewVehicleChoice(int model)
        {
            vehiclePreview=model;
            // Offline showroom only. A connected crew sees the selected ride
            // until the purchase/use action sends an actual driver change.
            session.World.PreviewVehicle(model);session.World.PreviewPaint(P.paint);Show("Vehicles");
        }
        private void UseVehicleChoice()
        {
            int model=vehiclePreview;
            if(!P.starterChosen)
            {
                Confirm("Make "+RecoveryVehicle.ModelNames[model]+" your Stock vehicle?\n\nYou get one starter for free. Your choice is saved; other starter variants can be bought later.",()=>{if(session.ChooseStarter(model))Show("Main");else Show("Vehicles");},"Vehicles");return;
            }
            if(P.OwnsVehicle(model)){session.SelectVehicle(model);Show(returnPage);return;}
            int price=RecoveryEconomy.VehiclePrice(model);
            if(P.credits<price)return;
            Confirm("Buy "+RecoveryVehicle.ModelNames[model]+" for "+price.ToString("N0")+" CR and use it?\n\nYour current vehicle and its fitted parts stay saved.",
                ()=>{if(session.BuyVehicle(model))Show(returnPage);else Show("Vehicles");},"Vehicles");
        }
        private void BuildVehicles()
        {
            bool choosing=!P.starterChosen;
            Header(choosing?"CHOOSE YOUR FIRST RIDE":"VEHICLES",choosing?"Pick one for free. This becomes your Stock vehicle. Upgrade its body in the workshop.":"Other starter models offer a different feel. Upgrade your body in the workshop for more pull.");Wallet(pageRoot,P.credits);
            int selected=Mathf.Clamp(vehiclePreview,0,3);bool owned=P.OwnsVehicle(selected);int price=RecoveryEconomy.VehiclePrice(selected);
            var stats=Panel(pageRoot,76,220,390,330);
            Text(stats,RecoveryVehicle.ModelNames[selected],24,24,340,52,35,Amber,true);
            Text(stats,choosing?"FREE STARTER CHOICE":selected==P.starterVehicle?"YOUR STOCK · OWNED":owned?"OWNED":price.ToString("N0")+" CR",24,81,340,30,19,owned?Paper:Amber,true);
            string[] labels={"BASE PULL","BASE MASS","BASE TOP SPEED"};
            string[] values={(RecoveryBodyBalance.StockDrive(selected)*.004f).ToString("0.0")+" kN",RecoveryBodyBalance.StockMass(selected).ToString("N0")+" kg",(RecoveryBodyBalance.StockSpeed(selected)*3.6f).ToString("0")+" km/h"};
            for(int i=0;i<3;i++){Text(stats,labels[i],24,130+i*60,170,24,16,Muted,true);Text(stats,values[i],194,126+i*60,172,32,22,Paper,true).alignment=TextAlignmentOptions.Right;}
            string[] roles={"Balanced starter. A little of everything.","Slightly more pulling and anchoring weight. Slower travel.","Light and nimble. Faster travel with slightly less pull.","A stable middle ground, with a little extra pull."};
            Text(pageRoot,roles[selected]+"\n"+(choosing?"Your starter. Make it your own in the workshop.":selected==P.vehicle?"Current vehicle":owned?"Select Use vehicle to switch":"Preview only · not purchased"),80,574,450,124,18,Muted);
            string[] paints={"ORIGINAL","CREAM","OLIVE","MUSTARD"};
            for(int i=0;i<paints.Length;i++){int paint=i;Button(pageRoot,paints[i],620+i*220,634,200,()=>{session.SelectPaint(paint);Show("Vehicles");},P.paint==i,48);}
            for(int i=0;i<4;i++)
            {
                int model=i;string state=choosing?"FREE CHOICE":P.OwnsVehicle(i)?i==P.starterVehicle?"YOUR STOCK":"OWNED":RecoveryEconomy.VehiclePrice(i).ToString("N0")+" CR";
                var choice=Button(pageRoot,RecoveryVehicle.ModelNames[i]+"\n"+state,76+i*368,714,348,()=>PreviewVehicleChoice(model),selected==i,64);choice.name="Vehicle choice "+i;
            }
            string label=choosing?"CHOOSE "+RecoveryVehicle.ModelNames[selected].ToUpperInvariant():owned?selected==P.vehicle?"CONTINUE":"USE VEHICLE":P.credits<price?"NEED "+(price-P.credits).ToString("N0")+" CR":"BUY & USE · "+price.ToString("N0")+" CR";
            var primary=Button(pageRoot,label,1084,808,440,UseVehicleChoice,true);primary.name="Vehicle primary action";primary.interactable=choosing||owned||P.credits>=price;
            vehicleControlsHint=Footer(choosing?"Title":returnPage);
        }
    }
}

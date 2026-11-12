using TMPro;
using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private int vehiclePreview,starterPaint;
        private UnityEngine.UI.Button[] starterChoices,starterPaintChoices;
        private UnityEngine.UI.Button starterConfirm,starterBack;
        private TMP_Text starterCompareHint,starterRotateHint;
        private static readonly string[] StarterRoles={"Balanced","Strongest pull","Quick & light","Steady all-rounder"};
        private static readonly string[] StarterDescriptions={"Comfortable travel and steady pulling.","A little more pulling power, with a slower pace.","The lightest ride and the quickest on the road.","Steady pulling with a little extra anchoring weight."};
        private static readonly string[] StarterPaintNames={"Original","Cream","Olive","Mustard"};
        private void PreviewVehicleChoice(int model)
        {if(vehiclePreview==model)return;vehiclePreview=model;session.World.PreviewVehicle(model);session.World.PreviewPaint(starterPaint);Show("Starter");}
        private void PreviewStarterPaint(int paint)
        {if(starterPaint==paint)return;starterPaint=paint;session.World.PreviewPaint(paint);Show("Starter");}
        private void UseVehicleChoice()
        {
            if(P.starterChosen)return;
            ConfirmAction("Make "+RecoveryVehicle.ModelNames[vehiclePreview]+" your Stock vehicle?\n\nYou receive one starter for free. Buy different bodies and equipment later in the workshop.",()=>{
                if(session.ChooseStarter(vehiclePreview,starterPaint))Show(session.Expedition!=null?"Main":"TutorialOffer");
                else Show("Starter");
            },"Starter","CHOOSE "+RecoveryVehicle.ModelNames[vehiclePreview].ToUpperInvariant());
        }
        private void BuildVehicles()
        {
            Panel(pageRoot,0,0,1600,164,new Color(.03f,.04f,.05f,.64f));
            Icon(pageRoot,RecoveryIcon.Hook,28,47,32,Amber);
            Text(pageRoot,"YOUR FIRST RIDE",80,36,1444,65,42,Paper,true);
            Text(pageRoot,"One free starter. Upgrade any ride at the workshop.",82,112,1442,34,20,Muted);
            int selected=Mathf.Clamp(vehiclePreview,0,3);
            if(Application.isPlaying){session.World.PreviewVehicle(selected);session.World.PreviewPaint(starterPaint);}
            starterCompareHint=ControlText(Text(pageRoot,"",80,178,432,26,16,Paper));
            Text(pageRoot,"Pull · Weight · Top speed",80,205,432,22,15,Muted);
            starterChoices=new UnityEngine.UI.Button[4];
            for(int i=0;i<4;i++)
            {
                int model=i;bool active=i==selected;
                var choice=Button(pageRoot,"",76,228+i*130,440,()=>PreviewVehicleChoice(model),active,112);
                choice.name="Starter "+i;starterChoices[i]=choice;
                choice.gameObject.AddComponent<RecoveryMenuPreviewFocus>().Configure(()=>PreviewVehicleChoice(model));
                Color title=Paper,secondary=Muted;
                Text(choice.transform,RecoveryVehicle.ModelNames[i],22,9,346,38,28,title,true);
                Text(choice.transform,StarterRoles[i],22,48,396,25,17,secondary);
                string stats=(RecoveryBodyBalance.StockDrive(i)*.004f).ToString("0.0")+" kN   ·   "+RecoveryBodyBalance.StockMass(i).ToString("0")+" kg   ·   "+(RecoveryBodyBalance.StockSpeed(i)*3.6f).ToString("0")+" km/h";
                Text(choice.transform,stats,22,79,396,27,17,title,true);
                if(active)Icon(choice.transform,RecoveryIcon.Check,394,18,22,Amber);
            }
            var hint=Panel(pageRoot,550,184,620,38,new Color(.03f,.04f,.05f,.72f));
            starterRotateHint=ControlText(Text(hint,"",18,3,584,30,16,Paper));
            var details=Panel(pageRoot,550,598,974,96);
            Text(details,RecoveryVehicle.ModelNames[selected],24,9,926,38,29,Paper,true);
            Text(details,StarterDescriptions[selected],24,50,926,34,20,Muted);
            var paintRow=Panel(pageRoot,550,710,974,52);
            Text(paintRow,"PAINT",18,11,104,30,16,Muted,true);
            starterPaintChoices=new UnityEngine.UI.Button[4];
            for(int i=0;i<4;i++)
            {
                int paint=i;var swatch=Button(paintRow,"",126+i*68,4,58,()=>PreviewStarterPaint(paint),starterPaint==i,44);
                swatch.name="Paint "+i;starterPaintChoices[i]=swatch;
                Panel(swatch.transform,10,8,38,28,RecoveryPaintPalette.Swatch(i,selected));
                swatch.gameObject.AddComponent<RecoveryMenuPreviewFocus>().Configure(()=>PreviewStarterPaint(paint));
            }
            Text(paintRow,StarterPaintNames[starterPaint],420,11,212,30,19,Paper,true);
            Text(paintRow,"You can repaint it later",654,11,302,30,16,Muted).alignment=TextAlignmentOptions.MidlineRight;
            starterConfirm=Button(pageRoot,"CHOOSE "+RecoveryVehicle.ModelNames[selected].ToUpperInvariant(),1004,808,520,UseVehicleChoice,true);
            starterBack=Button(pageRoot,"BACK",76,808,230,()=>MenuBack());BuildMenuFooter();
            preferredMenuFocus="Starter "+selected;
            UpdateStarterHints();
        }
        private void UpdateStarterHints()
        {
            if(page!="Starter")return;
            bool pad=session.UsingGamepad;
            if(starterCompareHint!=null)starterCompareHint.text=pad?"D-pad · Compare rides":"W / S or Arrows · Compare rides";
            if(starterRotateHint!=null)starterRotateHint.text=pad?"Right stick · Rotate    "+RecoveryInputPrompts.RightStickPressLabel(RecoveryInputPrompts.Family(RecoveryInputDevices.Pad))+" · Reset view":"LMB · Rotate    C · Reset view";
        }
        private void ConfigureStarterNavigation()
        {
            if(page!="Starter"||starterChoices==null||starterPaintChoices==null)return;
            var car=starterChoices[Mathf.Clamp(vehiclePreview,0,3)];var paint=starterPaintChoices[Mathf.Clamp(starterPaint,0,3)];
            void Link(UnityEngine.UI.Selectable control,UnityEngine.UI.Selectable up,UnityEngine.UI.Selectable down,UnityEngine.UI.Selectable left,UnityEngine.UI.Selectable right)
            {var nav=control.navigation;nav.mode=UnityEngine.UI.Navigation.Mode.Explicit;nav.selectOnUp=up;nav.selectOnDown=down;nav.selectOnLeft=left;nav.selectOnRight=right;control.navigation=nav;}
            for(int i=0;i<4;i++)Link(starterChoices[i],i>0?starterChoices[i-1]:null,i<3?starterChoices[i+1]:paint,null,paint);
            for(int i=0;i<4;i++)Link(starterPaintChoices[i],car,starterConfirm,i>0?starterPaintChoices[i-1]:car,i<3?starterPaintChoices[i+1]:null);
            Link(starterBack,starterChoices[3],null,null,starterConfirm);
            Link(starterConfirm,paint,null,starterBack,null);
        }
    }
}

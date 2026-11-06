using TMPro;
using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private int vehiclePreview,starterPaint;
        private void PreviewVehicleChoice(int model)
        {if(vehiclePreview==model)return;vehiclePreview=model;session.World.PreviewVehicle(model);session.World.PreviewPaint(starterPaint);Show("Starter");}
        private void UseVehicleChoice()
        {
            if(P.starterChosen)return;
            ConfirmAction("Make "+RecoveryVehicle.ModelNames[vehiclePreview]+" your Stock vehicle?\n\nYou receive one starter for free. Buy different bodies and equipment later in the workshop.",()=>{
                if(session.ChooseStarter(vehiclePreview)){session.SelectPaint(starterPaint);Show("TutorialOffer");}
                else Show("Starter");
            },"Starter","CHOOSE "+RecoveryVehicle.ModelNames[vehiclePreview].ToUpperInvariant());
        }
        private void BuildVehicles()
        {
            Header("CHOOSE YOUR FIRST RIDE","One free starter. Make it your own in the workshop.");
            int selected=Mathf.Clamp(vehiclePreview,0,3);if(Application.isPlaying){session.World.PreviewVehicle(selected);session.World.PreviewPaint(starterPaint);}
            var card=Panel(pageRoot,76,225,420,330);Text(card,RecoveryVehicle.ModelNames[selected],26,24,368,48,34,Amber,true);
            Text(card,"FREE STARTER",26,86,368,30,16,Muted,true);
            string[] labels={"PULL","WEIGHT","TOP SPEED"};string[] values={(RecoveryBodyBalance.StockDrive(selected)*.004f).ToString("0.0")+" kN",RecoveryBodyBalance.StockMass(selected).ToString("N0")+" kg",(RecoveryBodyBalance.StockSpeed(selected)*3.6f).ToString("0")+" km/h"};
            for(int i=0;i<3;i++){Text(card,labels[i],26,143+i*56,170,30,16,Muted);Text(card,values[i],196,138+i*56,198,38,23,Paper,true).alignment=TextAlignmentOptions.MidlineRight;}
            string[] roles={"A balanced start. Comfortable travel and steady pulling.","A little more pull and weight. A little less speed.","Light and nimble. Faster travel with a little less pull.","A steady all-rounder with a little extra pulling power."};
            Text(pageRoot,roles[selected],80,585,420,100,21,Muted);
            string[] colors={"ORIGINAL","CREAM","OLIVE","MUSTARD"};for(int i=0;i<4;i++){int paint=i;Button(pageRoot,colors[i],620+i*220,637,200,()=>{starterPaint=paint;session.World.PreviewPaint(paint);Show("Starter");},starterPaint==i,44);}
            for(int i=0;i<4;i++){int model=i;var choice=Button(pageRoot,RecoveryVehicle.ModelNames[i],76+i*368,714,348,()=>PreviewVehicleChoice(model),selected==i,64);choice.name="Starter "+i;choice.gameObject.AddComponent<RecoveryMenuPreviewFocus>().Configure(()=>PreviewVehicleChoice(model));}
            Button(pageRoot,"CHOOSE "+RecoveryVehicle.ModelNames[selected].ToUpperInvariant(),1084,808,440,UseVehicleChoice,true);
            Footer("Title");preferredMenuFocus="Starter "+selected;
        }
    }
}

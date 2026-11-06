using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private string settingsMessage="";
        private bool displayInitialized,displayDraftFullscreen,displayPending;
        private int displayDraftQuality,displayDraftResolution;
        private Vector2Int[] displaySizes=Array.Empty<Vector2Int>();
        private int oldWidth,oldHeight,oldQuality;
        private FullScreenMode oldMode;
        private float displayDeadline;
        private void BuildSettings()
        {
            if(firstSetup){BuildFirstSetup();return;}
            Header("SETTINGS","Adjust your comfort. Audio and controls save automatically; display changes need confirmation.");
            string[] keys={"Audio","Imagine","Comenzi","Accesibilitate","Joc"};string[] labels={"Audio","Display","Controls","Accessibility","Game"};
            for(int i=0;i<keys.Length;i++){string tab=keys[i];menuTabs.Add(Button(pageRoot,labels[i],76+i*292,196,272,()=>{settingsTab=tab;settingsMessage="";Show("Settings");},settingsTab==tab));}
            var body=Panel(pageRoot,76,278,866,480);var details=Panel(pageRoot,966,278,558,480);
            Icon(details,RecoveryIcon.Settings,28,28,32,Amber);Text(details,"MAKE IT COMFORTABLE",76,24,454,48,25,Paper,true);
            menuHelp=Text(details,SettingsDescription(),28,106,502,266,20,Muted);menuHelp.fontSizeMin=16;
            status=Text(details,settingsMessage,28,385,502,68,16,Amber);
            switch(settingsTab)
            {
                case "Audio":
                    Slider(body,"Master volume",P.master,0,1,24,v=>P.master=v);
                    Slider(body,"Vehicle and sound effects",P.effects,0,1,154,v=>P.effects=v);
                    Slider(body,"Interface",P.ui,0,1,284,v=>{P.ui=v;if(uiAudio!=null&&clickClip!=null)uiAudio.PlayOneShot(clickClip,v);});
                    break;
                case "Imagine":
                    InitializeDisplayDraft();
                    Slider(body,"Brightness",P.brightness,.7f,1.4f,20,v=>P.brightness=v);
                    SettingButton(body,"QUALITY: "+QualityLabel(displayDraftQuality),136,()=>{displayDraftQuality=(displayDraftQuality+1)%2;Show("Settings");},"Performance reduces visual cost. Quality keeps more detail. Apply changes to preview the selected preset.");
                    SettingButton(body,displayDraftFullscreen?"MODE: FULLSCREEN":"MODE: WINDOWED",210,()=>{displayDraftFullscreen=!displayDraftFullscreen;Show("Settings");},"Choose fullscreen or a resizable window. An unconfirmed display change reverts automatically.");
                    SettingButton(body,"RESOLUTION: "+displaySizes[displayDraftResolution].x+" × "+displaySizes[displayDraftResolution].y,284,()=>{displayDraftResolution=(displayDraftResolution+1)%displaySizes.Length;Show("Settings");},"Cycles through unique screen sizes. Apply when ready; you have 15 seconds to keep the new display settings.");
                    Button(body,"APPLY DISPLAY CHANGES",30,382,806,ApplyDisplay,true);
                    break;
                case "Comenzi":
                    if(session.UsingGamepad)
                    {
                        Text(body,RecoveryInputPrompts.Family(RecoveryInputDevices.Pad)+" CONTROLLER",24,10,800,34,21,Amber,true);
                        string[] controls=ControllerGuide().Split('\n');
                        for(int i=0;i<controls.Length;i++)
                            ControlText(Text(body,controls[i],24+(i/5)*420,54+(i%5)*48,398,44,18,Paper));
                    }
                    else for(int i=0;i<RecoveryMenuSettings.BindingFields.Length;i++){
                        string field=RecoveryMenuSettings.BindingFields[i];int col=i/6,row=i%6;
                        var button=Button(body,RecoveryMenuSettings.BindingLabels[i]+"  ["+typeof(RecoveryProfile).GetField(field).GetValue(P)+"]",20+col*420,18+row*44,406,()=>{pendingBinding=field;settingsMessage="Press a key. Esc cancels.";status.text=settingsMessage;},false,40);
                        ControlText(button.GetComponentInChildren<TMPro.TMP_Text>());
                        menuDescriptions[button.gameObject]="Choose a keyboard key for "+RecoveryMenuSettings.BindingLabels[i].ToLowerInvariant()+". Duplicate and reserved bindings are rejected.";
                    }

                    var invert=Button(body,"INVERT CAMERA Y: "+OnOff(P.invertCamera),30,350,806,()=>{P.invertCamera=!P.invertCamera;SaveMenuPreference();},false,40);menuDescriptions[invert.gameObject]="Reverses vertical camera movement for mouse and right stick.";
                    Slider(body,"Camera sensitivity",P.cameraSensitivity,.3f,2,400,v=>P.cameraSensitivity=v);
                    break;
                case "Accesibilitate":
                    Slider(body,"Text / HUD size",P.textScale,.85f,1.3f,24,v=>P.textScale=v);
                    SettingButton(body,"REDUCED MOTION: "+OnOff(P.reducedMotion),160,()=>{P.reducedMotion=!P.reducedMotion;SaveMenuPreference();},"Reduces camera movement and removes menu fades and animated scrolling.");
                    SettingButton(body,"TENSION SYMBOLS: "+OnOff(P.colorHelp),240,()=>{P.colorHelp=!P.colorHelp;SaveMenuPreference();},"Adds a clear ! warning to high cable loads, alongside the existing warning text and colour.");
                    break;
                case "Joc":
                    SettingButton(body,"TUTORIAL HINTS: "+OnOff(P.tutorial),24,()=>{P.tutorial=!P.tutorial;SaveMenuPreference();},"Show short contextual help while learning. Mission objectives and cable warnings always remain visible.");
                    SettingButton(body,"CONTROLLER VIBRATION: "+OnOff(P.vibration),104,()=>{P.vibration=!P.vibration;SaveMenuPreference();},"Gentle controller feedback when a cable is under high load.");
                    SettingButton(body,"HOW TO PLAY",184,()=>Show("Help"),"Read the driving, recovery and workshop guide. Start or replay training from the main menu.");
                    var reset=SettingButton(body,"RESET PROGRESS",316,()=>ConfirmAction("Reset all career progress?\n\nCredits, purchased equipment, recoveries, saved setups and your starter choice will be erased. Audio, video and control preferences will be kept.",()=>{session.ResetProgress();firstSetup=false;Show("Starter");},"Settings","RESET PROGRESS"),"Erase career progress and choose a new starter. Your audio, video and control settings are kept. Leave the session before resetting.");
                    reset.interactable=!session.Connected&&!session.Connecting;
                    if(session.Connected)Text(body,"Return to the main menu to reset progress.",30,398,806,46,16,Muted);
                    break;
            }
            Button(pageRoot,"RESTORE TAB DEFAULTS",1100,808,424,()=>ConfirmAction("Restore the default "+labels[Array.IndexOf(keys,settingsTab)].ToLowerInvariant()+" settings? Career progress is kept.",RestoreSettingsTab,"Settings","RESTORE DEFAULTS"));
            Footer(returnPage);
            MenuHint(pageRoot,"previous","Previous tab",440,790,280);MenuHint(pageRoot,"next","Next tab",760,790,250);
        }
        private void BuildFirstSetup()
        {
            Header("WELCOME TO TOW TROUBLE","A few comfort settings, then choose your first ride.");
            var panel=Panel(pageRoot,236,248,1128,460);
            Text(panel,"TAKE YOUR TIME",30,24,1068,48,29,Paper,true);
            Text(panel,"You can change these settings later. Your first car comes next.",30,84,1068,48,20,Muted);
            Slider(panel,"Brightness",P.brightness,.7f,1.4f,161,v=>P.brightness=v);
            Slider(panel,"Text / HUD size",P.textScale,.85f,1.3f,291,v=>P.textScale=v);
            Button(pageRoot,"CHOOSE YOUR FIRST RIDE",960,790,564,()=>{P.onboarded=true;P.language="en";P.Save();firstSetup=false;Show("Starter");},true);
            Button(pageRoot,"BACK",76,790,230,()=>Show("Title"));BuildMenuFooter();
        }
        private UnityEngine.UI.Button SettingButton(RectTransform body,string label,float y,Action action,string description)
        {var button=Button(body,label,30,y,806,action);menuDescriptions[button.gameObject]=description;return button;}
        private string SettingsDescription()=>settingsTab=="Audio"?"Set the sound to suit your space. Vehicle audio and interface sounds are separate. Muting audio never removes visual feedback.":settingsTab=="Imagine"?"Choose your display settings on the left, then Apply. Keep the result within 15 seconds or the previous settings return automatically.":settingsTab=="Comenzi"?(session.UsingGamepad?"Controls follow your connected controller automatically. Switch to your keyboard or mouse whenever you prefer.\n\nAdjust camera sensitivity below, or invert its vertical movement.":"Select an action to change its keyboard binding. Each key can have one action; menu navigation keys stay reserved.\n\nConnect a controller to see its button layout."):settingsTab=="Accesibilitate"?"Choose readable text and comfortable camera movement. Cable warnings can include symbols as well as colour.":"Contextual hints help you learn at your own pace. Reset Progress starts a new career and keeps your preferences.";
        private static string OnOff(bool value)=>value?"ON":"OFF";
        private void SaveMenuPreference(){session.ApplySettings();P.Save();Show("Settings");}
        private string ControllerGuide()
        {
            var family=RecoveryInputPrompts.Family(RecoveryInputDevices.Pad);
            if(!session.UsingGamepad)return P.forwardKey+" / "+P.reverseKey+" · Drive / reverse\n"+P.leftKey+" / "+P.rightKey+" · Steer\n"+P.hookKey+" · Cable\n"+P.brakeKey+" · Brake\n"+P.reelKey+" / "+P.extendKey+" · Reel in / pay out\nRMB · Camera\nC · Reset camera\n"+P.garageKey+" · Garage\n"+P.boardKey+" · Recoveries";
            return RecoveryInputPrompts.TriggerLabels(family)+" · Drive / reverse\nLeft stick · Steer\n"+RecoveryInputPrompts.ConfirmLabel(family)+" · Cable\n"+RecoveryInputPrompts.CancelLabel(family)+" · Brake\n"+RecoveryInputPrompts.PreviousTabLabel(family)+" / "+RecoveryInputPrompts.NextTabLabel(family)+" · Reel in / pay out\nRight stick · Camera\n"+RecoveryInputPrompts.NorthLabel(family)+" · Reset camera\n"+RecoveryInputPrompts.WestLabel(family)+" · Garage\n"+RecoveryInputPrompts.LeftStickPressLabel(family)+" · Recoveries";
        }
        private static string QualityLabel(int index)=>index==0?"PERFORMANCE":"QUALITY";
        private void InitializeDisplayDraft()
        {
            if(displayInitialized)return;displayInitialized=true;displayDraftQuality=Mathf.Clamp(P.quality,0,1);displayDraftFullscreen=Screen.fullScreen;
            displaySizes=RecoveryMenuSettings.Resolutions(Screen.resolutions,Screen.width,Screen.height);
            if(displaySizes.Length==0)displaySizes=new[]{new Vector2Int(1280,720)};
            displayDraftResolution=Mathf.Max(0,Array.IndexOf(displaySizes,new Vector2Int(Screen.width,Screen.height)));
        }
        private void ApplyDisplay()
        {
            oldWidth=Screen.width;oldHeight=Screen.height;oldMode=Screen.fullScreenMode;oldQuality=P.quality;
            displayPending=true;displayDeadline=Time.realtimeSinceStartup+15;
            P.quality=displayDraftQuality;session.ApplySettings();
            var size=displaySizes[displayDraftResolution];Screen.SetResolution(size.x,size.y,displayDraftFullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
            ConfirmAction("Keep the new display settings?\n\nThe previous settings return automatically after 15 seconds.",()=>{displayPending=false;P.displayWidth=Screen.width;P.displayHeight=Screen.height;P.displayMode=(int)Screen.fullScreenMode;P.Save();displayInitialized=false;settingsMessage="Display settings saved";Show("Settings");},"Settings","KEEP SETTINGS");
        }
        private void UpdateDisplayReview()
        {
            if(!displayPending)return;
            if(Time.realtimeSinceStartup>=displayDeadline){RevertDisplay();return;}
            if(page=="Confirm"&&pageRoot!=null){var t=pageRoot.GetComponentsInChildren<TMP_Text>().FirstOrDefault(x=>x.text.StartsWith("Keep the new display settings?"));if(t!=null)t.text="Keep the new display settings?\n\nReverting in "+Mathf.CeilToInt(displayDeadline-Time.realtimeSinceStartup)+" seconds.";}
        }
        private void RevertDisplay()
        {
            if(!displayPending)return;displayPending=false;
            Screen.SetResolution(oldWidth,oldHeight,oldMode);P.quality=oldQuality;session.ApplySettings();displayInitialized=false;
            settingsMessage="Previous display settings restored";Show("Settings");
        }
        private void RestoreDisplayOnExit()
        {
            if(!displayPending)return;displayPending=false;
            Screen.SetResolution(oldWidth,oldHeight,oldMode);
            if(session!=null&&session.Profile!=null){session.Profile.quality=oldQuality;session.ApplySettings();}
        }
        private void RestoreSettingsTab()
        {
            var defaults=new RecoveryProfile();
            string[] fields=settingsTab=="Audio"?new[]{"master","effects","ui"}:settingsTab=="Imagine"?new[]{"brightness","quality"}:settingsTab=="Comenzi"?RecoveryMenuSettings.BindingFields.Concat(new[]{"cameraSensitivity","controllerFamily","invertCamera"}).ToArray():settingsTab=="Accesibilitate"?new[]{"textScale","reducedMotion","colorHelp"}:new[]{"tutorial","vibration"};
            foreach(var name in fields){var field=typeof(RecoveryProfile).GetField(name);field.SetValue(P,field.GetValue(defaults));}
            displayInitialized=false;settingsMessage="Defaults restored";SaveMenuPreference();
        }
    }
}

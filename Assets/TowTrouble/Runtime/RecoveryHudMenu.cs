using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private int helpTab;
        private string helpReturn="Main";
        private bool busySolo=true, busyJoin,busyTraining;
        private const float BusyPanelDelay=.5f;
        private bool busyPresentationPending;
        private float busyRevealAt;
        private TMP_Text lobbyState;
        private UnityEngine.UI.Button lobbyReady;
        private string textEntryDraft, textEntryLabel, textEntryOriginal;
        private Action<string> textEntryCommit;
        private int textEntryLimit;
        private UnityEngine.UI.ScrollRect menuScroll;
        private void BuildMain()
        {
            Panel(pageRoot,48,35,460,746);Logo(pageRoot,76,58,63);
            Text(pageRoot,"BAD ROADS. GOOD COMPANY.",82,235,392,30,15,Muted,true);Wallet(pageRoot,P.credits);
            Button(pageRoot,RecoveryPlatform.MultiplayerAvailable?"PLAY SOLO":"PLAY",80,300,396,()=>StartMenuSession(true),true,62);
            bool multiplayer=RecoveryPlatform.MultiplayerAvailable;
            if(multiplayer)Button(pageRoot,"PLAY CO-OP",80,380,396,()=>Show("Coop"));
            Button(pageRoot,"HOW TO PLAY",80,multiplayer?450:380,396,()=>Show("Help"));
            Button(pageRoot,"SETTINGS",80,multiplayer?520:450,396,()=>{returnPage="Main";firstSetup=false;Show("Settings");});
            Button(pageRoot,"CREDITS",80,634,190,()=>Show("Credits"),false,42);
            Button(pageRoot,"QUIT",286,634,190,()=>Application.Quit(),false,42);
            Text(pageRoot,"TOW COUNTY · RECOVERY SERVICE",82,728,394,28,15,Muted);
            var catalog=session.GarageCatalog;var build=P.Build(P.vehicle);var body=catalog?.Find(build.Item(GarageSlot.Assembly,catalog));
            bool modified=build.Items!=null&&build.Items.Any(id=>!string.IsNullOrEmpty(id)&&!id.StartsWith("stock_",StringComparison.Ordinal)&&!id.StartsWith("paint_",StringComparison.Ordinal));
            var card=Panel(pageRoot,1064,664,460,154);Icon(card,RecoveryIcon.Body,18,15,28,Amber);
            Text(card,modified?"YOUR RIDE · MODIFIED":"YOUR RIDE · STOCK",58,14,382,26,15,Muted,true);
            Text(card,body!=null?RecoveryGarageWorkflow.DisplayName(body,P,catalog):RecoveryVehicle.ModelNames[P.vehicle],20,48,420,44,27,Paper,true);
            if(catalog!=null){var perf=GaragePerformance.Calculate(build,catalog);Text(card,(perf.Drive*.004f).ToString("0.0")+" kN pull   ·   "+(perf.Cable/1000).ToString("0")+" kN cable",20,106,420,30,16,Paper);}
            BuildMenuFooter();
        }
        private void StartMenuSession(bool solo)
        {
            if(!solo&&!RecoveryPlatform.MultiplayerAvailable){Show("Main");return;}
            busySolo=solo;busyJoin=busyTraining=false;session.Host(solo,true);Show("Busy");
        }
        private void StartMenuTraining()
        {busySolo=true;busyJoin=false;busyTraining=true;session.RequestTraining();Show("Busy");}
        private bool DeferBusyPresentation()
        {
            if(!session.Connecting||session.Leaving)return false;
            busyPresentationPending=true;busyRevealAt=Time.unscaledTime+BusyPanelDelay;status=null;
            SetMenuNavigation(true);
            return true;
        }
        private void UpdateBusyPresentation(float now)
        {
            // Read readiness again: successful startup must win over a timer/error view in this frame.
            if(!busyPresentationPending||page!="Busy"||session.ReadyForPresentation)return;
            if(session.Connecting&&!session.Leaving&&now<busyRevealAt)return;
            busyPresentationPending=false;
            Panel(pageRoot,0,0,1600,900,new Color(.03f,.04f,.035f,.42f));
            BuildBusy();ConfigureMenuPage();
            if(Application.isPlaying)StartCoroutine(FocusFirst());
        }
        private void BuildBusy()
        {
            var panel=Panel(pageRoot,430,285,740,330);
            Text(panel,session.Leaving?"RETURNING TO MENU":session.Connecting?"GETTING READY":"SESSION COULD NOT START",30,26,680,48,30,Paper,true);
            status=Text(panel,session.Status,30,100,680,92,22,Muted);
            var retry=Button(panel,session.Connecting?"CANCEL":"RETRY",30,228,330,()=>{if(session.Connecting){session.CancelConnection();if(session.Training)session.EndTraining(false);Show("Main");}else if(busyJoin){session.Join(textEntryOriginal??"127.0.0.1");Show("Busy");}else if(busyTraining){if(session.Training)session.EndTraining(false);StartMenuTraining();}else StartMenuSession(busySolo);},true);
            var back=Button(panel,"BACK TO MENU",380,228,330,()=>MenuBack());retry.interactable=back.interactable=!session.Leaving;BuildMenuFooter();
        }
        private void BuildCoop()
        {
            if(!RecoveryPlatform.MultiplayerAvailable)return;
            Header("PLAY CO-OP","Two drivers. One recovery. Connect directly using the host's address.");
            Input(pageRoot,"YOUR NAME",P.name,76,218,1448,v=>{P.name=CleanName(v);P.Save();});
            var host=Panel(pageRoot,76,346,710,390);var join=Panel(pageRoot,810,346,714,390);
            Icon(host,RecoveryIcon.Crew,26,27,30,Amber);Text(host,"HOST A SESSION",70,22,610,44,27,Paper,true);
            Text(host,"Open the workshop yard for your crew. Share your computer's local IP address with your friend.",28,97,654,104,21,Muted);
            Text(host,"Direct IP / LAN · UDP 7777 · 2 players",28,221,654,48,16,Muted);
            Button(host,"HOST SESSION",28,301,654,()=>StartMenuSession(false),true);
            Text(join,"JOIN A FRIEND",28,22,654,44,27,Paper,true);
            address=Input(join,"HOST ADDRESS",RecoveryProfile.Storage.GetString(LastHostPreference,""),28,94,658,v=>{if(Application.isPlaying&&!P.SuppressSaving){RecoveryProfile.Storage.SetString(LastHostPreference,v.Trim());SaveLocalPreferences();}});
            Text(join,"Ask the host for their IP address. 127.0.0.1 connects to this computer only.",28,221,658,60,16,Muted);
            Button(join,"JOIN SESSION",28,301,658,()=>{if(string.IsNullOrWhiteSpace(address.text)){status.text="Enter your friend's host address first.";return;}busyJoin=true;textEntryOriginal=address.text.Trim();session.Join(textEntryOriginal);Show("Busy");});
            status=Text(pageRoot,session.Status,326,770,1198,50,17,Muted);Footer();
        }
        private static void SaveLocalPreferences() {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!string.IsNullOrEmpty(RecoveryProfile.ValidationStorageKey)) { RecoveryProfile.Storage.Flush(); return; }
#endif
            HalvaStudio.Save.SaveManager.RequestSave(); }
        private string LastHostPreference {
            get {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if(!string.IsNullOrEmpty(RecoveryProfile.ValidationStorageKey))return RecoveryProfile.ValidationStorageKey+".host";
#endif
                return "TowTrouble.LastHost";
            }
        }
        private static string CleanName(string value){value=(value??"").Replace("<","").Replace(">","").Trim();return string.IsNullOrEmpty(value)?"Driver":value.Substring(0,Mathf.Min(20,value.Length));}
        private void BuildLobby()
        {
            if(!RecoveryPlatform.MultiplayerAvailable)return;
            Header("YOUR CREW","Get ready, then head out into Tow County together.");
            var side=Panel(pageRoot,76,224,430,504);
            Text(side,"WORKSHOP YARD",28,25,374,45,28,Paper,true);
            summary=Text(side,"Explore together. The host chooses recoveries after departure.",28,101,374,100,22,Muted);
            slots=Text(side,"",28,226,374,70,19,Paper);
            lobbyState=Text(side,"",28,336,374,122,23,Amber,true);
            for(int i=0;i<2;i++){var card=Panel(pageRoot,540+i*496,568,470,156);Icon(card,RecoveryIcon.Body,20,18,28,i==0?Blue:Orange);Text(card,"DRIVER "+(i+1),62,16,384,32,17,Muted,true);worldNames[i]=Text(card,"",20,61,430,78,21,Paper,true);RecoveryLocalizedText.Bind(worldNames[i],false);}
            lobbyReady=Button(pageRoot,"READY",540,756,410,()=>session.ToggleReady(),true);readyText=lobbyReady.GetComponentInChildren<TMP_Text>();
            startButton=Button(pageRoot,"START EXPLORING",974,756,536,()=>session.Deploy());
            Button(pageRoot,"LEAVE SESSION",76,808,360,()=>ConfirmAction("Leave this session and return to the main menu?",()=>{session.LeaveToMain();Show("Busy");},"Lobby","LEAVE SESSION"));
            BuildMenuFooter();preferredMenuFocus="READY";
        }
        private void RefreshMenuConnection()
        {
            if(!Application.isPlaying)return;
            if(page=="Busy"&&status!=null){status.text=session.Status;if(!session.Connecting&&!session.Connected&&status.text!=busyStatus){busyStatus=status.text;Show("Busy");}}
            if(page!="Lobby"||!session.Connected)return;
            var world=session.World;
            slots.text=world.Occupied+" / 2 drivers\n"+(session.IsHost?"You are hosting":"Connected to host");
            for(int i=0;i<2;i++)worldNames[i].text=world.OccupiedSlot(i)?world.DriverName(i)+"\n"+RecoveryLocalization.Translate(world.Ready(i)?"Ready to depart":"Getting ready"):RecoveryLocalization.Translate("Waiting for a friend…");
            readyText.text=world.Ready(session.LocalSlot)?"NOT READY":"READY";
            startButton.interactable=session.IsHost&&world.CanStart;
            lobbyState.text=world.Occupied<2?"Waiting for another driver":!world.Ready(session.LocalSlot)?"Mark yourself ready":!world.CanStart?"Waiting for your friend":session.IsHost?"Your crew is ready":"Waiting for the host to depart";
        }
        private string busyStatus;
        private void BuildPause()
        {
            bool cable=session.World!=null&&session.World.HasContract&&session.LocalSlot>=0&&session.World.Attached(session.LocalSlot);
            float height=cable?650:552;
            var card=Panel(pageRoot,500,(900-height)*.5f,600,height);Text(card,"PAUSED",30,24,540,56,38,Paper,true);
            Text(card,session.Training?"Practice mode · your career equipment and money are safe.":session.Solo?(RecoveryPlatform.MultiplayerAvailable?"Take your time. Solo play is paused.":"Take your time. The game is paused."):"Your car stays braked. Your crew keeps playing.",30,96,540,76,20,Muted);
            Button(card,"RESUME",30,194,540,()=>{session.ConsumeMenuInput();session.ToggleMenu();Show("Gameplay");},true);
            Button(card,session.Training?"HOW TO PLAY":"RECOVERIES",30,262,540,()=>{if(session.Training){Show("Help");return;}session.CloseOverlay();session.ToggleBoard();Show("Board");});
            Button(card,"SETTINGS",30,330,540,()=>{returnPage="Pause";firstSetup=false;Show("Settings");});
            Button(card,"RESET VEHICLE",30,398,540,()=>{session.ConsumeMenuInput();session.ResetAtCheckpoint();Show("Gameplay");});
            Button(card,session.Training?"END TRAINING":"MAIN MENU",30,466,540,()=>ConfirmAction(session.Training?"End training? Your career progress is unchanged.":session.World.HasContract?"Return to the main menu? This recovery will be abandoned without payment. Purchased equipment and earned credits stay saved.":"Leave this session and return to the main menu?",()=>{session.LeaveToMain();Show("Busy");},"Pause",session.Training?"END TRAINING":"MAIN MENU"));
            BuildCableDetails(card);BuildMenuFooter();
        }
        private void OpenTextEntry(TMP_InputField field)
        {
            textEntryDraft=field.text;textEntryLabel=field.name;textEntryLimit=field.characterLimit;
            textEntryCommit=value=>{if(textEntryLabel=="YOUR NAME"){P.name=CleanName(value);P.Save();}else if (!P.SuppressSaving){RecoveryProfile.Storage.SetString(LastHostPreference,value.Trim());SaveLocalPreferences();}};
            Show("TextEntry");
        }
        private void BuildTextEntry()
        {
            Header(textEntryLabel,"Select letters with the D-pad or left stick. Choose Done to save, or Back to cancel.");
            var panel=Panel(pageRoot,250,220,1100,480);
            var entry=Text(panel,"",30,22,1040,64,25,Paper,true);entry.enableAutoSizing=false;entry.textWrappingMode=TextWrappingModes.NoWrap;
            string draft=textEntryDraft??"",visible=draft+"│";
            // Keep the caret and the end being edited readable even for a long host address.
            while(draft.Length>0&&entry.GetLocalizedPreferredValues(visible).x>1040){draft=draft.Substring(1);visible="…"+draft+"│";}
            RecoveryLocalizedText.Bind(entry,false);entry.text=visible;
            const string letters="1234567890abcdefghijklmnopqrstuvwxyz.-:_";
            for(int i=0;i<letters.Length;i++){char ch=letters[i];Button(panel,ch.ToString(),30+i%10*104,112+i/10*66,94,()=>{if(textEntryDraft.Length<textEntryLimit)textEntryDraft+=ch;Show("TextEntry");},false,52);}
            Button(panel,"SPACE",30,390,220,()=>{if(textEntryLabel=="YOUR NAME"&&textEntryDraft.Length<textEntryLimit)textEntryDraft+=" ";Show("TextEntry");});
            Button(panel,"DELETE",272,390,220,()=>{if(textEntryDraft.Length>0)textEntryDraft=textEntryDraft.Substring(0,textEntryDraft.Length-1);Show("TextEntry");});
            Button(panel,"DONE",514,390,272,()=>{textEntryCommit(textEntryDraft);Show("Coop");},true);
            Button(panel,"CANCEL",808,390,262,()=>Show("Coop"));BuildMenuFooter();preferredMenuFocus="a";
        }
        private void BuildCredits()
        {
            Header("CREDITS","Tow Trouble · Bad roads. Good company.");
            if(creditsNotices)BuildNotices(Panel(pageRoot,176,224,1248,488));
            else
            {
                var card=Panel(pageRoot,250,258,1100,368);Icon(card,RecoveryIcon.Crew,30,30,40,Amber);
                Text(card,"TOW TROUBLE",86,24,984,54,37,Paper,true);
                Text(card,"An independent vehicle recovery game",30,106,1040,48,25,Paper);
                Text(card,"Thank you for exploring Tow County and bringing its stranded drivers home.",30,182,1040,68,22,Muted);
                Button(card,"THIRD-PARTY NOTICES",30,286,650,()=>{creditsNotices=true;Show("Credits");});
            }
            Footer();
        }
        private bool creditsNotices;
        private void BuildNotices(RectTransform card)
        {
            // Separate the scrollable legal text from the readable game credits.
            var scrollRoot=Node("Third-party notices",card,28,24,1192,408);menuScroll=scrollRoot.gameObject.AddComponent<RecoverySmoothScrollRect>();
            menuScroll.horizontal=false;menuScroll.scrollSensitivity=45;menuScroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
            var viewport=Stretch("Viewport",scrollRoot);viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color=Color.white;viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=false;
            string notices=Resources.Load<TextAsset>("TowThirdPartyNotices")?.text??"Third-party notices could not be loaded.";
            var content=Node("Notices content",viewport,0,0,1150,500);var text=Text(content,notices,0,0,1150,500,16,Muted);text.enableAutoSizing=false;RecoveryLocalizedText.Bind(text,false);
            float height=Mathf.Max(420,text.GetLocalizedPreferredValues(notices,1150,0).y+20);content.sizeDelta=new Vector2(1150,height);text.rectTransform.sizeDelta=content.sizeDelta;
            menuScroll.content=content;menuScroll.viewport=viewport;
            ControlText(Text(card,session.UsingGamepad?"Right stick · Scroll":"Page Up / Page Down / Mouse wheel · Scroll",28,440,1192,36,18,Muted));
        }
    }
}

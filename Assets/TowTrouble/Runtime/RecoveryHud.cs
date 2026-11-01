using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace TowTrouble
{
    public sealed partial class RecoveryHud : MonoBehaviour
    {
        [SerializeField] private DemoSession session;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Texture2D[] contractImages;
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Texture2D logo;
        private RectTransform root, pageRoot;
        private GameObject gameplay;
        private string page="", returnPage="Main", settingsTab="Audio", pendingBinding;
        private int zone;
        private string confirmationReturn="Pause",focusName;
        private bool wasOnline, wasComplete, firstSetup;
        private RecoveryPhase previousPhase;
        private float refreshAt;
        private TMP_Text objective, mudFeedback, crew, speed, cableText, actionText, marker, partnerText, status, slots, readyText, summary, prompt;
        private UnityEngine.UI.Image cableFill, partnerFill;
        private GameObject cableGroup, partnerGroup;
        private UnityEngine.UI.Button startButton;
        private TMP_InputField address;
        private AudioSource uiAudio;
        private AudioClip clickClip;
        private readonly Dictionary<TMP_Text,float> textSizes=new Dictionary<TMP_Text,float>();
        private readonly TMP_Text[] worldNames=new TMP_Text[2];
        private readonly TMP_Text[] hudNames=new TMP_Text[2];
        private readonly RectTransform[] hudPlates=new RectTransform[2];
        private static readonly Color Ink=new Color(.065f,.075f,.095f,.94f), Paper=new Color(1,.96f,.83f), Amber=new Color(1,.71f,.23f), Muted=new Color(.79f,.79f,.72f), Blue=new Color(.15f,.43f,1), Orange=new Color(1,.37f,.05f);
        private RecoveryProfile P=>session.Profile;
        private string L(string ro,string en)=>P.L(ro,en);
        public void Configure(DemoSession owner,TMP_FontAsset asset){session=owner;font=asset;}
        public void ConfigureImages(Texture2D[] values){contractImages=values;}
        public void ConfigureSkin(Sprite value){panelSprite=value;}
        public void ConfigureLogo(Texture2D value){logo=value;}
        private void Start()
        {
            if(EventSystem.current==null)new GameObject("UI Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var canvas=new GameObject("Tow Trouble UI",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;root=(RectTransform)canvas.transform;
            uiAudio=gameObject.AddComponent<AudioSource>();uiAudio.playOnAwake=false;clickClip=AudioClip.Create("UI confirm",1500,1,44100,false);var samples=new float[1500];for(int i=0;i<samples.Length;i++)samples[i]=Mathf.Sin(i*.14f)*.05f*(1-i/(float)samples.Length);clickClip.SetData(samples,0);
            BuildGameplay();Show("Title");
        }
        private void OnDestroy(){if(clickClip!=null)Destroy(clickClip);}
        private RectTransform Node(string name,Transform parent,float x,float y,float width,float height)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(width,height);return rt;}
        private RectTransform Stretch(string name,Transform parent)
        {var rt=Node(name,parent,0,0,0,0);rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;return rt;}
        private RectTransform Panel(Transform parent,float x,float y,float width,float height,Color? color=null)
        {
            var rt=Node("Panel",parent,x,y,width,height);var image=rt.gameObject.AddComponent<UnityEngine.UI.Image>();var tint=color??Ink;image.color=tint;image.raycastTarget=false;
            if(height>28&&width>100){image.color=Color.clear;Plate(rt,tint);}
            return rt;
        }
        private TMP_Text Text(Transform parent,string text,float x,float y,float width,float height,float size=22,Color? color=null,bool bold=false)
        {var rt=Node(text,parent,x,y,width,height);var t=rt.gameObject.AddComponent<TextMeshProUGUI>();t.font=bold&&size>=26&&displayFont!=null?displayFont:font;t.text=text;t.fontSize=size*P.textScale;t.enableAutoSizing=true;t.fontSizeMin=size*Mathf.Min(1,P.textScale);t.fontSizeMax=size*P.textScale;t.color=color??Paper;t.fontStyle=bold&&t.font!=displayFont?FontStyles.Bold:FontStyles.Normal;t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;textSizes[t]=size;return t;}
        private UnityEngine.UI.Button Button(Transform parent,string label,float x,float y,float width,Action action,bool primary=false,float height=54)
        {
            var rt=Panel(parent,x,y,width,height);rt.name=label;var img=rt.GetComponent<UnityEngine.UI.Image>();img.raycastTarget=true;img.color=Color.clear;
            var plate=rt.GetComponentInChildren<RecoveryPlateGraphic>();if(plate==null)plate=Plate(rt,Ink,primary);plate.Style=primary?RecoveryPlateStyle.Gold:RecoveryPlateStyle.Charcoal;
            var button=rt.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=plate;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.97f,.87f);colors.selectedColor=Color.white;colors.pressedColor=new Color(.85f,.77f,.62f);colors.disabledColor=new Color(.9f,.9f,.9f,1);colors.fadeDuration=.1f;button.colors=colors;
            float padding=width<150?8:22;var text=Text(rt,label,padding,0,width-padding*2,height,21,primary?Ink:Paper,true);text.alignment=TextAlignmentOptions.MidlineLeft;
            if(displayFont!=null){text.font=displayFont;text.fontStyle=FontStyles.Normal;}
            text.enableAutoSizing=true;text.fontSizeMin=14;text.fontSizeMax=21*P.textScale;
            var icon=ActionIcon(label);if(icon.HasValue&&width>260&&height>=48)ButtonIcon(button,icon.Value,primary);
            rt.gameObject.AddComponent<RecoveryUiFeedback>();button.onClick.AddListener(()=>{uiAudio.PlayOneShot(clickClip,P.ui);action();});return button;
        }
        private void Header(string title,string sub)
        {Panel(pageRoot,0,0,1600,196,new Color(.03f,.04f,.05f,.64f));Icon(pageRoot,RecoveryIcon.Hook,28,65,34,Amber);Text(pageRoot,title,80,58,1120,76,46,Paper,true);Panel(pageRoot,82,136,82,4,Amber);Text(pageRoot,sub,82,151,1400,40,18,Muted);}
        private void Logo(Transform parent,float x,float y,float size=82)
        {if(logo!=null){var rt=Node("Tow Trouble Logo",parent,x,y,size*6.0f,size*2.55f);var image=rt.gameObject.AddComponent<UnityEngine.UI.RawImage>();image.texture=logo;image.raycastTarget=false;}else Text(parent,"TOW\nTROUBLE",x,y,630,size*2.5f,size,Amber,true);}
        private void Footer(string back="Main")
        {Panel(pageRoot,0,850,1600,50,new Color(.03f,.04f,.05f,.52f));Button(pageRoot,L("ÎNAPOI","BACK"),76,808,230,()=>Show(back));Text(pageRoot,session.UsingGamepad?"A  "+L("Confirmă","Confirm")+"     B  "+L("Înapoi","Back"):"ENTER  "+L("Confirmă","Confirm")+"     ESC  "+L("Înapoi","Back"),340,866,1184,28,16,Paper).alignment=TextAlignmentOptions.Right;}
        public void Show(string target)
        {
            focusName=page==target?EventSystem.current?.currentSelectedGameObject?.name:null;
            if(pageRoot!=null){foreach(var t in pageRoot.GetComponentsInChildren<TMP_Text>())textSizes.Remove(t);EventSystem.current?.SetSelectedGameObject(null);Destroy(pageRoot.gameObject);pageRoot=null;}
            if(target=="Garage"&&page!="Garage")PrepareGarage();
            page=target;session.View=target;pendingBinding=null;session.World.ShowSinglePreview(target!="Title"&&target!="Lobby"&&(!session.Connected||session.World.Phase==RecoveryPhase.Lobby),Mathf.Max(0,session.LocalSlot));
            if(target=="Gameplay"){gameplay.SetActive(true);return;}
            gameplay.SetActive(false);pageRoot=Stretch(target,root);
            if(target!="Title"&&target!="Main"&&target!="Lobby"&&target!="Vehicles"&&target!="Garage")Panel(pageRoot,0,0,1600,900,new Color(.03f,.04f,.035f,.42f));
            switch(target){case "Garage":BuildGarage();break;case "Board":BuildBoard();break;case "Title":BuildTitle();break;case "Main":BuildMain();break;case "Contracts":BuildContracts();break;case "Vehicles":BuildVehicles();break;case "Coop":BuildCoop();break;case "Lobby":BuildLobby();break;case "Settings":BuildSettings();break;case "Credits":BuildCredits();break;case "Pause":BuildPause();break;case "Results":BuildResults();break;}
            StartCoroutine(FocusFirst());
        }
        private IEnumerator FocusFirst()
        {
            var target=pageRoot;string remembered=focusName;yield return null;if(page=="Gameplay"||target==null||target!=pageRoot||!target.gameObject.activeInHierarchy)yield break;
            var controls=target.GetComponentsInChildren<UnityEngine.UI.Selectable>();UnityEngine.UI.Selectable preferred=null;
            foreach(var control in controls)if(control.IsInteractable())
            {if(control.name==remembered){preferred=control;break;}if(preferred==null)preferred=control;var plate=control.GetComponentInChildren<RecoveryPlateGraphic>();if(remembered==null&&plate!=null&&plate.Style==RecoveryPlateStyle.Gold){preferred=control;break;}}
            if(preferred!=null&&EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(preferred.gameObject);
        }
        private void BuildTitle()
        {
            Logo(pageRoot,100,70,96);Text(pageRoot,L("DRUMURI GRELE. PRIETENI BUNI.","BAD ROADS. GOOD COMPANY."),108,333,700,40,21,Paper,true);
            var titlePrompt=Panel(pageRoot,450,738,700,72);titlePrompt.GetComponentInChildren<RecoveryPlateGraphic>().Style=RecoveryPlateStyle.Gold;
            prompt=Text(pageRoot,"",0,750,1600,48,26,Ink,true);prompt.alignment=TextAlignmentOptions.Center;
            Text(pageRoot,"TOW TROUBLE  •  SOLO / CO-OP",76,847,1000,25,13,Muted);
        }
        private void BuildMain()
        {
            Panel(pageRoot,48,35,490,830,new Color(.035f,.045f,.04f,.9f));Logo(pageRoot,80,55,66);
            Text(pageRoot,L("DRUMURI GRELE. PRIETENI BUNI.","BAD ROADS. GOOD COMPANY."),86,238,410,28,15,Muted,true);Wallet(pageRoot,P.credits);
            float y=276;
            if(P.completedMask!=0){Button(pageRoot,L("CONTINUĂ","CONTINUE"),80,y,410,()=>session.Host(true,true),true);y+=60;}
            Button(pageRoot,L("EXPLOREAZĂ","EXPLORE"),80,y,410,()=>session.Host(true,true),P.completedMask==0);y+=60;
            Button(pageRoot,L("TUTORIAL / CONTRACTE","TUTORIAL / CONTRACTS"),80,y,410,()=>{if(session.World.Map!=null)session.OpenTutorial();else Show("Contracts");});y+=60;
            Button(pageRoot,L("JOACĂ ÎN DOI","PLAY CO-OP"),80,y,410,()=>Show("Coop"));y+=60;
            Button(pageRoot,L("VEHICULE","VEHICLES"),80,y,410,()=>{returnPage="Main";Show("Vehicles");});y+=60;
            Button(pageRoot,L("SETĂRI","SETTINGS"),80,y,410,()=>{returnPage="Main";Show("Settings");});y+=60;
            Button(pageRoot,L("CREDITE","CREDITS"),80,y,410,()=>Show("Credits"));y+=60;
            Button(pageRoot,L("IEȘIRE","QUIT"),80,y,410,()=>Application.Quit());
            var vehicleCard=Panel(pageRoot,1094,710,430,122);Icon(vehicleCard,RecoveryIcon.Body,20,17,30,Amber);Text(vehicleCard,L("MAȘINA TA","YOUR RIDE"),62,14,344,25,14,Muted,true);
            Text(vehicleCard,RecoveryVehicle.ModelNames[P.vehicle],22,49,386,47,34,Paper,true);Text(pageRoot,session.World.Map!=null?"TOW COUNTY · RECOVERY SERVICE":L("VALEA VERDE · SERVICIUL DE RECUPERARE","GREEN VALLEY · RECOVERY SERVICE"),900,844,624,28,14,Paper,true).alignment=TextAlignmentOptions.Right;
        }
        private void BuildContracts()
        {
            Header(L("ALEGE CONTRACTUL","CHOOSE A CONTRACT"),L("O mașină blocată. Un plan bun. O tragere lină.","A stranded car. A good plan. A steady pull."));
            for(int i=0;i<3;i++){int z=i;Button(pageRoot,L(RecoveryContract.Zones[i],new[]{"Green Valley","Blue Lake","Red Canyon"}[i]),76+i*486,198,466,()=>{zone=z;Show("Contracts");},zone==i);}
            var left=Panel(pageRoot,76,278,580,480);var right=Panel(pageRoot,680,278,844,480);
            if(zone==0)
            {
                for(int i=0;i<3;i++){int id=i;var c=RecoveryContract.Get(i);string medal=P.Completed(i)?"+  ":P.Unlocked(i)?"o  ":"—  ";Button(left,medal+c.Id.ToString("00")+"  "+c.TitleFor(P),24,24+i*92,532,()=>{session.SelectContract(id);Show("Contracts");},session.SelectedContract==i,76);}
                Text(left,L("SOLO / CO-OP 2\nFinalizează contractele pentru a deschide următorul traseu.","SOLO / CO-OP 2\nComplete contracts to unlock the next route."),28,332,520,110,20,Muted);
                var selectedContract=RecoveryContract.Get(session.SelectedContract);
                if(contractImages!=null&&contractImages.Length>session.SelectedContract&&contractImages[session.SelectedContract]!=null)
                {ContractPhoto(right,session.SelectedContract,18,18,808,220);}
                Text(right,selectedContract.TitleFor(P),30,258,780,44,31,Paper,true);Text(right,selectedContract.DescriptionFor(P)+"\n"+L("Teren: ","Terrain: ")+selectedContract.TerrainFor(P)+"   •   "+L("Recomandat: Scout / Hauler","Recommended: Scout / Hauler"),30,310,780,90,20,Muted);
                Text(right,L("Medalii: finalizare · fără resetări · fără cablu rupt","Medals: complete · no resets · no broken cable"),30,423,780,40,17,Amber);
                Button(pageRoot,L("SCHIMBĂ MAȘINA","CHANGE VEHICLE"),680,790,300,()=>{returnPage="Contracts";Show("Vehicles");});
                startButton=Button(pageRoot,L("PORNEȘTE CONTRACTUL","START CONTRACT"),1004,790,520,()=>{if(session.Connected){session.SelectContract(session.SelectedContract);Show("Lobby");}else session.Host(true);},true);
                startButton.interactable=P.Unlocked(session.SelectedContract)&&(!session.Connected||session.IsHost);
            }
            else
            {
                string[] titles=zone==1?new[]{"04  Pârâul","06  Malul moale","07  Drumul lung"}:new[]{"08  Înghițit de nisip","09  Curba suspendată","10  Ultima tragere","12  Două unghiuri · Duo"};
                for(int i=0;i<titles.Length;i++)Text(left,titles[i],28,30+i*72,520,52,25,Muted,true);
                Text(right,L("ALTE DRUMURI NE AȘTEAPTĂ","MORE ROADS AHEAD"),36,45,760,60,32,Amber,true);
                Text(right,L("Intervențiile din această regiune nu sunt încă disponibile. Între timp, echipa are nevoie de tine în Valea Verde.","Recoveries in this region are not available yet. Your crew needs you in Green Valley."),36,137,760,220,25,Paper);
            }
            Footer();
        }
        private void BuildVehicles()
        {
            Header(L("VEHICULE","VEHICLES"),L("Alege partenerul pentru următoarea recuperare.","Choose your partner for the next recovery."));
            var stats=Panel(pageRoot,76,220,390,330);int selected=P.vehicle;
            Text(stats,RecoveryVehicle.ModelNames[selected],24,24,340,52,35,Amber,true);
            string[] labels={L("TRACȚIUNE","TRACTION"),L("MANEVRABILITATE","HANDLING"),L("STABILITATE","STABILITY")};
            float[] values={RecoveryVehicle.PullForces[selected]/3900f,RecoveryVehicle.SteeringAngles[selected]/35f,RecoveryVehicle.Masses[selected]/1750f};
            for(int i=0;i<3;i++){Text(stats,labels[i],24,100+i*65,340,25,17,Paper,true);Panel(stats,24,134+i*65,340,7,new Color(.25f,.28f,.24f));Panel(stats,24,134+i*65,340*values[i],7,Amber);}
            Text(pageRoot,L("Fiecare mașină are punctele sale forte.\nTrage cu mouse-ul sau folosește stickul drept pentru rotire.","Every ride has its strengths.\nDrag the mouse or use the right stick to rotate."),80,580,450,120,18,Muted);
            string[] paintLabels={L("ORIGINAL","ORIGINAL"),L("CREM","CREAM"),L("MĂSLINIU","OLIVE"),L("MUȘTAR","MUSTARD")};for(int i=0;i<4;i++){int c=i;Button(pageRoot,paintLabels[i],620+i*220,634,200,()=>{session.SelectPaint(c);Show("Vehicles");},P.paint==i,48);}
            for(int i=0;i<4;i++){int v=i;bool unlocked=i==0||i==3||P.Completed(i==1?0:1);Button(pageRoot,(unlocked?"":"—  ")+RecoveryVehicle.ModelNames[i],76+i*368,714,348,()=>{if(unlocked){session.SelectVehicle(v);Show("Vehicles");}},P.vehicle==i,64).interactable=unlocked;}
            Button(pageRoot,L("SELECTEAZĂ","SELECT"),1184,808,340,()=>Show(returnPage),true);Footer(returnPage);
        }
        private TMP_InputField Input(Transform parent,string label,string value,float x,float y,float width,Action<string> changed)
        {
            Text(parent,label,x,y,width,30,17,Muted,true);var rt=Panel(parent,x,y+34,width,54,new Color(.03f,.045f,.04f));rt.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var input=rt.gameObject.AddComponent<TMP_InputField>();var viewport=Node("Viewport",rt,14,4,width-28,46);var text=Text(viewport,"",0,0,width-28,46,23);text.alignment=TextAlignmentOptions.MidlineLeft;input.textViewport=viewport;input.textComponent=(TextMeshProUGUI)text;input.text=value;input.characterLimit=label.Contains("IP")?253:20;input.onEndEdit.AddListener(v=>changed(v));return input;
        }
        private void BuildCoop()
        {
            Header(L("JOACĂ ÎN DOI","PLAY CO-OP"),L("Două mașini. Două cabluri. Aceeași țintă.","Two cars. Two cables. One recovery."));
            var card=Panel(pageRoot,76,225,650,520);
            Input(card,L("NUMELE TĂU","YOUR NAME"),P.name,30,30,590,v=>{P.name=v;P.Save();});
            Text(card,L("GAZDĂ","HOST"),30,149,590,36,26,Amber,true);
            Text(card,L("Porniți împreună, apoi alegeți intervenția cu Tab. Prietenul se conectează la IP-ul acestui calculator, pe portul UDP 7777.","Start together, then choose a recovery with Tab. Your friend joins this computer's IP address on UDP port 7777."),30,201,590,110,22,Paper);
            Button(card,L("CREEAZĂ SESIUNE","HOST SESSION"),30,350,590,()=>session.Host(false,true),true);
            var join=Panel(pageRoot,754,225,770,520);Text(join,L("ALĂTURĂ-TE ECHIPEI","JOIN THE CREW"),30,30,710,50,31,Paper,true);
            address=Input(join,"IP / HOST",PlayerPrefs.GetString("TowTrouble.LastHost","127.0.0.1"),30,122,710,v=>PlayerPrefs.SetString("TowTrouble.LastHost",v));
            Button(join,L("CONECTEAZĂ","JOIN SESSION"),30,252,710,()=>session.Join(address.text));
            status=Text(join,"",30,341,710,130,20,Muted);Footer();
        }
        private void BuildLobby()
        {
            Header(L("ECHIPA DE RECUPERARE","RECOVERY CREW"),L("Gazda pornește contractul când amândoi sunt pregătiți.","The host starts when both drivers are ready."));
            for(int i=0;i<2;i++){var plate=Panel(pageRoot,430+i*460,560,420,120);Text(plate,"PLAYER "+(i+1),20,14,380,30,22,i==0?Blue:Orange,true);worldNames[i]=Text(plate,"",20,48,380,62,21,Paper,true);}
            var side=Panel(pageRoot,76,220,340,520);Text(side,L("CONTRACT","CONTRACT"),24,24,292,25,16,Amber,true);
            summary=Text(side,"",24,70,292,140,25,Paper,true);slots=Text(side,"",24,220,292,105,18,Muted);
            Button(side,session.World.Map!=null?L("CONTRACTE DUPĂ PORNIRE","JOBS AFTER DEPLOYMENT"):L("ALEGE CONTRACTUL","CHOOSE CONTRACT"),24,370,292,()=>Show("Contracts")).interactable=session.World.Map==null;
            Button(side,L("SCHIMBĂ MAȘINA","CHANGE VEHICLE"),24,444,292,()=>{returnPage="Lobby";Show("Vehicles");});
            var ready=Button(pageRoot,L("PREGĂTIT","READY"),470,752,420,()=>session.ToggleReady());readyText=ready.GetComponentInChildren<TMP_Text>();
            startButton=Button(pageRoot,L("PORNEȘTE CONTRACTUL","START CONTRACT"),920,752,590,()=>session.Deploy(),true);
            Button(pageRoot,L("PĂRĂSEȘTE ECHIPA","LEAVE CREW"),76,808,340,()=>Confirm(L("Părăsești sesiunea?","Leave this session?"),()=>session.Leave(),"Lobby"));
        }
        private void BuildGameplay()
        {
            gameplay=Stretch("Mission HUD",root).gameObject;
            objective=Text(gameplay.transform,"",30,25,800,82,24,Paper,true);objective.GetComponent<TextMeshProUGUI>().outlineWidth=.12f;
            mudFeedback=Text(gameplay.transform,"",30,116,980,48,18,Amber,true);
            garagePrompt=Text(gameplay.transform,"",440,842,720,42,19,Amber,true);garagePrompt.alignment=TextAlignmentOptions.Center;
            crew=Text(gameplay.transform,"",1140,25,430,90,20,Paper,true);crew.alignment=TextAlignmentOptions.Right;
            var cable=Node("Cable HUD",gameplay.transform,35,762,520,110);cableGroup=cable.gameObject;
            cableText=Text(cable,"",0,0,520,28,20,Paper,true);var track=Panel(cable,0,35,370,20,new Color(.08f,.09f,.08f,.75f));track.gameObject.AddComponent<UnityEngine.UI.Outline>().effectColor=Paper;
            var fill=Stretch("Cable tension",track);fill.offsetMin=new Vector2(3,3);fill.offsetMax=new Vector2(-3,-3);cableFill=fill.gameObject.AddComponent<UnityEngine.UI.Image>();
            actionText=Text(cable,"",0,66,650,52,17,Paper,true);
            winchText=Text(gameplay.transform,"",35,620,850,76,18,Paper,true);
            var partner=Node("Partner cable",gameplay.transform,35,706,370,46);partnerGroup=partner.gameObject;partnerText=Text(partner,"",0,0,370,24,16,Paper,true);var pt=Panel(partner,0,28,280,10,new Color(.08f,.09f,.08f,.75f));partnerFill=Stretch("Partner tension",pt).gameObject.AddComponent<UnityEngine.UI.Image>();
            var s=Node("Speed",gameplay.transform,1260,800,310,70);speed=Text(s,"",0,0,310,70,44,Paper,true);speed.alignment=TextAlignmentOptions.Right;
            marker=Text(gameplay.transform,"",0,0,340,70,18,Amber,true);marker.alignment=TextAlignmentOptions.Center;
            for(int i=0;i<2;i++)
            {
                hudPlates[i]=Node("Player "+(i+1)+" world tag",gameplay.transform,0,0,300,70);
                var plate=Panel(hudPlates[i],0,0,300,56,i==0?Blue:Orange);
                var arrow=Panel(hudPlates[i],143,49,14,14,i==0?Blue:Orange);arrow.localRotation=Quaternion.Euler(0,0,45);
                hudNames[i]=Text(plate,"",8,0,284,56,18,Paper,true);hudNames[i].alignment=TextAlignmentOptions.Center;
            }
        }
        private void BuildPause()
        {
            var card=Panel(pageRoot,520,150,560,610);Text(card,L("PAUZĂ","PAUSED"),32,30,496,60,41,Paper,true);
            Text(card,session.Solo?L("Ia o pauză. Mașina rămâne frânată.","Take a break. Your car stays braked."):L("Meniul este local. Echipa continuă să joace.","This menu is local. Your crew keeps playing."),32,108,496,70,20,Muted);
            Button(card,L("CONTINUĂ","RESUME"),32,207,496,()=>{session.ToggleMenu();Show("Gameplay");},true);
            Button(card,L("REVINO LA PUNCT SIGUR","RESET TO SAFE POINT"),32,277,496,()=>{session.ResetAtCheckpoint();Show("Gameplay");});
            Button(card,L("REÎNCEPE CONTRACTUL","RESTART CONTRACT"),32,347,496,()=>Confirm(L("Reîncepi și pierzi progresul acestui contract?","Restart and lose this run's progress?"),()=>{session.RestartContract();Show(session.World.FreeRoam?"Gameplay":"Lobby");},"Pause")).interactable=session.IsHost&&(!session.World.FreeRoam||session.World.HasContract);
            Button(card,L("SETĂRI","SETTINGS"),32,417,496,()=>{returnPage="Pause";Show("Settings");});
            if(session.World.FreeRoam)Button(card,L("INTERVENȚII","RECOVERIES"),32,487,496,()=>{session.CloseOverlay();session.ToggleBoard();Show("Board");});
            else Button(card,L("ÎNAPOI LA CONTRACTE","BACK TO CONTRACTS"),32,487,496,()=>Confirm(L("Părăsești misiunea curentă?","Leave the current mission?"),()=>{if(session.World.FreeRoam){session.CloseOverlay();session.ToggleBoard();Show("Board");}else if(session.IsHost){session.Replay();session.ToggleMenu();Show("Contracts");}else session.Leave();},"Pause"));
        }
        private void Confirm(string question,Action yes,string back)
        {
            if(pageRoot!=null){foreach(var t in pageRoot.GetComponentsInChildren<TMP_Text>())textSizes.Remove(t);EventSystem.current?.SetSelectedGameObject(null);Destroy(pageRoot.gameObject);}page="Confirm";focusName=null;confirmationReturn=back;session.View="Confirm";pageRoot=Stretch("Confirmation",root);Panel(pageRoot,0,0,1600,900,new Color(0,0,0,.65f));var card=Panel(pageRoot,475,300,650,290);Text(card,question,32,30,586,130,29,Paper,true);Button(card,L("CONFIRMĂ","CONFIRM"),32,200,270,yes,true);Button(card,L("RENUNȚĂ","CANCEL"),328,200,290,()=>Show(back));StartCoroutine(FocusFirst());
        }
        private void BuildResults()
        {
            session.RecordCompletion();var w=session.World;var card=Panel(pageRoot,440,126,720,650);Text(card,L("CONTRACT ÎNCHEIAT","CONTRACT COMPLETE"),36,34,648,60,40,Amber,true);
            Text(card,w.Contract.TitleFor(P),36,111,648,48,30,Paper,true);
            for(int i=0;i<3;i++){bool earned=i==0||(i==1?w.Resets(0)+w.Resets(1)==0:w.Snaps==0);var medal=Node("Medal "+i,card,126+i*190,175,66,66).gameObject.AddComponent<RecoveryMedalGraphic>();medal.color=earned?Amber:new Color(.25f,.28f,.24f);}
            string data=L("Timp   ","Time   ")+TimeSpan.FromSeconds(w.Elapsed).ToString(@"mm\:ss")+"\n"+L("Resetări   ","Resets   ")+(w.Resets(0)+w.Resets(1))+"   •   "+L("Cabluri rupte   ","Broken cables   ")+w.Snaps;
            data+="\n"+L("Recompensă: ","Reward: ")+session.LastReward+" CR   •   "+L("Sold: ","Balance: ")+P.credits+" CR";
            data+="\n"+(w.Resets(0)+w.Resets(1)==0?"+ ":"— ")+L("Fără resetări","No resets")+"    "+(w.Snaps==0?"+ ":"— ")+L("Fără cablu rupt","No cable breaks");
            if(w.Cooperative)data+="\n"+w.DriverName(0)+" / "+w.DriverName(1)+"  •  "+Mathf.RoundToInt(w.PullSeconds(0))+"s / "+Mathf.RoundToInt(w.PullSeconds(1))+"s "+L("în tracțiune","pulling");
            Text(card,data,36,270,648,160,21,Paper);
            if(w.FreeRoam)
            {Button(card,L("CONTINUĂ EXPLORAREA","CONTINUE EXPLORING"),36,470,648,()=>{session.FinishExplorationContract();Show("Gameplay");},true).interactable=session.IsHost;return;}
            Button(card,L("URMĂTORUL CONTRACT","NEXT CONTRACT"),36,449,648,()=>{session.Replay();session.SelectContract(Mathf.Min(2,w.ContractIndex+1));Show("Contracts");},true).interactable=session.IsHost;
            Button(card,L("REJOACĂ","REPLAY"),36,521,308,()=>{session.RestartContract();Show("Lobby");}).interactable=session.IsHost;
            Button(card,L("CONTRACTE","CONTRACTS"),368,521,316,()=>{session.Replay();Show("Contracts");}).interactable=session.IsHost;
            Button(card,L("VEHICULE","VEHICLES"),36,590,648,()=>{session.Replay();returnPage="Lobby";Show("Vehicles");}).interactable=session.IsHost;
        }
        private void Slider(Transform parent,string label,float value,float min,float max,float y,Action<float> change)
        {
            var text=Text(parent,label+"   "+Mathf.RoundToInt(value*100)+"%",30,y,770,32,22,Paper,true);var rt=Node(label,parent,30,y+48,750,20);var bg=Panel(rt,0,7,750,6,new Color(.25f,.3f,.26f));var fill=Stretch("Fill",bg);var fg=fill.gameObject.AddComponent<UnityEngine.UI.Image>();fg.color=Amber;
            var handle=Node("Handle",rt,0,-4,25,28);var hg=handle.gameObject.AddComponent<UnityEngine.UI.Image>();hg.color=Color.clear;var knob=Plate(handle,Amber,true);
            var slider=rt.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=knob;slider.direction=UnityEngine.UI.Slider.Direction.LeftToRight;slider.minValue=min;slider.maxValue=max;slider.value=value;
            slider.onValueChanged.AddListener(v=>{change(v);text.text=label+"   "+Mathf.RoundToInt(v*100)+"%";foreach(var entry in textSizes)if(entry.Key!=null){entry.Key.fontSize=entry.Value*P.textScale;entry.Key.fontSizeMax=entry.Value*P.textScale;entry.Key.fontSizeMin=entry.Value*Mathf.Min(1,P.textScale);}session.ApplySettings();P.Save();});
        }
        private void BuildSettings()
        {
            Header(firstSetup?L("BUN VENIT ÎN TOW TROUBLE","WELCOME TO TOW TROUBLE"):L("SETĂRI","SETTINGS"),firstSetup?L("Alege limba, luminozitatea și dimensiunea textului.","Choose language, brightness and text size."):L("Salvate automat pe acest calculator.","Saved automatically on this computer."));
            string[] tabs={"Audio","Imagine","Comenzi","Accesibilitate","Joc"};for(int i=0;i<tabs.Length;i++){string tab=tabs[i];Button(pageRoot,L(tab,new[]{"Audio","Display","Controls","Accessibility","Game"}[i]),76+i*292,196,272,()=>{settingsTab=tab;Show("Settings");},settingsTab==tab);}
            var body=Panel(pageRoot,76,278,1448,480);
            if(firstSetup){Button(body,L("LIMBĂ: ROMÂNĂ","LANGUAGE: ENGLISH"),30,30,750,()=>{P.language=P.language=="ro"?"en":"ro";P.Save();Show("Settings");});Slider(body,L("Luminozitate","Brightness"),P.brightness,.7f,1.4f,128,v=>P.brightness=v);Slider(body,L("Dimensiune text / HUD","Text / HUD size"),P.textScale,.85f,1.3f,258,v=>P.textScale=v);Button(pageRoot,L("SĂ ÎNCEPEM","LET'S GO"),1100,808,424,()=>{P.onboarded=true;P.Save();firstSetup=false;Show("Main");},true);return;}
            switch(settingsTab)
            {
                case "Audio":Slider(body,L("Volum general","Master volume"),P.master,0,1,30,v=>P.master=v);Slider(body,L("Motoare și efecte","Engines and effects"),P.effects,0,1,160,v=>P.effects=v);Slider(body,L("Interfață","Interface"),P.ui,0,1,290,v=>P.ui=v);break;
                case "Imagine":
                    Slider(body,L("Luminozitate","Brightness"),P.brightness,.7f,1.4f,30,v=>P.brightness=v);
                    Button(body,L("CALITATE: ","QUALITY: ")+QualitySettings.names[Mathf.Clamp(P.quality,0,QualitySettings.names.Length-1)],30,160,750,()=>{P.quality=(P.quality+1)%QualitySettings.names.Length;session.ApplySettings();P.Save();Show("Settings");});
                    Button(body,Screen.fullScreen?L("MOD: ECRAN COMPLET","MODE: FULLSCREEN"):L("MOD: FEREASTRĂ","MODE: WINDOWED"),30,240,750,()=>{Screen.fullScreen=!Screen.fullScreen;StartCoroutine(RefreshSettings());});
                    Button(body,L("REZOLUȚIE: ","RESOLUTION: ")+Screen.width+" × "+Screen.height,30,320,750,()=>{var values=Screen.resolutions;if(values.Length==0)return;int index=Array.FindIndex(values,r=>r.width==Screen.width&&r.height==Screen.height);var r=values[(index+1)%values.Length];Screen.SetResolution(r.width,r.height,Screen.fullScreenMode);StartCoroutine(RefreshSettings());});break;
                case "Comenzi":
                    string[] bindings={"hookKey","reelKey","extendKey","resetKey","forwardKey","reverseKey","leftKey","rightKey","brakeKey"};string[] labels={L("Conectează / desprinde","Attach / detach"),L("Scurtează cablul","Shorten cable"),L("Alungește cablul","Lengthen cable"),L("Punct sigur","Safe checkpoint"),L("Accelerație","Accelerate"),L("Marșarier","Reverse"),L("Stânga","Left"),L("Dreapta","Right"),L("Frână","Brake")};
                    for(int i=0;i<bindings.Length;i++){string binding=bindings[i];var field=typeof(RecoveryProfile).GetField(binding);int col=i<4?0:1,row=i<4?i:i-4;Button(body,labels[i]+"   [ "+field.GetValue(P)+" ]",30+col*710,24+row*60,680,()=>{pendingBinding=binding;status.text=L("Apasă o tastă. ESC anulează.","Press a key. ESC cancels.");},false,48);}
                    status=Text(body,L("Controller: RT/LT conducere, B frână, A cablu, LB/RB troliu, Y cameră.","Controller: RT/LT drive, B brake, A cable, LB/RB winch, Y camera."),30,333,1350,40,18,Muted);Slider(body,L("Sensibilitate cameră","Camera sensitivity"),P.cameraSensitivity,.3f,2,390,v=>P.cameraSensitivity=v);break;
                case "Accesibilitate":
                    Slider(body,L("Dimensiune text / HUD","Text / HUD size"),P.textScale,.85f,1.3f,30,v=>P.textScale=v);
                    Button(body,L("MIȘCARE CAMERĂ REDUSĂ: ","REDUCED CAMERA MOTION: ")+(P.reducedMotion?"ON":"OFF"),30,170,750,()=>{P.reducedMotion=!P.reducedMotion;P.Save();Show("Settings");});
                    Button(body,L("SIMBOLURI TENSIUNE: ","TENSION SYMBOLS: ")+(P.colorHelp?"ON":"OFF"),30,250,750,()=>{P.colorHelp=!P.colorHelp;P.Save();Show("Settings");});
                    Button(body,L("INVERSARE CAMERĂ: ","INVERT CAMERA: ")+(P.invertCamera?"ON":"OFF"),30,330,750,()=>{P.invertCamera=!P.invertCamera;P.Save();Show("Settings");});break;
                case "Joc":
                    Button(body,L("LIMBĂ: ROMÂNĂ","LANGUAGE: ENGLISH"),30,30,750,()=>{P.language=P.language=="ro"?"en":"ro";P.Save();Show("Settings");});
                    Button(body,L("INDICATOARE TUTORIAL: ","TUTORIAL HINTS: ")+(P.tutorial?"ON":"OFF"),30,110,750,()=>{P.tutorial=!P.tutorial;P.Save();Show("Settings");});
                    Button(body,L("VIBRAȚII CONTROLLER: ","CONTROLLER VIBRATION: ")+(P.vibration?"ON":"OFF"),30,190,750,()=>{P.vibration=!P.vibration;P.Save();Show("Settings");});break;
            }
            Footer(returnPage);
        }
        private IEnumerator RefreshSettings(){yield return null;yield return null;if(page=="Settings")Show("Settings");}
        private void BuildCredits()
        {
            Header(L("CREDITE","CREDITS"),"TOW TROUBLE");var card=Panel(pageRoot,76,220,1448,530);
            Text(card,"Stylized Nature & Environment MEGA PACK — So Stylized\nLow Poly Road Pack — Broken Vector\nHot Rod Constructor: Modular Battle Cars — Slava Z.\nHot Rod Constructor: Modular Off-Road Cars — Slava Z.\nLiving Mud URP — Dinopunch\nFishNet — FirstGearGames\nTextMesh Pro / Input System / URP — Unity",40,40,1368,310,26,Paper);
            Text(card,L("Imaginile conceptuale sunt referințe de artă. Misiunile sunt construite în Unity din asseturile originale.","Concept images are art references. Missions are built in Unity using the original assets."),40,402,1368,85,22,Muted);Footer();
        }
        private void Update()
        {
            if(root==null)return;bool online=session.Connected&&session.LocalSlot>=0;var w=session.World;
            if(pendingBinding!=null&&Keyboard.current!=null)
            {foreach(var key in Keyboard.current.allKeys)if(key.wasPressedThisFrame){if(key.keyCode!=Key.Escape)typeof(RecoveryProfile).GetField(pendingBinding).SetValue(P,key.keyCode.ToString());P.Save();pendingBinding=null;Show("Settings");return;}}
            if(page=="Title"&&!online)
            {
                prompt.text=session.UsingGamepad?L("APASĂ A PENTRU A CONTINUA","PRESS A TO CONTINUE"):L("APASĂ O TASTĂ PENTRU A CONTINUA","PRESS ANY KEY TO CONTINUE");
                if((Keyboard.current!=null&&Keyboard.current.anyKey.wasPressedThisFrame)||(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame)||(Gamepad.current!=null&&Gamepad.current.buttonSouth.wasPressedThisFrame))
                {if(P.onboarded)Show("Main");else{firstSetup=true;Show("Settings");}}return;
            }
            if(online&&!wasOnline)Show(w.Phase==RecoveryPhase.Lobby?"Lobby":"Gameplay");
            if(!online&&wasOnline)Show("Coop");
            if(online)
            {
                if(w.Phase==RecoveryPhase.Complete&&!wasComplete)Show("Results");
                else if(w.Phase!=previousPhase&&w.Phase!=RecoveryPhase.Complete){if(w.Phase!=RecoveryPhase.Lobby||(page!="Contracts"&&page!="Vehicles"))Show(w.Phase==RecoveryPhase.Lobby?"Lobby":"Gameplay");}
                if(session.MenuOpen&&page=="Gameplay"&&session.View!="Garage"&&session.View!="Board")Show("Pause");
                if(!session.MenuOpen&&page=="Pause")Show("Gameplay");
                wasComplete=w.Phase==RecoveryPhase.Complete;previousPhase=w.Phase;
            }
            if(online&&(session.View=="Garage"||session.View=="Board")&&page!=session.View)Show(session.View);
            else if(online&&(page=="Garage"||page=="Board")&&session.View=="Gameplay")Show("Gameplay");
            wasOnline=online;
            bool back=(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)||(Gamepad.current!=null&&Gamepad.current.buttonEast.wasPressedThisFrame);
            if(back&&page=="Confirm"){Show(confirmationReturn);return;}
            if(back&&page=="Lobby")Confirm(L("Părăsești sesiunea?","Leave this session?"),()=>session.Leave(),"Lobby");
            else if(back&&page!="Gameplay"&&page!="Pause"&&page!="Confirm"&&page!="Results"&&page!="Lobby"&&page!="Garage"&&page!="Board")Show(page=="Settings"||page=="Vehicles"?returnPage:"Main");
            if(Time.unscaledTime<refreshAt)return;refreshAt=Time.unscaledTime+.08f;
            if(status!=null&&pendingBinding==null&&page=="Coop")status.text=session.Status;
            if(page=="Lobby"&&online)
            {
                summary.text=w.Map!=null?L("EXPLORARE LIBERĂ\nAlegeți intervenția cu TAB după pornire.","FREE EXPLORATION\nChoose a job with TAB after deployment."):w.Contract.TitleFor(P)+"\n"+w.Contract.TerrainFor(P)+"\nSOLO / CO-OP";slots.text=w.Occupied+" / 2\n"+(session.IsHost?L("GAZDĂ • IP / LAN","HOST • IP / LAN"):L("CONECTAT LA GAZDĂ","CONNECTED TO HOST"))+"\nUDP 7777";
                for(int i=0;i<2;i++)if(worldNames[i]!=null)worldNames[i].text=w.OccupiedSlot(i)?w.DriverName(i)+" • "+RecoveryVehicle.ModelNames[w.VehicleIndex(i)]+"\n"+(w.Ready(i)?L("PREGĂTIT","READY"):L("SE PREGĂTEȘTE","GETTING READY")):L("AȘTEPTĂM UN PRIETEN…","WAITING FOR A FRIEND…");
                readyText.text=w.Ready(session.LocalSlot)?L("NU SUNT GATA","NOT READY"):L("PREGĂTIT","READY");startButton.interactable=session.IsHost&&w.CanStart;
            }
            if(page=="Board"&&online&&startButton!=null)startButton.interactable=session.IsHost&&w.Phase==RecoveryPhase.Explore&&!w.CrewInGarage;
            if(page=="Gameplay"&&online)RefreshGameplay();
        }
        private void RefreshGameplay()
        {
            marker.color=Amber;
            var w=session.World;int slot=session.LocalSlot,other=1-slot;bool attached=w.Attached(slot);float load=w.Tension(slot);
            RefreshWinchFeedback(w,slot,attached);
            if(w.Phase==RecoveryPhase.Explore)
            {objective.text=L("EXPLORARE LIBERĂ","FREE EXPLORATION");crew.text=P.credits+" CR";speed.text=Mathf.RoundToInt(w.Drivers[slot].Speed*3.6f)+" <size=27>km/h</size>";cableGroup.SetActive(false);partnerGroup.SetActive(false);mudFeedback.gameObject.SetActive(false);garagePrompt.text=w.AtGarage(slot)?L("G · Intră în garaj    |    TAB · Contracte","G · Enter garage    |    TAB · Contracts"):L("TAB · Contracte    |    Garaj: ","TAB · Contracts    |    Garage: ")+Mathf.RoundToInt(Vector3.Distance(w.Drivers[slot].transform.position,w.GaragePosition))+" m";PlaceMarker(marker,w.GaragePosition+Vector3.up*3,Camera.main);marker.text=L("GARAJ","GARAGE");foreach(var tag in hudPlates)tag.gameObject.SetActive(false);return;}
            garagePrompt.text=w.FreeRoam?L("TAB · Contracte","TAB · Contracts"):"";
            objective.text=w.Phase==RecoveryPhase.Deliver?L("LIVREAZĂ ÎN ZONA SIGURĂ","DELIVER TO THE SAFE ZONE"):L("RECUPEREAZĂ MAȘINA","RECOVER THE CAR");
            crew.text=w.Cooperative?"CO-OP  "+w.Occupied+"/2"+(w.Occupied<2?"\n"+L("COECHIPIER DECONECTAT","CREWMATE DISCONNECTED")+"\n"+(session.UsingGamepad?"START":"ESC")+"  "+L("Meniu / Lobby","Menu / Lobby"):""):"";speed.text=Mathf.RoundToInt(w.Drivers[slot].Speed*3.6f)+" <size=27>km/h</size>";
            if(w.ClearingObstacle)objective.text=L("1/2 · ELIBEREAZĂ ACCESUL — TRAGE COPACUL ÎN LATERAL","1/2 · CLEAR ACCESS — PULL THE FALLEN TREE ASIDE");
            var wheelSlip=w.Drivers[slot].WheelSlip;float slip=Mathf.Max(wheelSlip.x,wheelSlip.y,wheelSlip.z,wheelSlip.w);
            mudFeedback.gameObject.SetActive(P.tutorial&&w.Drivers[slot].InMud);
            mudFeedback.text=slip>.25f?L("PATINARE · Redu accelerația pentru mai multă tracțiune.","WHEELSPIN · Ease off for better traction."):L("NOROI · Accelerează progresiv și păstrează cablul întins.","MUD · Build throttle gently and keep the cable taut.");
            RefreshExtractionFeedback(w);
            bool near=Vector3.Distance(w.Drivers[slot].RearHitch,w.TowPoint)<RecoveryRules.AttachRange;
            cableGroup.SetActive(attached||near);cableFill.transform.parent.gameObject.SetActive(attached);
            string level=load>=1?L("! LA LIMITĂ · RISC DE RUPERE","! AT LIMIT · BREAK RISK"):load>.82f?L("! REDU SARCINA","! EASE THE LOAD"):load>.6f?L("SUB SARCINĂ","UNDER LOAD"):L("SIGUR","SAFE");
            cableText.text=attached?L("CABLU  ","CABLE  ")+(P.colorHelp?level:""):L("CÂRLIG DISPONIBIL","HITCH IN RANGE");
            cableFill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(load),1);cableFill.color=load>=1?new Color(1,.12f,.05f):load>.82f?new Color(1,.43f,.05f):load>.6f?new Color(1,.72f,.05f):new Color(.16f,.9f,.27f);
            actionText.text=attached?(session.UsingGamepad?"A  "+L("Desprinde","Detach")+"   LB / RB  "+L("Troliu","Winch"):P.hookKey+"  "+L("Desprinde","Detach")+"   "+P.reelKey+" / "+P.extendKey+"  "+L("Scurtează / Alungește","Shorten / Lengthen")):(session.UsingGamepad?"A":P.hookKey)+"  "+L("Conectează cablul","Attach cable");
            if(P.tutorial&&w.ContractIndex==0&&!attached)actionText.text+="\n"+L("Cârlig spate → cârlig față. Accelerează lin.","Rear hitch → front hitch. Pull gently.");
            partnerGroup.SetActive(w.Cooperative&&w.Attached(other));partnerText.text=w.DriverName(other)+"  /  "+L("CABLU","CABLE");partnerFill.rectTransform.anchorMax=new Vector2(w.Tension(other),1);partnerFill.color=other==0?Blue:Orange;
            var camera=Camera.main;if(camera==null)return;
            for(int i=0;i<2;i++)
            {
                var tag=hudPlates[i];var viewport=camera.WorldToViewportPoint(w.Drivers[i].transform.position+Vector3.up*3.2f);
                bool visible=w.Cooperative&&w.OccupiedSlot(i)&&viewport.z>0&&viewport.x>.08f&&viewport.x<.92f&&viewport.y>.14f&&viewport.y<.88f;
                tag.gameObject.SetActive(visible);
                if(visible){tag.anchorMin=tag.anchorMax=new Vector2(viewport.x,viewport.y);tag.pivot=new Vector2(.5f,0);tag.anchoredPosition=Vector2.zero;int signal=w.Signal(i);string[] signals={"",L("GATA","READY"),L("TRAGE","PULL"),L("OPREȘTE","STOP")};hudNames[i].text=w.DriverName(i)+"  /  P"+(i+1)+(signal==0?"":"\n"+signals[Mathf.Clamp(signal,0,3)]);float width=Mathf.Clamp(hudNames[i].GetPreferredValues().x+26,130,280);tag.sizeDelta=new Vector2(width,70);((RectTransform)hudNames[i].transform.parent).sizeDelta=new Vector2(width,56);hudNames[i].rectTransform.sizeDelta=new Vector2(width-16,56);((RectTransform)tag.GetChild(1)).anchoredPosition=new Vector2(width*.5f-7,-49);}
            }
            if(RefreshExtractionMarker(w,camera))return;
            var goal=w.Phase==RecoveryPhase.Deliver?w.Contract.Delivery+Vector3.up*3:w.TowTargetPosition+Vector3.up*3;
            PlaceMarker(marker,goal,camera);marker.text=w.Phase==RecoveryPhase.Deliver?L("ZONĂ SIGURĂ","SAFE ZONE")+"  "+Mathf.RoundToInt(Vector3.Distance(w.Casualty.transform.position,w.Contract.Delivery))+" m":w.ClearingObstacle?L("1/2 · TRAGE COPACUL ÎN LATERAL","1/2 · PULL THE LOG ASIDE"):L("MAȘINA DE RECUPERAT","STRANDED CAR");
        }
        private static void PlaceMarker(TMP_Text text,Vector3 position,Camera camera)
        {var p=camera.WorldToViewportPoint(position);text.gameObject.SetActive(p.z>0&&p.x>.06f&&p.x<.94f&&p.y>.12f&&p.y<.88f);text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(p.x,p.y);text.rectTransform.pivot=new Vector2(.5f,0);text.rectTransform.anchoredPosition=Vector2.zero;}
    }
}

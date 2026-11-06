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
        private int displayedSpeed = -1;
        private RectTransform exploreHintPlate, exploreHintKeycap;
        private TMP_Text exploreHintBinding;
        private float exploreHintTextScale;
        private TMP_Text objective, crew, speed, marker, status, slots, readyText, summary, prompt;
        private UnityEngine.UI.Button startButton;
        private TMP_InputField address;
        private void PlayUi(RecoverySound sound){session.Audio?.Play(sound);}
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
            var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;scaler.matchWidthOrHeight=.5f;root=(RectTransform)canvas.transform;
            BuildGameplay();BuildPersistenceHint();Show(session.ConsumeReturnToMain()?"Main":"Title");
        }
        private void OnDestroy(){SetMenuNavigation(false);SetGarageNavigation(false);SetBoardNavigation(false);DisposeRoadTrail();}
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
        private TMP_Text ControlText(TMP_Text value) { RecoveryPromptText.Bind(value); return value; }
        private TMP_Text Text(Transform parent,string text,float x,float y,float width,float height,float size=22,Color? color=null,bool bold=false)
        {var rt=Node(text,parent,x,y,width,height);var t=rt.gameObject.AddComponent<TextMeshProUGUI>();t.font=bold&&size>=26&&displayFont!=null?displayFont:font;t.text=text;t.fontSize=size*P.textScale;t.enableAutoSizing=true;t.fontSizeMin=size*Mathf.Min(1,P.textScale);t.fontSizeMax=size*P.textScale;t.color=color??Paper;t.fontStyle=bold&&t.font!=displayFont?FontStyles.Bold:FontStyles.Normal;t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;textSizes[t]=size;if(parent.name.Contains("control hint")||parent.name=="Control hint")t.gameObject.AddComponent<RecoveryControlGlyph>().Configure(session);return t;}
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
            rt.gameObject.AddComponent<RecoveryUiFeedback>();button.onClick.AddListener(()=>{PlayUi(label.StartsWith("BACK",StringComparison.Ordinal)?RecoverySound.Back:RecoverySound.Confirm);action();});return button;
        }
        private void Header(string title,string sub)
        {Panel(pageRoot,0,0,1600,196,new Color(.03f,.04f,.05f,.64f));Icon(pageRoot,RecoveryIcon.Hook,28,65,34,Amber);Text(pageRoot,title,80,58,1120,76,46,Paper,true);Panel(pageRoot,82,136,82,4,Amber);Text(pageRoot,sub,82,151,1400,40,18,Muted);}
        private void Logo(Transform parent,float x,float y,float size=82)
        {if(logo!=null){var rt=Node("Tow Trouble Logo",parent,x,y,size*6.0f,size*2.55f);var image=rt.gameObject.AddComponent<UnityEngine.UI.RawImage>();image.texture=logo;image.raycastTarget=false;}else Text(parent,"TOW\nTROUBLE",x,y,630,size*2.5f,size,Amber,true);}
        private TMP_Text Footer(string back="Main")
        {
            Button(pageRoot,"BACK",76,808,230,()=>MenuBack());
            return BuildMenuFooter();
        }
        public void Show(string target)
        {
            if(target=="Help"&&page!="Help")helpReturn=page=="Settings"?"Settings":session.Connected?"Pause":"Main";
            if(target=="Vehicles")target=P.starterChosen?"Main":"Starter";
            if(target=="Contracts")target="Help";
            if(target=="Main"&&!P.starterChosen)target="Starter";
            if(target=="Starter"&&P.starterChosen)target="Main";
            if(target=="Starter"&&page!="Starter"&&page!="Confirm") { vehiclePreview=P.vehicle; starterPaint=P.paint; }
            RememberMenuFocus();
            if(pageRoot!=null)pageRoot.gameObject.SetActive(false);
            if(page=="Settings"&&target!="Settings"&&target!="Confirm")displayInitialized=false;
            SetMenuNavigation(false);
            if(page=="Garage"&&target!="Garage")RememberGarageScroll();
            if(target!="Garage")SetGarageNavigation(false);
            if(target=="Garage")BeginGaragePage(page!="Garage");
            bool enteringBoard = target == "Board" && page != "Board";
            if (page == "Board") RememberBoardScroll();
            SetBoardNavigation(target == "Board");
            if(target=="Garage")SetGarageNavigation(true);

            if (target == "Board") BeginBoardPage(enteringBoard);
            if (target != "Gameplay") HideRoadTrail();
            if (target == "Board")
            {
                newRecoveryAvailable = false;
            }
            focusName=MenuRememberedFocus(target);
            if(pageRoot!=null){foreach(var t in pageRoot.GetComponentsInChildren<TMP_Text>(true))textSizes.Remove(t);EventSystem.current?.SetSelectedGameObject(null);Destroy(pageRoot.gameObject);pageRoot=null;}
            if(target=="Garage"&&page!="Garage")PrepareGarage();
            page=target;session.View=target;pendingBinding=null;BeginMenuPage();session.World.ShowSinglePreview(target!="Title"&&target!="Lobby"&&(!session.Connected||session.World.Phase==RecoveryPhase.Lobby),Mathf.Max(0,session.LocalSlot));
            if(target=="Gameplay"){gameplay.SetActive(true);return;}
            gameplay.SetActive(false);pageRoot=Node(target,root,0,0,1600,900);pageRoot.anchorMin=pageRoot.anchorMax=pageRoot.pivot=Vector2.one*.5f;pageRoot.anchoredPosition=Vector2.zero;
            if(target=="Main")session.PreviewFittedRide();
            if(target!="Title"&&target!="Main"&&target!="Lobby"&&target!="Starter"&&target!="Garage")Panel(pageRoot,0,0,1600,900,new Color(.03f,.04f,.035f,.42f));
            switch(target){case "Garage":BuildGarage();break;case "Board":BuildBoard();break;case "Title":BuildTitle();break;case "Main":BuildMain();break;case "Help":BuildHelp();break;case "Starter":BuildVehicles();break;case "TutorialOffer":BuildTutorialOffer();break;case "TutorialDone":case "TrainingComplete":BuildTutorialDone();break;case "TextEntry":BuildTextEntry();break;case "Busy":BuildBusy();break;case "Confirm":BuildConfirmation();break;case "Coop":BuildCoop();break;case "Lobby":BuildLobby();break;case "Settings":BuildSettings();break;case "Credits":BuildCredits();break;case "Pause":BuildPause();break;case "Results":BuildResults();break;}
            ConfigureMenuPage();
            StartCoroutine(FocusFirst());
        }
        private IEnumerator FocusFirst()
        {
            var target=pageRoot;string remembered=focusName;yield return null;if(page=="Gameplay"||target==null||target!=pageRoot||!target.gameObject.activeInHierarchy)yield break;
            if (page == "Board") { FocusBoardRecovery(); yield break; }
            if (page == "Garage") { FocusGarageUI(); yield break; }
            if (IsGeneralMenu) { FocusMenuControl(); yield break; }
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


        private TMP_InputField Input(Transform parent,string label,string value,float x,float y,float width,Action<string> changed)
        {
            Text(parent,label,x,y,width,30,17,Muted,true);var rt=Panel(parent,x,y+34,width,54,new Color(.03f,.045f,.04f));rt.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var input=rt.gameObject.AddComponent<TMP_InputField>();var viewport=Node("Viewport",rt,14,4,width-28,46);var text=Text(viewport,"",0,0,width-28,46,23);text.alignment=TextAlignmentOptions.MidlineLeft;input.textViewport=viewport;input.textComponent=(TextMeshProUGUI)text;input.text=value;input.characterLimit=label.Contains("ADDRESS")||label.Contains("IP")?253:20;input.richText=false;input.shouldActivateOnSelect=false;rt.name=label;input.onEndEdit.AddListener(v=>changed(v));return input;
        }


        private void BuildGameplay()
        {
            gameplay=Stretch("Mission HUD",root).gameObject;
            objective=Text(gameplay.transform,"",30,25,800,82,24,Paper,true);objective.GetComponent<TextMeshProUGUI>().outlineWidth=.12f;
            BuildExploreHint();
            crew=Text(gameplay.transform,"",1140,25,430,90,20,Paper,true);crew.alignment=TextAlignmentOptions.Right;
            BuildCableHud();
            speed = Text(gameplay.transform, "", 1330, 819, 238, 51, 28, Paper, true);
            speed.name = "Rolling speed";
            speed.alignment = TextAlignmentOptions.MidlineRight;
            speed.GetComponent<TextMeshProUGUI>().outlineWidth = .13f;
            speed.gameObject.SetActive(false);
            marker=Text(gameplay.transform,"",0,0,340,70,18,Amber,true);marker.alignment=TextAlignmentOptions.Center;
            marker.GetComponent<TextMeshProUGUI>().outlineWidth=.12f;
            BuildWaypoint();
            BuildMissionStatus();
            BuildHitchPrompt();
            BuildRoadTrail();
            for(int i=0;i<2;i++)
            {
                hudPlates[i]=Node("Player "+(i+1)+" world tag",gameplay.transform,0,0,300,70);
                hudPlates[i].pivot = new Vector2(.5f, 0f);
                hudPlates[i].gameObject.SetActive(false);
                var plate=Panel(hudPlates[i],0,0,300,56,i==0?Blue:Orange);
                var arrow=Panel(hudPlates[i],143,49,14,14,i==0?Blue:Orange);arrow.localRotation=Quaternion.Euler(0,0,45);
                hudNames[i]=Text(plate,"",8,0,284,56,18,Paper,true);hudNames[i].alignment=TextAlignmentOptions.Center;
            }
        }

        private void Confirm(string question,Action yes,string back)
        {
            pendingConfirmation=yes; confirmationQuestion=question; confirmationReturn=back;
            Show("Confirm");
        }
        private void Slider(Transform parent,string label,float value,float min,float max,float y,Action<float> change)
        {
            var text=Text(parent,label+"   "+Mathf.RoundToInt(value*100)+"%",30,y,770,32,22,Paper,true);var rt=Node(label,parent,30,y+48,750,20);var bg=Panel(rt,0,7,750,6,new Color(.25f,.3f,.26f));var fill=Stretch("Fill",bg);var fg=fill.gameObject.AddComponent<UnityEngine.UI.Image>();fg.color=Amber;
            var handle=Node("Handle",rt,0,-4,25,28);var hg=handle.gameObject.AddComponent<UnityEngine.UI.Image>();hg.color=Color.clear;var knob=Plate(handle,Amber,true);
            var slider=rt.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=knob;slider.direction=UnityEngine.UI.Slider.Direction.LeftToRight;slider.minValue=min;slider.maxValue=max;slider.value=value;
            slider.onValueChanged.AddListener(v=>{if(!Application.isPlaying)return;change(v);text.text=label+"   "+Mathf.RoundToInt(v*100)+"%";foreach(var entry in textSizes)if(entry.Key!=null){entry.Key.fontSize=entry.Value*P.textScale;entry.Key.fontSizeMax=entry.Value*P.textScale;entry.Key.fontSizeMin=entry.Value*Mathf.Min(1,P.textScale);}session.ApplySettings();P.Save();});
        }

        private IEnumerator RefreshSettings(){yield return null;yield return null;if(page=="Settings")Show("Settings");}

        private void Update()
        {
            if(root==null)return;bool online=session.Connected&&session.LocalSlot>=0;var w=session.World;
            if(!online&&session.View=="Starter"&&page!="Starter"&&page!="Confirm")Show("Starter");
            RefreshPersistenceHint();UpdateMenuPresentation();
            if (online) ObserveAssistance(w, session.LocalSlot);
            else { assistanceWorld = null; exploreHintWanted = hitchWanted = false; }
            UpdateHintFades();
            if(pendingBinding!=null&&Keyboard.current!=null)
            {foreach(var key in Keyboard.current.allKeys)if(key.wasPressedThisFrame){if(key.keyCode==Key.Escape){pendingBinding=null;Show("Settings");return;} string error=RecoveryMenuSettings.BindingError(P,pendingBinding,key.keyCode); if(error!=null){settingsMessage=error+". Choose another key, or Esc to cancel.";status.text=settingsMessage;return;}typeof(RecoveryProfile).GetField(pendingBinding).SetValue(P,key.keyCode.ToString());P.Save();pendingBinding=null;settingsMessage="Control saved";Show("Settings");return;}}
            if(page=="Title"&&!online)
            {
                string confirm=RecoveryInputPrompts.ConfirmLabel(RecoveryInputPrompts.Family(RecoveryInputDevices.Pad));
                prompt.text=session.UsingGamepad?L("APASĂ "+confirm+" PENTRU A CONTINUA","PRESS "+confirm+" TO CONTINUE"):L("APASĂ O TASTĂ PENTRU A CONTINUA","PRESS ANY KEY TO CONTINUE");
                if((Keyboard.current!=null&&Keyboard.current.anyKey.wasPressedThisFrame)||(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame)||RecoveryInputPrompts.Confirm(RecoveryInputDevices.Pad)?.wasPressedThisFrame==true)
                {if(P.onboarded)Show("Main");else{firstSetup=true;Show("Settings");}}return;
            }
            if(online&&!wasOnline)Show(w.Phase==RecoveryPhase.Lobby?"Lobby":"Gameplay");
            if(!online&&wasOnline&&!session.Leaving)Show("Coop");
            if(online)
            {
                if(w.Phase==RecoveryPhase.Complete&&!wasComplete)Show(session.Training?"TutorialDone":"Results");
                else if(w.Phase!=previousPhase&&w.Phase!=RecoveryPhase.Complete){if(w.Phase!=RecoveryPhase.Lobby||(page!="Contracts"&&page!="Vehicles"))Show(w.Phase==RecoveryPhase.Lobby?"Lobby":"Gameplay");}
                if(session.MenuOpen&&page=="Gameplay"&&session.View!="Garage"&&session.View!="Board")Show("Pause");
                if(!session.MenuOpen&&page=="Pause")Show("Gameplay");
                wasComplete=w.Phase==RecoveryPhase.Complete;previousPhase=w.Phase;
            }
            if(online&&(session.View=="Garage"||session.View=="Board")&&page!=session.View&&
                !(page=="Confirm"&&confirmationReturn==session.View))Show(session.View);
            else if(online&&(page=="Garage"||page=="Board")&&session.View=="Gameplay")Show("Gameplay");
            wasOnline=online;
            if (HandleGarageControls(online)) return;
            if (HandleBoardControls(online)) return;
            if (HandleMenuControls()) return;
            if (page == "Gameplay" && online) RefreshCableHud(w, session.LocalSlot);
            if(Time.unscaledTime<refreshAt)return;refreshAt=Time.unscaledTime+.08f;
            if (page == "Pause" && online) RefreshCableDetails(w, session.LocalSlot);
            if(status!=null&&pendingBinding==null&&page=="Coop")status.text=session.Status;
            if(page=="Gameplay"&&online)RefreshGameplay();
        }
        private void RefreshGameplay()
        {
            var w=session.World;int slot=session.LocalSlot;
            RefreshRollingSpeed(w.Drivers[slot].Speed);
            crew.text = w.Cooperative && w.Occupied < 2 ? L("Coechipier deconectat", "Crewmate disconnected") : "";
            if(session.Training){RefreshTrainingHud();return;}
            if (w.Phase == RecoveryPhase.Explore)
            {
                RefreshMissionStatus(w, slot, false);
                if (!waypointGarage) ClearRecoveryGuidance();
                objective.text = "";
                marker.gameObject.SetActive(false);
                cableGroup.SetActive(false);
                partnerGroup.SetActive(false);
                if (UrgentAssistance) ClearRecoveryGuidance();
                else RefreshGarageGuidance(w, slot, Camera.main);
                if (!RefreshUrgentHint(w, slot)) RefreshExploreHint(w, slot);
                foreach (var tag in hudPlates) tag.gameObject.SetActive(false);
                return;
            }
            exploreHintWanted = false;
            RefreshUrgentHint(w, slot);
            bool workingAtSite = UpdateApproach(w, slot);
            RefreshMissionStatus(w, slot, workingAtSite);
            var camera=Camera.main;if(camera==null){ClearRecoveryGuidance();marker.gameObject.SetActive(false);return;}
            RefreshCrewTagText(w);
            RefreshRecoveryGuidance(w, slot, camera, workingAtSite);
            if (UrgentAssistance) HideGuidanceNote();
        }
        private string ContractControl()=>session.UsingGamepad ? RecoveryInputPrompts.LeftStickPressLabel(RecoveryInputPrompts.Family(RecoveryInputDevices.Pad)) : P.ReadKey(P.boardKey, Key.Tab).ToString();

        private void BuildExploreHint()
        {
            exploreHintPlate = Node("Exploration action", gameplay.transform, 0, 0, 180, 38);
            exploreHintPlate.anchorMin = exploreHintPlate.anchorMax = new Vector2(.5f, 0f);
            exploreHintPlate.pivot = new Vector2(.5f, 0f);
            exploreHintFade = exploreHintPlate.gameObject.AddComponent<CanvasGroup>();
            exploreHintFade.alpha = 0f; exploreHintFade.blocksRaycasts = false;
            exploreHintPlate.anchoredPosition = new Vector2(0f, 24f);
            var background = exploreHintPlate.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = Ink;
            background.raycastTarget = false;

            exploreHintKeycap = Panel(exploreHintPlate, 8, 6, 26, 26, Paper); exploreHintKeycap.name="HUD control hint";
            exploreHintBinding = Text(exploreHintKeycap, "", 0, 0, 26, 26, 16, Ink, true);
            exploreHintBinding.alignment = TextAlignmentOptions.Center;
            garagePrompt = Text(exploreHintPlate, "", 42, 0, 132, 38, 17, Paper, true);
            garagePrompt.alignment = TextAlignmentOptions.MidlineLeft;
            exploreHintDetail = Text(exploreHintPlate, "", 8, 36, 420, 25, 13, Muted);
            exploreHintDetail.alignment = TextAlignmentOptions.MidlineLeft;
            exploreHintPlate.gameObject.SetActive(false);
        }

        private void RefreshExploreHint(RecoveryWorld world, int slot)
        {
            // The persistent mission status owns the contracts instruction now.
            if (!waypointGarage && session.CanOpenGarage)
            {
                string key = session.UsingGamepad ? RecoveryInputPrompts.WestLabel(RecoveryInputDevices.Pad!=null?RecoveryInputPrompts.Family(RecoveryInputDevices.Pad):RecoveryControllerFamily.Xbox) : P.ReadKey(P.garageKey, Key.G).ToString();
                ShowExploreHint(key, "Customize");
            }
            else exploreHintWanted = false;
        }

        private void ShowExploreHint(string key, string action, string detail = null)
        {
            exploreHintWanted = true;
            if (key == exploreHintBinding.text && action == garagePrompt.text &&
                (detail ?? "") == exploreHintDetail.text && exploreHintTextScale == P.textScale) return;
            exploreHintTextScale = P.textScale;
            exploreHintBinding.text = key;
            garagePrompt.text = action;
            exploreHintDetail.text = detail ?? "";
            bool hasKey = !string.IsNullOrEmpty(key), hasDetail = !string.IsNullOrEmpty(detail);
            exploreHintKeycap.gameObject.SetActive(hasKey);
            exploreHintDetail.gameObject.SetActive(hasDetail);
            float scale = Mathf.Max(1f, P.textScale), pad = 8f * scale;
            float keyHeight = 26f * scale, lineHeight = 38f * scale;
            float keyWidth = hasKey ? Mathf.Max(keyHeight, Mathf.Ceil(exploreHintBinding.GetPreferredValues(key).x) + pad) : 0f;
            float labelWidth = Mathf.Ceil(garagePrompt.GetPreferredValues(action).x) + 3f;
            float labelLeft = hasKey ? pad * 2 + keyWidth : pad;
            float width = Mathf.Max(labelLeft + labelWidth + pad,
                hasDetail ? exploreHintDetail.GetPreferredValues(detail).x + pad * 2 : 0f);
            exploreHintKeycap.anchoredPosition = new Vector2(pad, -(lineHeight - keyHeight) * .5f);
            exploreHintKeycap.sizeDelta = exploreHintBinding.rectTransform.sizeDelta = new Vector2(keyWidth, keyHeight);
            garagePrompt.rectTransform.anchoredPosition = new Vector2(labelLeft, 0f);
            garagePrompt.rectTransform.sizeDelta = new Vector2(width - labelLeft - pad, lineHeight);
            exploreHintDetail.rectTransform.anchoredPosition = new Vector2(pad, -lineHeight);
            exploreHintDetail.rectTransform.sizeDelta = new Vector2(width - pad * 2, 25f * scale);
            exploreHintPlate.sizeDelta = new Vector2(width, lineHeight + (hasDetail ? 27f * scale : 0f));
        }

        private void RefreshRollingSpeed(float metersPerSecond)
        {
            float kmh = Mathf.Abs(metersPerSecond) * 3.6f;
            bool visible = kmh >= (speed.gameObject.activeSelf ? .55f : 1.1f);
            if (visible != speed.gameObject.activeSelf) speed.gameObject.SetActive(visible);
            if (!visible) return;
            int value = Mathf.RoundToInt(kmh);
            if (value == displayedSpeed) return;
            displayedSpeed = value;
            speed.text = value + " <size=65%>km/h</size>";
        }

        private void RefreshCrewTagText(RecoveryWorld world)
        {
            if (!world.Cooperative) return;
            for (int i = 0; i < hudNames.Length; i++)
            {
                if (!world.OccupiedSlot(i)) continue;
                int signal = Mathf.Clamp(world.Signal(i), 0, 3);
                string message = signal switch
                {
                    1 => L("GATA", "READY"),
                    2 => L("TRAGE", "PULL"),
                    3 => L("OPREȘTE", "STOP"),
                    _ => ""
                };
                var label = hudNames[i];
                string caption = world.DriverName(i) + "  /  P" + (i + 1) + (signal == 0 ? "" : "\n" + message);
                if (label.text != caption) label.text = caption;
                float width = Mathf.Clamp(label.GetPreferredValues().x + 26f, 130f, 280f);
                var tag = hudPlates[i];
                if (Mathf.Approximately(tag.sizeDelta.x, width)) continue;
                tag.sizeDelta = new Vector2(width, 70f);
                ((RectTransform)label.transform.parent).sizeDelta = new Vector2(width, 56f);
                label.rectTransform.sizeDelta = new Vector2(width - 16f, 56f);
                ((RectTransform)tag.GetChild(1)).anchoredPosition = new Vector2(width * .5f - 7f, -49f);
            }
        }

        private void PositionCrewTags(RecoveryWorld world, Camera camera)
        {
            if (gameplay == null || !gameplay.activeInHierarchy) return;
            bool showCrew = camera != null && world != null && world.Cooperative && world.HasContract;
            for (int i = 0; i < hudPlates.Length; i++)
            {
                var tag = hudPlates[i];
                if (tag == null) continue;
                bool visible = showCrew && world.OccupiedSlot(i) && !string.IsNullOrEmpty(hudNames[i].text)
                    && (i != session.LocalSlot || world.Signal(i) != 0);
                if (visible)
                {
                    var point = camera.WorldToViewportPoint(world.Drivers[i].transform.position + Vector3.up * 3.2f);
                    visible = point.z > 0f && point.x > .08f && point.x < .92f && point.y > .14f && point.y < .88f;
                    if (visible)
                    {
                        tag.anchorMin = tag.anchorMax = new Vector2(point.x, point.y);
                        tag.anchoredPosition = Vector2.zero;
                    }
                }
                if (tag.gameObject.activeSelf != visible) tag.gameObject.SetActive(visible);
            }
        }


    }
}

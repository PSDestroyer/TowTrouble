using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private string renderedMenuFocusKey,configuredMenuPage,menuDeviceStyle;
        private bool menuOwnsNavigation, menuPreviousNavigation, menuRelease;
        private EventSystem menuEventSystem;
        private int menuFrame, menuX, menuY;
        private float menuRepeat;
        private readonly Dictionary<string,string> menuFocus = new Dictionary<string,string>();
        private readonly List<UnityEngine.UI.Button> menuTabs = new List<UnityEngine.UI.Button>();
        private readonly List<Tuple<TMP_Text,string>> menuHints = new List<Tuple<TMP_Text,string>>();
        private readonly Dictionary<GameObject,string> menuDescriptions = new Dictionary<GameObject,string>();
        private TMP_Text menuHelp, menuFooter;
        private Action pendingConfirmation;
        private string confirmationQuestion, confirmationAction = "CONFIRM", preferredMenuFocus;
        private UnityEngine.UI.Selectable[] menuControls = Array.Empty<UnityEngine.UI.Selectable>();
        private bool IsGeneralMenu => page!="Gameplay" && page!="Garage" && page!="Board" && !(page=="Confirm"&&confirmationReturn=="Board");
        private void SetMenuNavigation(bool active)
        {
            if(active&&!menuOwnsNavigation&&EventSystem.current!=null)
            {menuEventSystem=EventSystem.current;menuPreviousNavigation=menuEventSystem.sendNavigationEvents;menuEventSystem.sendNavigationEvents=false;menuOwnsNavigation=true;}
            else if(!active&&menuOwnsNavigation)
            {if(menuEventSystem!=null)menuEventSystem.sendNavigationEvents=menuPreviousNavigation;menuOwnsNavigation=false;menuEventSystem=null;}
        }
        private void RememberMenuFocus()
        {if(IsGeneralMenu&&page!="Confirm"&&EventSystem.current?.currentSelectedGameObject!=null)menuFocus[renderedMenuFocusKey??MenuFocusKey(page)]=EventSystem.current.currentSelectedGameObject.name;}
        private string MenuFocusKey(string target)=>target=="Settings"?target+":"+(firstSetup?"First":settingsTab):target;
        private string MenuRememberedFocus(string target)=>target=="Confirm"?null:menuFocus.TryGetValue(MenuFocusKey(target),out var value)?value:null;
        private void BeginMenuPage()
        {screenShades.Clear();screenBands.Clear();menuTabs.Clear();menuHints.Clear();menuDescriptions.Clear();menuScroll=null;menuHelp=null;menuFooter=null;preferredMenuFocus=null;}
        private TMP_Text BuildMenuFooter()
        {
            Panel(pageRoot,76,838,1448,48,new Color(.03f,.04f,.05f,.88f));
            NavigationHint(94,"nav-primary");
            Text(pageRoot,"/",176,842,16,40,20,Muted).alignment=TextAlignmentOptions.Midline;
            NavigationHint(196,"nav-secondary");
            menuFooter=Text(pageRoot,"Navigate",292,842,480,40,20,Paper);menuFooter.alignment=TextAlignmentOptions.MidlineLeft;
            MenuHint(pageRoot,"confirm","Select",1050,842,230);
            MenuHint(pageRoot,"back","Back",1330,842,190);
            return menuFooter;
        }
        private void NavigationHint(float x,string kind)
        {
            var cap=Panel(pageRoot,x,842,78,40,Paper);cap.name="Menu control hint";
            var key=Text(cap,MenuKey(kind),0,0,78,40,16,Ink,true);key.alignment=TextAlignmentOptions.Center;
            menuHints.Add(Tuple.Create(key,kind));
        }
        private void MenuHint(Transform parent,string kind,string label,float x,float y,float width)
        {
            MenuGlyph(parent,kind,x,y);
            Text(parent,label,x+68,y,width-68,40,18,Muted).alignment=TextAlignmentOptions.MidlineLeft;
        }
        private TMP_Text MenuGlyph(Transform parent,string kind,float x,float y,float width=60,float height=40)
        {
            var cap=Panel(parent,x,y,width,height,Paper);cap.name="Menu control hint";
            var key=Text(cap,MenuKey(kind),0,0,width,height,16,Ink,true);key.alignment=TextAlignmentOptions.Center;
            menuHints.Add(Tuple.Create(key,kind));return key;
        }
        private string MenuKey(string kind)
        {
            var f=RecoveryInputPrompts.Family(RecoveryInputDevices.Pad);
            if(kind=="nav-primary")return session.UsingGamepad?"D-pad":"WASD";
            if(kind=="nav-secondary")return session.UsingGamepad?"Left stick":"Arrows";
            if(kind=="rotate")return session.UsingGamepad?"Right stick":"LMB";
            if(kind=="tire-previous")return session.UsingGamepad?RecoveryInputPrompts.TriggerLabels(f).Split('/')[0].Trim():"T";
            if(kind=="tire-next")return session.UsingGamepad?RecoveryInputPrompts.TriggerLabels(f).Split('/')[1].Trim():"R";
            if(!session.UsingGamepad)return kind=="back"?"Esc":kind=="previous"?"Q":kind=="next"?"E":"Enter";
            return kind=="back"?RecoveryInputPrompts.CancelLabel(f):kind=="previous"?RecoveryInputPrompts.PreviousTabLabel(f):kind=="next"?RecoveryInputPrompts.NextTabLabel(f):RecoveryInputPrompts.ConfirmLabel(f);
        }
        private void ConfigureMenuPage()
        {
            renderedMenuFocusKey=MenuFocusKey(page);
            SetMenuNavigation(IsGeneralMenu);
            bool entering=configuredMenuPage!=page;
            menuRelease=true;menuFrame=Time.frameCount;if(entering){menuX=menuY=0;configuredMenuPage=page;}
            if(!IsGeneralMenu)return;
            Canvas.ForceUpdateCanvases();
            menuControls=pageRoot.GetComponentsInChildren<UnityEngine.UI.Selectable>();
            // Explicit rows avoid uGUI's diagonal jumps from a tab into the middle of a settings column.
            var body=menuControls.Where(c=>!menuTabs.Contains(c as UnityEngine.UI.Button)).ToArray();
            Vector2 Center(UnityEngine.UI.Selectable c)=>pageRoot.InverseTransformPoint(((RectTransform)c.transform).TransformPoint(((RectTransform)c.transform).rect.center));
            foreach(var control in menuControls)
            {
                var nav=control.navigation;nav.mode=UnityEngine.UI.Navigation.Mode.Explicit;
                Vector2 at=Center(control);
                var peers=menuTabs.Contains(control as UnityEngine.UI.Button)?menuTabs.Cast<UnityEngine.UI.Selectable>().ToArray():body;
                UnityEngine.UI.Selectable Find(int x,int y)
                {
                    var valid=peers.Where(c=>c!=control).Where(c=>{var d=Center(c)-at;return x!=0?Mathf.Abs(d.y)<28&&d.x*x>1:d.y*y>28;});
                    if(x!=0)return valid.OrderBy(c=>Mathf.Abs(Center(c).x-at.x)).FirstOrDefault();
                    return valid.OrderBy(c=>Mathf.Abs(Center(c).y-at.y)+Mathf.Abs(Center(c).x-at.x)*2f).FirstOrDefault();
                }
                nav.selectOnLeft=Find(-1,0);nav.selectOnRight=Find(1,0);nav.selectOnUp=Find(0,1);nav.selectOnDown=Find(0,-1);
                if(menuTabs.Contains(control as UnityEngine.UI.Button))nav.selectOnDown=body.OrderByDescending(c=>Center(c).y).FirstOrDefault();
                else if(nav.selectOnUp==null&&menuTabs.Count>0)nav.selectOnUp=menuTabs.FirstOrDefault(c=>c.GetComponentInChildren<RecoveryPlateGraphic>().Style==RecoveryPlateStyle.Gold)??menuTabs[0];
                control.navigation=nav;
            }
            ConfigureStarterNavigation();
            if(Application.isPlaying&&!P.reducedMotion&&(page!="Starter"||entering))StartCoroutine(FadeMenu(pageRoot));
        }
        private IEnumerator FadeMenu(RectTransform target)
        {
            var group=target.gameObject.AddComponent<CanvasGroup>();group.alpha=0;
            float value=0;while(target!=null&&target==pageRoot&&value<1){value+=Time.unscaledDeltaTime/ .12f;group.alpha=Mathf.SmoothStep(0,1,value);yield return null;}
            if(group!=null)group.alpha=1;
        }
        private void FocusMenuControl()
        {
            if(EventSystem.current==null)return;
            var preferred=menuControls.FirstOrDefault(c=>c!=null&&c.IsInteractable()&&c.name==(focusName??preferredMenuFocus));
            if(preferred==null)preferred=menuControls.FirstOrDefault(c=>c!=null&&c.IsInteractable()&&c.GetComponentInChildren<RecoveryPlateGraphic>()?.Style==RecoveryPlateStyle.Gold);
            if(preferred==null)preferred=menuControls.FirstOrDefault(c=>c!=null&&c.IsInteractable());
            if(preferred!=null)EventSystem.current.SetSelectedGameObject(preferred.gameObject);
        }
        private bool HandleMenuControls()
        {
            if(!IsGeneralMenu)return false;
            SetMenuNavigation(true);
            if(page=="Title"||Time.frameCount==menuFrame)return true;
            if(menuScroll!=null){float scroll=(RecoveryInputDevices.Pad?.rightStick.y.ReadValue()??0)*Time.unscaledDeltaTime*650; if(Keyboard.current?.pageUpKey.isPressed==true)scroll+=Time.unscaledDeltaTime*650;if(Keyboard.current?.pageDownKey.isPressed==true)scroll-=Time.unscaledDeltaTime*650;var content=menuScroll.content;float range=Mathf.Max(0,content.rect.height-menuScroll.viewport.rect.height);content.anchoredPosition=new Vector2(content.anchoredPosition.x,Mathf.Clamp(content.anchoredPosition.y-scroll,0,range));}
            var keyboard=Keyboard.current;var pad=RecoveryInputDevices.Pad;
            bool cancel=keyboard?.escapeKey.wasPressedThisFrame==true||RecoveryInputPrompts.Cancel(pad)?.wasPressedThisFrame==true;
            var selected=EventSystem.current?.currentSelectedGameObject?.GetComponent<UnityEngine.UI.Selectable>();
            if(selected is TMP_InputField editing && editing.isFocused)
            {
                if(cancel||keyboard?.enterKey.wasPressedThisFrame==true){editing.DeactivateInputField();session.ConsumeMenuInput();menuRelease=true;}
                return true;
            }
            if(cancel){session.ConsumeMenuInput();MenuBack();return true;}
            if(menuTabs.Count>1&&(keyboard?.qKey.wasPressedThisFrame==true||pad?.leftShoulder.wasPressedThisFrame==true||keyboard?.eKey.wasPressedThisFrame==true||pad?.rightShoulder.wasPressedThisFrame==true))
            {
                int index=menuTabs.FindIndex(b=>b.GetComponentInChildren<RecoveryPlateGraphic>().Style==RecoveryPlateStyle.Gold);
                bool next=keyboard?.eKey.wasPressedThisFrame==true||pad?.rightShoulder.wasPressedThisFrame==true;
                menuTabs[(Mathf.Max(0,index)+(next?1:menuTabs.Count-1))%menuTabs.Count].onClick.Invoke();session.ConsumeMenuInput();return true;
            }
            GarageDirection(keyboard,pad,out int x,out int y);
            if(x==0&&y==0){menuX=menuY=0;}
            else if(x!=menuX||y!=menuY||Time.unscaledTime>=menuRepeat)
            {
                bool first=x!=menuX||y!=menuY;menuX=x;menuY=y;menuRepeat=Time.unscaledTime+(first?.35f:.11f);
                if(selected==null)FocusMenuControl();
                else if(selected is UnityEngine.UI.Slider slider&&x!=0)slider.value=Mathf.Clamp(slider.value+x*(slider.maxValue-slider.minValue)*.025f,slider.minValue,slider.maxValue);
                else
                {
                    var next=y<0?selected.navigation.selectOnUp:y>0?selected.navigation.selectOnDown:x<0?selected.navigation.selectOnLeft:selected.navigation.selectOnRight;
                    int guard=0;while(next!=null&&!next.IsInteractable()&&guard++<menuControls.Length)next=y<0?next.navigation.selectOnUp:y>0?next.navigation.selectOnDown:x<0?next.navigation.selectOnLeft:next.navigation.selectOnRight;
                    if(next!=null&&next.IsInteractable()){EventSystem.current.SetSelectedGameObject(next.gameObject);PlayMenuMove();}
                }
            }
            bool held=keyboard!=null&&(keyboard.enterKey.isPressed||keyboard.numpadEnterKey.isPressed)||RecoveryInputPrompts.Confirm(pad)?.isPressed==true;
            bool press=keyboard!=null&&(keyboard.enterKey.wasPressedThisFrame||keyboard.numpadEnterKey.wasPressedThisFrame)||RecoveryInputPrompts.Confirm(pad)?.wasPressedThisFrame==true;
            if(menuRelease){if(!held)menuRelease=false;}
            else if(press)
            {
                menuRelease=true;session.ConsumeMenuInput();
                selected=EventSystem.current?.currentSelectedGameObject?.GetComponent<UnityEngine.UI.Selectable>();
                if(page=="Starter"&&selected!=null&&selected.name.StartsWith("Starter "))UseVehicleChoice();
                else if(selected is UnityEngine.UI.Button button&&button.IsInteractable())button.onClick.Invoke();
                else if(selected is TMP_InputField field){if(session.UsingGamepad)OpenTextEntry(field);else field.ActivateInputField();}
            }
            return true;
        }
        private void PlayMenuMove(){PlayUi(RecoverySound.Navigate);}
        private void MenuBack()
        {
            if(displayPending){RevertDisplay();return;}
            displayInitialized=false;
            if(page=="Confirm"){Show(confirmationReturn);return;}
            if(page=="TextEntry"){Show("Coop");return;}
            if(page=="Credits"&&creditsNotices){creditsNotices=false;Show("Credits");return;}
            if(page=="Busy"){session.CancelConnection();if(session.Training)session.EndTraining(false);Show("Main");return;}
            if(page=="Lobby"){ConfirmAction("Leave this session?",session.LeaveToMain,"Lobby","LEAVE SESSION");return;}
            if(page=="Pause"){session.ConsumeMenuInput();session.ToggleMenu();Show("Gameplay");return;}
            if(page=="Starter"||firstSetup){Show("Title");return;}
            if(page=="TutorialOffer"){SkipTutorialOffer();return;}
            if(page=="TutorialDone"||page=="TrainingComplete"){ContinueTutorial();return;}
            if(page=="Results"){if(session.IsHost){session.FinishExplorationContract();Show("Gameplay");}return;}
            if(page=="Settings"){Show(returnPage);return;}
            if(page=="Help"){Show(helpReturn);return;}
            if(page!="Main"&&page!="Title")Show("Main");
        }
        private void ConfirmAction(string question,Action yes,string back,string action)
        {confirmationAction=action;Confirm(question,yes,back);}
        private void BuildConfirmation()
        {
            var shade=Panel(pageRoot,0,0,1600,900,new Color(0,0,0,.75f));shade.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            const float width=700,padding=28,gap=16;
            var card=Panel(pageRoot,450,0,width,300);card.name="Confirmation card";
            Icon(card,RecoveryIcon.Flag,padding,26,30,Amber);
            Text(card,displayPending?"KEEP DISPLAY SETTINGS?":"PLEASE CONFIRM",76,24,width-104,42,26,Paper,true);
            var message=Text(card,confirmationQuestion,padding,86,width-padding*2,300,23,Muted);
            message.enableAutoSizing=false;
            float messageHeight=Mathf.Clamp(Mathf.Ceil(message.GetPreferredValues(confirmationQuestion,width-padding*2,float.PositiveInfinity).y)+8,40,300);
            message.rectTransform.sizeDelta=new Vector2(width-padding*2,messageHeight);message.enableAutoSizing=true;
            float buttonY=86+messageHeight+24,height=buttonY+64+padding;
            card.sizeDelta=new Vector2(width,height);card.anchoredPosition=new Vector2((1600-width)*.5f,-(900-height)*.5f);
            float buttonWidth=(width-padding*2-gap)*.5f;
            var confirm=Button(card,confirmationAction,padding,buttonY,buttonWidth,()=>{var action=pendingConfirmation;pendingConfirmation=null;action?.Invoke();},true,64);
            var cancel=Button(card,"CANCEL",padding+buttonWidth+gap,buttonY,buttonWidth,()=>MenuBack(),false,64);
            preferredMenuFocus="CANCEL";
            if(confirmationReturn=="Board")ConfigureBoardDialog();
            else {ConfirmationHint(confirm,"confirm");ConfirmationHint(cancel,"back");}
        }
        private void ConfirmationHint(UnityEngine.UI.Button button,string kind)
        {
            foreach(var icon in button.GetComponentsInChildren<RecoveryIconGraphic>())icon.gameObject.SetActive(false);
            var label=button.GetComponentInChildren<TMP_Text>();
            var cap=Panel(button.transform,12,11,60,42,Ink);cap.name="Menu control hint";
            var key=Text(cap,MenuKey(kind),0,0,60,42,16,Paper,true);key.alignment=TextAlignmentOptions.Center;
            menuHints.Add(Tuple.Create(key,kind));
            label.rectTransform.anchoredPosition=new Vector2(86,0);
            label.rectTransform.sizeDelta=new Vector2(((RectTransform)button.transform).rect.width-100,64);
        }
        private void UpdateMenuPresentation()
        {
            string deviceStyle=session.UsingGamepad+":"+RecoveryInputPrompts.Family(RecoveryInputDevices.Pad);
            if(menuDeviceStyle!=deviceStyle){menuDeviceStyle=deviceStyle;if(page=="Help"||page=="Credits"||page=="Settings"&&settingsTab=="Comenzi") {float y=menuScroll!=null?menuScroll.content.anchoredPosition.y:0;Show(page);if(menuScroll!=null)menuScroll.content.anchoredPosition=new Vector2(0,y);return;} }
            if(page=="Results"&&resultInputHint!=null&&resultInputHost)resultInputHint.text=MenuKey("confirm");
            UpdateStarterHints();
            foreach(var hint in menuHints)if(hint.Item1!=null)hint.Item1.text=MenuKey(hint.Item2);
            if(menuFooter!=null)menuFooter.text="Navigate";
            if(menuHelp!=null&&EventSystem.current?.currentSelectedGameObject!=null&&menuDescriptions.TryGetValue(EventSystem.current.currentSelectedGameObject,out var help))menuHelp.text=help;
            if(page=="Pause"&&session.Connected)RefreshCableDetails(session.World,session.LocalSlot);
            RefreshMenuConnection();
            UpdateDisplayReview();
        }
    }
}

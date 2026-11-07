using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private readonly Dictionary<string,UnityEngine.UI.Button> garageCards=new Dictionary<string,UnityEngine.UI.Button>();
        private readonly Dictionary<GarageSlot,UnityEngine.UI.Button> garageCategoryButtons=new Dictionary<GarageSlot,UnityEngine.UI.Button>();
        private readonly List<UnityEngine.UI.Button> garageModalButtons=new List<UnityEngine.UI.Button>();
        private readonly List<GarageHint> garageHints=new List<GarageHint>();
        private RecoverySmoothScrollRect garageListScroll;
        private UnityEngine.UI.ScrollRect garageReviewScroll;
        private TMP_Text garageTabHint,garageControlHint,garageDialogHint;
        private EventSystem garageEventSystem;
        private bool garageOwnsNavigation,garagePreviousNavigation,garageSubmitRelease;
        private int garageOpenedFrame,garageMoveX,garageMoveY,garageDialogIndex;
        private float garageNextMove;
        private string garageHintStyle;
        private sealed class GarageHint { public UnityEngine.UI.Button Button;public RectTransform Cap;public TMP_Text Key,Caption;public string Kind; }

        private void SetGarageNavigation(bool enabled)
        {
            if(enabled&&!garageOwnsNavigation&&EventSystem.current!=null)
            {garageEventSystem=EventSystem.current;garagePreviousNavigation=garageEventSystem.sendNavigationEvents;garageEventSystem.sendNavigationEvents=false;garageOwnsNavigation=true;}
            else if(!enabled&&garageOwnsNavigation)
            {if(garageEventSystem!=null)garageEventSystem.sendNavigationEvents=garagePreviousNavigation;garageOwnsNavigation=false;garageEventSystem=null;session.GarageCameraInputBlocked=false;}
        }
        private void BeginGaragePage(bool entering)
        {
            if(entering){garageOpenedFrame=Time.frameCount;garageSubmitRelease=true;garageMoveX=garageMoveY=0;}
            garageHintStyle=null;garageReviewScroll=null;garageDialogHint=null;
        }
        private UnityEngine.UI.Button GarageButton(Transform parent,string label,float x,float y,float width,Action action,bool primary=false,float height=54)
        {
            var button=Button(parent,label,x,y,width,action,primary,height);
            var nav=button.navigation;nav.mode=UnityEngine.UI.Navigation.Mode.None;button.navigation=nav;return button;
        }
        private void GarageFont(TMP_Text text,float size)
        {textSizes[text]=size;text.fontSize=text.fontSizeMax=size*P.textScale;text.fontSizeMin=size*Mathf.Min(1,P.textScale);}
        private UnityEngine.UI.Button GarageOverlayButton(Transform parent,string label,float x,float y,float width,Action action,bool primary=false)
        {
            int index=garageModalButtons.Count;
            var button=GarageButton(parent,label,x,y,width,()=>{garageDialogIndex=index;action();},primary,48);garageModalButtons.Add(button);return button;
        }
        private void FocusGarageUI()
        {
            if(EventSystem.current==null)return;UnityEngine.UI.Button focus=null;
            if(garageOverlay!=GarageOverlay.None)
            {
                if(garageModalButtons.Count==0)return;
                garageDialogIndex=Mathf.Clamp(garageDialogIndex,0,garageModalButtons.Count-1);
                if(!garageModalButtons[garageDialogIndex].interactable)
                    for(int i=0;i<garageModalButtons.Count;i++)if(garageModalButtons[i].interactable){garageDialogIndex=i;break;}
                focus=garageModalButtons[garageDialogIndex];
            }
            else if(garageCategoryFocus||garageSelected==null)garageCategoryButtons.TryGetValue(garageSlot,out focus);
            else garageCards.TryGetValue(garageSelected,out focus);
            if(focus!=null&&EventSystem.current.currentSelectedGameObject!=focus.gameObject)EventSystem.current.SetSelectedGameObject(focus.gameObject);
        }
        private void AddGarageHint(UnityEngine.UI.Button button,string kind)
        {
            foreach(var icon in button.GetComponentsInChildren<RecoveryIconGraphic>())icon.gameObject.SetActive(false);
            var caption=button.GetComponentInChildren<TMP_Text>();var cap=Panel(button.transform,8,4,26,22,Paper);cap.name="Garage control hint";
            var key=Text(cap,"",0,0,26,22,12,Ink,true);key.alignment=TextAlignmentOptions.Center;
            garageHints.Add(new GarageHint{Button=button,Cap=cap,Key=key,Caption=caption,Kind=kind});
        }
        private string GarageBinding(string kind)
        {
            bool pad=session.UsingGamepad;var family=RecoveryInputPrompts.Family(RecoveryInputDevices.Pad);
            switch(kind)
            {
                case "confirm":return pad?RecoveryInputPrompts.ConfirmLabel(family):"Enter";
                case "back":return pad?RecoveryInputPrompts.CancelLabel(family):"Esc";
                case "filter":return pad?RecoveryInputPrompts.WestLabel(family):"Tab";
                case "setups":return pad?RecoveryInputPrompts.MenuLabel(family):"P";
                case "reset":return pad?RecoveryInputPrompts.RightStickPressLabel(family):"C";
                case "recommend":return pad?RecoveryInputPrompts.NorthLabel(family):"F";
                case "sell":return pad?RecoveryInputPrompts.SelectLabel(family):"Del";
                default:return "";
            }
        }
        private void RefreshGarageHints()
        {
            string style=session.UsingGamepad?RecoveryInputPrompts.Family(RecoveryInputDevices.Pad).ToString():"Keyboard";
            if(garageHintStyle==style)return;garageHintStyle=style;
            foreach(var hint in garageHints)
            {
                string key=GarageBinding(hint.Kind);hint.Key.text=key;
                var rect=(RectTransform)hint.Button.transform;float width=Mathf.Max(48,Mathf.Ceil(hint.Key.GetPreferredValues(key).x)+12);
                float height=Mathf.Min(40,rect.rect.height);bool gold=hint.Button.GetComponentInChildren<RecoveryPlateGraphic>().Style==RecoveryPlateStyle.Gold;
                hint.Cap.GetComponent<UnityEngine.UI.Image>().color=gold?Ink:Paper;hint.Key.color=gold?Paper:Ink;
                hint.Cap.anchoredPosition=new Vector2(9,-(rect.rect.height-height)*.5f);hint.Cap.sizeDelta=hint.Key.rectTransform.sizeDelta=new Vector2(width,height);
                hint.Caption.rectTransform.anchoredPosition=new Vector2(width+20,0);hint.Caption.rectTransform.sizeDelta=new Vector2(Mathf.Max(20,rect.rect.width-width-30),rect.rect.height);
            }
            bool gamepad=session.UsingGamepad;var family=RecoveryInputPrompts.Family(RecoveryInputDevices.Pad);
            if(garageTabHint!=null)garageTabHint.text=gamepad?RecoveryInputPrompts.PreviousTabLabel(family)+" / "+RecoveryInputPrompts.NextTabLabel(family)+" · Tabs":"Q / E · Tabs";
            if(garageControlHint!=null)
            {
                garageControlHint.text=gamepad?"D-pad / Left stick · Select    Right stick · Rotate"+(garageSlot==GarageSlot.Wheels?" · "+RecoveryInputPrompts.TriggerLabels(family)+": Tire type":"")
                    :"WASD / Arrows · Select    LMB · Rotate"+(garageSlot==GarageSlot.Wheels?" · R / T: Tire type":"");
            }
            if(garageDialogHint!=null)garageDialogHint.text=garageOverlay==GarageOverlay.Setups
                ?GarageBinding("confirm")+": Activate highlighted button · "+GarageBinding("back")+": Back"
                :gamepad?"Right stick: Scroll review · "+GarageBinding("back")+": Cancel":"Mouse wheel · Scroll review";
        }
        private bool HandleGarageControls(bool online)
        {
            if(!online||page!="Garage"||!session.GarageOpen){SetGarageNavigation(false);return false;}
            SetGarageNavigation(true);RefreshGarageHints();FocusGarageUI();
            if(Time.frameCount==garageOpenedFrame)return true;
            var keyboard=Keyboard.current;var pad=RecoveryInputDevices.Pad;
            bool cancel=keyboard?.escapeKey.wasPressedThisFrame==true||RecoveryInputPrompts.Cancel(pad)?.wasPressedThisFrame==true;
            if(cancel)
            {
                session.ConsumeMenuInput();
                if(garageOverlay==GarageOverlay.Overwrite){garageOverlay=GarageOverlay.Setups;garageSubmitRelease=true;Show("Garage");}
                else if(garageOverlay!=GarageOverlay.None)CloseGarageOverlay();else CloseGarage();
                return true;
            }
            bool menu=keyboard?.pKey.wasPressedThisFrame==true||pad?.startButton.wasPressedThisFrame==true;
            if(menu){if(garageOverlay==GarageOverlay.Setups)CloseGarageOverlay();else if(garageOverlay==GarageOverlay.None)OpenGarageOverlay(GarageOverlay.Setups);return true;}
            if(garageOverlay==GarageOverlay.None)
            {
                bool previous=keyboard?.qKey.wasPressedThisFrame==true||pad?.leftShoulder.wasPressedThisFrame==true;
                bool next=keyboard?.eKey.wasPressedThisFrame==true||pad?.rightShoulder.wasPressedThisFrame==true;
                if(previous!=next){SelectGarageSection((garageSection+(next?1:2))%3);garageMoveX=garageMoveY=0;return true;}
                if(keyboard?.tabKey.wasPressedThisFrame==true||pad?.buttonWest.wasPressedThisFrame==true){ToggleGarageOwned();return true;}
                if(keyboard?.rKey.wasPressedThisFrame==true||pad?.rightTrigger.wasPressedThisFrame==true){CycleGarageTires(1);return true;}
                if(keyboard?.tKey.wasPressedThisFrame==true||pad?.leftTrigger.wasPressedThisFrame==true){CycleGarageTires(-1);return true;}
                if(keyboard?.cKey.wasPressedThisFrame==true||pad?.rightStickButton.wasPressedThisFrame==true)ResetGarageView();
                if(keyboard?.fKey.wasPressedThisFrame==true||pad?.buttonNorth.wasPressedThisFrame==true){JumpGarageRecommendation();return true;}
                if(keyboard?.deleteKey.wasPressedThisFrame==true||pad?.selectButton.wasPressedThisFrame==true){RequestGarageSale();return true;}
            }
            else if(garageReviewScroll!=null)
            {
                float delta=pad!=null?pad.rightStick.y.ReadValue():0;
                if(keyboard?.pageDownKey.isPressed==true)delta=-1;if(keyboard?.pageUpKey.isPressed==true)delta=1;
                if(Mathf.Abs(delta)>.2f)garageReviewScroll.verticalNormalizedPosition=Mathf.Clamp01(garageReviewScroll.verticalNormalizedPosition+delta*Time.unscaledDeltaTime);
            }
            GarageDirection(keyboard,pad,out int x,out int y);
            if(x==0&&y==0){garageMoveX=garageMoveY=0;}
            else if(x!=garageMoveX||y!=garageMoveY||Time.unscaledTime>=garageNextMove)
            {
                bool first=x!=garageMoveX||y!=garageMoveY;garageMoveX=x;garageMoveY=y;garageNextMove=Time.unscaledTime+(first?.35f:.12f);MoveGarageSelection(x,y);
            }
            bool held=keyboard!=null&&(keyboard.enterKey.isPressed||keyboard.numpadEnterKey.isPressed)||RecoveryInputPrompts.Confirm(pad)?.isPressed==true;
            bool pressed=keyboard!=null&&(keyboard.enterKey.wasPressedThisFrame||keyboard.numpadEnterKey.wasPressedThisFrame)||RecoveryInputPrompts.Confirm(pad)?.wasPressedThisFrame==true;
            if(garageSubmitRelease){if(!held)garageSubmitRelease=false;}
            else if(pressed)
            {
                garageSubmitRelease=true;session.ConsumeMenuInput();
                if(garageOverlay!=GarageOverlay.None){FocusGarageUI();if(garageModalButtons.Count>0&&garageModalButtons[garageDialogIndex].interactable)garageModalButtons[garageDialogIndex].onClick.Invoke();}
                else GaragePrimaryAction();
            }
            return true;
        }
        private static void GarageDirection(Keyboard keyboard,Gamepad pad,out int x,out int y)
        {
            x=(keyboard!=null&&(keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed)?1:0)-(keyboard!=null&&(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed)?1:0);
            y=(keyboard!=null&&(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed)?1:0)-(keyboard!=null&&(keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed)?1:0);
            if(x==0&&y==0&&pad!=null)
            {var stick=pad.leftStick.ReadValue()+pad.dpad.ReadValue();if(Mathf.Abs(stick.x)>.55f)x=stick.x>0?1:-1;if(Mathf.Abs(stick.y)>.55f)y=stick.y>0?-1:1;}
            if(y!=0)x=0;
        }
        private void MoveGarageSelection(int x,int y)
        {
            if(garageOverlay!=GarageOverlay.None)
            {
                int step=garageOverlay==GarageOverlay.Setups?(y!=0?y*3:x):(y!=0?y:x);
                int next=Mathf.Clamp(garageDialogIndex+step,0,garageModalButtons.Count-1);
                int direction=step<0?-1:1;
                while(next>=0&&next<garageModalButtons.Count&&!garageModalButtons[next].interactable)next+=direction;
                if(next>=0&&next<garageModalButtons.Count)garageDialogIndex=next;FocusGarageUI();return;
            }
            if(garageCategoryFocus)
            {
                if(x>0){garageCategoryFocus=false;Show("Garage");return;}
                var slots=RecoveryGarageWorkflow.Slots(garageSection);int index=Array.IndexOf(slots,garageSlot);
                int next=Mathf.Clamp(index+y,0,slots.Length-1);
                if(y!=0&&next!=index)SelectGarageSlot(slots[next],true);return;
            }
            int current=garageCandidates.FindIndex(p=>p.Id==garageSelected);
            if(x<0&&(current<=0||current%3==0)){garageCategoryFocus=true;Show("Garage");return;}
            if(current<0)return;
            int target=RecoveryGarageWorkflow.GridIndex(current,garageCandidates.Count,x,y);
            if(target!=current)SelectGaragePart(garageCandidates[target].Id);
        }
    }
}

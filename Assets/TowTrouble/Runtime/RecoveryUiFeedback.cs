using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace TowTrouble
{
    public sealed class RecoveryUiFeedback : MonoBehaviour,ISelectHandler,IDeselectHandler,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISubmitHandler
    {
        private RectTransform rect;
        private Vector2 origin;
        private RecoveryPlateGraphic plate;
        private UnityEngine.UI.Button button;
        private CanvasGroup group;
        private bool selected,hovered,pressed,layoutDriven;
        private UnityEngine.UI.Slider slider;
        private TMPro.TMP_Text sliderLabel;
        private UnityEngine.UI.Graphic sliderFill;
        private RecoveryPlateGraphic sliderFrame;
        private Color sliderTextColor,sliderFillColor;
        private float submitUntil;
        public bool PreferredFocus { get; private set; }
        private int cachedChildren = -1;
        private readonly Dictionary<UnityEngine.UI.Graphic,Color> foreground = new Dictionary<UnityEngine.UI.Graphic,Color>();
        private readonly List<(UnityEngine.UI.Image cap, TMPro.TMP_Text key)> hints = new List<(UnityEngine.UI.Image, TMPro.TMP_Text)>();
        private static readonly Color Ink = new Color(.065f,.075f,.095f,.94f), Paper = new Color(1,.96f,.83f);
        private void Awake(){EnsureReferences();}
        private void EnsureReferences()
        {
            if(rect!=null)return;
            rect=(RectTransform)transform;origin=rect.anchoredPosition;layoutDriven=transform.parent!=null&&transform.parent.GetComponent<UnityEngine.UI.LayoutGroup>()!=null;
            plate=GetComponentInChildren<RecoveryPlateGraphic>();button=GetComponent<UnityEngine.UI.Button>();
            group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();
        }
        public void ConfigureButton(bool preferredFocus)
        { EnsureReferences();PreferredFocus=preferredFocus;RefreshButton(); }
        public void ConfigureHint(UnityEngine.UI.Image cap,TMPro.TMP_Text key)
        { hints.Add((cap,key));RefreshButton(); }
        private void CacheForeground()
        {
            if(cachedChildren==transform.childCount)return;cachedChildren=transform.childCount;
            // Only foreground directly on the button. Photo badges and other inset panels keep their own contrast.
            foreach(Transform child in transform)
            {
                UnityEngine.UI.Graphic graphic=child.GetComponent<TMPro.TMP_Text>();
                if(graphic==null)graphic=child.GetComponent<RecoveryIconGraphic>();
                if(graphic!=null&&!foreground.ContainsKey(graphic))foreground.Add(graphic,graphic.color);
            }
        }
        private void RefreshButton()
        {
            if(button==null)return;
            CacheForeground();bool available=button.IsActive()&&button.IsInteractable(),focus=available&&selected;
            var style=focus?RecoveryPlateStyle.Gold:RecoveryPlateStyle.Charcoal;
            if(plate!=null&&plate.Style!=style){plate.Style=style;plate.SetVerticesDirty();}
            foreach(var item in foreground)if(item.Key!=null)item.Key.color=focus?Ink:item.Value;
            foreach(var hint in hints)
            {if(hint.cap!=null)hint.cap.color=focus?Ink:Paper;if(hint.key!=null)hint.key.color=focus?Paper:Ink;}
            plate?.State(focus,available&&(pressed||Time.unscaledTime<submitUntil));
            if(group!=null)group.alpha=available?1:.48f;
        }
        public void ConfigureSlider(TMPro.TMP_Text label,UnityEngine.UI.Graphic fill,RecoveryPlateGraphic frame,RecoveryPlateGraphic handle)
        {
            slider=GetComponent<UnityEngine.UI.Slider>();sliderLabel=label;sliderFill=fill;sliderFrame=frame;plate=handle;
            sliderTextColor=label.color;sliderFillColor=fill.color;slider.transition=UnityEngine.UI.Selectable.Transition.None;
            RefreshSlider();
        }
        private void RefreshSlider()
        {
            if(slider==null)return;
            bool available=slider.IsActive()&&slider.IsInteractable(),focus=available&&selected;
            var focusRow=sliderFrame.transform.parent.gameObject;
            if(focusRow.activeSelf!=focus)focusRow.SetActive(focus);
            sliderFrame.State(focus,pressed);sliderLabel.color=focus?new Color(1,.71f,.23f):available&&hovered?Color.white:sliderTextColor;
            sliderFill.color=focus?new Color(1,.96f,.83f):sliderFillColor;
            var style=focus?RecoveryPlateStyle.Gold:RecoveryPlateStyle.Charcoal;
            if(plate.Style!=style){plate.Style=style;plate.SetVerticesDirty();}
            plate.State(available&&(focus||hovered),available&&pressed);
            if(group!=null)group.alpha=available?1:.48f;
        }
        public void OnSelect(BaseEventData e){selected=true;RefreshSlider();RefreshButton();}
        public void OnDeselect(BaseEventData e){selected=false;pressed=false;RefreshSlider();RefreshButton();}
        public void OnPointerEnter(PointerEventData e)
        {
            hovered=true;
            if(button!=null&&button.IsActive()&&button.IsInteractable()&&EventSystem.current!=null)
                EventSystem.current.SetSelectedGameObject(gameObject,e);
            RefreshSlider();RefreshButton();
        }
        public void OnPointerExit(PointerEventData e){hovered=false;RefreshSlider();}
        public void OnPointerDown(PointerEventData e){pressed=true;RefreshSlider();}
        public void OnPointerUp(PointerEventData e){pressed=false;RefreshSlider();}
        public void OnSubmit(BaseEventData e){submitUntil=Time.unscaledTime+.09f;}
        private void OnEnable()
        { EnsureReferences();selected=EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject==gameObject;RefreshSlider();RefreshButton(); }
        private void OnDisable(){selected=hovered=pressed=false;submitUntil=0;RefreshSlider();RefreshButton();}
        private void Update()
        {
            if(slider!=null){RefreshSlider();return;}
            RefreshButton();
            bool enabled=button==null||button.IsInteractable(),focus=enabled&&selected,down=enabled&&(pressed||Time.unscaledTime<submitUntil);
            float blend=1-Mathf.Exp(-18*Time.unscaledDeltaTime);
            if(!layoutDriven)rect.anchoredPosition=Vector2.Lerp(rect.anchoredPosition,origin+new Vector2(focus?3:0,down?-2:0),blend);
            if(group!=null)group.alpha=enabled?1:.48f;
        }
    }
}

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
        private void Awake(){rect=(RectTransform)transform;origin=rect.anchoredPosition;layoutDriven=transform.parent!=null&&transform.parent.GetComponent<UnityEngine.UI.LayoutGroup>()!=null;plate=GetComponentInChildren<RecoveryPlateGraphic>();button=GetComponent<UnityEngine.UI.Button>();group=gameObject.AddComponent<CanvasGroup>();}
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
        public void OnSelect(BaseEventData e){selected=true;RefreshSlider();}
        public void OnDeselect(BaseEventData e){selected=false;RefreshSlider();}
        public void OnPointerEnter(PointerEventData e){hovered=true;RefreshSlider();}
        public void OnPointerExit(PointerEventData e){hovered=false;RefreshSlider();}
        public void OnPointerDown(PointerEventData e){pressed=true;RefreshSlider();}
        public void OnPointerUp(PointerEventData e){pressed=false;RefreshSlider();}
        public void OnSubmit(BaseEventData e){submitUntil=Time.unscaledTime+.09f;}
        private void OnDisable(){if(slider!=null){selected=hovered=pressed=false;RefreshSlider();}}
        private void Update()
        {
            if(slider!=null){RefreshSlider();return;}
            bool enabled=button==null||button.IsInteractable(),focus=enabled&&(selected||hovered),down=enabled&&(pressed||Time.unscaledTime<submitUntil);
            plate?.State(focus,down);float blend=1-Mathf.Exp(-18*Time.unscaledDeltaTime);
            if(!layoutDriven)rect.anchoredPosition=Vector2.Lerp(rect.anchoredPosition,origin+new Vector2(focus?3:0,down?-2:0),blend);
            if(group!=null)group.alpha=enabled?1:.48f;
        }
    }
}

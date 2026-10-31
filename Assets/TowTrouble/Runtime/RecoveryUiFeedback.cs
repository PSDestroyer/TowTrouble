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
        private bool selected,hovered,pressed;
        private float submitUntil;
        private void Awake(){rect=(RectTransform)transform;origin=rect.anchoredPosition;plate=GetComponentInChildren<RecoveryPlateGraphic>();button=GetComponent<UnityEngine.UI.Button>();group=gameObject.AddComponent<CanvasGroup>();}
        public void OnSelect(BaseEventData e){selected=true;}
        public void OnDeselect(BaseEventData e){selected=false;}
        public void OnPointerEnter(PointerEventData e){hovered=true;}
        public void OnPointerExit(PointerEventData e){hovered=false;}
        public void OnPointerDown(PointerEventData e){pressed=true;}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        public void OnSubmit(BaseEventData e){submitUntil=Time.unscaledTime+.09f;}
        private void Update()
        {
            bool enabled=button==null||button.IsInteractable(),focus=enabled&&(selected||hovered),down=enabled&&(pressed||Time.unscaledTime<submitUntil);
            plate?.State(focus,down);float blend=1-Mathf.Exp(-18*Time.unscaledDeltaTime);
            rect.anchoredPosition=Vector2.Lerp(rect.anchoredPosition,origin+new Vector2(focus?3:0,down?-2:0),blend);
            if(group!=null)group.alpha=enabled?1:.48f;
        }
    }
}

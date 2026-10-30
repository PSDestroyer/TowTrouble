using UnityEngine;
using UnityEngine.EventSystems;

namespace TowTrouble
{
    // A visible border and a short movement accompany keyboard/controller focus.
    public sealed class RecoveryUiFeedback : MonoBehaviour,ISelectHandler,IDeselectHandler,IPointerEnterHandler,IPointerExitHandler
    {
        private RectTransform rect;
        private Vector2 origin;
        private UnityEngine.UI.Outline outline;
        private bool selected, hovered;
        private void Awake(){rect=(RectTransform)transform;origin=rect.anchoredPosition;outline=gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(1f,.69f,.2f);outline.effectDistance=new Vector2(2,-2);outline.enabled=false;}
        public void OnSelect(BaseEventData e){selected=true;}
        public void OnDeselect(BaseEventData e){selected=false;}
        public void OnPointerEnter(PointerEventData e){hovered=true;}
        public void OnPointerExit(PointerEventData e){hovered=false;}
        private void Update(){bool focused=selected||hovered;outline.enabled=focused;rect.anchoredPosition=Vector2.Lerp(rect.anchoredPosition,origin+Vector2.right*(focused?6:0),1-Mathf.Exp(-18*Time.unscaledDeltaTime));}
    }
}

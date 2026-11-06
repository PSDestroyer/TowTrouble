using UnityEngine;
using UnityEngine.EventSystems;
namespace TowTrouble
{
    // Controller/keyboard selection scrolls the focused card into the same viewport used by the mouse.
    public sealed class RecoveryGarageCardFocus : MonoBehaviour,ISelectHandler
    {
        private UnityEngine.UI.ScrollRect scroll;
        public void Configure(UnityEngine.UI.ScrollRect value){scroll=value;}
        public static float VisibleOffset(float top,float bottom,float viewportHeight,float current,float contentHeight)
        {
            float next=top<current?top:bottom>current+viewportHeight?bottom-viewportHeight:current;
            return Mathf.Clamp(next,0,Mathf.Max(0,contentHeight-viewportHeight));
        }
        public void OnSelect(BaseEventData data)
        {
            // A partly visible mouse target must not move away between pointer down and click.
            if(data is PointerEventData)return;
            if(scroll==null||scroll.content==null||scroll.viewport==null)return;
            var rect=(RectTransform)transform;float top=-rect.anchoredPosition.y;
            float next=VisibleOffset(top,top+rect.rect.height,scroll.viewport.rect.height,scroll.content.anchoredPosition.y,scroll.content.rect.height);
            if(scroll is RecoverySmoothScrollRect smooth){smooth.ScrollTo(next);return;}
            scroll.StopMovement();scroll.content.anchoredPosition=new Vector2(0,next);
        }
    }
}

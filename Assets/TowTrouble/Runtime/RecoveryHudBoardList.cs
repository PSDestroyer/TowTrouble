using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private void BuildBoardCard(Transform parent,RecoveryContract[] contracts,int id,float x,float y,Vector3 position,UnityEngine.UI.ScrollRect scroll)
        {
            var contract=contracts[id];bool active=boardIndex==id;float distance=Vector3.Distance(position,contract.Target);
            var card=Button(parent,contract.TitleFor(P),x,y,580,()=>SelectBoardRecovery(id),active,142);
            card.gameObject.name="board_recovery_"+id;
            var navigation=card.navigation;navigation.mode=UnityEngine.UI.Navigation.Mode.None;card.navigation=navigation;
            boardCards[id]=card;
            var title=card.GetComponentInChildren<TMP_Text>();title.rectTransform.anchoredPosition=new Vector2(216,-12);title.rectTransform.sizeDelta=new Vector2(344,50);title.font=displayFont!=null?displayFont:font;title.fontSize=24*P.textScale;
            ContractPhoto(card.transform,id,14,14,184,112);
            Text(card.transform,contract.Difficulty(P)+"  ·  "+Mathf.RoundToInt(distance)+" m",218,65,340,25,16,active?Ink:Muted);Icon(card.transform,RecoveryIcon.Coins,218,103,22,active?Ink:Amber);Text(card.transform,contract.Reward.ToString("N0")+" CR",250,94,300,36,23,active?Ink:Amber,true);
            if(scroll!=null)card.gameObject.AddComponent<RecoveryGarageCardFocus>().Configure(scroll);
        }
        private void BuildCompletedRecoveries(List<int> entries,RecoveryContract[] contracts,Vector3 position)
        {
            // Scrollbar uses the existing inter-column gap; card and detail widths are unchanged.
            var holder=Node("Completed recoveries scroll",pageRoot,76,232,604,458);
            var scroll=holder.gameObject.AddComponent<RecoverySmoothScrollRect>();scroll.ReducedMotion=P.reducedMotion;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=40;scroll.decelerationRate=.10f;
            var viewport=Node("Viewport",holder,0,0,580,458);viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color=Color.white;viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=false;
            var content=Node("Completed recoveries content",viewport,0,0,580,0);
            var layout=content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();layout.spacing=16;layout.childAlignment=TextAnchor.UpperLeft;layout.childControlWidth=false;layout.childControlHeight=false;layout.childForceExpandWidth=false;layout.childForceExpandHeight=false;
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport;scroll.content=content;boardScroll=scroll;
            var barRoot=Node("Completed scrollbar",holder,588,0,10,458);barRoot.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.08f,.09f,.10f,.9f);
            var area=Stretch("Sliding area",barRoot);area.offsetMin=new Vector2(2,2);area.offsetMax=new Vector2(-2,-2);var handle=Stretch("Handle",area);var graphic=handle.gameObject.AddComponent<UnityEngine.UI.Image>();graphic.color=Amber;
            var bar=barRoot.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();bar.targetGraphic=graphic;bar.handleRect=handle;bar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;var navigation=bar.navigation;navigation.mode=UnityEngine.UI.Navigation.Mode.None;bar.navigation=navigation;
            scroll.verticalScrollbar=bar;
            foreach(int id in entries)BuildBoardCard(content,contracts,id,0,0,position,scroll);
            float remembered=boardCompletedScrollY;UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);Canvas.ForceUpdateCanvases();scroll.Rebuild(UnityEngine.UI.CanvasUpdate.PostLayout);scroll.StopMovement();
            float range=Mathf.Max(0,content.rect.height-viewport.rect.height),offset=Mathf.Clamp(remembered,0,range);scroll.verticalNormalizedPosition=range>0?1-offset/range:1;bar.SetValueWithoutNotify(range>0?1-offset/range:1);barRoot.gameObject.SetActive(range>.01f);
            var current=pageRoot;scroll.onValueChanged.AddListener(_=>{if(pageRoot==current)boardCompletedScrollY=content.anchoredPosition.y;});
        }
    }
}

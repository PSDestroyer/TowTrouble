using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private const float GarageCategoryWidth=RecoveryGarageLayout.CategoryWidth,GaragePreviewX=RecoveryGarageLayout.PreviewX,
            GaragePreviewWidth=RecoveryGarageLayout.PreviewWidth,GarageCatalogX=RecoveryGarageLayout.CatalogX,GarageCatalogWidth=RecoveryGarageLayout.CatalogWidth;
        private float garageScrollY;
        private void BuildGarageCatalog(List<GaragePart> candidates)
        {
            var list=Panel(pageRoot,GarageCatalogX,212,GarageCatalogWidth,548);list.name="Garage catalogue";
            Icon(list,SlotIcon(garageSlot),20,16,28,Amber);Text(list,SlotName(garageSlot),60,12,GarageCatalogWidth-160,36,23,Paper,true);
            var count=Text(list,candidates.Count.ToString(),GarageCatalogWidth-86,17,64,26,17,Muted);count.alignment=TextAlignmentOptions.Right;
            var scrollRoot=Node("Continuous parts scroll",list,16,64,GarageCatalogWidth-32,468);
            var scroll=scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=40;scroll.decelerationRate=.10f;
            var viewport=Node("Viewport",scrollRoot,0,0,GarageCatalogWidth-52,468);
            var maskImage=viewport.gameObject.AddComponent<UnityEngine.UI.Image>();maskImage.color=Color.white;maskImage.raycastTarget=true;
            var mask=viewport.gameObject.AddComponent<UnityEngine.UI.Mask>();mask.showMaskGraphic=false;
            var content=Node("Three column parts grid",viewport,0,0,GarageCatalogWidth-52,0);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.sizeDelta=Vector2.zero;
            float cellWidth=(GarageCatalogWidth-52-20)/3;
            var grid=content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();grid.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=3;grid.cellSize=new Vector2(cellWidth,220);grid.spacing=new Vector2(10,12);grid.childAlignment=TextAnchor.UpperLeft;
            var fitter=content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();fitter.horizontalFit=UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;fitter.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport;scroll.content=content;
            var barRoot=Node("Catalogue scrollbar",scrollRoot,GarageCatalogWidth-44,0,12,468);var track=barRoot.gameObject.AddComponent<UnityEngine.UI.Image>();track.color=new Color(.08f,.09f,.10f,.9f);
            var area=Stretch("Sliding area",barRoot);area.offsetMin=new Vector2(2,2);area.offsetMax=new Vector2(-2,-2);
            var handle=Stretch("Handle",area);var handleImage=handle.gameObject.AddComponent<UnityEngine.UI.Image>();handleImage.color=Amber;
            var bar=barRoot.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();bar.targetGraphic=handleImage;bar.handleRect=handle;bar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            var nav=bar.navigation;nav.mode=UnityEngine.UI.Navigation.Mode.None;bar.navigation=nav;
            scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=UnityEngine.UI.ScrollRect.ScrollbarVisibility.Permanent;
            foreach(var part in candidates)BuildGarageCard(content,part,cellWidth,scroll);
            if(candidates.Count==0)Text(list,L("Nicio piesă compatibilă.","No compatible parts."),24,100,GarageCatalogWidth-48,50,18,Muted);
            float remembered=garageScrollY;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);Canvas.ForceUpdateCanvases();
            scroll.Rebuild(UnityEngine.UI.CanvasUpdate.PostLayout);scroll.StopMovement();
            float range=Mathf.Max(0,content.rect.height-viewport.rect.height),offset=Mathf.Clamp(remembered,0,range);
            scroll.verticalNormalizedPosition=range>0?1-offset/range:1;
            bar.SetValueWithoutNotify(range>0?1-offset/range:1);barRoot.gameObject.SetActive(range>.01f);
            var currentPage=pageRoot;scroll.onValueChanged.AddListener(_=>{if(pageRoot==currentPage)garageScrollY=content.anchoredPosition.y;});
        }
        private string GarageCardName(GaragePart part)
        {
            string label=PartName(part);
            if(part.Slot==GarageSlot.Wheels&&label.StartsWith("Anvelope ")){label=label.Substring(9);label=char.ToUpperInvariant(label[0])+label.Substring(1);}
            if(part.Slot==GarageSlot.Cable||part.Slot==GarageSlot.Winch){int capacity=label.IndexOf(" · ",System.StringComparison.Ordinal);if(capacity>=0)label=label.Substring(0,capacity);}
            return label;
        }
        private void BuildGarageCard(Transform content,GaragePart part,float width,UnityEngine.UI.ScrollRect scroll)
        {
            var catalog=session.GarageCatalog;bool active=garageSelected==part.Id,owned=part.Price==0||P.Owns(part.Id);
            var card=Button(content,"",0,0,width,()=>{garageScrollY=scroll.content.anchoredPosition.y;garageSelected=part.Id;garageDraft.Set(part,catalog);garageDraft.Normalize(catalog);session.PreviewGarage(garageDraft);Show("Garage");},active,220);
            card.gameObject.name=part.Id;var label=card.GetComponentInChildren<TMP_Text>();label.gameObject.name="Garage item name";label.text=GarageCardName(part);label.font=font;label.fontStyle=FontStyles.Bold;label.fontSize=17*P.textScale;label.rectTransform.anchoredPosition=new Vector2(12,-95);label.rectTransform.sizeDelta=new Vector2(width-24,66);label.alignment=TextAlignmentOptions.TopLeft;
            var imageBack=Panel(card.transform,9,9,width-18,80,new Color(.10f,.13f,.14f,.96f));imageBack.name="Part image";
            if(part.Thumbnail!=null)
            {
                var image=Stretch("Item thumbnail",imageBack).gameObject.AddComponent<UnityEngine.UI.RawImage>();image.texture=part.Thumbnail;image.uvRect=part.ThumbnailUV;image.raycastTarget=false;
            }
            else
            {
                Icon(imageBack,SlotIcon(part.Slot),(width-18-54)*.5f,17,54,Amber);
                if(part.Slot==GarageSlot.Paint){var swatch=Panel(imageBack,12,12,18,18,new[]{new Color(.43f,.24f,.12f),Paper,new Color(.43f,.48f,.24f),new Color(.95f,.65f,.16f)}[Mathf.Clamp(part.PaintIndex,0,3)]);swatch.GetComponent<UnityEngine.UI.Image>().color=new[]{new Color(.43f,.24f,.12f),Paper,new Color(.43f,.48f,.24f),new Color(.95f,.65f,.16f)}[Mathf.Clamp(part.PaintIndex,0,3)];}
            }
            if(P.Build(P.vehicle).Item(part.Slot,catalog)==part.Id)Icon(card.transform,RecoveryIcon.Check,width-32,14,18,Amber);
            string price=owned?(part.Price==0?L("STOCK","STOCK"):L("DEȚINUT","OWNED")):RecoveryEconomy.Price(P,part).ToString("N0")+" CR";
            Text(card.transform,price,12,166,width-24,25,17,active?Ink:Amber,true).alignment=TextAlignmentOptions.MidlineLeft;
            string performance=RecoveryGarageCardInfo.Performance(part,P.language=="en",P.vehicle);
            if(!string.IsNullOrEmpty(performance))Text(card.transform,performance,12,196,width-24,20,part.Slot==GarageSlot.Assembly?12:13,active?Ink:Muted,true);
            card.gameObject.AddComponent<RecoveryGarageCardFocus>().Configure(scroll);
        }
    }
}

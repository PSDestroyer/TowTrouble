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
            Icon(list,SlotIcon(garageSlot),20,16,28,Amber);Text(list,SlotName(garageSlot)+(!garageCategoryFocus?"  >":""),60,12,GarageCatalogWidth-160,36,23,!garageCategoryFocus?Amber:Paper,true);
            var count=Text(list,candidates.Count.ToString(),GarageCatalogWidth-86,17,64,26,17,Muted);count.alignment=TextAlignmentOptions.Right;
            Text(list,"Price: low to high",20,43,GarageCatalogWidth-40,22,15,Muted);
            var all=GarageButton(list,"ALL",16,70,116,()=>{if(garageOwnedOnly)ToggleGarageOwned();},!garageOwnedOnly,36);
            var owned=GarageButton(list,"OWNED",140,70,188,()=>{if(!garageOwnedOnly)ToggleGarageOwned();},garageOwnedOnly,36);
            foreach(var button in new[]{all,owned})GarageFont(button.GetComponentInChildren<TMP_Text>(),15);
            AddGarageHint(owned,"filter");
            float top=118,height=414;
            if(garageSlot==GarageSlot.Wheels)
            {
                string[] roles={"ALL","ROAD","MIXED","MUD","SNOW","SAND"};float width=(GarageCatalogWidth-52-25)/6;
                for(int i=0;i<roles.Length;i++){int role=i;var button=GarageButton(list,roles[i],16+i*(width+5),114,width,()=>{garageTireFilter=role;garageScrollY=0;Show("Garage");},garageTireFilter==i,32);GarageFont(button.GetComponentInChildren<TMP_Text>(),15);}
                top=158;height=374;
            }
            var scrollRoot=Node("Continuous parts scroll",list,16,top,GarageCatalogWidth-32,height);
            var scroll=scrollRoot.gameObject.AddComponent<RecoverySmoothScrollRect>();scroll.ReducedMotion=P.reducedMotion;garageListScroll=scroll;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=40;scroll.decelerationRate=.10f;
            var viewport=Node("Viewport",scrollRoot,0,0,GarageCatalogWidth-52,height);
            var maskImage=viewport.gameObject.AddComponent<UnityEngine.UI.Image>();maskImage.color=Color.white;maskImage.raycastTarget=true;
            var mask=viewport.gameObject.AddComponent<UnityEngine.UI.Mask>();mask.showMaskGraphic=false;
            var content=Node("Three column parts grid",viewport,0,0,GarageCatalogWidth-52,0);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.sizeDelta=Vector2.zero;
            float cellWidth=(GarageCatalogWidth-52-20)/3;
            var grid=content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();grid.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=3;grid.cellSize=new Vector2(cellWidth,garageSlot==GarageSlot.Assembly?292:270);grid.spacing=new Vector2(10,12);grid.childAlignment=TextAnchor.UpperLeft;
            var fitter=content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();fitter.horizontalFit=UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;fitter.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport;scroll.content=content;
            var barRoot=Node("Catalogue scrollbar",scrollRoot,GarageCatalogWidth-44,0,12,height);var track=barRoot.gameObject.AddComponent<UnityEngine.UI.Image>();track.color=new Color(.08f,.09f,.10f,.9f);
            var area=Stretch("Sliding area",barRoot);area.offsetMin=new Vector2(2,2);area.offsetMax=new Vector2(-2,-2);
            var handle=Stretch("Handle",area);var handleImage=handle.gameObject.AddComponent<UnityEngine.UI.Image>();handleImage.color=Amber;
            var bar=barRoot.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();bar.targetGraphic=handleImage;bar.handleRect=handle;bar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            var nav=bar.navigation;nav.mode=UnityEngine.UI.Navigation.Mode.None;bar.navigation=nav;
            scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=UnityEngine.UI.ScrollRect.ScrollbarVisibility.Permanent;
            foreach(var part in candidates)BuildGarageCard(content,part,cellWidth,scroll);
            if(candidates.Count==0)Text(list,"No matching parts. Switch to All or another tire type.",24,top+24,GarageCatalogWidth-48,100,18,Muted);
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
            string label=GarageName(part);
            if(part.Slot==GarageSlot.Wheels&&label.StartsWith("Anvelope ")){label=label.Substring(9);label=char.ToUpperInvariant(label[0])+label.Substring(1);}
            if(part.Slot==GarageSlot.Cable||part.Slot==GarageSlot.Winch){int capacity=label.IndexOf(" · ",System.StringComparison.Ordinal);if(capacity>=0)label=label.Substring(0,capacity);}
            return label;
        }
        private void BuildGarageCard(Transform content,GaragePart part,float width,UnityEngine.UI.ScrollRect scroll)
        {
            var catalog=session.GarageCatalog;bool active=garageSelected==part.Id,owned=part.Price==0||P.Owns(part.Id);
            var card=GarageButton(content,"",0,0,width,()=>SelectGaragePart(part.Id),active,part.Slot==GarageSlot.Assembly?292:270);garageCards[part.Id]=card;
            card.gameObject.name=part.Id;var label=card.GetComponentInChildren<TMP_Text>();label.gameObject.name="Garage item name";label.text=GarageCardName(part);label.font=font;label.fontStyle=FontStyles.Bold;GarageFont(label,17);label.fontSizeMin=15;label.rectTransform.anchoredPosition=new Vector2(12,-102);label.rectTransform.sizeDelta=new Vector2(width-24,76);label.alignment=TextAlignmentOptions.TopLeft;
            var imageBack=Panel(card.transform,9,9,width-18,90,new Color(.10f,.13f,.14f,.96f));imageBack.name="Part image";
            if(part.Thumbnail!=null)
            {
                var image=Stretch("Item thumbnail",imageBack).gameObject.AddComponent<UnityEngine.UI.RawImage>();image.texture=part.Thumbnail;image.uvRect=part.ThumbnailUV;image.raycastTarget=false;
            }
            else
            {
                Icon(imageBack,SlotIcon(part.Slot),(width-18-54)*.5f,17,54,Amber);
                if(part.Slot==GarageSlot.Paint){var palette=RecoveryPaintPalette.Current;var build=garageDraft??P.Build(P.vehicle);var tint=palette!=null?palette.SwatchFor(build,catalog,part.PaintIndex):RecoveryPaintPalette.Swatch(part.PaintIndex,P.vehicle);Panel(imageBack,12,12,18,18,tint);}
            }
            var state=RecoveryGarageWorkflow.State(P,part,catalog);
            string badge=active&&state!=GarageItemState.Equipped?"PREVIEW":state==GarageItemState.Equipped?"EQUIPPED":owned?"OWNED":null;
            if(badge!=null){var strip=Panel(imageBack,0,0,width-18,26,new Color(.06f,.08f,.09f,.96f));Text(strip,badge,5,0,width-28,26,15,badge=="PREVIEW"?Amber:Paper,true);}
            if(garageRecommendation?.Id==part.Id)Text(imageBack,"SUGGESTED",5,62,width-28,26,15,Amber,true);
            string price=owned?(part.Price==0?L("STOCK","STOCK"):L("DEȚINUT","OWNED")):RecoveryEconomy.Price(P,part).ToString("N0")+" CR";
            Text(card.transform,price,12,184,width-24,28,17,Amber,true).alignment=TextAlignmentOptions.MidlineLeft;
            string performance=RecoveryGarageWorkflow.AppearanceOnly(part)?"Appearance only":RecoveryGarageCardInfo.Performance(part,true,P.vehicle);
            if(part.Slot==GarageSlot.Assembly)performance=performance.Replace(" kg · "," kg\n").Replace("Top speed: ","");
            if(!string.IsNullOrEmpty(performance))Text(card.transform,performance,12,216,width-24,part.Slot==GarageSlot.Assembly?70:48,15,Muted,true);
            card.gameObject.AddComponent<RecoveryGarageCardFocus>().Configure(scroll);
        }
    }
}

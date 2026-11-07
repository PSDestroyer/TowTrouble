using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private RectTransform hudSafeRoot;
        private Vector2 screenRootSize;
        private Rect screenSafeArea;
        private int layoutScreenWidth,layoutScreenHeight;
        private float nextWindowResize;
        private readonly List<RectTransform> screenShades=new List<RectTransform>();
        private readonly Dictionary<RectTransform,float> screenBands=new Dictionary<RectTransform,float>();
        private readonly Vector3[] screenCorners=new Vector3[4];
        private Rect ScreenSafeBounds
        {
            get{var size=root.rect.size;var safe=RecoveryScreenLayout.Safe(screenSafeArea);return new Rect(safe.x*size.x,safe.y*size.y,safe.width*size.x,safe.height*size.y);}
        }
        private Rect HudSafeBounds
        {
            get
            {
                var area=ScreenSafeBounds;
                if(area.width>1600){area.x+=(area.width-1600)*.5f;area.width=1600;}
                return area;
            }
        }
        private void ConfigureHudScreen()
        {
            hudSafeRoot=Node("HUD safe area",gameplay.transform,0,0,1600,900);
            foreach(var item in new[]{missionStatusPanel,guidanceNote,crew.rectTransform,speed.rectTransform,
                (RectTransform)cableGroup.transform,(RectTransform)partnerGroup.transform,exploreHintPlate})
            {
                var position=item.anchoredPosition;item.SetParent(hudSafeRoot,false);item.anchoredPosition=position;
            }
            crew.rectTransform.anchorMin=crew.rectTransform.anchorMax=crew.rectTransform.pivot=Vector2.one;
            crew.rectTransform.anchoredPosition=new Vector2(-30,-25);
            speed.rectTransform.anchorMin=speed.rectTransform.anchorMax=speed.rectTransform.pivot=new Vector2(1,0);
            speed.rectTransform.anchoredPosition=new Vector2(-32,30);
            RefreshScreenLayout(true);
        }
        private void RefreshScreenLayout(bool force=false)
        {
            if(root==null)return;
            var size=root.rect.size;int width=Mathf.Max(1,Screen.width),height=Mathf.Max(1,Screen.height);
            var pixels=Screen.safeArea;var safe=RecoveryScreenLayout.Safe(new Rect(pixels.x/width,pixels.y/height,pixels.width/width,pixels.height/height));
            if(!force&&size==screenRootSize&&safe==screenSafeArea&&width==layoutScreenWidth&&height==layoutScreenHeight)return;
            screenRootSize=size;screenSafeArea=safe;layoutScreenWidth=width;layoutScreenHeight=height;
            ApplyScreenLayout(size,safe);
        }
        // Separate geometry from Screen APIs so Editor previews can exercise notches and resizes safely.
        private void ApplyScreenLayout(Vector2 size,Rect safe)
        {
            screenSafeArea=RecoveryScreenLayout.Safe(safe);
            var content=RecoveryScreenLayout.Content(size.x/Mathf.Max(1,size.y),screenSafeArea);
            if(pageRoot!=null)
            {
                pageRoot.anchorMin=pageRoot.anchorMax=pageRoot.pivot=Vector2.one*.5f;
                pageRoot.anchoredPosition=Vector2.Scale(content.center-Vector2.one*.5f,size);
                pageRoot.localScale=Vector3.one*(content.width*size.x/1600);
            }
            if(hudSafeRoot!=null)
            {
                var bounds=HudSafeBounds;
                hudSafeRoot.anchorMin=hudSafeRoot.anchorMax=hudSafeRoot.pivot=Vector2.zero;
                hudSafeRoot.anchoredPosition=bounds.position;hudSafeRoot.sizeDelta=bounds.size;
            }
            foreach(var shade in screenShades)if(shade!=null)FitScreenShade(shade);
            foreach(var band in screenBands)if(band.Key!=null)FitScreenBand(band.Key,band.Value);
            if(persistenceHint!=null)
            {
                persistenceHint.anchorMin=persistenceHint.anchorMax=persistenceHint.pivot=Vector2.one;
                persistenceHint.anchoredPosition=new Vector2(-(1-screenSafeArea.xMax)*size.x-22,-(1-screenSafeArea.yMax)*size.y-12);
            }
        }
        private RectTransform ScreenShade(Transform parent,Color color)
        {
            var rect=Node("Screen dimmer",parent,0,0,1600,900);
            var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=true;
            screenShades.Add(rect);FitScreenShade(rect);return rect;
        }
        private void FitScreenShade(RectTransform shade)
        {
            if(root==null||shade.parent==null)return;
            var parent=(RectTransform)shade.parent;root.GetWorldCorners(screenCorners);
            Vector3 min=parent.InverseTransformPoint(screenCorners[0]),max=parent.InverseTransformPoint(screenCorners[2]);
            shade.anchorMin=shade.anchorMax=shade.pivot=Vector2.one*.5f;
            shade.anchoredPosition=(Vector2)((min+max)*.5f)-parent.rect.center;
            shade.sizeDelta=(Vector2)(max-min);
        }
        private RectTransform ScreenBand(Transform parent,float height,Color color)
        {
            var rect=Node("Screen header background",parent,0,0,1600,height);
            var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;
            screenBands.Add(rect,height);FitScreenBand(rect,height);return rect;
        }
        private void FitScreenBand(RectTransform band,float height)
        {
            var parent=(RectTransform)band.parent;root.GetWorldCorners(screenCorners);
            var min=parent.InverseTransformPoint(screenCorners[0]);var max=parent.InverseTransformPoint(screenCorners[2]);
            float bottom=parent.rect.yMax-height;
            band.anchorMin=band.anchorMax=band.pivot=Vector2.one*.5f;
            band.anchoredPosition=new Vector2((min.x+max.x)*.5f,(max.y+bottom)*.5f)-parent.rect.center;
            band.sizeDelta=new Vector2(max.x-min.x,Mathf.Max(height,max.y-bottom));
        }
        private void MaintainReadableWindow()
        {
#if UNITY_STANDALONE && !UNITY_EDITOR
            if(Screen.fullScreen||RecoveryScreenLayout.Readable(new Vector2Int(Screen.width,Screen.height))||Time.unscaledTime<nextWindowResize)return;
            var display=Screen.currentResolution;
            if(display.width<RecoveryScreenLayout.MinimumWidth||display.height<RecoveryScreenLayout.MinimumHeight)return;
            nextWindowResize=Time.unscaledTime+1;
            Screen.SetResolution(Mathf.Max(Screen.width,RecoveryScreenLayout.MinimumWidth),Mathf.Max(Screen.height,RecoveryScreenLayout.MinimumHeight),FullScreenMode.Windowed);
#endif
        }
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    // Text remains the fallback for missing/unknown glyphs. Resources contain only the small approved button set.
    public sealed class RecoveryControlGlyph : MonoBehaviour
    {
        private DemoSession session;
        private TMP_Text label;
        private UnityEngine.UI.RawImage picture;
        private UnityEngine.UI.Image keycap;
        private Color keycapColor;
        private Color textColor;
        private string previous;
        private static readonly Dictionary<string,Texture2D> cache = new Dictionary<string,Texture2D>();
        public void Configure(DemoSession value) { session=value; label=GetComponent<TMP_Text>();textColor=label.color;keycap=transform.parent.GetComponent<UnityEngine.UI.Image>();if(keycap!=null)keycapColor=keycap.color; }
        private void LateUpdate()
        {
            if(label==null||session==null)return;
            var family=RecoveryInputPrompts.Family(Gamepad.current);
            string key=session.UsingGamepad?family+":"+label.text:"Keyboard";
            if(key==previous)return;previous=key;
            if(label.color.a>.01f)textColor=label.color;
            var texture=session.UsingGamepad?Load(family,label.text):null;
            if(keycap!=null)keycap.color=texture!=null?new Color(.08f,.10f,.13f,.96f):keycapColor;
            if(texture!=null&&picture==null)
            {
                var go=new GameObject("Controller glyph",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));go.transform.SetParent(transform,false);
                picture=go.GetComponent<UnityEngine.UI.RawImage>();picture.raycastTarget=false;
                var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
                rect.sizeDelta=Vector2.one*Mathf.Min(28,Mathf.Max(20,label.rectTransform.rect.height));
            }
            if(picture!=null){picture.texture=texture;picture.gameObject.SetActive(texture!=null);}
            label.color=texture!=null?new Color(textColor.r,textColor.g,textColor.b,0):textColor;
        }
        private static Texture2D Load(RecoveryControllerFamily family,string token)
        {
            string file=family==RecoveryControllerFamily.PlayStation?"PS_"+token:family==RecoveryControllerFamily.Nintendo?"Nintendo_"+token:"Xbox_"+token;
            if(!cache.TryGetValue(file,out var texture)){texture=Resources.Load<Texture2D>("ControllerGlyphs/"+file);cache[file]=texture;}
            return texture;
        }
    }
}

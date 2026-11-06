using TMPro;
using UnityEngine;
namespace TowTrouble
{
    // Standalone hints use the same atlas as inline text, with aspect-correct sizing independent of font metrics.
    public sealed class RecoveryControlGlyph : MonoBehaviour
    {
        private TMP_Text label;
        private UnityEngine.UI.RawImage picture;
        private UnityEngine.UI.Image background;
        private string previous;
        private bool previousPad;
        private RecoveryControllerFamily previousFamily;
        private Vector2 previousSize;
        public void Configure(DemoSession session)
        {
            label=GetComponent<TMP_Text>();background=transform.parent.GetComponent<UnityEngine.UI.Image>();
            var go=new GameObject("Input glyph",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));go.transform.SetParent(transform,false);
            picture=go.GetComponent<UnityEngine.UI.RawImage>();picture.raycastTarget=false;
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
            label.margin=Vector4.zero;
        }
        private void LateUpdate()
        {
            if(label==null)return;
            var family=RecoveryInputPrompts.Family(RecoveryInputDevices.Pad);
            bool pad=RecoveryInputDevices.UsingGamepad;Vector2 size=label.rectTransform.rect.size;
            if(previous==label.text&&previousPad==pad&&previousFamily==family&&previousSize==size)return;
            previous=label.text;previousPad=pad;previousFamily=family;previousSize=size;
            string name=RecoveryPromptText.GlyphName(label.text,pad,family);
            var atlas=RecoveryPromptText.Atlas;int index=atlas!=null?atlas.GetSpriteIndexFromName(name):-1;
            bool found=index>=0;picture.gameObject.SetActive(found);label.enabled=!found;
            if(background!=null){background.enabled=!found;background.color=new Color(.18f,.21f,.23f,1);}
            if(!found){label.color=new Color(1,.96f,.83f);return;}
            var glyph=atlas.spriteCharacterTable[index].glyph.glyphRect;
            picture.name="Input glyph "+name;picture.texture=atlas.spriteSheet;
            picture.uvRect=new Rect(glyph.x/(float)atlas.spriteSheet.width,glyph.y/(float)atlas.spriteSheet.height,glyph.width/(float)atlas.spriteSheet.width,glyph.height/(float)atlas.spriteSheet.height);
            float aspect=glyph.width/(float)glyph.height;
            float height=Mathf.Min(size.y,size.x/aspect);
            picture.rectTransform.sizeDelta=new Vector2(height*aspect,height);
        }
    }
}

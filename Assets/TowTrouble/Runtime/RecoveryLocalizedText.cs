using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace TowTrouble
{
    // Translation is applied at rendering, leaving source labels and control identities stable.
    public sealed class RecoveryLocalizedText : MonoBehaviour,ITextPreprocessor
    {
        private TMP_Text label;
        private TMP_FontAsset originalFont,desiredFont;
        private bool translate=true;
        public static RecoveryLocalizedText Bind(TMP_Text text,bool translateText=true)
        {
            var value=text.GetComponent<RecoveryLocalizedText>()??text.gameObject.AddComponent<RecoveryLocalizedText>();
            if(value.label==null)value.originalFont=text.font;
            value.label=text;value.translate=translateText;
            if(!(text.textPreprocessor is RecoveryPromptText))text.textPreprocessor=value;
            value.Refresh(null);return value;
        }
        public string PreprocessText(string text)
        {
            string value=translate?RecoveryLocalization.Translate(text):text;
            if(label!=null)label.isRightToLeftText=RecoveryLocalization.Code=="ar"&&RTLTMPro.TextUtils.IsRTLInput(value??"");
            return RecoveryLocalization.Shape(value);
        }
        private void LateUpdate(){if(label!=null&&desiredFont!=null&&label.font!=desiredFont)label.font=desiredFont;}
        private void OnEnable(){LocalizationSettings.SelectedLocaleChanged+=Refresh;if(label!=null)Refresh(null);}
        private void OnDisable(){if(LocalizationSettings.HasSettings)LocalizationSettings.SelectedLocaleChanged-=Refresh;}
        private void Refresh(Locale locale)
        {
            if(label==null||!RecoveryLocalization.Ready)return;
            bool display=RecoveryLocalization.Catalog.WesternDisplay==originalFont;
            var font=RecoveryLocalization.Font(display);if(font!=null){desiredFont=font;label.font=font;}
            label.havePropertiesChanged=true;label.SetAllDirty();
        }
    }
}

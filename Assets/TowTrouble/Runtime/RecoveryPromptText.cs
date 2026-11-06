using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace TowTrouble
{
    // Only opt-in hint text is tokenized: prices, descriptions and player names remain ordinary text.
    public sealed class RecoveryPromptText : MonoBehaviour, ITextPreprocessor
    {
        private TMP_Text label;
        private bool single;
        private bool hasStyle, renderedPad;
        private RecoveryControllerFamily renderedFamily;
        private static TMP_SpriteAsset atlas;
        private static readonly Regex tokens = new Regex(@"(?<![\w])(?:Right stick|Left stick|D-pad|LMB|RMB|Wheel|Shift|Ctrl|Alt|Page Up|Page Down|Mouse wheel|Drag car|WASD|Arrows|UpArrow|DownArrow|LeftArrow|RightArrow|Backspace|Space|Enter|Escape|Esc|Tab|Delete|Del|Square|Triangle|Circle|Cross|Options|Share|Menu|View|R-stick|L-stick|NumpadEnter|LeftShift|RightShift|LeftCtrl|RightCtrl|LeftAlt|RightAlt|F(?:1[0-2]|[1-9])|PageUp|PageDown|Home|End|Insert|Backquote|LeftBracket|RightBracket|Semicolon|Quote|Backslash|Comma|Period|Slash|Minus|Equals|L[123BT]|R[123BT]|ZL|ZR|LS|RS|Up|Down|Left|Right|[A-Z0-9]|[+\-])(?![\w])", RegexOptions.CultureInvariant);
        public static TMP_SpriteAsset Atlas => atlas != null ? atlas : atlas = Resources.Load<TMP_SpriteAsset>("InputGlyphs");
        public static RecoveryPromptText Bind(TMP_Text text, bool singleKey = false)
        {
            var component = text.GetComponent<RecoveryPromptText>() ?? text.gameObject.AddComponent<RecoveryPromptText>();
            component.label = text; component.single = singleKey; text.spriteAsset = Atlas; text.textPreprocessor = component; return component;
        }
        public static string GlyphName(string token, bool pad, RecoveryControllerFamily family)
        {
            string key = token.Trim('[', ']');
            if (!pad)
            {
                if(key.StartsWith("Numpad",StringComparison.Ordinal)&&key.Length==7&&char.IsDigit(key[6]))key=key.Substring(6);

                switch (key)
                {
                    case "Escape": key = "Esc"; break; case "Delete": key = "Del"; break;
                    case "UpArrow": key = "Up"; break; case "DownArrow": key = "Down"; break; case "LeftArrow": key = "Left"; break; case "RightArrow": key = "Right"; break;
                    case "Page Up": key = "PageUp"; break; case "Page Down": key = "PageDown"; break; case "NumpadEnter": key = "Enter"; break;
                    case "LeftShift": case "RightShift": key = "Shift"; break; case "LeftCtrl": case "RightCtrl": key = "Ctrl"; break; case "LeftAlt": case "RightAlt": key = "Alt"; break;
                    case "Mouse wheel": key = "Wheel"; break; case "Drag car": key = "LMB"; break;
                }
                return "Keyboard_" + key;
            }
            switch (key)
            {
                case "D-pad": key = "Dpad"; break;
                case "Up": case "Down": case "Left": case "Right": key="Dpad"+key; break; case "Left stick": case "LS": key = "LeftStick"; break;
                case "Right stick": key = "RightStick"; break;
            }
            return (family == RecoveryControllerFamily.PlayStation ? "PS_" : family == RecoveryControllerFamily.Nintendo ? "Nintendo_" : "Xbox_") + key;
        }
        public static string Format(string text, bool pad, RecoveryControllerFamily family, bool singleKey = false)
        {
            if (string.IsNullOrEmpty(text) || Atlas == null) return text;
            string Replace(string token)
            {
                string name = GlyphName(token, pad, family);
                if (Atlas.GetSpriteIndexFromName(name) < 0) return token;
                return "<sprite name=\"" + name + "\" tint=0>";
            }
            if (singleKey) return Replace(text);
            // Brackets were an old text-only keycap; the glyph already supplies its frame.
            string result = tokens.Replace(text, m => Replace(m.Value));
            result = result.Replace("[<sprite", "<sprite").Replace("tint=0>]", "tint=0>");
            return result.Replace("tint=0>", "tint=0></size>").Replace("<sprite name=", "<size=145%><sprite name=");
        }
        public string PreprocessText(string text) => Format(text, RecoveryInputDevices.UsingGamepad, RecoveryInputPrompts.Family(RecoveryInputDevices.Pad), single);
        private void LateUpdate()
        {
            if (label == null) return;
            bool pad=RecoveryInputDevices.UsingGamepad;var family=RecoveryInputPrompts.Family(RecoveryInputDevices.Pad);
            if(hasStyle&&renderedPad==pad&&renderedFamily==family)return;
            hasStyle=true;renderedPad=pad;renderedFamily=family;label.SetAllDirty();label.ForceMeshUpdate(false,true);
        }
    }
}

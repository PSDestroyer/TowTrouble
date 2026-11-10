using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryLocalizationTests
    {
        [Test] public void RegionsCollapseButChineseScriptsRemainDistinct()
        {
            Assert.AreEqual(29,RecoveryLanguage.Codes.Distinct().Count());
            Assert.AreEqual("es",RecoveryLanguage.Normalize("es-MX"));
            Assert.AreEqual("pt",RecoveryLanguage.Normalize("pt_PT"));
            Assert.AreEqual("no",RecoveryLanguage.Normalize("nb-NO"));
            Assert.AreEqual("zh-Hans",RecoveryLanguage.Normalize("zh-CN"));
            Assert.AreEqual("zh-Hant",RecoveryLanguage.Normalize("zh-TW"));
            Assert.AreEqual("en",RecoveryLanguage.Normalize("unsupported"));
        }
        [Test] public void LanguageSurvivesProfileParsingAndPreferenceCopy()
        {
            var profile=RecoveryProfile.Parse("{\"version\":4,\"language\":\"ja-JP\"}");
            Assert.AreEqual("ja",profile.language);
            var target=new RecoveryProfile();RecoveryMenuSettings.CopyPreferences(profile,target);
            Assert.AreEqual("ja",target.language);
        }
        [Test] public void RenderTranslationKeepsCanonicalIdentityAndProtectsUserText()
        {
            string before=RecoveryLocalization.Code;GameObject go=null;
            try
            {
                RecoveryLocalization.Select("ja");
                go=new GameObject("Transient localization test",typeof(RectTransform));
                var label=go.AddComponent<TextMeshProUGUI>();label.text="Settings";
                var localized=RecoveryLocalizedText.Bind(label);
                Assert.AreEqual("Settings",label.text);
                Assert.AreNotEqual(label.text,localized.PreprocessText(label.text));
                RecoveryLocalizedText.Bind(label,false);
                Assert.AreEqual("Settings",localized.PreprocessText("Settings"));
                Assert.AreEqual("127.0.0.1",localized.PreprocessText("127.0.0.1"));
            }
            finally{if(go!=null)Object.DestroyImmediate(go);RecoveryLocalization.Select(before);}
        }
        [Test] public void ResultsKeepAmountsPercentagesAndDistinctBonusOutcomes()
        {
            string before=RecoveryLocalization.Code;
            try
            {
                foreach(string code in RecoveryLanguage.Codes)
                {
                    RecoveryLocalization.Select(code);
                    foreach(string source in new[]{"Base  2,500 CR   +   Bonuses  350 CR  ·  First recovery +25%","Replay base  2,500 CR   +   Bonuses  350 CR"})
                    {
                        string translated=RecoveryLocalization.Translate(source);
                        StringAssert.Contains("2,500 CR",translated,code);
                        StringAssert.Contains("350 CR",translated,code);
                        if(source.Contains("+25%"))StringAssert.Contains("+25%",translated,code);
                        if(code!="en")Assert.AreNotEqual(source,translated,code);
                    }
                    string earned=RecoveryLocalization.Translate("+10% bonus");
                    StringAssert.Contains("+10%",earned,code);
                    Assert.AreNotEqual(earned,RecoveryLocalization.Translate("Bonus missed"),code);
                    if(code!="en")Assert.AreNotEqual("the drop-off",RecoveryLocalization.Translate("the drop-off"),code);
                }
            }
            finally{RecoveryLocalization.Select(before);}
        }
        [Test] public void DynamicValuesAndMandatoryTreeStepSurviveLocalization()
        {
            string before=RecoveryLocalization.Code;
            try
            {
                RecoveryLocalization.Select("ro");
                string price=RecoveryLocalization.Translate("BUY & FIT · 12345 CR");
                StringAssert.Contains("12345 CR",price);StringAssert.DoesNotContain("BUY",price);
                string stats=RecoveryLocalization.Translate("Mud · four-wheel set. Road -6%, mud +18%, sand -5%, snow +5%. Mass +24 kg. Mud resistance -6%. Speed -2%.");
                foreach(string number in new[]{"-6%","+18%","-5%","+5%","+24 kg","-2%"})StringAssert.Contains(number,stats);
                string tree=RecoveryLocalization.Translate("The wheels dug two deep muddy ruts. Clear the tree first, then pull straight out of the tracks.");
                StringAssert.Contains("copacul",tree.ToLowerInvariant());
                StringAssert.DoesNotContain("Clear the tree",tree);
            }
            finally{RecoveryLocalization.Select(before);}
        }
    }
}

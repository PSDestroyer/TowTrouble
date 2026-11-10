using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.TextCore.LowLevel;

namespace TowTrouble.Editor
{
    public static class RecoveryLocalizationAuthoring
    {
        private const string Root="Assets/TowTrouble/Localization";
        [MenuItem("Tow Trouble/Localization/Refresh local tables")]
        public static void Refresh()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before authoring localization tables.");
            Directory.CreateDirectory(Root+"/Tables");Directory.CreateDirectory(Root+"/Locales");Directory.CreateDirectory(Root+"/Resources/RecoveryLocalizedFonts");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            T Asset<T>(string path)where T:ScriptableObject
            {var value=AssetDatabase.LoadAssetAtPath<T>(path);if(value==null){value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);}return value;}
            void Save(UnityEngine.Object value){EditorUtility.SetDirty(value);AssetDatabase.SaveAssetIfDirty(value);}
            var catalog=Asset<RecoveryLocalizationCatalog>(Root+"/Resources/RecoveryLocalizationCatalog.asset");
            catalog.WesternBody=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TowTrouble/Art/UI/Theme/Fonts/TowText SDF.asset");
            catalog.WesternDisplay=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TowTrouble/Art/UI/Theme/Fonts/TowDisplay SDF.asset");
            var rows=JArray.Parse(File.ReadAllText(Root+"/Source.json"));
            var aliases=File.Exists(Root+"/Aliases.json")?JObject.Parse(File.ReadAllText(Root+"/Aliases.json")):new JObject();
            catalog.Entries=rows.Select(r=>new RecoveryTextEntry{Key=(string)r["key"],Source=(string)r["source"],Context=string.Join("; ",r["contexts"]?.Values<string>()??Array.Empty<string>()),Aliases=aliases.Properties().Where(a=>(string)a.Value==(string)r["source"]).Select(a=>a.Name).ToArray()}).ToArray();
            var shared=Asset<SharedTableData>(Root+"/Tables/Text Shared.asset");shared.TableCollectionName="TowTrouble.Text";
            foreach(var entry in catalog.Entries)shared.AddKey(entry.Key);
            var fontShared=Asset<SharedTableData>(Root+"/Tables/Fonts Shared.asset");fontShared.TableCollectionName="TowTrouble.Fonts";fontShared.AddKey("Body");fontShared.AddKey("Display");
            var fonts=new[]{"Greek","Arabic","Thai","Japanese","Korean","SimplifiedChinese","TraditionalChinese","Cyrillic"};
            foreach(string name in fonts)
            {
                string path=Root+"/Resources/RecoveryLocalizedFonts/"+name+".asset";
                if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path)!=null)continue;
                string source=name=="Cyrillic"?"Assets/TowTrouble/Art/UI/Theme/Fonts/TowText.ttf":Root+"/Fonts/"+name+(name.Contains("Chinese")||name=="Japanese"||name=="Korean"?".otf":".ttf");
                var file=AssetDatabase.LoadAssetAtPath<Font>(source);if(file==null)throw new InvalidOperationException("Missing licensed font: "+source);
                var font=TMP_FontAsset.CreateFontAsset(file,48,6,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);font.name="Tow "+name;
                font.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>();font.material.mainTexture=font.atlasTexture;
                AssetDatabase.CreateAsset(font,path);
                foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
                AssetDatabase.AddObjectToAsset(font.material,font);Save(font);Save(font.material);
                if(!AssetDatabase.LoadAllAssetsAtPath(path).Any(a=>a is Material))throw new InvalidOperationException("Font material was not persisted: "+path);
            }
            catalog.Languages=new RecoveryLocaleData[RecoveryLanguage.Codes.Length];
            for(int i=0;i<RecoveryLanguage.Codes.Length;i++)
            {
                string code=RecoveryLanguage.Codes[i];string localePath=Root+"/Locales/"+code+".asset";
                var locale=AssetDatabase.LoadAssetAtPath<Locale>(localePath);
                if(locale==null){locale=Locale.CreateLocale(code);locale.LocaleName=RecoveryLanguage.Names[i];AssetDatabase.CreateAsset(locale,localePath);}
                var table=Asset<StringTable>(Root+"/Tables/Text_"+code+".asset");table.SharedData=shared;table.LocaleIdentifier=locale.Identifier;
                var file=Root+"/Translations/"+code+".json";var values=File.Exists(file)?JObject.Parse(File.ReadAllText(file)):new JObject();
                foreach(var entry in catalog.Entries)
                {
                    string value=(string)values[entry.Source];if(string.IsNullOrEmpty(value))value=entry.Source;
                    var item=table.GetEntry(entry.Key)??table.AddEntry(entry.Key,value);item.Value=value;item.IsSmart=false;
                }
                var assets=Asset<AssetTable>(Root+"/Tables/Fonts_"+code+".asset");assets.SharedData=fontShared;assets.LocaleIdentifier=locale.Identifier;
                string font=code=="el"?"Greek":code=="ar"?"Arabic":code=="th"?"Thai":code=="ja"?"Japanese":code=="ko"?"Korean":code=="zh-Hans"?"SimplifiedChinese":code=="zh-Hant"?"TraditionalChinese":code=="ru"||code=="uk"||code=="bg"||code=="cs"||code=="hu"||code=="pl"||code=="tr"||code=="vi"?"Cyrillic":null;
                string fontPath=font==null?null:Root+"/Resources/RecoveryLocalizedFonts/"+font+".asset";
                foreach(string key in new[]{"Body","Display"})
                {
                    var target=font==null?(key=="Body"?catalog.WesternBody:catalog.WesternDisplay):AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
                    var entry=assets.GetEntry(key)??assets.AddEntry(key,AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target)));entry.Guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target));
                }
                catalog.Languages[i]=new RecoveryLocaleData{Locale=locale,Strings=table,Assets=assets,FontResource=font==null?"":"RecoveryLocalizedFonts/"+font};
                Save(locale);Save(table);Save(assets);
            }
            Save(shared);Save(fontShared);Save(catalog);
            var settings=Asset<RecoveryLocalizationSettings>(Root+"/Resources/RecoveryLocalizationSettings.asset");
            settings.SetAvailableLocales(new RecoveryLocalLocales(catalog));
            settings.SetStringDatabase(new LocalizedStringDatabase{TableProvider=new RecoveryLocalTables(catalog),DefaultTable="TowTrouble.Text"});
            settings.SetAssetDatabase(new LocalizedAssetDatabase{TableProvider=new RecoveryLocalTables(catalog),DefaultTable="TowTrouble.Fonts"});
            settings.GetStartupLocaleSelectors().Clear();settings.GetStartupLocaleSelectors().Add(new SpecificLocaleSelector{LocaleId=new LocaleIdentifier("en")});
            LocalizationEditorSettings.ActiveLocalizationSettings=settings;LocalizationSettings.PreloadBehavior=PreloadBehavior.NoPreloading;Save(settings);
            var addressables=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if(addressables!=null)
            {
                addressables.BuildAddressablesWithPlayerBuild=UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
                addressables.DisableCatalogUpdateOnStartup=true;Save(addressables);
            }
            RecoveryLocalization.ReloadCatalog();
        }
    }
}

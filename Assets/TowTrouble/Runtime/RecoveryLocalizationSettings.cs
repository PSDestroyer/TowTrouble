using System;
using UnityEngine.Scripting;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TowTrouble
{
    // All data ships with the player. Package APIs remain the single owner of the selected locale.
    [Preserve] public sealed class RecoveryLocalizationSettings : LocalizationSettings
    {
        [NonSerialized] private AsyncOperationHandle<LocalizationSettings> ready;
        public override AsyncOperationHandle<LocalizationSettings> GetInitializationOperation()
        {
            // The standard initializer starts Addressables even with a custom table provider.
            // Our local providers have no catalog, bundle or remote dependency to initialize.
            if(!ready.IsValid())ready=Addressables.ResourceManager.CreateCompletedOperation<LocalizationSettings>(this,null);
            return ready;
        }
    }
    [Serializable, Preserve] public sealed class RecoveryLocalLocales : ILocalesProvider
    {
        [SerializeField] private RecoveryLocalizationCatalog catalog;
        [NonSerialized] private List<Locale> locales;
        public RecoveryLocalLocales(RecoveryLocalizationCatalog value){catalog=value;}
        public List<Locale> Locales
        {
            get
            {
                if(locales==null){locales=new List<Locale>();foreach(var item in catalog.Languages)locales.Add(item.Locale);}
                return locales;
            }
        }
        public Locale GetLocale(LocaleIdentifier id)
        {
            string code=RecoveryLanguage.Normalize(id.Code);
            foreach(var locale in Locales)if(locale.Identifier.Code==code)return locale;
            return null;
        }
        public void AddLocale(Locale locale){if(!Locales.Contains(locale))Locales.Add(locale);}
        public bool RemoveLocale(Locale locale)=>Locales.Remove(locale);
    }
    [Serializable, Preserve] public sealed class RecoveryLocalTables : ITableProvider
    {
        [SerializeField] private RecoveryLocalizationCatalog catalog;
        public RecoveryLocalTables(RecoveryLocalizationCatalog value){catalog=value;}
        public AsyncOperationHandle<TTable> ProvideTableAsync<TTable>(string tableCollectionName,Locale locale) where TTable:LocalizationTable
        {
            var entry=catalog.Find(locale.Identifier.Code);LocalizationTable table=null;
            if(typeof(TTable)==typeof(StringTable)&&tableCollectionName=="TowTrouble.Text")table=entry?.Strings;
            if(typeof(TTable)==typeof(AssetTable)&&tableCollectionName=="TowTrouble.Fonts")
            {
                table=entry?.Assets;
                if(entry!=null)
                {
                    var body=string.IsNullOrEmpty(entry.FontResource)?catalog.WesternBody:Resources.Load<TMPro.TMP_FontAsset>(entry.FontResource);
                    var display=string.IsNullOrEmpty(entry.FontResource)?catalog.WesternDisplay:body;
                    entry.Assets.GetEntry("Body").SetAssetOverride(body);
                    entry.Assets.GetEntry("Display").SetAssetOverride(display);
                }
            }
            // A missing project table fails locally; it must never fall through to Addressables/web loading.
            return Addressables.ResourceManager.CreateCompletedOperation(table as TTable,table==null?"Local localization table is missing: "+tableCollectionName:null);
        }
    }
}

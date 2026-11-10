using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace TowTrouble
{
    [Serializable] public sealed class RecoveryTextEntry
    {
        public string Key, Source, Context;
        public string[] Aliases=Array.Empty<string>();
    }
    [Serializable] public sealed class RecoveryLocaleData
    {
        public Locale Locale;
        public StringTable Strings;
        public AssetTable Assets;
        public string FontResource;
    }
    public sealed class RecoveryLocalizationCatalog : ScriptableObject
    {
        public RecoveryTextEntry[] Entries=Array.Empty<RecoveryTextEntry>();
        public RecoveryLocaleData[] Languages=Array.Empty<RecoveryLocaleData>();
        public TMP_FontAsset WesternBody,WesternDisplay;
        public RecoveryLocaleData Find(string code)
        {
            code=RecoveryLanguage.Normalize(code);
            foreach(var item in Languages)if(item.Locale.Identifier.Code==code)return item;
            return null;
        }
    }
}

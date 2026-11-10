using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using RTLTMPro;

namespace TowTrouble
{
    public static class RecoveryLocalization
    {
        private sealed class Template
        {
            public RecoveryTextEntry Entry;
            public Regex Match;
            public string Prefix;
        }
        private sealed class Trie
        {
            public readonly Dictionary<char,Trie> Next=new Dictionary<char,Trie>();
            public RecoveryTextEntry Entry;
        }
        private static RecoveryLocalizationCatalog catalog;
        private static readonly Dictionary<string,RecoveryTextEntry> exact=new Dictionary<string,RecoveryTextEntry>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string,string> rendered=new Dictionary<string,string>(StringComparer.Ordinal);
        private static readonly List<Template> templates=new List<Template>();
        private static Trie fragments;
        private static string cacheLocale;
        private static readonly Dictionary<string,string> values=new Dictionary<string,string>();
        private static readonly FastStringBuilder arabic=new FastStringBuilder(2048);
        private static readonly Regex fields=new Regex(@"\{(\d+)\}",RegexOptions.CultureInvariant);
        public static string Code {get{Initialize();return LocalizationSettings.HasSettings&&LocalizationSettings.SelectedLocale!=null?RecoveryLanguage.Normalize(LocalizationSettings.SelectedLocale.Identifier.Code):"en";}}
        public static bool Ready => catalog!=null;
        public static RecoveryLocalizationCatalog Catalog {get{Initialize();return catalog;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){catalog=null;exact.Clear();rendered.Clear();values.Clear();templates.Clear();fragments=null;cacheLocale=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if(catalog!=null)return;
            var settings=Resources.Load<RecoveryLocalizationSettings>("RecoveryLocalizationSettings");
            if(settings!=null)LocalizationSettings.Instance=settings;
            catalog=Resources.Load<RecoveryLocalizationCatalog>("RecoveryLocalizationCatalog");
            if(catalog==null)return; // Editor authoring may still be creating the catalog.
            fragments=new Trie();
            foreach(var entry in catalog.Entries)
            {
                if(string.IsNullOrEmpty(entry.Source))continue;
                exact[entry.Source]=entry;
                foreach(var alias in entry.Aliases)if(!string.IsNullOrEmpty(alias))exact[alias]=entry;
                if(fields.IsMatch(entry.Source))
                {
                    var prefix=entry.Source.Substring(0,entry.Source.IndexOf('{'));
                    string pattern=Regex.Escape(entry.Source);
                    foreach(Match field in fields.Matches(entry.Source))pattern=pattern.Replace(Regex.Escape(field.Value),"(?<p"+field.Groups[1].Value+">.+?)");
                    templates.Add(new Template{Entry=entry,Prefix=prefix,Match=new Regex("^"+pattern+"$",RegexOptions.CultureInvariant|RegexOptions.Singleline)});
                }
                else
                {
                    var node=fragments;
                    foreach(char c in entry.Source){char key=char.ToUpperInvariant(c);if(!node.Next.TryGetValue(key,out var next)){next=new Trie();node.Next.Add(key,next);}node=next;}
                    node.Entry=entry;
                }
            }
            templates.Sort((a,b)=>b.Prefix.Length.CompareTo(a.Prefix.Length));
        }
        public static void ReloadCatalog(){Reset();Initialize();}
        public static void Select(string code)
        {
            Initialize();if(catalog==null)return;
            var locale=catalog.Find(code)?.Locale;if(locale==null)return;
            if(LocalizationSettings.SelectedLocale!=locale)LocalizationSettings.SelectedLocale=locale;
            rendered.Clear();cacheLocale=null;
        }
        public static string Translate(string source)
        {
            if(string.IsNullOrEmpty(source))return source;
            Initialize();if(catalog==null||Code=="en")return source;
            string code=Code;
            if(cacheLocale!=code)
            {
                rendered.Clear();values.Clear();cacheLocale=code;
                var table=LocalizationSettings.StringDatabase.GetTable("TowTrouble.Text");
                if(table!=null)foreach(var entry in catalog.Entries)values[entry.Key]=table.GetEntry(entry.Key)?.LocalizedValue??entry.Source;
            }
            if(rendered.TryGetValue(source,out var cached))return cached;
            string value=TranslateCore(source,0);
            if(rendered.Count>=2048)rendered.Clear();rendered[source]=value;return value;
        }
        private static string Entry(RecoveryTextEntry entry,string source)
        {
            if(!values.TryGetValue(entry.Key,out string value))return source;
            if(string.IsNullOrEmpty(value))return source;
            bool upper=false,lower=false;
            foreach(char c in source){upper|=char.IsUpper(c);lower|=char.IsLower(c);}
            return upper&&!lower?value.ToUpper(LocalizationSettings.SelectedLocale.Identifier.CultureInfo??CultureInfo.InvariantCulture):value;
        }
        private static string TranslateCore(string source,int depth)
        {
            if(depth>3)return source;
            if(exact.TryGetValue(source,out var direct)&&!fields.IsMatch(direct.Source))
            {var value=Entry(direct,source);if(!string.Equals(value,source,StringComparison.OrdinalIgnoreCase))return value;}
            foreach(var template in templates)
            {
                if((!values.TryGetValue(template.Entry.Key,out var templateValue)||string.Equals(templateValue,template.Entry.Source,StringComparison.OrdinalIgnoreCase)))continue;
                if(template.Prefix.Length>0&&!source.StartsWith(template.Prefix,StringComparison.OrdinalIgnoreCase))continue;
                var match=template.Match.Match(source);if(!match.Success)continue;
                int max=-1;foreach(Match field in fields.Matches(template.Entry.Source))max=Math.Max(max,int.Parse(field.Groups[1].Value));
                var arguments=new object[max+1];
                for(int i=0;i<arguments.Length;i++)arguments[i]=TranslateCore(match.Groups["p"+i].Value,depth+1);
                try{return string.Format(Entry(template.Entry,source),arguments);}catch(FormatException){return source;}
            }
            var output=new StringBuilder(source.Length+20);bool replaced=false;
            for(int i=0;i<source.Length;)
            {
                if(source[i]=='<')
                {int end=source.IndexOf('>',i);if(end>=0){output.Append(source,i,end-i+1);i=end+1;continue;}}
                var node=fragments;RecoveryTextEntry found=null;int length=0;
                for(int j=i;j<source.Length&&node.Next.TryGetValue(char.ToUpperInvariant(source[j]),out node);j++)
                {
                    if(node.Entry==null||(!values.TryGetValue(node.Entry.Key,out var fragmentValue)||string.Equals(fragmentValue,node.Entry.Source,StringComparison.OrdinalIgnoreCase)))continue;
                    int end=j+1;
                    bool start=i==0||!char.IsLetterOrDigit(source[i-1])||!char.IsLetterOrDigit(source[i]);
                    bool finish=end==source.Length||!char.IsLetterOrDigit(source[end])||!char.IsLetterOrDigit(source[j]);
                    if(start&&finish){found=node.Entry;length=end-i;}
                }
                if(found!=null){output.Append(Entry(found,source.Substring(i,length)));i+=length;replaced=true;}
                else output.Append(source[i++]);
            }
            return replaced?output.ToString():source;
        }
        public static string Shape(string text)
        {
            if(Code!="ar"||string.IsNullOrEmpty(text)||!TextUtils.IsRTLInput(text))return text;
            arabic.Clear();RTLSupport.FixRTL(text,arabic,false,true,true);arabic.Reverse();return arabic.ToString();
        }
        public static TMP_FontAsset Font(bool display)
        {
            Initialize();if(catalog==null)return null;
            var key=display?"Display":"Body";
            return LocalizationSettings.AssetDatabase.GetLocalizedAsset<TMP_FontAsset>("TowTrouble.Fonts",key);
        }
    }
}

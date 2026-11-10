using System;

namespace TowTrouble
{
    public static class RecoveryLanguage
    {
        // Regional Spanish/Portuguese aliases collapse; Chinese writing systems stay distinct.
        public static readonly string[] Codes = { "en","fr","de","it","es","pt","ro","bg","cs","da","nl","fi","el","hu","id","ja","ko","ms","no","pl","ru","zh-Hans","sv","zh-Hant","th","tr","uk","vi","ar" };
        public static readonly string[] Names = { "English","French","German","Italian","Spanish","Portuguese","Romanian","Bulgarian","Czech","Danish","Dutch","Finnish","Greek","Hungarian","Indonesian","Japanese","Korean","Malay","Norwegian","Polish","Russian","Simplified Chinese","Swedish","Traditional Chinese","Thai","Turkish","Ukrainian","Vietnamese","Arabic" };
        public static string Normalize(string code)
        {
            if(string.IsNullOrWhiteSpace(code))return "en";
            code=code.Trim().Replace('_','-');
            if(code.StartsWith("zh",StringComparison.OrdinalIgnoreCase))
                return code.IndexOf("Hant",StringComparison.OrdinalIgnoreCase)>=0 || code.EndsWith("TW",StringComparison.OrdinalIgnoreCase) || code.EndsWith("HK",StringComparison.OrdinalIgnoreCase) || code.EndsWith("MO",StringComparison.OrdinalIgnoreCase) ? "zh-Hant":"zh-Hans";
            string root=code.Split('-')[0].ToLowerInvariant();if(root=="nb"||root=="nn")root="no";
            for(int i=0;i<Codes.Length;i++)if(string.Equals(Codes[i],root,StringComparison.OrdinalIgnoreCase))return Codes[i];
            return "en";
        }
        public static string Name(string code) => Names[Array.IndexOf(Codes,Normalize(code))];
    }
}

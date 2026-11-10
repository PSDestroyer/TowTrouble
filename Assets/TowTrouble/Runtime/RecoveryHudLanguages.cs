using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private void BuildLanguageSettings()
        {
            var panel=Panel(pageRoot,76,278,1448,480);
            for(int i=0;i<RecoveryLanguage.Codes.Length;i++)
            {
                string code=RecoveryLanguage.Codes[i];bool selected=RecoveryLanguage.Normalize(P.language)==code;
                var button=Button(panel,RecoveryLanguage.Names[i],20+(i%5)*286,18+(i/5)*72,270,()=>
                {
                    P.language=code;session.ApplySettings();P.Save();
                    if(firstSetup)settingsTab="Audio";
                    Show("Settings");
                },selected,58);
                if(selected){Panel(button.transform,12,51,246,3,Amber);Icon(button.transform,RecoveryIcon.Flag,236,18,20,Amber);}
            }
            if(firstSetup)Button(pageRoot,"BACK",76,808,230,()=>{settingsTab="Audio";Show("Settings");});
            else Footer(returnPage);
            if(firstSetup)BuildMenuFooter();
        }
    }
}

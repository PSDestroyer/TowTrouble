using TMPro;
using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private RectTransform persistenceHint;
        private TMP_Text persistenceMessage;
        private void BuildPersistenceHint()
        {
            persistenceHint=Panel(root,0,0,390,36);
            persistenceHint.name="Save status";persistenceHint.anchorMin=persistenceHint.anchorMax=new Vector2(1,1);persistenceHint.pivot=new Vector2(1,1);persistenceHint.anchoredPosition=new Vector2(-22,-12);
            persistenceMessage=Text(persistenceHint,"",12,0,366,36,15,Amber);
            persistenceMessage.alignment=TextAlignmentOptions.MidlineLeft;persistenceHint.gameObject.SetActive(false);
        }
        private void RefreshPersistenceHint()
        {
            if(persistenceHint==null)return;
            var storage=RecoveryProfile.Storage;bool failed=!string.IsNullOrEmpty(storage.Error);
            if(persistenceHint.gameObject.activeSelf!=failed)persistenceHint.gameObject.SetActive(failed);
            if(!failed)return;
            persistenceHint.SetAsLastSibling();
            persistenceMessage.text=storage.CanWrite?"Save pending · retrying":"Save unavailable · original data preserved";
        }
    }
}

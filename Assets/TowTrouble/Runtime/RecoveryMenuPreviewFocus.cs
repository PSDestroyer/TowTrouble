using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace TowTrouble
{
    public sealed class RecoveryMenuPreviewFocus : MonoBehaviour, ISelectHandler
    {
        private Action preview;
        private bool pending;
        public void Configure(Action action){preview=action;}
        public void OnSelect(BaseEventData data){pending=true;}
        private void LateUpdate(){if(pending){pending=false;preview?.Invoke();}}
    }
}

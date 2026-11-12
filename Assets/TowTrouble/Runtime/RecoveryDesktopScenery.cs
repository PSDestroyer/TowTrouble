using UnityEngine;

namespace TowTrouble
{
    /// <summary>Optional desktop art layer; no per-frame work or gameplay state.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-500)]
    public sealed class RecoveryDesktopScenery : MonoBehaviour
    {
        private void Awake()
        {
            if (RecoveryPlatform.UsesSwitchRendering) gameObject.SetActive(false);
        }
    }
}

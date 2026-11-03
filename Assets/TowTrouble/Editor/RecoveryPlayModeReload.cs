using UnityEditor;
using UnityEngine;

namespace TowTrouble.Editor
{
    internal static class RecoveryPlayModeReload
    {
        private static bool reloadLocked;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && !reloadLocked &&
                Object.FindFirstObjectByType<DemoSession>() != null)
            {
                // FishNet creates ServerManager.Objects in Awake; a mid-session reload discards it.
                EditorApplication.LockReloadAssemblies();
                reloadLocked = true;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode && reloadLocked)
            {
                reloadLocked = false;
                EditorApplication.UnlockReloadAssemblies();
            }
        }
    }
}

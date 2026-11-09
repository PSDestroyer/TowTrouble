namespace TowTrouble
{
    /// <summary>Build-target capability policy. A Nintendo-labelled controller does not change it.</summary>
    public static class RecoveryPlatform
    {
#if UNITY_EDITOR
        // Scoped Editor validation only; never included in a player build.
        public static bool PreviewSinglePlayerOnly { get; set; }
#endif
        public static bool MultiplayerAvailable
        {
            get
            {
#if UNITY_SWITCH
                return false;
#elif UNITY_EDITOR
                return !PreviewSinglePlayerOnly;
#else
                return true;
#endif
            }
        }
        public static string MenuPage(string page)
            => !MultiplayerAvailable && (page == "Coop" || page == "Lobby" || page == "TextEntry") ? "Main" : page;
    }
}

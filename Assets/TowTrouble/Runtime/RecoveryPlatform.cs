namespace TowTrouble
{
    /// <summary>Build-target capability policy. A Nintendo-labelled controller does not change it.</summary>
    public static class RecoveryPlatform
    {
#if UNITY_EDITOR
        // Scoped Editor validation only; never included in a player build.
        public static bool PreviewSinglePlayerOnly { get; set; }
#endif
        // Platform selection is independent of controller labels and multiplayer previews.
#if UNITY_SWITCH
        public static string GraphicsQuality => "Switch";
        public static ushort FrameRateLimit => 30;
#else
#if UNITY_STANDALONE || UNITY_EDITOR
        public static string GraphicsQuality => "PC";
#else
        public static string GraphicsQuality => null; // Respect the platform's configured default.
#endif
        public static ushort FrameRateLimit => 60;
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
        public static bool OfflineOnly => !MultiplayerAvailable;

        public static string MenuPage(string page)
            => !MultiplayerAvailable && (page == "Coop" || page == "Lobby" || page == "TextEntry") ? "Main" : page;
    }
}

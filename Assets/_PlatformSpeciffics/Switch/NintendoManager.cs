#if UNITY_SWITCH && !UNITY_EDITOR
using System.IO;
namespace _Assets._PlatformSpeciffics.Switch
{
    public static class NintendoManager
    {
        private static nn.account.Uid userId;
        private static nn.account.UserHandle userHandle;
        private static bool initialized;
        public static void Initialize()
        {
            if (initialized) return;
            nn.account.Account.Initialize();
            if (!nn.account.Account.TryOpenPreselectedUser(ref userHandle))
                throw new IOException("Select a Nintendo user before loading progress.");
            var result = nn.account.Account.GetUserId(ref userId, userHandle);
            if (!result.IsSuccess()) throw new IOException("Unable to load the selected Nintendo user: " + result);
            initialized = true;
        }
        public static nn.account.Uid GetUserID() { Initialize(); return userId; }
    }
}
#endif

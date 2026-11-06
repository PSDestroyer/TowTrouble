#if UNITY_SWITCH && !UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using HalvaStudio.Save;

namespace _Assets._PlatformSpeciffics.Switch
{
    public sealed class NintendoSaveStorage : ISaveStorage
    {
        public string Read(bool backup = false) => NintendoSave.Read(backup);
        public void Write(string document, string previousValidDocument) => NintendoSave.Write(document, previousValidDocument);
    }
    public static class NintendoSave
    {
        // Keep the imported archive/file names so existing console data remains reachable.
        private const string MountName = "MountForklift", FileName = "GameData";
        private static bool mounted;
        public static void Initialize()
        {
            if (mounted) return;
            NintendoManager.Initialize();
            Check(nn.fs.SaveData.Mount(MountName, NintendoManager.GetUserID()), "mount local user save");
            mounted = true;
        }
        private static void Check(nn.Result result, string action)
        { if (!result.IsSuccess()) throw new IOException("Unable to " + action + ": " + result); }
        public static string Read(bool backup = false)
        {
            Initialize(); string path = MountName + ":/" + FileName + (backup ? ".backup" : "");
            var handle = new nn.fs.FileHandle();
            var result = nn.fs.File.Open(ref handle, path, nn.fs.OpenFileMode.Read);
            if (nn.fs.FileSystem.ResultPathNotFound.Includes(result)) return null;
            Check(result, "open save");
            try
            {
                long size = 0; Check(nn.fs.File.GetSize(ref size, handle), "read save size");
                if (size <= 0 || size > SaveStore.MaximumBytes) throw new IOException("Invalid console save size.");
                byte[] bytes = new byte[(int)size]; Check(nn.fs.File.Read(handle, 0, bytes, size), "read save");
                return Encoding.UTF8.GetString(bytes);
            }
            finally { nn.fs.File.Close(handle); }
        }
        public static void Write(string document, string previousValidDocument)
        {
            Initialize();
            UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();
            try
            {
                // Native journaling commits the complete archive after both files are closed.
                if (!string.IsNullOrEmpty(previousValidDocument)) WriteFile(FileName + ".backup", previousValidDocument);
                WriteFile(FileName, document);
                Check(nn.fs.FileSystem.Commit(MountName), "commit save");
            }
            finally { UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection(); }
        }
        private static void WriteFile(string name, string document)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(document);
            if (bytes.Length > SaveStore.MaximumBytes) throw new IOException("Console save exceeds supported size.");
            string path = MountName + ":/" + name; var handle = new nn.fs.FileHandle();
            var result = nn.fs.File.Open(ref handle, path, nn.fs.OpenFileMode.Write);
            if (nn.fs.FileSystem.ResultPathNotFound.Includes(result))
            { Check(nn.fs.File.Create(path, bytes.LongLength), "create save"); result = nn.fs.File.Open(ref handle, path, nn.fs.OpenFileMode.Write); }
            Check(result, "open save for writing");
            try
            {
                Check(nn.fs.File.SetSize(handle, bytes.LongLength), "resize save");
                Check(nn.fs.File.Write(handle, 0, bytes, bytes.LongLength, nn.fs.WriteOption.Flush), "write save");
            }
            finally { nn.fs.File.Close(handle); }
        }
    }
}
#endif

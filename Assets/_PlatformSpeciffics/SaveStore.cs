using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HalvaStudio.Save
{
    // Storage is local to this device/user. Network messages never contain this document.
    public interface ISaveStorage
    {
        string Read(bool backup = false);
        void Write(string document, string previousValidDocument);
    }

    public sealed class FileSaveStorage : ISaveStorage
    {
        public string Path { get; }
        public FileSaveStorage(string path) { Path = System.IO.Path.GetFullPath(path); }
        public string Read(bool backup = false)
        {
            string path = Path + (backup ? ".backup" : "");
            if (!File.Exists(path)) return null;
            if (new FileInfo(path).Length > SaveStore.MaximumBytes) throw new IOException("Save file exceeds the supported size.");
            return File.ReadAllText(path, Encoding.UTF8);
        }
        public void Write(string document, string previousValidDocument)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            if (!string.IsNullOrEmpty(previousValidDocument)) AtomicWrite(Path + ".backup", previousValidDocument);
            AtomicWrite(Path, document);
        }
        private static void AtomicWrite(string path, string value)
        {
            string temp = path + ".tmp";
            try
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(value);
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }

    public sealed class MemorySaveStorage : ISaveStorage
    {
        public string Document, Backup;
        public string Read(bool backup = false) => backup ? Backup : Document;
        public void Write(string document, string previousValidDocument)
        { if (previousValidDocument != null) Backup = previousValidDocument; Document = document; }
    }

    public sealed class SaveStore
    {
        public const int MaximumBytes = 4 * 1024 * 1024;
        private readonly ISaveStorage storage;
        private string lastValidDocument;
        public SaveManager.SaveData Data { get; private set; }
        public bool CanWrite { get; private set; } = true;
        public bool Dirty { get; private set; }
        public string Error { get; private set; }
        public bool RecoveredBackup { get; private set; }
        public SaveStore(ISaveStorage storage)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Load();
        }
        private void Load()
        {
            bool missing = false;
            try
            {
                string raw = storage.Read(); missing = raw == null;
                if (!missing) { Data = Decode(raw); lastValidDocument = raw; return; }
            }
            catch (Exception e) when (Expected(e)) { Error = e.Message; }
            try
            {
                string backup = storage.Read(true);
                if (!string.IsNullOrEmpty(backup))
                { Data = Decode(backup); lastValidDocument = backup; RecoveredBackup = true; Dirty = true; return; }
            }
            catch (Exception e) when (Expected(e)) { Error = e.Message; missing = false; }
            Data = new SaveManager.SaveData();
            // Never overwrite an unreadable or newer save with defaults.
            CanWrite = missing && Error == null;
        }
        public string GetString(string key, string fallback = "") => Data.values.TryGetValue(key, out var value) ? value : fallback;
        public bool Contains(string key) => Data.values.ContainsKey(key);
        public void SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("A save key is required.");
            value = value ?? "";
            if (GetString(key, null) == value) return;
            Data.values[key] = value; Dirty = true;
        }
        public void Delete(string key) { if (Data.values.Remove(key)) Dirty = true; }
        public void SuspendWrites(string reason) { CanWrite=false;Error=reason; }
        public void MarkDirty() { Dirty = true; }
        public bool Flush()
        {
            if (!CanWrite) return false;
            if (!Dirty) return true;
            try
            {
                string raw = Encode(Data);
                if (Encoding.UTF8.GetByteCount(raw) > MaximumBytes) throw new IOException("Save data exceeds the supported size.");
                storage.Write(raw, lastValidDocument); lastValidDocument = raw; Dirty = false; Error = null; return true;
            }
            catch (Exception e) when (Expected(e)) { Error = e.Message; return false; }
        }
        private static bool Expected(Exception e) => e is IOException || e is UnauthorizedAccessException || e is JsonException || e is ArgumentException || e is NotSupportedException || e is System.Security.SecurityException;
        private sealed class Document { public int saveFormat = 1; public string payload, checksum; }
        private static string Digest(string value)
        { using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value))); }
        public static string Encode(SaveManager.SaveData data)
        { string payload = JsonConvert.SerializeObject(data); return JsonConvert.SerializeObject(new Document { payload = payload, checksum = Digest(payload) }); }
        public static SaveManager.SaveData Decode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || Encoding.UTF8.GetByteCount(raw) > MaximumBytes) throw new ArgumentException("Invalid save document.");
            var root = JObject.Parse(raw);
            SaveManager.SaveData data;
            if (root["saveFormat"] != null)
            {
                var document = root.ToObject<Document>();
                if (document.saveFormat != 1 || string.IsNullOrEmpty(document.payload) || document.checksum != Digest(document.payload)) throw new ArgumentException("Unsupported or damaged save document.");
                data = JsonConvert.DeserializeObject<SaveManager.SaveData>(document.payload);
            }
            else
            {
                // Preserve data from the imported manager's original save.json format.
                if (root["values"] == null && root["boughtCars"] == null && root["money"] == null) throw new ArgumentException("Unrecognized save format.");
                data = root.ToObject<SaveManager.SaveData>();
            }
            if (data == null) throw new ArgumentException("Empty save data.");
            if (data.values == null) data.values = new Dictionary<string, string>();
            if (data.boughtCars == null) data.boughtCars = new Dictionary<string, bool>();
            return data;
        }
    }
}

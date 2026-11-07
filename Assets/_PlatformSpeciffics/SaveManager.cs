using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HalvaStudio.Save
{
    // The imported SaveManager is the single persistence entry point, on desktop and Switch.
    public sealed class SaveManager : MonoBehaviour
    {
        private static SaveManager instance;
        private static SaveStore store;
        private static readonly Dictionary<string, SaveStore> isolated = new Dictionary<string, SaveStore>();
        private double nextWrite;
        private string reportedError;
        public static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
        public SaveData saveData => Store.Data;
        public static SaveStore Store
        {
            get
            {
                if (store == null)
                {
#if UNITY_SWITCH && !UNITY_EDITOR
                    store = new SaveStore(new _Assets._PlatformSpeciffics.Switch.NintendoSaveStorage());
#else
                    store = new SaveStore(new FileSaveStorage(SavePath));
#endif
                }
                if (Application.isPlaying && instance == null)
                    new GameObject("SaveManager").AddComponent<SaveManager>();
                return store;
            }
        }
#if UNITY_EDITOR
        // Editor deletion must invalidate the loaded store as well as removing its files.
        public static void InvalidateEditorCache()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before clearing the save cache.");
            store = null;
        }
#endif
        // Validation never touches the user's save file, including its backup.
        public static SaveStore Isolated(string name)
        { if (!isolated.TryGetValue(name, out var value)) isolated[name] = value = new SaveStore(new MemorySaveStorage()); return value; }
        public static void ReleaseIsolated(string name) { isolated.Remove(name); }
        public static SaveManager Instance
        { get { var value = Store; return instance; } }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this; DontDestroyOnLoad(gameObject); var value = Store;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; store = null; isolated.Clear(); }
        public void AwakeInit() { var value = Store; }
        public void Save(bool forceSave = false) { Store.MarkDirty(); RequestSave(forceSave); }
        public static bool RequestSave(bool force = false)
        {
            var value = Store;
            if (!Application.isPlaying || force) return Flush();
#if UNITY_SWITCH && !UNITY_EDITOR
            // Coalesce repeated purchases/settings changes without discarding pending data.
            if (instance != null && Time.realtimeSinceStartupAsDouble < instance.nextWrite) return value.CanWrite;
#endif
            return Flush();
        }
        private static bool Flush()
        {
            if (store == null) return true;
            bool success = store.Flush();
            if (instance != null)
            {
                instance.nextWrite = Time.realtimeSinceStartupAsDouble + 5;
                if (!success && instance.reportedError != store.Error)
                { instance.reportedError = store.Error; Debug.LogWarning("[Tow Trouble] Save pending: " + store.Error); }
                if (success) instance.reportedError = null;
            }
            return success;
        }
        private void Update()
        { if (store != null && store.Dirty && Time.realtimeSinceStartupAsDouble >= nextWrite) Flush(); }
        private void OnApplicationPause(bool paused) { if (paused) Flush(); }
        private void OnApplicationFocus(bool focused) { if (!focused) Flush(); }
        private void OnApplicationQuit() { Flush(); }
        private void OnDestroy() { if (instance == this) { Flush(); instance = null; } }
        public void SaveCar(string name, bool owned) { saveData.boughtCars[name] = owned; Save(); }
        public bool IsCarBought(string name) => saveData.boughtCars.TryGetValue(name, out var owned) && owned;
        [Serializable] public sealed class SaveData
        {
            public Dictionary<string, string> values = new Dictionary<string, string>();
            public Dictionary<string, bool> boughtCars = new Dictionary<string, bool>();
            // Preserve unused fields in previously imported saves without coupling the game to them.
            [JsonExtensionData] public IDictionary<string, JToken> extra;
        }
    }
}

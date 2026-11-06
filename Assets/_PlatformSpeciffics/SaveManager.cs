using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using _Assets._PlatformSpeciffics.Switch;
using System.IO;

namespace HalvaStudio.Save
{
    public class SaveManager : Singleton<SaveManager>
    {
        public SaveData saveData;
        [SerializeField] private SaveData defaultSaveData;

        public override void AwakeInit()
        {
            Debug.Log("Initializing SaveManager...");
            Load();
        }

        private void Load()
        {
            if (saveData == null)
            {
                saveData = new SaveData();
            }

#if UNITY_EDITOR
            saveData = (SaveData)LoadEditor(typeof(SaveData));
#else
            saveData = (SaveData)LoadSwitch(typeof(SaveData));
#endif
        }

        public void Save(bool forceSave = false)
        {
            Debug.Log("Saving data...");
#if UNITY_EDITOR
            SaveEditor(saveData);
#else
            SaveSwitch(saveData, forceSave);
#endif
        }

        #region Editor

        public void SaveEditor(object saveObject)
        {
            try
            {
                string jsonFile = JsonConvert.SerializeObject(saveObject);
                string savePath = GetSavePath();

                File.WriteAllText(savePath, jsonFile);

                Debug.Log("Save completed.");
            }
            catch (Exception e)
            {
                Debug.LogError("Error saving data: " + e.Message);
            }
        }

        private string GetSavePath()
        {
            string savePath = Path.Combine(Application.persistentDataPath, "save.json");
            Debug.Log("Save Path: " + savePath);
            return savePath;
        }

        public object LoadEditor(System.Type objectType)
        {
            string savePath = GetSavePath();
            object returnObject = null;

            if (File.Exists(savePath))
            {
                try
                {
                    string jsonFile = File.ReadAllText(savePath);
                    returnObject = JsonConvert.DeserializeObject(jsonFile, objectType);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error loading data: " + e.Message);
                    returnObject = defaultSaveData ?? new SaveData();
                }
            }
            else
            {
                Debug.LogError("Save file not found. Using default data.");
                returnObject = defaultSaveData ?? new SaveData();
            }

            return returnObject;
        }

        #endregion
#if UNITY_SWITCH && !UNITY_EDITOR
        #region Switch

        public void SaveSwitch(object saveObject, bool forceSave = false)
        {
            try
            {
                string jsonFile = JsonConvert.SerializeObject(saveObject);
                NintendoSave.Save(jsonFile, forceSave);
                Debug.Log("Save completed.");
            }
            catch (Exception e)
            {
                Debug.LogError("Error saving data: " + e.Message);
            }
        }

        public object LoadSwitch(System.Type objectType)
        {
            bool successful = false;
            string jsonFile = NintendoSave.Load(ref successful);

            if (jsonFile == null)
            {
                Debug.LogError("Save file not found. Using default data.");
                return defaultSaveData ?? new SaveData();
            }

            try
            {
                return JsonConvert.DeserializeObject(jsonFile, objectType);
            }
            catch (Exception e)
            {
                Debug.LogError("Error deserializing data: " + e.Message);
                return defaultSaveData ?? new SaveData();
            }
        }

        #endregion
#endif
        [System.Serializable]
        public class SaveData
        {
            [Header("Player Data")]
            public int money;
            public int currentWeaponID;

            [Header("Settings")]
            public int lookSensitivity;
            public string difficulty;
            public float soundLevel;
            public float musicLevel;
            public bool vibrationsState;
            public bool indicatorState;

            public int currentLevel;
            public int GiftCount;
            // public List<int> giftColected;
            // public List<int> missionStars;
            public float averagRating;
            public int[] rating;
    
            //Player Data
            public bool canAirHit;
            public bool canSwim;
            public bool canDash;
            public bool canParachute;
            public bool canJetPack;
            
            
            
            public Dictionary<string, bool> boughtCars = new Dictionary<string, bool>(); // Initialize dictionary
        }

        #region Custom Methods

        public void SaveCar(string carName, bool isBought)
        {
            if (saveData.boughtCars == null)
            {
                saveData.boughtCars = new Dictionary<string, bool>();
            }

            saveData.boughtCars[carName] = isBought;
        }

        public bool IsCarBought(string carName)
        {
            return saveData.boughtCars.ContainsKey(carName) && saveData.boughtCars[carName];
        }

        #endregion
    }
}

using System;
using HalvaStudio.Save;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    // Versioned local-user profile. No network authority or scene references are persisted.
    [Serializable] public sealed class RecoveryProfile
    {
        public int version=4, completedMask, medalMask, noResetMask, noSnapMask, lastContract, vehicle, paint;
        public int ownedVehicles,starterVehicle=-1;
        public bool starterChosen;
        public bool onboarded, reducedMotion, invertCamera, vibration=true, tutorial=true, colorHelp=true;
        public float master=.8f, effects=.8f, ui=.7f, brightness=1f, textScale=1f, cameraSensitivity=1f;
        public int quality=1;
        public int paintSelectionRevision;
        public bool tutorialOffered, tutorialCompleted;
        public int displayWidth,displayHeight,displayMode=-1;
        public int controllerFamily; // 0 automatic; 1 Xbox, 2 PlayStation, 3 Nintendo
        [NonSerialized] public bool SuppressSaving;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static string ValidationStorageKey;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetValidationScope() { ValidationStorageKey=null; }
#endif
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ValidationBootstrap()
        {
            string isolated = UnityEditor.SessionState.GetString("TowTrouble.ValidationProfile", "");
            if (!string.IsNullOrEmpty(isolated)) ValidationStorageKey = isolated;
        }
#endif
        private static string StorageKey { get {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!string.IsNullOrEmpty(ValidationStorageKey)) return ValidationStorageKey;
#endif
            return Key; } }
        public string forwardKey="W",reverseKey="S",leftKey="A",rightKey="D",brakeKey="Space";
        public string language="en", name="Driver", hookKey="F", resetKey="R", reelKey="Q", extendKey="E";
        public int cableControlsRevision;
        public int credits,lastPayout;
        public List<string> ownedParts=new List<string>();
        public List<string> paidRuns=new List<string>();
        public RecoveryBuild[] builds;
        public List<GaragePreset> presets=new List<GaragePreset>();
        public string boardKey="Tab",garageKey="G";
        public bool Owns(string id)=>ownedParts!=null&&ownedParts.Contains(id);
        public bool OwnsVehicle(int model)=>model>=0&&model<4&&(ownedVehicles&(1<<model))!=0;
        public RecoveryBuild Build(int model)
        {EnsureGarage();return builds[Mathf.Clamp(model,0,3)];}
        public void EnsureGarage()
        {
            if(ownedParts==null)ownedParts=new List<string>();if(paidRuns==null)paidRuns=new List<string>();if(presets==null)presets=new List<GaragePreset>();
            if(builds==null||builds.Length!=4){var old=builds;builds=new RecoveryBuild[4];if(old!=null)Array.Copy(old,builds,Mathf.Min(old.Length,4));}
            for(int i=0;i<4;i++){if(builds[i]==null)builds[i]=new RecoveryBuild();builds[i].Model=i;}
            credits=Mathf.Clamp(credits,0,1000000000);
        }
        public static RecoveryProfile Parse(string saved)
        {
            var p=string.IsNullOrEmpty(saved)?new RecoveryProfile():JsonUtility.FromJson<RecoveryProfile>(saved);
            if(p==null)return new RecoveryProfile();
            if(p.version<1||p.version>4)throw new ArgumentException("Unsupported profile version");
            p.EnsureGarage();
            if(p.version==1)
            {
                p.credits=0;for(int i=0;i<3;i++)if(p.Completed(i))p.credits+=RecoveryEconomy.LegacyPayout(RecoveryContract.Get(i).Reward,(p.noResetMask&(1<<i))!=0,(p.noSnapMask&(1<<i))!=0);
                for(int i=1;i<4;i++)p.ownedParts.Add("paint_"+i);
                if(p.paint>0)p.builds[Mathf.Clamp(p.vehicle,0,3)].Items=new[]{"paint_"+p.paint};
                p.version=2;
            }
            if(p.version<3)
            {
                // Old profiles had no vehicle receipts. Keep the active ride,
                // but mission completion no longer grants other models for free.
                p.ownedVehicles=1|(1<<Mathf.Clamp(p.vehicle,0,3));
                foreach(var build in p.builds)
                    if(Array.Exists(build.Items??Array.Empty<string>(),id=>!string.IsNullOrEmpty(id)&&p.Owns(id)&&!id.StartsWith("stock_",StringComparison.Ordinal)&&!id.StartsWith("paint_",StringComparison.Ordinal)))p.ownedVehicles|=1<<build.Model;
                foreach(var preset in p.presets)if(preset?.Build!=null&&preset.Build.Model>=0&&preset.Build.Model<4)p.ownedVehicles|=1<<preset.Build.Model;
                p.version=3;
            }
            if(p.version<4)
            {
                bool played=p.completedMask!=0||p.credits>0||p.vehicle!=0||(p.ownedVehicles&~1)!=0||p.presets.Count>0||p.ownedParts.Exists(id=>!string.IsNullOrEmpty(id)&&!id.StartsWith("paint_",StringComparison.Ordinal)&&!id.StartsWith("stock_",StringComparison.Ordinal));
                p.starterChosen=played;p.starterVehicle=played?Mathf.Clamp(p.vehicle,0,3):-1;
                if(played)p.ownedVehicles|=1<<p.starterVehicle;else p.ownedVehicles=0;
                p.version=4;
            }
            // Upgrade the old default pair once; preserve custom bindings and later rebinds.
            if (p.cableControlsRevision == 0)
            {
                if (string.Equals(p.hookKey, "E", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.extendKey, "F", StringComparison.OrdinalIgnoreCase))
                { p.hookKey = "F"; p.extendKey = "E"; }
                p.cableControlsRevision = 1;
            }
            if(p.paintSelectionRevision==0)
            {
                // The old starter stored its color globally while normalization inserted Stock paint.
                // Preserve an explicit paid finish; otherwise recover that selected starter color once.
                if(p.starterChosen&&p.paint>0&&p.paint<=3)
                {
                    var build=p.Build(p.vehicle);bool explicitColor=Array.Exists(build.Items??Array.Empty<string>(),id=>id=="paint_1"||id=="paint_2"||id=="paint_3");
                    if(!explicitColor){var ids=new List<string>(build.Items??Array.Empty<string>());ids.RemoveAll(id=>id=="stock_Paint");string id="paint_"+p.paint;ids.Add(id);build.Items=ids.ToArray();if(!p.Owns(id))p.ownedParts.Add(id);}
                }
                p.paintSelectionRevision=1;
            }
            p.language = string.Equals(p.language, "ro", StringComparison.OrdinalIgnoreCase) ? "ro" : "en";
            p.EnsureGarage();p.Clamp();return p;
        }
        public static SaveStore Storage { get {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!string.IsNullOrEmpty(ValidationStorageKey)) return SaveManager.Isolated(ValidationStorageKey);
#endif
            return SaveManager.Store;
        } }
        private const string Key="TowTrouble.Profile.v1";
        public static RecoveryProfile Load()
        {
            string saved=Storage.GetString(StorageKey,"");
            try {return Parse(saved);}
            catch(ArgumentException){Storage.SuspendWrites("The career could not be read. Original save preserved.");Debug.LogWarning("[Tow Trouble] Career could not be read; original save preserved. Saving is disabled until the file is recovered.");return new RecoveryProfile();}
        }
        public void Save()
        {
            if (SuppressSaving) return;
#if UNITY_EDITOR
            // Preview canvases can rebuild sliders in Edit Mode. They must never save a career.
            if (!Application.isPlaying && string.IsNullOrEmpty(ValidationStorageKey)) return;
#endif
            cableControlsRevision = 1;
            EnsureGarage();Clamp();var previous=Storage.GetString(StorageKey,"");if(!string.IsNullOrEmpty(previous))Storage.SetString(StorageKey+".previous",previous);
            Storage.SetString(StorageKey,JsonUtility.ToJson(this));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!string.IsNullOrEmpty(ValidationStorageKey)) { Storage.Flush(); return; }
#endif
            SaveManager.RequestSave();
        }
        private void Clamp(){controllerFamily=Mathf.Clamp(controllerFamily,0,3);ownedVehicles&=15;paint=Mathf.Clamp(paint,0,3);vehicle=Mathf.Clamp(vehicle,0,3);if(starterChosen){starterVehicle=Mathf.Clamp(starterVehicle,0,3);ownedVehicles|=1<<starterVehicle;if(!OwnsVehicle(vehicle))vehicle=starterVehicle;}else{starterVehicle=-1;ownedVehicles=0;}lastContract=Mathf.Clamp(lastContract,0,2);master=Mathf.Clamp01(master);effects=Mathf.Clamp01(effects);ui=Mathf.Clamp01(ui);brightness=Mathf.Clamp(brightness,.7f,1.4f);textScale=Mathf.Clamp(textScale,.85f,1.3f);cameraSensitivity=Mathf.Clamp(cameraSensitivity,.3f,2f);}
        public bool Completed(int id)=>(completedMask&(1<<id))!=0;
        public bool Unlocked(int id)=>id==0||Completed(id-1);
        public bool Clean(int id)=>(medalMask&(1<<id))!=0;
        public void RecordFinish(int id,bool noResets,bool noSnaps){completedMask|=1<<id;if(noResets)noResetMask|=1<<id;if(noSnaps)noSnapMask|=1<<id;if(noResets&&noSnaps)medalMask|=1<<id;if(id<3)lastContract=Mathf.Min(2,id+1);}
        public void Finish(int id,bool noResets,bool noSnaps){RecordFinish(id,noResets,noSnaps);Save();}
        public Key ReadKey(string binding,Key fallback)=>Enum.TryParse(binding,true,out Key parsed)&&Enum.IsDefined(typeof(Key),parsed)&&parsed!=UnityEngine.InputSystem.Key.None?parsed:fallback;
        public string L(string ro,string en)=>language=="en"?en:ro;
    }
}

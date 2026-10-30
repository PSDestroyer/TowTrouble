using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    // Small, versioned desktop profile. No network authority or scene references are persisted.
    [Serializable] public sealed class RecoveryProfile
    {
        public int version=1, completedMask, medalMask, noResetMask, noSnapMask, lastContract, vehicle, paint;
        public bool onboarded, reducedMotion, invertCamera, vibration=true, tutorial=true, colorHelp=true;
        public float master=.8f, effects=.8f, ui=.7f, brightness=1f, textScale=1f, cameraSensitivity=1f;
        public int quality=1;
        public string forwardKey="W",reverseKey="S",leftKey="A",rightKey="D",brakeKey="Space";
        public string language="ro", name="Driver", hookKey="E", resetKey="R", reelKey="Q", extendKey="F";
        private const string Key="TowTrouble.Profile.v1";
        public static RecoveryProfile Load()
        {
            string saved=PlayerPrefs.GetString(Key,"");
            try { var value=JsonUtility.FromJson<RecoveryProfile>(saved);if(value!=null&&value.version==1){value.Clamp();return value;}if(value!=null&&value.version!=1){PlayerPrefs.SetString(Key+".unsupportedBackup",saved);Debug.LogWarning("[TowTrouble] Versiune de profil necunoscută; copia originală a fost păstrată.");} }
            catch(ArgumentException){ Debug.LogWarning("[TowTrouble] Profilul nu a putut fi citit; sunt folosite valorile implicite."); }
            return new RecoveryProfile();
        }
        public void Save(){Clamp();PlayerPrefs.SetString(Key,JsonUtility.ToJson(this));PlayerPrefs.Save();}
        private void Clamp(){paint=Mathf.Clamp(paint,0,3);vehicle=Mathf.Clamp(vehicle,0,3);lastContract=Mathf.Clamp(lastContract,0,2);master=Mathf.Clamp01(master);effects=Mathf.Clamp01(effects);ui=Mathf.Clamp01(ui);brightness=Mathf.Clamp(brightness,.7f,1.4f);textScale=Mathf.Clamp(textScale,.85f,1.3f);cameraSensitivity=Mathf.Clamp(cameraSensitivity,.3f,2f);}
        public bool Completed(int id)=>(completedMask&(1<<id))!=0;
        public bool Unlocked(int id)=>id==0||Completed(id-1);
        public bool Clean(int id)=>(medalMask&(1<<id))!=0;
        public void Finish(int id,bool noResets,bool noSnaps){completedMask|=1<<id;if(noResets)noResetMask|=1<<id;if(noSnaps)noSnapMask|=1<<id;if(noResets&&noSnaps)medalMask|=1<<id;lastContract=Mathf.Min(2,id+1);Save();}
        public Key ReadKey(string binding,Key fallback)=>Enum.TryParse(binding,true,out Key parsed)&&Enum.IsDefined(typeof(Key),parsed)&&parsed!=UnityEngine.InputSystem.Key.None?parsed:fallback;
        public string L(string ro,string en)=>language=="en"?en:ro;
    }
}

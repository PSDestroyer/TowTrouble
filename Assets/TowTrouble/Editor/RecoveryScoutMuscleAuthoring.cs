using System;
using UnityEditor;
using UnityEngine;
namespace TowTrouble.Editor
{
    public static class RecoveryScoutMuscleAuthoring
    {
        [MenuItem("Tow Trouble/Audio/Configure Scout Muscle")]
        public static void Configure()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Configure Scout audio in Edit Mode.");
            const string root="Assets/TowTrouble/Art/Audio/Library/Vehicles/Scout Muscle/";
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AudioClip Clip(string label)
            {
                string path=root+"Scout Muscle "+label+".wav";
                var importer=AssetImporter.GetAtPath(path) as AudioImporter;
                if(importer==null)throw new InvalidOperationException("Missing prepared Scout clip: "+path);
                importer.forceToMono=true;importer.loadInBackground=false;
                var settings=importer.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.preloadAudioData=true;
                settings.compressionFormat=AudioCompressionFormat.ADPCM;
                settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=22050;
                importer.defaultSampleSettings=settings;importer.SetOverrideSampleSettings("Switch",settings);importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            var bank=AssetDatabase.LoadAssetAtPath<RecoveryAudioBank>("Assets/TowTrouble/Art/Audio/Resources/RecoveryAudioBank.asset");
            if(bank==null)throw new InvalidOperationException("Missing recovery audio bank.");
            bank.ScoutMuscleIdle=Clip("Idle");bank.ScoutMuscleLow=Clip("Low");bank.ScoutMuscleHigh=Clip("High");
            EditorUtility.SetDirty(bank);AssetDatabase.SaveAssetIfDirty(bank);
        }
    }
}

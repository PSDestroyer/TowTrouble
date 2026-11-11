using System;
using UnityEditor;
using UnityEngine;
namespace TowTrouble.Editor
{
    public static class RecoveryIgnitionAudioAuthoring
    {
        [MenuItem("Tow Trouble/Audio/Configure engine ignition")]
        public static void Configure()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Configure fleet audio in Edit Mode.");
            const string root="Assets/TowTrouble/Art/Audio/Library/Vehicles/Ignition/";
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AudioClip Clip(string vehicle,string label)
            {
                string path=root+vehicle+" "+label+".wav";
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
            var profiles=new RecoveryIgnitionSounds[4];
            for(int model=0;model<4;model++)
            {
                string vehicle=RecoveryVehicle.ModelNames[model];
                var previous=bank.IgnitionFor(model);
                profiles[model]=new RecoveryIgnitionSounds{Model=model,Start=Clip(vehicle,"Start"),Stop=Clip(vehicle,"Stop"),StartGain=previous!=null?previous.StartGain:.16f,StopGain=previous!=null?previous.StopGain:.14f};
            }
            bank.Ignitions=profiles;
            EditorUtility.SetDirty(bank);AssetDatabase.SaveAssetIfDirty(bank);
        }
    }
}

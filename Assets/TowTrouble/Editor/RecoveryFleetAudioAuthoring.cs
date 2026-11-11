using System;
using UnityEditor;
using UnityEngine;
namespace TowTrouble.Editor
{
    public static class RecoveryFleetAudioAuthoring
    {
        [MenuItem("Tow Trouble/Audio/Configure fleet engines")]
        public static void Configure()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Configure fleet audio in Edit Mode.");
            const string root="Assets/TowTrouble/Art/Audio/Library/Vehicles/Fleet Engines/";
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
            var profiles=new RecoveryVehicleEngineSounds[3];
            for(int model=1;model<=3;model++)
            {
                string vehicle=RecoveryVehicle.ModelNames[model];
                var previous=bank.EngineFor(model);
                profiles[model-1]=new RecoveryVehicleEngineSounds{Model=model,Idle=Clip(vehicle,"Idle"),Low=Clip(vehicle,"Low"),High=Clip(vehicle,"High"),Gain=previous!=null?previous.Gain:.68f};
            }
            bank.VehicleEngines=profiles;
            EditorUtility.SetDirty(bank);AssetDatabase.SaveAssetIfDirty(bank);
        }
    }
}

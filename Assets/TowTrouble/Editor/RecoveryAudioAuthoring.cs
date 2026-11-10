using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace TowTrouble.Editor
{
    public static class RecoveryAudioAuthoring
    {
        private const string Root="Assets/TowTrouble/Art/Audio/";
        private const string Library=Root+"Library/";
        private const string BankPath=Root+"Resources/RecoveryAudioBank.asset";
        [MenuItem("Tow Trouble/Audio/Route scene audio sources")]
        public static void RouteSceneSources()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Route scene audio in Edit Mode.");
            var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
            var bank=AssetDatabase.LoadAssetAtPath<RecoveryAudioBank>(BankPath);
            if(world==null||bank==null)throw new InvalidOperationException("Open the recovery scene and configure its audio bank first.");
            var scene=world.gameObject.scene;
            if(scene.isDirty)throw new InvalidOperationException("Save your existing scene changes before routing audio.");
            foreach(var car in world.Drivers.Concat(new[]{world.Casualty}))
            {
                var source=car.GetComponent<AudioSource>();if(source==null)continue;
                Undo.RecordObject(source,"Route legacy vehicle audio");
                source.Stop();source.enabled=false;source.playOnAwake=false;source.dopplerLevel=0;
                source.clip=bank.EngineIdle;source.outputAudioMixerGroup=bank.Vehicles;
                EditorUtility.SetDirty(source);
            }
            Undo.FlushUndoRecordObjects();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        [MenuItem("Tow Trouble/Audio/Refresh expanded soundtrack")]
        public static void RefreshSoundtrack()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before configuring music.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string title in new[]{"Wooden Inn","One Step at a Time","First Light Particles","Deep Blue Ambient Piano"})
            {
                var importer=AssetImporter.GetAtPath(Library+"Music/"+title+".wav") as AudioImporter;
                if(importer==null)throw new InvalidOperationException("Missing music: "+title);
                importer.forceToMono=false;importer.loadInBackground=true;
                var settings=importer.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.Streaming;settings.preloadAudioData=false;
                settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.65f;
                settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=44100;
                importer.defaultSampleSettings=settings;importer.SetOverrideSampleSettings("Switch",settings);importer.SaveAndReimport();
            }
            var bank=AssetDatabase.LoadAssetAtPath<RecoveryAudioBank>(BankPath);
            if(bank==null)throw new InvalidOperationException("Audio bank is missing.");
            ConfigureSoundtrack(bank);EditorUtility.SetDirty(bank);AssetDatabase.SaveAssetIfDirty(bank);
        }
        private static void ConfigureSoundtrack(RecoveryAudioBank bank)
        {
            AudioClip Music(string title)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Library+"Music/"+title+".wav")??throw new InvalidOperationException("Missing music: "+title);
            bank.MenuMusic=Music("Revelation");
            bank.ExploreMusic=new[]{Music("Sunset Plains"),bank.MenuMusic,Music("Wooden Inn"),Music("One Step at a Time"),Music("First Light Particles"),Music("Deep Blue Ambient Piano")};
            bank.MusicGain=.36f;bank.EngineGain=.85f;bank.MusicCrossfade=4;bank.MenuMusicGap=3;bank.ExploreMusicGap=16;
        }
        [MenuItem("Tow Trouble/Audio/Configure audio library")]
        public static void Configure()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Configure audio in Edit Mode.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>(Root+"Mixing/TowTrouble.mixer");
            if(mixer==null)throw new InvalidOperationException("Create TowTrouble mixer in the Audio Mixer window first.");
            var groups=mixer.FindMatchingGroups("");
            AudioMixerGroup Group(string name)=>groups.Single(x=>x.name==name);
            foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{Library.TrimEnd('/')}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(AudioImporter)AssetImporter.GetAtPath(path);
                bool music=path.Contains("/Music/"),ambient=path.Contains("/Ambience/");
                importer.forceToMono=!music;importer.ambisonic=false;importer.loadInBackground=music||ambient;
                var settings=importer.defaultSampleSettings;settings.preloadAudioData=!music&&!ambient;
                settings.loadType=music||ambient?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=music||ambient?AudioCompressionFormat.Vorbis:AudioCompressionFormat.ADPCM;
                settings.quality=music?.65f:.5f;settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=music?44100u:ambient?32000u:22050u;
                importer.defaultSampleSettings=settings;
                // Explicit target policy; real Switch import/build remains a devkit validation step.
                importer.SetOverrideSampleSettings("Switch",settings);
                importer.SaveAndReimport();
            }
            if(!AssetDatabase.IsValidFolder(Root+"Resources"))AssetDatabase.CreateFolder(Root.TrimEnd('/'),"Resources");
            var bank=AssetDatabase.LoadAssetAtPath<RecoveryAudioBank>(BankPath);
            if(bank==null){bank=ScriptableObject.CreateInstance<RecoveryAudioBank>();AssetDatabase.CreateAsset(bank,BankPath);}
            Undo.RecordObject(bank,"Configure Tow Trouble audio");
            bank.Vehicles=Group("Vehicles");bank.Recovery=Group("Recovery");bank.UI=Group("UI");bank.Ambience=Group("Ambience");bank.Music=Group("Music");
            AudioClip Clip(string file)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Library+file)??throw new InvalidOperationException("Missing audio: "+file);
            bank.EngineIdle=Clip("Vehicles/Engine Idle.wav");bank.EngineDrive=Clip("Vehicles/Engine Drive.wav");bank.EngineLoad=Clip("Recovery/Motor loop.wav");bank.WinchMotor=bank.EngineLoad;
            bank.Wind=Clip("Ambience/Wind_1.wav");bank.MountainWind=Clip("Ambience/Wind_2.wav");bank.Birds=Clip("Ambience/Birds_1.wav");bank.Insects=Clip("Ambience/Forest_Insects.wav");bank.Water=Clip("Ambience/Water_1.wav");bank.Garage=Clip("Foley/impactMetal_light_001.ogg");
            ConfigureSoundtrack(bank);
            var sounds=new List<RecoverySoundSet>();
            void Add(RecoverySound sound,RecoveryAudioBus bus,float gain,int priority,float cooldown,params string[] clips)
            {sounds.Add(new RecoverySoundSet{Sound=sound,Bus=bus,Gain=gain,Priority=priority,Cooldown=cooldown,Clips=clips.Select(Clip).ToArray()});}
            const RecoveryAudioBus ui=RecoveryAudioBus.UI,rec=RecoveryAudioBus.Recovery;
            Add(RecoverySound.Navigate,ui,.16f,100,.075f,"UI/select_001.ogg");
            Add(RecoverySound.Confirm,ui,.28f,80,.09f,"UI/click_001.ogg");
            Add(RecoverySound.Back,ui,.25f,80,.12f,"UI/back_001.ogg");
            Add(RecoverySound.Open,ui,.22f,110,.16f,"UI/open_001.ogg");
            Add(RecoverySound.Close,ui,.22f,110,.16f,"UI/close_001.ogg");
            Add(RecoverySound.Reject,ui,.26f,40,.3f,"UI/error_001.ogg");
            Add(RecoverySound.Fit,ui,.35f,60,.2f,"Recovery/metalClick.ogg");
            Add(RecoverySound.Purchase,ui,.36f,60,.25f,"Recovery/handleCoins.ogg");
            Add(RecoverySound.Accept,ui,.33f,55,.4f,"UI/confirmation_001.ogg");
            Add(RecoverySound.Complete,ui,.45f,15,2,"UI/confirmation_002.ogg");
            Add(RecoverySound.Attach,rec,.50f,35,.15f,"Recovery/metalLatch.ogg");
            Add(RecoverySound.Detach,rec,.40f,40,.15f,"Recovery/metalClick.ogg");
            Add(RecoverySound.Snap,rec,.58f,20,.4f,"Recovery/Cable Snap.wav");
            Add(RecoverySound.Release,rec,.4f,30,.7f,"Foley/impactSoft_heavy_000.ogg");
            Add(RecoverySound.Impact,rec,.42f,50,.22f,"Foley/impactMetal_medium_000.ogg","Foley/impactMetal_medium_001.ogg");
            Add(RecoverySound.Obstacle,rec,.48f,50,.6f,"Foley/impactWood_medium_000.ogg","Foley/impactWood_medium_001.ogg");
            Add(RecoverySound.Signal,rec,.38f,40,.8f,"UI/pluck_001.ogg");
            Add(RecoverySound.Creak,rec,.18f,120,.45f,"Recovery/creak1.ogg","Recovery/winch - Marker #2.wav","Recovery/winch - Marker #3.wav");
            Add(RecoverySound.WinchStart,rec,.32f,70,.2f,"Recovery/Motor start.wav");
            Add(RecoverySound.WinchStop,rec,.28f,70,.2f,"Recovery/Motor end.wav");
            Add(RecoverySound.EngineStart,RecoveryAudioBus.Vehicles,.30f,90,1,"Vehicles/Engine Start.wav");
            bank.Sounds=sounds.ToArray();
            bank.Surfaces=new[]{
                new RecoverySurfaceSounds{Surface=RecoveryAudioSurface.Dirt,Gain=.10f,Clips=Enumerable.Range(1,3).Select(i=>Clip("Surfaces/Footstep_Dirt_"+i+".wav")).ToArray()},
                new RecoverySurfaceSounds{Surface=RecoveryAudioSurface.Sand,Gain=.12f,Clips=Enumerable.Range(1,3).Select(i=>Clip("Surfaces/Sand_"+i+".wav")).ToArray()},
                new RecoverySurfaceSounds{Surface=RecoveryAudioSurface.Mud,Gain=.20f,Clips=Enumerable.Range(1,4).Select(i=>Clip("Surfaces/mud_0"+i+".ogg")).ToArray()},
                new RecoverySurfaceSounds{Surface=RecoveryAudioSurface.Snow,Gain=.16f,Clips=Enumerable.Range(0,3).Select(i=>Clip("Foley/footstep_snow_00"+i+".ogg")).ToArray()},
                new RecoverySurfaceSounds{Surface=RecoveryAudioSurface.Wood,Gain=.13f,Clips=Enumerable.Range(0,3).Select(i=>Clip("Foley/footstep_wood_00"+i+".ogg")).ToArray()},
                new RecoverySurfaceSounds{Surface=RecoveryAudioSurface.Grass,Gain=.08f,Clips=Enumerable.Range(0,3).Select(i=>Clip("Foley/footstep_grass_00"+i+".ogg")).ToArray()}};
            bank.Waterfalls=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader!=null&&(r.sharedMaterial.shader.name=="TowTrouble/Gorge Falling Water"||r.name.IndexOf("waterfall",StringComparison.OrdinalIgnoreCase)>=0)).Select(r=>r.bounds.center).Distinct().ToArray();
            EditorUtility.SetDirty(bank);AssetDatabase.SaveAssetIfDirty(bank);
            Debug.Log("Tow Trouble audio configured: "+bank.Sounds.Length+" events, "+bank.Surfaces.Length+" surfaces, "+bank.Waterfalls.Length+" waterfall anchors.");
        }
    }
}

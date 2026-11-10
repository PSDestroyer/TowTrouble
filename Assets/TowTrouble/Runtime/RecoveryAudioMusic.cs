using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryAudioDirector
    {
        private RecoveryMusicPlaylist playlist;
        private AudioClip lastMusic;
        private readonly float[] musicFrom = new float[2], musicWeights = new float[2];
        private double musicScheduledAt;
        private float musicGapRemaining;
        private bool musicContextReady;

        private void Music(RecoveryWorld world, bool online, int slot)
        {
            if (playlist == null) playlist = new RecoveryMusicPlaylist(bank.ExploreMusic, GetInstanceID());
            bool menu = !online || world.Phase == RecoveryPhase.Lobby;
            if (!focused || appPaused)
            {
                for (int i = 0; i < music.Length; i++) music[i].volume = 0;
                return;
            }
            float delta = Mathf.Min(Time.unscaledDeltaTime, .1f);
            double now = AudioSettings.dspTime;
            musicGapRemaining = Mathf.Max(0, musicGapRemaining - delta);
            bool contextChanged = !musicContextReady || menu != musicMenu;
            // Complete a transition before reusing its outgoing deck. Rapid menu changes cannot cut a half-audible track.
            if ((contextChanged && (!trackStarted || musicBlend >= 1)) || (!trackStarted && musicGapRemaining <= 0))
            {
                var clip = menu ? bank.MenuMusic : playlist.Next(lastMusic);
                if (clip == null && !menu) clip = bank.MenuMusic;
                musicMenu = menu; musicContextReady = true;
                if (clip != null && music[musicIndex].clip == clip && music[musicIndex].isPlaying)
                { musicBlend = 1; trackStarted = true; }
                else if (clip != null)
                {
                    musicIndex = 1 - musicIndex;
                    var next = music[musicIndex]; next.Stop(); next.clip = clip; next.volume = 0;
                    musicWeights[musicIndex] = 0;
                    for (int i = 0; i < music.Length; i++) musicFrom[i] = musicWeights[i];
                    musicScheduledAt = now + .2;
                    next.PlayScheduled(musicScheduledAt);
                    musicBlend = 0; trackStarted = true; lastMusic = clip;
                }
                else musicGapRemaining = 5; // Missing music is a quiet fallback, never an every-frame restart.
            }
            float tension = online ? world.Tension(slot) : 0;
            bool stalled = online && world.Winch(slot).State == WinchState.Stalled;
            float duckTarget = RecoveryAudioPolicy.MusicDuck(session.MenuOpen, tension, stalled);
            // Recover the musical level slowly after a difficult pull; reduce it quickly for mechanical feedback.
            musicDuck = Mathf.MoveTowards(musicDuck, duckTarget, delta * (duckTarget < musicDuck ? 1.5f : .32f));
            if (now >= musicScheduledAt) musicBlend = Mathf.MoveTowards(musicBlend, 1, delta / Mathf.Max(.2f, bank.MusicCrossfade));
            float gain = BusGain(RecoveryAudioBus.Music) * bank.MusicGain * musicDuck;
            var active = music[musicIndex];
            float remaining = active.clip != null ? Mathf.Max(0, active.clip.length - active.time) : 0;
            float ending = trackStarted && active.isPlaying ? Mathf.SmoothStep(0, 1, remaining / 2.5f) : 1;
            for (int i = 0; i < music.Length; i++)
            {
                float weight = RecoveryAudioPolicy.MusicEnvelope(musicFrom[i], i == musicIndex, musicBlend);
                musicWeights[i] = weight;
                music[i].volume = gain * weight * (i == musicIndex ? ending : 1);
                if (i != musicIndex && musicBlend >= 1) { music[i].Stop(); musicWeights[i] = 0; }
            }
            if (trackStarted && now > musicScheduledAt + 1 && active.clip != null &&
                (active.clip.loadState == AudioDataLoadState.Failed || active.clip.loadState == AudioDataLoadState.Loaded && !active.isPlaying))
            {
                trackStarted = false;
                musicWeights[musicIndex] = musicFrom[musicIndex] = 0;
                musicGapRemaining = menu ? bank.MenuMusicGap : bank.ExploreMusicGap;
            }
        }
    }
}

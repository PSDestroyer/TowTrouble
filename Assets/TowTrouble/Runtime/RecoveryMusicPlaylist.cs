using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    /// <summary>A local shuffle bag; presentation never consumes Unity's gameplay random state.</summary>
    public sealed class RecoveryMusicPlaylist
    {
        private readonly AudioClip[] clips;
        private readonly System.Random random;
        private int cursor;
        public RecoveryMusicPlaylist(AudioClip[] source, int seed)
        {
            var unique = new List<AudioClip>();
            if (source != null) foreach (var clip in source) if (clip != null && !unique.Contains(clip)) unique.Add(clip);
            clips = unique.ToArray(); random = new System.Random(seed);
            // Keep the familiar opening track, then vary the rest of the first journey.
            Shuffle(1);
        }
        private void Shuffle(int first)
        {
            for (int i = clips.Length - 1; i > first; i--)
            { int next = random.Next(first, i + 1); var clip = clips[i]; clips[i] = clips[next]; clips[next] = clip; }
        }
        public AudioClip Next(AudioClip avoid)
        {
            if (clips.Length == 0) return null;
            if (cursor >= clips.Length) { cursor = 0; Shuffle(0); }
            if (clips.Length > 1 && clips[cursor] == avoid)
            {
                int other = cursor + 1;
                // When only the last item remains, start a new bag rather than repeating the menu theme.
                if (other >= clips.Length) { cursor = 0; Shuffle(0); other = 1; }
                while (other < clips.Length && clips[other] == avoid) other++;
                if (other >= clips.Length) other = 0;
                var clip = clips[cursor]; clips[cursor] = clips[other]; clips[other] = clip;
            }
            return clips[cursor++];
        }
    }
}

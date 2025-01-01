using System.Collections.Generic;
using UnityEngine;

using static DebugStats;

public class PlayerAudioSFX : MonoBehaviour {
    public static PlayerAudioSFX Instance;

    void Awake() {
        if (PlayerAudioSFX.Instance != null) {
            Debug.LogWarning("Multiple PlayerAudioSFX instances found - destroying new one.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    [Header("Audio sources")]
    [SerializeField] AudioSource meta;
    List<(AudioClip clip, float volume, float start)> meta_currentlyPlaying = new List<(AudioClip, float, float)>();

    // TODO: call this ..OneShot, depending on future functions
    public void playMetaSFXClip(AudioClip clip, float volume = 1f) {
        meta_currentlyPlaying.Add((clip, volume, Time.time));
        meta.PlayOneShot(clip, volume);
    }
    public static void PlayMetaSFXClip(AudioClip clip, float volume = 1f) => Instance?.playMetaSFXClip(clip, volume);

    void UPDATE_PrintStats() {
        if (meta_currentlyPlaying.Count == 0) return;

        STATS_SectionStart("Player Audio (SFX)");

        for (int i = 0; i < meta_currentlyPlaying.Count; ++i) {
            var (clip, volume, start) = meta_currentlyPlaying[i];

            STATS_SectionPrintLine($"{"meta:".bold()} '{clip.name}'  vol: {volume}  {Time.time - start, 0:##0.00}/{clip.length, 0:##0.00}");
            if (Time.time - start >= clip.length) meta_currentlyPlaying.Remove((clip, volume, start));
        }

        STATS_SectionEnd();
    }

    void LateUpdate() {
        UPDATE_PrintStats();
    }
}

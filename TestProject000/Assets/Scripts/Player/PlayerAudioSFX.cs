using System.Collections.Generic;
using UnityEngine;

using static CoreSystemFramework.Logging;

public class PlayerAudioSFX : MonoBehaviour {
    public static PlayerAudioSFX Instance;

    void Awake() {
        if (Instance != null) {
            Debug.LogWarning("Multiple PlayerAudioSFX instances found - destroying new one.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    [Header("Audio sources")]
    [SerializeField] AudioSource meta;

    [SerializeField] float meta_globalVolumeMult = 0.45f;

    // TEMP: improve this!
    List<(AudioClip clip, float volume, float start)> meta_currentlyPlaying = new List<(AudioClip, float, float)>();

    // TODO: call this ..OneShot, depending on future functions
    public void playMetaSFXClip(AudioClip clip, float volume = 1f, float speed = 1f) {
        if (!clip) {
            log("null clip was passed, not playing!");
            return;
        }

        meta_currentlyPlaying.Add((clip, volume * meta_globalVolumeMult, Time.time));
        meta.pitch = speed;
        meta.PlayOneShot(clip, volume);
    }
    public static void PlayMetaSFXClip(AudioClip clip, float volume = 1f, float speed = 1f) => Instance?.playMetaSFXClip(clip, volume, speed);

    public static bool PLAYERSFX_EnableStats = false;
    void UPDATE_PrintStats() {
        if (!PLAYERSFX_EnableStats) return;
        if (meta_currentlyPlaying.Count == 0) return;

        for (int i = 0; i < meta_currentlyPlaying.Count; ++i) {
            var (clip, volume, start) = meta_currentlyPlaying[i];

            STATS_PrintLine($"{"meta:".bold()} '{clip.name}'  vol: {volume}  {Time.time - start, 0:##0.00}/{clip.length, 0:##0.00}");
            if (Time.time - start >= clip.length) {
                meta_currentlyPlaying.Remove((clip, volume, start));
                break; // collection modified
            }
        }
    }

    void LateUpdate() {
        UPDATE_PrintStats();
    }
}

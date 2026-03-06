using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 1.5f)] public float pitchMin = 0.95f;
        [Range(0.5f, 1.5f)] public float pitchMax = 1.05f;
        public bool loop = false;

        [HideInInspector] public AudioSource source;
    }

    [Header("Sound Effects")]
    public Sound[] sfx;

    [Header("Music")]
    public Sound[] music;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSounds();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSounds()
    {
        // Create AudioSource components for each sound
        foreach (Sound s in sfx)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.loop = s.loop;
        }

        foreach (Sound s in music)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.loop = s.loop;
        }
    }

    // Play sound effect with random pitch variation
    public void PlaySFX(string soundName)
    {
        Sound s = System.Array.Find(sfx, sound => sound.name == soundName);
        if (s == null)
        {
            Debug.LogWarning("Sound not found: " + soundName);
            return;
        }

        s.source.pitch = Random.Range(s.pitchMin, s.pitchMax);
        s.source.PlayOneShot(s.clip, s.volume);
    }

    // Play music (looping)
    public void PlayMusic(string musicName)
    {
        Sound s = System.Array.Find(music, sound => sound.name == musicName);
        if (s == null)
        {
            Debug.LogWarning("Music not found: " + musicName);
            return;
        }

        // Stop other music first
        foreach (Sound m in music)
        {
            if (m.source.isPlaying)
                m.source.Stop();
        }

        s.source.Play();
    }

    // Stop music
    public void StopMusic(string musicName)
    {
        Sound s = System.Array.Find(music, sound => sound.name == musicName);
        if (s != null)
        {
            s.source.Stop();
        }
    }

    // Check if specific music is playing
    public bool IsMusicPlaying(string musicName)
    {
        Sound s = System.Array.Find(music, sound => sound.name == musicName);
        return s != null && s.source != null && s.source.isPlaying;
    }

    // Smooth crossfade between music tracks
    public void CrossfadeMusic(string newMusicName, float fadeDuration = 1f)
    {
        Sound newSound = System.Array.Find(music, sound => sound.name == newMusicName);
        if (newSound == null)
        {
            Debug.LogWarning("Music not found: " + newMusicName);
            return;
        }

        // Don't restart if already playing
        if (newSound.source.isPlaying) return;

        // Start crossfade coroutine
        StartCoroutine(CrossfadeCoroutine(newSound, fadeDuration));
    }

    private IEnumerator CrossfadeCoroutine(Sound newSound, float duration)
    {
        // Find currently playing music
        Sound currentSound = null;
        foreach (Sound m in music)
        {
            if (m.source.isPlaying)
            {
                currentSound = m;
                break;
            }
        }

        // Start new music at volume 0
        newSound.source.volume = 0f;
        newSound.source.Play();

        // Crossfade
        float elapsed = 0f;
        float startVolume = currentSound != null ? currentSound.source.volume : 0f;
        float targetVolume = newSound.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Fade out old music
            if (currentSound != null)
                currentSound.source.volume = Mathf.Lerp(startVolume, 0f, t);

            // Fade in new music
            newSound.source.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        // Stop old music
        if (currentSound != null)
        {
            currentSound.source.Stop();
            currentSound.source.volume = currentSound.volume; // Reset volume
        }

        // Ensure new music is at correct volume
        newSound.source.volume = targetVolume;
    }

    // Play random SFX from array (no consecutive repeats)
    private int lastRandomIndex = -1;
    public void PlayRandomSFX(string[] soundNames)
    {
        if (soundNames == null || soundNames.Length == 0) return;

        int index;
        do
        {
            index = Random.Range(0, soundNames.Length);
        } while (index == lastRandomIndex && soundNames.Length > 1);

        lastRandomIndex = index;
        PlaySFX(soundNames[index]);
    }

    // Play random music from array (no consecutive repeats)
    private int lastRandomMusicIndex = -1;
    public void PlayRandomMusic(string[] musicNames, float fadeDuration = 1f)
    {
        if (musicNames == null || musicNames.Length == 0) return;

        int index;
        do
        {
            index = Random.Range(0, musicNames.Length);
        } while (index == lastRandomMusicIndex && musicNames.Length > 1);

        lastRandomMusicIndex = index;
        CrossfadeMusic(musicNames[index], fadeDuration);
    }

    // ===== ENUM-BASED METHODS (Type-Safe) =====

    /// <summary>
    /// Play sound effect using enum (type-safe)
    /// </summary>
    public void PlaySFX(SFXType sfxType)
    {
        PlaySFX(sfxType.GetName());
    }

    /// <summary>
    /// Play music using enum (type-safe)
    /// </summary>
    public void PlayMusic(MusicType musicType)
    {
        PlayMusic(musicType.GetName());
    }

    /// <summary>
    /// Stop music using enum (type-safe)
    /// </summary>
    public void StopMusic(MusicType musicType)
    {
        StopMusic(musicType.GetName());
    }

    /// <summary>
    /// Check if music is playing using enum (type-safe)
    /// </summary>
    public bool IsMusicPlaying(MusicType musicType)
    {
        return IsMusicPlaying(musicType.GetName());
    }

    /// <summary>
    /// Crossfade music using enum (type-safe)
    /// </summary>
    public void CrossfadeMusic(MusicType musicType, float fadeDuration = 1f)
    {
        CrossfadeMusic(musicType.GetName(), fadeDuration);
    }

    /// <summary>
    /// Play random SFX from enum array (type-safe, no consecutive repeats)
    /// </summary>
    public void PlayRandomSFX(SFXType[] sfxTypes)
    {
        PlayRandomSFX(sfxTypes.GetNames());
    }

    /// <summary>
    /// Play random music from enum array (type-safe, no consecutive repeats)
    /// </summary>
    public void PlayRandomMusic(MusicType[] musicTypes, float fadeDuration = 1f)
    {
        PlayRandomMusic(musicTypes.GetNames(), fadeDuration);
    }

    /// <summary>
    /// Set pitch on all music sources.
    /// Called by GameManager during Vital View to create slow-motion audio effect.
    /// Pass 1f to restore normal pitch.
    /// </summary>
    public void SetAllMusicPitch(float pitch)
    {
        foreach (Sound m in music)
        {
            if (m.source != null)
                m.source.pitch = pitch;
        }
    }
}

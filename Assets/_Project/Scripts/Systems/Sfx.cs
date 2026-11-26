using UnityEngine;
using UnityEngine.Audio;

// plays one shot sounds from the sound bank flat for the player or placed in the world
public static class Sfx
{
    private const float NearDistance = 1f; // m inside which a world sound is at full volume

    // every clip mixer group and audio number the game uses
    public static SoundBank Bank { get; private set; }

    // loads the sound bank once before the first scene
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        Bank = Resources.Load<SoundBank>("SoundBank");
    }

    // a random clip from a set of variations or null when the set is empty
    public static AudioClip Pick(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    // a sound from the player that is the same in both ears
    public static void Flat(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        AudioSource source = Make(clip, Bank.sfxGroup, volume, pitch);
        if (source != null) source.Play();
    }

    // a sound at a point in the world that fades to nothing at the range in m
    public static void At(AudioClip clip, Vector3 position, float range = 0f, float volume = 1f, float pitch = 1f)
    {
        Place(clip, Bank.sfxGroup, position, range > 0f ? range : Bank.sfxRange, volume, pitch);
    }

    // a guard voice line at a point in the world
    public static void Voice(AudioClip clip, Vector3 position)
    {
        Place(clip, Bank.voiceGroup, position, Bank.voiceRange, 1f, 1f);
    }

    // a menu sound that plays while paused and keeps going through a scene change
    public static void Ui(AudioClip clip)
    {
        AudioSource source = Make(clip, Bank.uiGroup, 1f, 1f);
        if (source == null) return;
        source.ignoreListenerPause = true;
        Object.DontDestroyOnLoad(source.gameObject);
        source.Play();
    }

    // puts a source at a point with a linear fade out to its range and plays it
    private static void Place(AudioClip clip, AudioMixerGroup group, Vector3 position, float range, float volume, float pitch)
    {
        AudioSource source = Make(clip, group, volume, pitch);
        if (source == null) return;
        source.transform.position = position;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = NearDistance;
        source.maxDistance = range;
        source.Play();
    }

    // a throwaway object with a source that removes itself after the clip with room for the slow motion pitch
    private static AudioSource Make(AudioClip clip, AudioMixerGroup group, float volume, float pitch)
    {
        if (clip == null) return null;
        AudioSource source = new GameObject("Sound " + clip.name).AddComponent<AudioSource>();
        source.clip = clip;
        source.outputAudioMixerGroup = group;
        source.volume = volume;
        source.pitch = pitch;
        Object.Destroy(source.gameObject, clip.length / (pitch * Bank.slowMoPitch) + 0.1f);
        return source;
    }
}
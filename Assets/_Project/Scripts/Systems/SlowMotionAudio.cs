using UnityEngine;
using UnityEngine.Audio;

// lowers the pitch of world sounds while time is slowed so the whole world audibly slows down
public class SlowMotionAudio : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;

    private static readonly string[] PitchParameters = { "AmbiencePitch", "SfxPitch", "VoicePitch" }; // exposed on the mixer

    private float pitch = 1f;

    // eases the pitch in real time toward low while slowed and normal otherwise
    private void Update()
    {
        // a freeze for game over or the pause menu keeps the normal pitch
        bool slowed = Time.timeScale > 0f && Time.timeScale < 1f;
        float target = slowed ? Sfx.Bank.slowMoPitch : 1f;
        float step = (1f - Sfx.Bank.slowMoPitch) / Sfx.Bank.slowMoFade * Time.unscaledDeltaTime;
        pitch = Mathf.MoveTowards(pitch, target, step);
        SetPitch(pitch);
    }

    // back to normal pitch when the scene closes
    private void OnDestroy()
    {
        SetPitch(1f);
    }

    // the same pitch on every world sound group
    private void SetPitch(float value)
    {
        foreach (string parameter in PitchParameters) mixer.SetFloat(parameter, value);
    }
}
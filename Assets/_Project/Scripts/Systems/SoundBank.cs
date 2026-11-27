using UnityEngine;
using UnityEngine.Audio;

// every sound effect the mixer groups and the audio numbers in one asset that sfx loads from resources
[CreateAssetMenu(fileName = "SoundBank", menuName = "Clockwork Heist/Sound Bank")]
public class SoundBank : ScriptableObject
{
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup voiceGroup;
    public AudioMixerGroup uiGroup;

    public float sfxRange = 15f; // m a sound in the world can be heard
    public float voiceRange = 25f; // m a guard voice can be heard
    public float slowMoPitch = 0.5f; // pitch of world sounds while time is slowed
    public float slowMoFade = 0.2f; // real s for that pitch to drop or come back
    public float musicFade = 1f; // real s for the chase music to fade in or out
    public float walkStepLength = 1.4f; // m between steps when walking or crouching
    public float runStepLength = 1.9f; // m between steps when sprinting or running
    public float crouchStepVolume = 0.1f; // 0 to 1
    public float walkStepVolume = 0.3f; // 0 to 1
    public float runStepVolume = 0.5f; // 0 to 1

    public AudioClip[] stepMarble;
    public AudioClip[] stepWood;
    public AudioClip[] stepMetal;
    public AudioClip[] stepGrate;
    public AudioClip land;
    public AudioClip[] playerHurt;

    public AudioClip teleportWhoosh;
    public AudioClip gadgetThrow;
    public AudioClip stickyLand;
    public AudioClip decoySpawn;
    public AudioClip decoyDissipate;
    public AudioClip dartFire;
    public AudioClip dartHitBody;
    public AudioClip dartHitSurface;
    public AudioClip pickupGadget;

    public AudioClip drawBlade;
    public AudioClip drawClub;
    public AudioClip[] swingBlade;
    public AudioClip[] swingClub;
    public AudioClip[] bladeHit;
    public AudioClip clubHit;
    public AudioClip batonSwing;
    public AudioClip batonHit;
    public AudioClip bodyPickUp;
    public AudioClip bodyDrop;
    public AudioClip bodyStash;
    public AudioClip lootCoins;
    public AudioClip pickpocketRustle;

    public AudioClip[] guardHey;
    public AudioClip[] guardHuh;
    public AudioClip[] guardHurt;
    public AudioClip[] guardGiveUp;
    public AudioClip[] guardWakeUp;

    public AudioClip alarmBell;
    public AudioClip gameOverSting;
    public AudioClip spottedSting;

    public AudioClip cameraSpotted;
    public AudioClip hackTabletUp;
    public AudioClip hackTabletDown;
    public AudioClip hackStatic;
    public AudioClip hackCorrect;
    public AudioClip hackWrong;
    public AudioClip hackSuccess;
    public AudioClip hackFail;
    public AudioClip hackWarning;

    public AudioClip objectivePickup;
    public AudioClip objectiveSting;
    public AudioClip doorOpen;

    public AudioClip uiHover;
    public AudioClip uiClick;
    public AudioClip uiCheckpoint;

    // the footstep set for a kind of floor
    public AudioClip[] StepsFor(Surface surface)
    {
        switch (surface)
        {
            case Surface.Wood: return stepWood;
            case Surface.Metal: return stepMetal;
            case Surface.Grate: return stepGrate;
            default: return stepMarble;
        }
    }
}
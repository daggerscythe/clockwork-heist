using UnityEngine;

// player settings saved between runs and cheats and the tutorial switch that reset every launch
public static class GameOptions
{
    public const float MinSensitivity = 0.25f; // lowest mouse sensitivity multiplier
    public const float MaxSensitivity = 3f; // highest mouse sensitivity multiplier
    public const float MinBrightness = 0.5f; // darkest the brightness setting goes
    public const float MaxBrightness = 2f; // brightest the brightness setting goes

    private const string PixelationKey = "pixelation";
    private const string VolumeKey = "volume";
    private const string SensitivityKey = "sensitivity";
    private const string BrightnessKey = "brightness";

    // true draws the game with big pixels
    public static bool Pixelation { get; private set; } = true;

    // master volume from 0 to 1
    public static float Volume { get; private set; } = 1f;

    // multiplier on the look sensitivity from the game settings
    public static float Sensitivity { get; private set; } = 1f;

    // 1 is the game as made and higher lifts the dark parts of the screen
    public static float Brightness { get; private set; } = 1f;

    // gadgets and hack kits are never used up
    public static bool UnlimitedGadgets { get; private set; }

    // guard hits do no damage
    public static bool UnlimitedHealth { get; private set; }

    // tutorial notes and the guide arrow are off for the rest of this run
    public static bool TutorialOff { get; private set; }

    // reads the saved settings before the first scene loads and turns the cheats off
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        Pixelation = PlayerPrefs.GetInt(PixelationKey, 1) == 1;
        Volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        Sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1f);
        Brightness = PlayerPrefs.GetFloat(BrightnessKey, 1f);
        UnlimitedGadgets = false;
        UnlimitedHealth = false;
        TutorialOff = false;
        Apply();
    }

    // turns the pixel pass on or off and saves it
    public static void SetPixelation(bool on)
    {
        Pixelation = on;
        PlayerPrefs.SetInt(PixelationKey, on ? 1 : 0);
        PlayerPrefs.Save();
        Apply();
    }

    // sets the master volume from 0 to 1 and saves it
    public static void SetVolume(float volume)
    {
        Volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumeKey, Volume);
        PlayerPrefs.Save();
        Apply();
    }

    // sets the mouse sensitivity multiplier and saves it
    public static void SetSensitivity(float sensitivity)
    {
        Sensitivity = Mathf.Clamp(sensitivity, MinSensitivity, MaxSensitivity);
        PlayerPrefs.SetFloat(SensitivityKey, Sensitivity);
        PlayerPrefs.Save();
    }

    // sets the screen brightness and saves it
    public static void SetBrightness(float brightness)
    {
        Brightness = Mathf.Clamp(brightness, MinBrightness, MaxBrightness);
        PlayerPrefs.SetFloat(BrightnessKey, Brightness);
        PlayerPrefs.Save();
        Apply();
    }

    // cheat that keeps every gadget count from going down
    public static void SetUnlimitedGadgets(bool on)
    {
        UnlimitedGadgets = on;
    }

    // cheat that blocks guard damage
    public static void SetUnlimitedHealth(bool on)
    {
        UnlimitedHealth = on;
    }

    // the turn off tutorial button and a new run from the main menu
    public static void SetTutorialOff(bool off)
    {
        TutorialOff = off;
    }

    // hands the values to the systems that use them
    private static void Apply()
    {
        PixelateRenderFeature.Enabled = Pixelation;
        AudioListener.volume = Volume;
        Shader.SetGlobalFloat("_Brightness", Brightness);
    }
}
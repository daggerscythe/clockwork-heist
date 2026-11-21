using TMPro;
using UnityEngine;
using UnityEngine.UI;

// the settings rows shared by the pause menu and the main menu
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Toggle pixelationToggle;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeText;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityText;
    [SerializeField] private Toggle unlimitedGadgetsToggle;
    [SerializeField] private Toggle unlimitedHealthToggle;

    private const float MaxVolume = 100f; // the volume slider goes from 0 to this
    private const float SensitivityStep = 0.05f; // the sensitivity slider snaps to this

    // sets the slider ranges and listens to every row
    private void Awake()
    {
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = MaxVolume;
        volumeSlider.wholeNumbers = true;
        sensitivitySlider.minValue = GameOptions.MinSensitivity;
        sensitivitySlider.maxValue = GameOptions.MaxSensitivity;
        sensitivitySlider.wholeNumbers = false;

        pixelationToggle.onValueChanged.AddListener(GameOptions.SetPixelation);
        volumeSlider.onValueChanged.AddListener(OnVolume);
        sensitivitySlider.onValueChanged.AddListener(OnSensitivity);
        unlimitedGadgetsToggle.onValueChanged.AddListener(GameOptions.SetUnlimitedGadgets);
        unlimitedHealthToggle.onValueChanged.AddListener(GameOptions.SetUnlimitedHealth);
    }

    // every time the panel opens the rows show the current values
    private void OnEnable()
    {
        pixelationToggle.SetIsOnWithoutNotify(GameOptions.Pixelation);
        volumeSlider.SetValueWithoutNotify(Mathf.Round(GameOptions.Volume * MaxVolume));
        sensitivitySlider.SetValueWithoutNotify(GameOptions.Sensitivity);
        unlimitedGadgetsToggle.SetIsOnWithoutNotify(GameOptions.UnlimitedGadgets);
        unlimitedHealthToggle.SetIsOnWithoutNotify(GameOptions.UnlimitedHealth);
        ShowNumbers();
    }

    // the volume slider moved
    private void OnVolume(float value)
    {
        GameOptions.SetVolume(value / MaxVolume);
        ShowNumbers();
    }

    // the sensitivity slider moved and snaps to neat steps
    private void OnSensitivity(float value)
    {
        float snapped = Mathf.Round(value / SensitivityStep) * SensitivityStep;
        sensitivitySlider.SetValueWithoutNotify(snapped);
        GameOptions.SetSensitivity(snapped);
        ShowNumbers();
    }

    // the numbers next to the sliders
    private void ShowNumbers()
    {
        volumeText.text = Mathf.RoundToInt(GameOptions.Volume * MaxVolume).ToString();
        sensitivityText.text = GameOptions.Sensitivity.ToString("0.00");
    }
}
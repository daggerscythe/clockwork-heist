using TMPro;
using UnityEngine;
using UnityEngine.UI;

// shows the player info on screen
public class HUD : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerInteract interact;
    [SerializeField] private AbilityWheel wheel;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private GameObject promptPlate; // smoked glass behind the prompt
    [SerializeField] private GameObject promptKey; // e key cap shown when the prompt is something e does
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Image abilityIcon;
    [SerializeField] private TMP_Text abilityText;
    [SerializeField] private GameObject holdBar;
    [SerializeField] private Image holdFill;
    [SerializeField] private GameObject crosshair; // dot in the middle of the screen
    [SerializeField] private GameObject crosshairRing; // ring around the dot while e can be used

    private const string CountColor = "#F0C478"; // amber for gadget counts

    // the crosshair is on by default and other scripts can hide it for menus or cutscenes
    public bool CrosshairVisible { get; set; } = true;

    // keep everything matching the player
    private void Update()
    {
        // shrink the fill from the right so the sliced edges stay clean
        healthFill.rectTransform.anchorMax = new Vector2(playerHealth.HealthFraction, 1f);

        coinsText.text = inventory.Coins.ToString();

        // the prompt hides with the crosshair so menus and the hack screen stay clean
        bool showPrompt = CrosshairVisible && interact.Prompt != "";
        promptPlate.SetActive(showPrompt);
        promptKey.SetActive(interact.PromptHasKey);
        promptText.text = interact.Prompt;

        // what right click uses and how many are left
        Ability picked = wheel.Selected;
        abilityIcon.sprite = wheel.IconOf(picked);
        abilityText.text = picked == Ability.Blink
            ? AbilityWheel.NameOf(picked)
            : AbilityWheel.NameOf(picked) + "  <color=" + CountColor + ">\u00D7" + inventory.CountText(picked) + "</color>";

        // the hold bar only shows while pickpocketing
        holdBar.SetActive(interact.HoldFraction > 0f);
        holdFill.rectTransform.anchorMax = new Vector2(interact.HoldFraction, 1f);

        crosshair.SetActive(CrosshairVisible);
        crosshairRing.SetActive(CrosshairVisible && interact.PromptHasKey);
    }
}
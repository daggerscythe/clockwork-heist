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
    [SerializeField] private BlinkAbility blink;
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
    [SerializeField] private TMP_Text hintText; // tip from a hint trigger the keycard or the exit
    [SerializeField] private float hintTime = 8f; // real s a tip stays up
    [SerializeField, TextArea] private string menuNote = "You can access all notes in the menu (Esc)."; // shown once after the first note
    [SerializeField] private NotePopup notePopup; // card that stops the game for a tutorial note
    [SerializeField] private string cancelText = "(Left Click) Cancel Teleport"; // prompt while aiming teleport

    private const string CountColor = "#F0C478"; // amber for gadget counts
    private static bool menuNoteDone; // the menu note shows once each time the game starts
    private float hintEndTime; // real time the tip clears
    private string waitingNote = ""; // note that shows once the plate is free

    // the crosshair is on by default and other scripts can hide it for menus or cutscenes
    public bool CrosshairVisible { get; set; } = true;

    // shows a tip that clears itself after the hint time and replaces any tip already up
    public void ShowHint(string text)
    {
        hintText.text = text;
        hintEndTime = Time.unscaledTime + hintTime;
    }

    // a tutorial tip kept for the notes menu and shown on a card that stops the game unless the tutorial is off
    public void ShowNote(string text)
    {
        Notes.Add(text);
        if (GameOptions.TutorialOff) return;
        notePopup.Show(text);

        // the first note of the run is followed by the one about the notes menu
        if (menuNoteDone) return;
        menuNoteDone = true;
        waitingNote = menuNote;
    }

    // keep everything matching the player
    private void Update()
    {
        // shrink the fill from the right so the sliced edges stay clean
        healthFill.rectTransform.anchorMax = new Vector2(playerHealth.HealthFraction, 1f);

        coinsText.text = inventory.Coins.ToString();

        // while aiming teleport the prompt says how to cancel in place of what e does
        bool aiming = blink.IsAiming;
        string prompt = aiming ? cancelText : interact.Prompt;
        bool promptHasKey = !aiming && interact.PromptHasKey;

        // the prompt hides with the crosshair so menus and the hack screen stay clean
        bool showPrompt = CrosshairVisible && prompt != "";
        promptPlate.SetActive(showPrompt);
        promptKey.SetActive(promptHasKey);
        promptText.text = prompt;

        // what right click uses and how many are left
        Ability picked = wheel.Selected;
        abilityIcon.sprite = wheel.IconOf(picked);
        abilityText.text = picked == Ability.Blink
            ? AbilityWheel.NameOf(picked)
            : AbilityWheel.NameOf(picked) + "  <color=" + CountColor + ">\u00D7" + inventory.CountText(picked) + "</color>";

        // the hold bar only shows while pickpocketing
        holdBar.SetActive(interact.HoldFraction > 0f);
        holdFill.rectTransform.anchorMax = new Vector2(interact.HoldFraction, 1f);

        // a tip clears itself in real time so slow motion doesn't hold it up
        if (hintText.text != "" && Time.unscaledTime >= hintEndTime) hintText.text = "";

        // the menu note takes the plate once the note card is closed and the plate is free
        if (hintText.text == "" && waitingNote != "" && !NotePopup.IsOpen)
        {
            Sfx.Ui(Sfx.Bank.noteScribble);
            ShowHint(waitingNote);
            waitingNote = "";
        }

        crosshair.SetActive(CrosshairVisible);
        crosshairRing.SetActive(CrosshairVisible && promptHasKey);
    }
}
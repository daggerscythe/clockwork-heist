using TMPro;
using UnityEngine;
using UnityEngine.UI;

// every ability the wheel shows in slot order
public enum Ability { Blink, TeleportGadget, Decoy, SleepDart, HackKit }

// hold the middle mouse button to pick an ability from a ring while time slows
public class AbilityWheel : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private GameObject ring; // shown only while the wheel is open
    [SerializeField] private Image[] slotImages; // one per ability in the order of the list above
    [SerializeField] private TMP_Text[] slotTexts; // same order
    [SerializeField] private Color normalColor = new Color(0.1f, 0.1f, 0.15f, 0.8f); // slot that can be picked
    [SerializeField] private Color hoverColor = new Color(0.9f, 0.7f, 0.2f, 0.9f); // slot under the pointer
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.5f, 0.9f, 0.9f); // slot picked right now
    [SerializeField] private Color unavailableColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // slot that cannot be picked

    private const float PickDistance = 40f; // px the pointer must move from the center before a slot is picked
    private const float MaxPointer = 120f; // px the pointer can move from the center

    private static readonly string[] Names = { "Blink", "Teleport Gadget", "Decoy", "Sleep Dart", "Hack Kit" };

    private PlayerControls controls;
    private Vector2 pointer; // pretend mouse pointer from the center because the real one is locked
    private int hovered = -1; // slot under the pointer or -1 for none

    // true while the ring is showing
    public bool IsOpen { get; private set; }

    // the ability right click uses
    public Ability Selected { get; private set; } = Ability.Blink;

    // name shown for an ability on the wheel the hud and prompts
    public static string NameOf(Ability ability)
    {
        return Names[(int)ability];
    }

    // input is created in the player controller awake so it is read here
    private void Start()
    {
        controls = player.Controls;
        ring.SetActive(false);
    }

    // never leave time slowed or the view locked if this gets turned off while open
    private void OnDisable()
    {
        if (IsOpen) Close();
    }

    // open on press steer the pointer while held and pick on release
    private void Update()
    {
        // back to blink when the picked gadget runs out
        if (!CanSelect(Selected)) Selected = Ability.Blink;

        // the wheel cannot open in the middle of aiming
        if (!IsOpen && controls.Player.AbilityWheel.WasPressedThisFrame() && !controls.Player.AimUse.IsPressed()) Open();
        if (!IsOpen) return;

        pointer = Vector2.ClampMagnitude(pointer + controls.Player.Look.ReadValue<Vector2>(), MaxPointer);
        hovered = pointer.magnitude < PickDistance ? -1 : SlotAt(pointer);
        RefreshSlots();

        if (!controls.Player.AbilityWheel.IsPressed()) Close();
    }

    // blink always works gadgets need at least one and the hack kit is only used at cameras
    private bool CanSelect(Ability ability)
    {
        if (ability == Ability.Blink) return true;
        if (ability == Ability.HackKit) return false;
        return inventory.Count(ability) > 0;
    }

    // shows the ring slows time and freezes the view
    private void Open()
    {
        IsOpen = true;
        pointer = Vector2.zero;
        hovered = -1;
        player.LookLocked = true;
        ring.SetActive(true);
        TimeScaleManager.Instance.Request(this, settings.wheelTimeScale);
        RefreshSlots();
    }

    // picks the hovered slot if it can be picked and puts everything back
    private void Close()
    {
        if (hovered >= 0 && CanSelect((Ability)hovered)) Selected = (Ability)hovered;
        IsOpen = false;
        player.LookLocked = false;
        ring.SetActive(false);
        TimeScaleManager.Instance.Release(this);
    }

    // slot for a pointer direction with the first slot at the top going clockwise
    private int SlotAt(Vector2 direction)
    {
        float slotAngle = 360f / slotImages.Length;
        float fromTop = Mathf.Repeat(90f - Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, 360f);
        return Mathf.RoundToInt(fromTop / slotAngle) % slotImages.Length;
    }

    // colors each slot and writes its name and count
    private void RefreshSlots()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            Ability ability = (Ability)i;
            Color color = normalColor;
            if (!CanSelect(ability)) color = unavailableColor;
            else if (i == hovered) color = hoverColor;
            else if (ability == Selected) color = selectedColor;

            slotImages[i].color = color;
            slotTexts[i].text = ability == Ability.Blink ? Names[i] : Names[i] + "\n" + inventory.Count(ability);
        }
    }
}
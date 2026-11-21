using UnityEngine;

// e picks up drops and hides bodies takes coins hacks cameras and held behind a guard pickpockets
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInventory))]
public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private GameObject carriedModel; // body shown on the shoulder while carrying
    [SerializeField] private HackMinigame hackMinigame;

    private const float DropDistance = 0.8f; // m in front of the player a body is put down
    private const float WaistHeight = 0.5f; // m up from the feet where the drop checks for walls
    private const float FloorSearch = 3f; // m down to look for floor under a dropped body

    private PlayerController player;
    private PlayerInventory inventory;
    private PlayerControls controls;
    private int lookMask; // things the player can look at or that block the view
    private EnemyHealth carried; // body on the shoulder or null
    private float holdTime; // s e has been held for a pickpocket

    // what e does right now for the hud and empty when nothing
    public string Prompt { get; private set; } = "";

    // true when the prompt is something e does so the hud shows the key
    public bool PromptHasKey { get; private set; }

    // 0 to 1 of the pickpocket hold
    public float HoldFraction => holdTime / settings.pickpocketHoldTime;

    // grab components and build the mask
    private void Awake()
    {
        player = GetComponent<PlayerController>();
        inventory = GetComponent<PlayerInventory>();
        lookMask = ~LayerMask.GetMask("Player", "LightVolume", "Ignore Raycast");
        carriedModel.SetActive(false);
    }

    // input is created in the player controller awake so it is read here
    private void Start()
    {
        controls = player.Controls;
    }

    // empties the shoulder without dropping so a checkpoint restore can put the guard back
    public void ForgetCarried()
    {
        carried = null;
        holdTime = 0f;
        player.IsCarrying = false;
        carriedModel.SetActive(false);
    }

    // works out what e would do on whatever the player is looking at
    private void Update()
    {
        bool looking = LookAt(out RaycastHit hit);
        Collider target = looking && hit.distance <= settings.interactRange ? hit.collider : null;
        SecurityCamera aimedCamera = looking ? hit.collider.GetComponentInParent<SecurityCamera>() : null;
        bool pressed = controls.Player.Interact.WasPressedThisFrame();

        if (carried != null)
        {
            holdTime = 0f;
            UpdateCarrying(target, pressed);
            return;
        }

        // a camera in hack range can be hacked with a kit while standing on the ground
        if (aimedCamera != null)
        {
            holdTime = 0f;
            string blocked = WhyCantHack(aimedCamera);
            ShowPrompt(blocked ?? "Hack camera", blocked == null);
            if (pressed && blocked == null && player.IsGrounded) hackMinigame.Begin(aimedCamera);
            return;
        }

        // gadgets lying around including a stuck teleport gadget
        GadgetPickup pickup = target != null ? target.GetComponent<GadgetPickup>() : null;
        if (pickup != null)
        {
            holdTime = 0f;
            ShowPrompt("Take " + AbilityWheel.NameOf(pickup.Gadget), true);
            if (pressed) pickup.Take(inventory);
            return;
        }

        EnemyHealth guard = target != null ? target.GetComponentInParent<EnemyHealth>() : null;
        Loot loot = target != null ? target.GetComponentInParent<Loot>() : null;

        // a knocked out or dead guard can be looted and lifted
        if (guard != null && guard.IsDown)
        {
            holdTime = 0f;
            ShowPrompt("Pick up body", true);
            if (pressed) PickUp(guard, loot);
            return;
        }

        // an unaware guard with coins can be pickpocketed from behind
        if (guard != null && loot != null && loot.HasCoins && guard.IsUnaware(cameraTransform.position))
        {
            ShowPrompt("Hold to pickpocket", true);
            HoldPickpocket(loot);
            return;
        }

        holdTime = 0f;

        // a coin pile in the level
        if (guard == null && loot != null && loot.HasCoins)
        {
            ShowPrompt("Take coins", true);
            if (pressed) inventory.AddCoins(loot.TakeAll());
            return;
        }

        ShowPrompt("", false);
    }

    // sets the prompt text and whether the hud shows the e key with it
    private void ShowPrompt(string text, bool hasKey)
    {
        Prompt = text;
        PromptHasKey = hasKey;
    }

    // the first thing in front of the player as far as the hack range reaches
    private bool LookAt(out RaycastHit hit)
    {
        float reach = Mathf.Max(settings.interactRange, settings.hackRange);
        return Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, reach, lookMask, QueryTriggerInteraction.Ignore);
    }

    // why a camera can't be hacked right now or null when it can
    private string WhyCantHack(SecurityCamera securityCamera)
    {
        if (securityCamera.IsHacked) return "Camera hacked";
        if (!securityCamera.CanRetry) return "Hack again in " + Mathf.CeilToInt(securityCamera.RetryTime - Time.time) + " s";
        if (!inventory.Has(Ability.HackKit)) return "Need a Hack Kit";
        return null;
    }

    // loots the body then lifts it onto the shoulder
    private void PickUp(EnemyHealth body, Loot loot)
    {
        if (loot != null) inventory.AddCoins(loot.TakeAll());
        carried = body;
        body.PickUp();
        player.IsCarrying = true;
        carriedModel.SetActive(true);
    }

    // holding e fills up and then takes the coins and letting go starts over
    private void HoldPickpocket(Loot loot)
    {
        if (!controls.Player.Interact.IsPressed())
        {
            holdTime = 0f;
            return;
        }

        holdTime += Time.deltaTime;
        if (holdTime < settings.pickpocketHoldTime) return;
        inventory.AddCoins(loot.TakeAll());
        holdTime = 0f;
    }

    // while carrying e hides the body in a stash being looked at or drops it in front
    private void UpdateCarrying(Collider target, bool pressed)
    {
        BodyStash stash = target != null ? target.GetComponentInParent<BodyStash>() : null;
        ShowPrompt(stash != null ? "Hide body" : "Drop body", true);
        if (!pressed) return;

        if (stash != null) carried.Hide(stash.ExitPoint.position, stash.ExitPoint.rotation);
        else DropInFront();

        carried = null;
        player.IsCarrying = false;
        carriedModel.SetActive(false);
    }

    // puts the body on the floor in front and makes a thud guards can hear
    private void DropInFront()
    {
        Vector3 waist = transform.position + Vector3.up * WaistHeight;
        Vector3 spot = transform.position + transform.forward * DropDistance;

        // drop at the feet if a wall is in the way
        if (Physics.Raycast(waist, transform.forward, DropDistance, lookMask, QueryTriggerInteraction.Ignore))
            spot = transform.position;

        // find the floor so the body never floats
        if (Physics.Raycast(spot + Vector3.up, Vector3.down, out RaycastHit floor, FloorSearch, lookMask, QueryTriggerInteraction.Ignore))
            spot = floor.point;

        carried.Drop(spot, Quaternion.LookRotation(transform.right));
        Noise.Make(spot, settings.noiseBodyDrop);
    }
}
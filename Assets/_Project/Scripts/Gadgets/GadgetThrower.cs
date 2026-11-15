using UnityEngine;

// hold right click to aim the teleport gadget or decoy along an arc and let go to throw
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInventory))]
public class GadgetThrower : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private AbilityWheel wheel;
    [SerializeField] private LineRenderer arcLine;
    [SerializeField] private Renderer landingMarker;
    [SerializeField] private Material legalMaterial; // green when the gadget will work there
    [SerializeField] private Material illegalMaterial; // red when it will not
    [SerializeField] private ThrownGadget teleportGadgetPrefab;
    [SerializeField] private ThrownGadget decoyPrefab;

    private const float PreviewStep = 0.05f; // s of flight between arc points
    private const int PreviewSteps = 60; // so the preview shows 3 s of flight

    private PlayerControls controls;
    private PlayerInventory inventory;
    private CharacterController playerBody;
    private int solidMask; // things a gadget can hit
    private bool isAiming;
    private readonly Vector3[] points = new Vector3[PreviewSteps + 1]; // arc points reused every frame

    // grab components build the mask and hide the preview
    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        playerBody = GetComponent<CharacterController>();
        solidMask = ~LayerMask.GetMask("Player", "LightVolume", "Ignore Raycast");
        ShowPreview(false);
    }

    // input is created in the player controller awake so it is read here
    private void Start()
    {
        controls = GetComponent<PlayerController>().Controls;
    }

    // never leave time slowed if this gets turned off mid aim
    private void OnDisable()
    {
        if (isAiming) StopAim();
    }

    // aim while right click is held when a throwable gadget is picked
    private void Update()
    {
        bool throwablePicked = wheel.Selected == Ability.TeleportGadget || wheel.Selected == Ability.Decoy;
        if (!isAiming && throwablePicked && !wheel.IsOpen && controls.Player.AimUse.WasPressedThisFrame())
            StartAim();
        if (!isAiming) return;

        // left click cancels without throwing
        if (controls.Player.Attack.WasPressedThisFrame())
        {
            StopAim();
            return;
        }

        DrawArc();

        // letting go of right click throws
        if (!controls.Player.AimUse.IsPressed())
        {
            Throw();
            StopAim();
        }
    }

    // slows time and shows the arc
    private void StartAim()
    {
        isAiming = true;
        TimeScaleManager.Instance.Request(this, settings.aimTimeScale);
        ShowPreview(true);
    }

    // puts time back to normal and hides the arc
    private void StopAim()
    {
        isAiming = false;
        TimeScaleManager.Instance.Release(this);
        ShowPreview(false);
    }

    // turns the line and the landing marker on or off
    private void ShowPreview(bool show)
    {
        arcLine.enabled = show;
        landingMarker.gameObject.SetActive(show);
    }

    // follows the arc in small steps and colors it by whether the gadget would work where it lands
    private void DrawArc()
    {
        Vector3 origin = cameraTransform.position;
        Vector3 velocity = cameraTransform.forward * settings.throwSpeed;
        points[0] = origin;
        int count = 1;
        bool landed = false;
        RaycastHit hit = default;

        for (int i = 1; i <= PreviewSteps && !landed; i++)
        {
            Vector3 next = ThrownGadget.PointAt(origin, velocity, settings.projectileGravity, i * PreviewStep);
            landed = Physics.Linecast(points[count - 1], next, out hit, solidMask, QueryTriggerInteraction.Ignore);
            points[count] = landed ? hit.point : next;
            count++;
        }

        Material material = landed && WouldWork(hit) ? legalMaterial : illegalMaterial;
        arcLine.positionCount = count;
        for (int i = 0; i < count; i++) arcLine.SetPosition(i, points[i]);
        arcLine.sharedMaterial = material;

        landingMarker.gameObject.SetActive(landed);
        landingMarker.transform.position = points[count - 1];
        landingMarker.sharedMaterial = material;
    }

    // the same landing rules the gadgets use
    private bool WouldWork(RaycastHit hit)
    {
        if (wheel.Selected == Ability.Decoy) return DecoyGadget.CanStandOn(hit);
        return TeleportGadget.CanTeleportTo(hit, playerBody, solidMask, out _);
    }

    // uses one gadget and sends it flying from the eyes
    private void Throw()
    {
        if (!inventory.TryUse(wheel.Selected)) return;
        ThrownGadget prefab = wheel.Selected == Ability.Decoy ? decoyPrefab : teleportGadgetPrefab;
        ThrownGadget gadget = Instantiate(prefab, cameraTransform.position, Quaternion.identity);
        gadget.Launch(cameraTransform.position, cameraTransform.forward * settings.throwSpeed);
    }
}
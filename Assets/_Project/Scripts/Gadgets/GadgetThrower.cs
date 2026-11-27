using UnityEngine;

// hold right click to aim the teleport gadget or decoy along an arc and let go to throw
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInventory))]
public class GadgetThrower : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private AbilityWheel wheel;
    [SerializeField] private LineRenderer arcLine; // neutral color comes from its material
    [SerializeField] private ThrownGadget teleportGadgetPrefab;
    [SerializeField] private ThrownGadget decoyPrefab;

    private const float PreviewStep = 0.05f; // s of flight between arc points
    private const int PreviewSteps = 60; // so the preview shows 3 s of flight
    private const int HiddenSteps = 2; // first steps left out because a line right in front of the camera looks chunky

    private static readonly Vector3 HandOffset = new Vector3(0.3f, -0.25f, 0.2f); // m right down and forward of the eyes where throws leave the hand

    private PlayerControls controls;
    private PlayerInventory inventory;
    private int solidMask; // things a gadget can hit
    private bool isAiming;
    private readonly Vector3[] points = new Vector3[PreviewSteps + 1]; // arc points reused every frame

    // grab the inventory build the mask and hide the arc
    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        solidMask = ~LayerMask.GetMask("Player", "ShadowVolume", "Ignore Raycast");
        arcLine.enabled = false;
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

    // shows the arc and slows time only for the teleport gadget
    private void StartAim()
    {
        isAiming = true;
        arcLine.enabled = true;
        if (wheel.Selected == Ability.TeleportGadget) TimeScaleManager.Instance.Request(this, settings.aimTimeScale);
    }

    // hides the arc and drops the slow motion if there was any
    private void StopAim()
    {
        isAiming = false;
        arcLine.enabled = false;
        TimeScaleManager.Instance.Release(this);
    }

    // the right hand spot so the arc is seen from the side or the eyes if a wall is in the way
    private Vector3 ThrowOrigin()
    {
        Vector3 hand = cameraTransform.TransformPoint(HandOffset);
        bool blocked = Physics.Linecast(cameraTransform.position, hand, solidMask, QueryTriggerInteraction.Ignore);
        return blocked ? cameraTransform.position : hand;
    }

    // follows the arc in small steps until it hits something
    private void DrawArc()
    {
        Vector3 origin = ThrowOrigin();
        Vector3 velocity = cameraTransform.forward * settings.throwSpeed;
        points[0] = origin;
        int count = 1;
        bool landed = false;

        for (int i = 1; i <= PreviewSteps && !landed; i++)
        {
            Vector3 next = ThrownGadget.PointAt(origin, velocity, settings.projectileGravity, i * PreviewStep);
            landed = Physics.Linecast(points[count - 1], next, out RaycastHit hit, solidMask, QueryTriggerInteraction.Ignore);
            points[count] = landed ? hit.point : next;
            count++;
        }

        // draw from a little way out so the near end does not look chunky
        int first = Mathf.Min(HiddenSteps, count - 1);
        arcLine.positionCount = count - first;
        for (int i = first; i < count; i++) arcLine.SetPosition(i - first, points[i]);
    }

    // uses one gadget and sends it flying from the hand
    private void Throw()
    {
        if (!inventory.TryUse(wheel.Selected)) return;
        Vector3 origin = ThrowOrigin();
        ThrownGadget prefab = wheel.Selected == Ability.Decoy ? decoyPrefab : teleportGadgetPrefab;
        ThrownGadget gadget = Instantiate(prefab, origin, Quaternion.identity);
        gadget.Launch(origin, cameraTransform.forward * settings.throwSpeed);
        Sfx.Flat(Sfx.Bank.gadgetThrow);
    }
}
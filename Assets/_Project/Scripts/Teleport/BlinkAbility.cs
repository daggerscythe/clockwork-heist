using UnityEngine;

// hold right click to aim and let go to teleport anywhere out of lamplight
[RequireComponent(typeof(PlayerController))]
public class BlinkAbility : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private AbilityWheel wheel;
    [SerializeField] private Renderer marker;
    [SerializeField] private Material legalMaterial; // green
    [SerializeField] private Material illegalMaterial; // red

    private const float ShadowStep = 0.1f; // m between the checks back along the aim for the far edge of a shadow
    private const float FloorFacing = 0.5f; // a surface that faces up more than this is a floor and not a wall
    private const float MinAirDistance = 2f; // m air targets closer than this are skipped so aiming at a lit spot never hops the player a step

    private PlayerController player;
    private CharacterController controller;
    private PlayerControls controls;
    private int solidMask; // everything the player can bump into
    private bool isLegal;
    private bool landsOnFloor; // false when the target is on a wall or in the air so the player can grab a ledge there
    private Vector3 targetCenter; // where the middle of the capsule would end up
    private float readyTime; // game time when teleport can be used again

    // true while right click is held to aim
    public bool IsAiming { get; private set; }

    // grab components and build the layer masks
    private void Awake()
    {
        player = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();
        solidMask = ~LayerMask.GetMask("Player", "ShadowVolume", "Ignore Raycast");
        marker.gameObject.SetActive(false);
    }

    // input is created in the player controller awake so it is read here
    private void Start()
    {
        controls = player.Controls;
    }

    // never leave time slowed if this gets turned off mid aim
    private void OnDisable()
    {
        if (IsAiming) StopAim();
    }

    // aim while right click is held when teleport is the picked ability
    private void Update()
    {
        if (!IsAiming && wheel.Selected == Ability.Blink && !wheel.IsOpen && controls.Player.AimUse.WasPressedThisFrame())
            StartAim();
        if (!IsAiming) return;

        // left click cancels without teleporting
        if (controls.Player.Attack.WasPressedThisFrame())
        {
            StopAim();
            return;
        }

        FindTarget();
        ShowMarker();

        // letting go of right click teleports only when the target is legal
        if (!controls.Player.AimUse.IsPressed())
        {
            if (isLegal) Teleport();
            StopAim();
        }
    }

    // slows time and shows the marker
    private void StartAim()
    {
        IsAiming = true;
        TimeScaleManager.Instance.Request(this, settings.aimTimeScale);
        marker.gameObject.SetActive(true);
    }

    // puts time back to normal and hides the marker
    private void StopAim()
    {
        IsAiming = false;
        TimeScaleManager.Instance.Release(this);
        marker.gameObject.SetActive(false);
    }

    // works out where the player would land and if that spot is in shadow
    private void FindTarget()
    {
        Vector3 origin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward;
        bool ready = Time.time >= readyTime;
        bool hitSomething = Physics.Raycast(origin, direction, out RaycastHit hit, settings.blinkMaxDistance, solidMask, QueryTriggerInteraction.Ignore);
        float reach = hitSomething ? hit.distance : settings.blinkMaxDistance;

        // the spot against the surface the aim hits when that spot is in shadow
        if (hitSomething && TeleportSpot.Find(hit, controller.radius, controller.height, solidMask, out targetCenter) && InShadow(targetCenter))
        {
            isLegal = ready;
            landsOnFloor = hit.normal.y > FloorFacing;
            return;
        }

        // otherwise the farthest point of the aim still in shadow so the player can drop onto a ledge whose top they can't see
        for (float distance = reach - controller.radius; distance > MinAirDistance; distance -= ShadowStep)
        {
            Vector3 point = origin + direction * distance;
            if (!InShadow(point) || !TeleportSpot.FitsAt(point, controller.radius, controller.height, solidMask)) continue;
            targetCenter = point;
            isLegal = ready;
            landsOnFloor = false;
            return;
        }

        // no shadow along the aim so the red marker shows where the aim ends
        targetCenter = hitSomething ? hit.point + hit.normal * controller.radius : origin + direction * settings.blinkMaxDistance;
        isLegal = false;
    }

    // true when no part of the capsule at this center touches lamplight
    private bool InShadow(Vector3 center)
    {
        Vector3 toEnd = Vector3.up * (controller.height / 2f - controller.radius);
        return !Lamplight.IsLit(center) && !Lamplight.IsLit(center + toEnd) && !Lamplight.IsLit(center - toEnd);
    }

    // moves the marker to the target and colors it green or red
    private void ShowMarker()
    {
        // a unity capsule is 1 m wide and 2 m tall at scale 1
        marker.transform.position = targetCenter;
        marker.transform.localScale = new Vector3(controller.radius * 2f, controller.height / 2f, controller.radius * 2f);
        marker.sharedMaterial = isLegal ? legalMaterial : illegalMaterial;
    }

    // moves the player so their feet land under the target and starts the cooldown
    private void Teleport()
    {
        player.TeleportTo(targetCenter - Vector3.up * (controller.height / 2f));
        Sfx.Flat(Sfx.Bank.teleportWhoosh);
        readyTime = Time.time + settings.blinkCooldown;

        // arriving on a wall or in the air grabs a ledge in front like the player reached for it
        if (!landsOnFloor) player.TryVault();
    }
}
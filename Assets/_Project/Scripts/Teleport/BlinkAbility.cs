using UnityEngine;

// hold right click to aim and let go to teleport onto dark surfaces
[RequireComponent(typeof(PlayerController))]
public class BlinkAbility : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Renderer marker;
    [SerializeField] private Material legalMaterial; // green
    [SerializeField] private Material illegalMaterial; // red

    private const float SurfaceGap = 0.05f; // m between the capsule and the surface it lands on
    private const float NudgeStep = 0.1f; // m pushed away from the surface each try
    private const int NudgeTries = 3; // extra tries before the spot counts as blocked
    private const float LightCheckRadius = 0.05f; // m thickness of the light path check

    private PlayerController player;
    private CharacterController controller;
    private PlayerControls controls;
    private int solidMask; // everything the player can bump into
    private int lightMask; // only light volumes
    private int teleportableLayer;
    private bool isAiming;
    private bool isLegal;
    private Vector3 targetCenter; // where the middle of the capsule would end up
    private float readyTime; // game time when blink can be used again

    // grab components and build the layer masks
    private void Awake()
    {
        player = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();
        teleportableLayer = LayerMask.NameToLayer("Teleportable");
        lightMask = LayerMask.GetMask("LightVolume");
        solidMask = ~LayerMask.GetMask("Player", "LightVolume", "Ignore Raycast");
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
        if (isAiming) StopAim();
    }

    private void Update()
    {
        if (!isAiming && controls.Player.AimUse.WasPressedThisFrame())
            StartAim();
        if (!isAiming) return;

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
        isAiming = true;
        TimeScaleManager.Instance.Request(this, settings.aimTimeScale);
        marker.gameObject.SetActive(true);
    }

    // puts time back to normal and hides the marker
    private void StopAim()
    {
        isAiming = false;
        TimeScaleManager.Instance.Release(this);
        marker.gameObject.SetActive(false);
    }

    // works out where the player would land and if that spot is legal
    private void FindTarget()
    {
        Vector3 origin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward;
        float radius = controller.radius;
        float halfHeight = controller.height / 2f;

        // nothing in range so the marker floats at max distance as red
        if (!Physics.Raycast(origin, direction, out RaycastHit hit, settings.blinkMaxDistance, solidMask, QueryTriggerInteraction.Ignore))
        {
            targetCenter = origin + direction * settings.blinkMaxDistance;
            isLegal = false;
            return;
        }

        // push the capsule off the surface so it just touches it at any angle
        Vector3 normal = hit.normal;
        float offset = radius + (halfHeight - radius) * Mathf.Abs(normal.y) + SurfaceGap;
        Vector3 center = hit.point + normal * offset;

        // if it clips try a few small nudges away from the surface
        bool fits = CapsuleFits(center, radius, halfHeight);
        for (int i = 0; i < NudgeTries && !fits; i++)
        {
            center += normal * NudgeStep;
            fits = CapsuleFits(center, radius, halfHeight);
        }

        targetCenter = center;
        isLegal = hit.collider.gameObject.layer == teleportableLayer
            && fits
            && PathIsDark(origin, center)
            && Time.time >= readyTime;
    }

    // true when the player capsule fits here without touching anything
    private bool CapsuleFits(Vector3 center, float radius, float halfHeight)
    {
        Vector3 bottom = center - Vector3.up * (halfHeight - radius);
        Vector3 top = center + Vector3.up * (halfHeight - radius);
        return !Physics.CheckCapsule(bottom, top, radius, solidMask, QueryTriggerInteraction.Ignore);
    }

    // true when the line from the player to the target touches no light volume
    private bool PathIsDark(Vector3 from, Vector3 to)
    {
        return !Physics.CheckCapsule(from, to, LightCheckRadius, lightMask, QueryTriggerInteraction.Collide);
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
        readyTime = Time.time + settings.blinkCooldown;
    }
}
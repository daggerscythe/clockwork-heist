using UnityEngine;

// hold right click to aim the wrist dart and let go to fire a dart that knocks out a guard
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInventory))]
public class SleepDart : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private AbilityWheel wheel;
    [SerializeField] private Dart dartPrefab;

    private const float AimDistance = 100f; // m out to look for what the crosshair is on
    private static readonly Vector3 WristOffset = new Vector3(0.25f, -0.2f, 0.3f); // m right down and forward of the eyes where the launcher sits

    private PlayerControls controls;
    private PlayerInventory inventory;
    private int hitMask; // guards and anything that stops a dart
    private bool isAiming;

    // grab the inventory and build the mask
    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        hitMask = ~LayerMask.GetMask("Player", "ShadowVolume", "Ignore Raycast");
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

    // aim while right click is held when the dart is picked
    private void Update()
    {
        if (!isAiming && wheel.Selected == Ability.SleepDart && !wheel.IsOpen && controls.Player.AimUse.WasPressedThisFrame())
            StartAim();
        if (!isAiming) return;

        // left click cancels without firing
        if (controls.Player.Attack.WasPressedThisFrame())
        {
            StopAim();
            return;
        }

        // letting go of right click fires
        if (!controls.Player.AimUse.IsPressed())
        {
            Fire();
            StopAim();
        }
    }

    // slows time while aiming
    private void StartAim()
    {
        isAiming = true;
        TimeScaleManager.Instance.Request(this, settings.aimTimeScale);
    }

    // puts time back to normal
    private void StopAim()
    {
        isAiming = false;
        TimeScaleManager.Instance.Release(this);
    }

    // uses a dart and sends it from the wrist toward whatever the crosshair is on
    private void Fire()
    {
        if (!inventory.TryUse(Ability.SleepDart)) return;
        Sfx.Flat(Sfx.Bank.dartFire);

        Vector3 aimPoint = cameraTransform.position + cameraTransform.forward * AimDistance;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, AimDistance, hitMask, QueryTriggerInteraction.Ignore))
            aimPoint = hit.point;

        Vector3 origin = LaunchOrigin();
        Dart dart = Instantiate(dartPrefab);
        dart.Launch(origin, (aimPoint - origin).normalized * settings.dartSpeed);
    }

    // the wrist unless a wall is between it and the eyes and then the eyes
    private Vector3 LaunchOrigin()
    {
        Vector3 wrist = cameraTransform.TransformPoint(WristOffset);
        bool blocked = Physics.Linecast(cameraTransform.position, wrist, hitMask, QueryTriggerInteraction.Ignore);
        return blocked ? cameraTransform.position : wrist;
    }
}
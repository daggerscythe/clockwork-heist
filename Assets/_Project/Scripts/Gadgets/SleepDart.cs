using UnityEngine;

// hold right click to aim the wrist dart and let go to knock out a guard from range
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInventory))]
public class SleepDart : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private AbilityWheel wheel;

    private PlayerControls controls;
    private PlayerInventory inventory;
    private int hitMask; // guards and anything that stops a dart
    private bool isAiming;

    // grab the inventory and build the mask
    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        hitMask = ~LayerMask.GetMask("Player", "LightVolume", "Ignore Raycast");
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

    // uses a dart and knocks out a standing guard or makes a small noise where it misses
    private void Fire()
    {
        if (!inventory.TryUse(Ability.SleepDart)) return;

        // a shot into nothing is used up without a sound
        if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, settings.dartRange, hitMask, QueryTriggerInteraction.Ignore)) return;

        EnemyHealth target = hit.collider.GetComponentInParent<EnemyHealth>();
        if (target != null && target.IsAlive) target.KnockOut();
        else Noise.Make(hit.point, settings.dartMissNoiseRadius);
    }
}
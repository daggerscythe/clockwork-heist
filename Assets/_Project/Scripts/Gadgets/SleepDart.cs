using UnityEngine;

// hold right click to aim the wrist dart and let go to knock out a guard from range
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInventory))]
public class SleepDart : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private AbilityWheel wheel;
    [SerializeField] private Renderer marker;
    [SerializeField] private Material legalMaterial; // green when aimed at a standing guard
    [SerializeField] private Material illegalMaterial; // red anywhere else

    private PlayerControls controls;
    private PlayerInventory inventory;
    private int hitMask; // guards and anything that stops a dart
    private bool isAiming;

    // grab the inventory build the mask and hide the marker
    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        hitMask = ~LayerMask.GetMask("Player", "LightVolume", "Ignore Raycast");
        marker.gameObject.SetActive(false);
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

        bool hasHit = Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, settings.dartRange, hitMask, QueryTriggerInteraction.Ignore);
        EnemyHealth target = hasHit ? hit.collider.GetComponentInParent<EnemyHealth>() : null;
        bool onGuard = target != null && target.IsAlive;

        marker.transform.position = hasHit ? hit.point : cameraTransform.position + cameraTransform.forward * settings.dartRange;
        marker.sharedMaterial = onGuard ? legalMaterial : illegalMaterial;

        // letting go of right click fires
        if (!controls.Player.AimUse.IsPressed())
        {
            Fire(hasHit, hit.point, onGuard ? target : null);
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

    // uses a dart and knocks out a guard on a hit or makes a small noise where it misses
    private void Fire(bool hasHit, Vector3 point, EnemyHealth target)
    {
        if (!inventory.TryUse(Ability.SleepDart)) return;
        if (target != null) target.KnockOut();
        else if (hasHit) Noise.Make(point, settings.dartMissNoiseRadius);
    }
}
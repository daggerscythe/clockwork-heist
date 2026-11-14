using UnityEngine;

// q swaps blade and club and left click swings with a silent takedown from behind
[RequireComponent(typeof(PlayerController))]
public class MeleeController : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private WeaponViewmodel viewmodel;

    private const float HitRadius = 0.3f; // m thickness of the swing check so it is easy to land

    private PlayerController player;
    private PlayerControls controls;
    private int hitMask; // guards and anything that blocks a swing
    private bool bladeOut = true; // false means the club is out
    private float readyTime; // game time when the next swing or switch is allowed

    // grab the controller and build the mask
    private void Awake()
    {
        player = GetComponent<PlayerController>();
        hitMask = ~LayerMask.GetMask("Player", "LightVolume", "Ignore Raycast");
    }

    // input is created in the player controller awake so it is read here
    private void Start()
    {
        controls = player.Controls;
        viewmodel.Show(bladeOut);
    }

    // switch or swing when ready
    private void Update()
    {
        // the weapon is put away while carrying a body
        viewmodel.gameObject.SetActive(!player.IsCarrying);
        if (player.IsCarrying || Time.time < readyTime) return;

        if (controls.Player.ToggleWeapon.WasPressedThisFrame())
        {
            bladeOut = !bladeOut;
            viewmodel.PlaySwitch(bladeOut, settings.weaponSwitchTime);
            readyTime = Time.time + settings.weaponSwitchTime;
            return;
        }

        // right click held means left click belongs to aiming
        if (controls.Player.Attack.WasPressedThisFrame() && !controls.Player.AimUse.IsPressed())
            Swing();
    }

    // swings the weapon and hits the guard in front if there is one
    private void Swing()
    {
        float swingTime = bladeOut ? settings.bladeSwingTime : settings.clubSwingTime;
        viewmodel.PlaySwing(swingTime);
        readyTime = Time.time + swingTime;

        if (!Physics.SphereCast(cameraTransform.position, HitRadius, cameraTransform.forward, out RaycastHit hit, settings.takedownRange, hitMask, QueryTriggerInteraction.Ignore)) return;

        EnemyHealth target = hit.collider.GetComponentInParent<EnemyHealth>();
        if (target == null || !target.IsAlive) return;

        // unaware and the player is outside its cone so this is a silent takedown
        if (target.IsUnaware(cameraTransform.position))
        {
            if (bladeOut) target.Kill();
            else target.KnockOut();
            return;
        }

        // open combat only works with the blade
        if (bladeOut) target.TakeDamage(settings.bladeDamage);
    }
}
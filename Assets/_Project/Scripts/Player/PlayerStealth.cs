using UnityEngine;

// reports how loud the player is and how easy they are to spot
[RequireComponent(typeof(PlayerController))]
public class PlayerStealth : MonoBehaviour
{
    [SerializeField] private GameSettings settings;

    private const float ChestProbe = 0.1f; // m radius of the shadow check at the chest

    private PlayerController player;
    private CharacterController controller;
    private int shadowMask; // only shadow volumes

    // how far away enemies can hear player
    public float NoiseRadius { get; private set; }

    // how many times longer enemies take to spot the player right now
    public float DetectTimeMultiplier { get; private set; }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();
        shadowMask = LayerMask.GetMask("ShadowVolume");
    }

    // work out the noise and visibility every frame
    private void Update()
    {
        NoiseRadius = GetNoiseRadius();
        DetectTimeMultiplier = GetDetectTimeMultiplier();

        // footsteps reach every guard inside the noise radius
        if (NoiseRadius > 0f) Noise.Make(transform.position, NoiseRadius);
    }

    // standing still is silent and moving depends on movement
    private float GetNoiseRadius()
    {
        if (!player.IsMoving) return 0f;
        if (player.IsCrouching) return settings.noiseCrouch;
        if (player.IsSprinting) return settings.noiseSprint;
        return settings.noiseWalk;
    }

    // shadow and crouching make the player slower to spot and sprinting makes them faster
    private float GetDetectTimeMultiplier()
    {
        float multiplier = IsInShadow() ? settings.darkTimeMultiplier : 1f;
        if (player.IsCrouching) multiplier *= settings.crouchTimeMultiplier;
        else if (player.IsSprinting) multiplier *= settings.sprintTimeMultiplier;
        return multiplier;
    }

    // true when the middle of the player is inside a shadow volume
    private bool IsInShadow()
    {
        Vector3 chest = transform.position + Vector3.up * (controller.height / 2f);
        return Physics.CheckSphere(chest, ChestProbe, shadowMask, QueryTriggerInteraction.Collide);
    }

    // draws the noise ring in the scene view when selected
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, NoiseRadius);
    }
}
using UnityEngine;

// reports how loud the player is and how easy they are to spot
[RequireComponent(typeof(PlayerController))]
public class PlayerStealth : MonoBehaviour
{
    [SerializeField] private GameSettings settings;

    private const float HeadBelowTop = 0.2f; // m from the top of the capsule to the eyes

    private PlayerController player;
    private CharacterController controller;

    // how far away enemies can hear player
    public float NoiseRadius { get; private set; }

    // how many times longer enemies take to spot the player right now
    public float DetectTimeMultiplier { get; private set; }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();
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

    // true when no lamp reaches the player's head or chest the two points guards look for
    private bool IsInShadow()
    {
        Vector3 chest = transform.position + Vector3.up * (controller.height / 2f);
        Vector3 head = transform.position + Vector3.up * (controller.height - HeadBelowTop);
        return !Lamplight.IsLit(chest) && !Lamplight.IsLit(head);
    }

    // draws the noise ring in the scene view when selected
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, NoiseRadius);
    }
}
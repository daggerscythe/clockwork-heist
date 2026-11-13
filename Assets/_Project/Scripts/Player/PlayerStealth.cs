using UnityEngine;

// reports how loud the player is and how easy they are to spot
[RequireComponent(typeof(PlayerController))]
public class PlayerStealth : MonoBehaviour
{
    [SerializeField] private GameSettings settings;

    private PlayerController player;
    private CharacterController controller;
    private int lightMask; // only light volumes

    // how far away enemies can hear player
    public float NoiseRadius { get; private set; }

    // how many times longer enemies take to spot the player right now
    public float DetectTimeMultiplier { get; private set; }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();
        lightMask = LayerMask.GetMask("LightVolume");
    }

    // work out the noise and visibility every frame
    private void Update()
    {
        NoiseRadius = GetNoiseRadius();
        DetectTimeMultiplier = GetDetectTimeMultiplier();
    }

    // standing still is silent and moving depends on movement
    private float GetNoiseRadius()
    {
        if (!player.IsMoving) return 0f;
        if (player.IsCrouching) return settings.noiseCrouch;
        if (player.IsSprinting) return settings.noiseSprint;
        return settings.noiseWalk;
    }

    // darkness and crouching make the player slower to spot and sprinting makes them faster
    private float GetDetectTimeMultiplier()
    {
        float multiplier = IsInLight() ? 1f : settings.darkTimeMultiplier;
        if (player.IsCrouching) multiplier *= settings.crouchTimeMultiplier;
        else if (player.IsSprinting) multiplier *= settings.sprintTimeMultiplier;
        return multiplier;
    }

    // true when any part of the player capsule touches a light volume
    private bool IsInLight()
    {
        Vector3 bottom = transform.position + Vector3.up * controller.radius;
        Vector3 top = transform.position + Vector3.up * (controller.height - controller.radius);
        return Physics.CheckCapsule(bottom, top, controller.radius, lightMask, QueryTriggerInteraction.Collide);
    }

    // draws the noise ring in the scene view when selected
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, NoiseRadius);
    }
}
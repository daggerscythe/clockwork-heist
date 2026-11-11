using UnityEngine;

// reports how loud the player is
[RequireComponent(typeof(PlayerController))]
public class PlayerStealth : MonoBehaviour
{
    [SerializeField] private GameSettings settings;

    private PlayerController player;

    // how far away enemies can hear player
    public float NoiseRadius { get; private set; }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    // work out the noise every frame
    private void Update()
    {
        NoiseRadius = GetNoiseRadius();
    }

    // standing still is silent and moving depends on movement
    private float GetNoiseRadius()
    {
        if (!player.IsMoving) return 0f;
        if (player.IsCrouching) return settings.noiseCrouch;
        if (player.IsSprinting) return settings.noiseSprint;
        return settings.noiseWalk;
    }

    // draws the noise ring in the scene view when selected
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, NoiseRadius);
    }
}
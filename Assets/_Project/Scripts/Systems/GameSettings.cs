using UnityEngine;

// All tunable values for Clockwork Heist
[CreateAssetMenu(fileName = "GameSettings", menuName = "Clockwork Heist/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Player")]
    public float playerWalkSpeed = 3f; // m/s
    public float playerCrouchSpeed = 2f; // m/s
    public float playerSprintSpeed = 5f; // m/s
    public float playerMaxHealth = 100f;
    public float capsuleRadius = 0.35f; // m
    public float capsuleHeight = 1.8f; // m
    public float crouchCapsuleHeight = 1.1f; // m

    [Header("Teleport (Blink)")]
    public float blinkMaxDistance = 15f; // m
    public float blinkCooldown = 3f; // s
    public float aimTimeScale = 0.1f; // time scale while aiming

    [Header("Thrown gadgets")]
    public float throwSpeed = 14f; // m/s
    public float projectileGravity = 9.81f; // m/s^2
    public float stuckGadgetDespawnSeconds = 300f; // 5 min of not being activated or picked up

    [Header("Ability wheel")]
    public float wheelTimeScale = 0.1f;

    [Header("Enemies")]
    public float visionConeAngle = 60f; // degrees front facing
    public float visionMaxDistance = 15f; // m
    public float enemyWalkSpeed = 2f; // m/s
    public float enemyRunSpeed = 5f; // m/s
    public float enemyMaxHealth = 100f;
    public float enemyHitDamage = 20f;
    public float enemyAttackRange = 1.5f; // m
    public float enemyAttackInterval = 1f; // s
    public float confusionTime = 5f; // s
    public float alertShoutRadius = 12f; // m
    public float bodySearchTime = 10f; // s after seeing a body
    public float wakeSearchTime = 5f; // s after waking up

    [Header("Detection (multipliers apply to time-to-detect)")]
    public float detectTimeClose = 0.5f; // s
    public float detectTimeFar = 1.5f; // s
    public float closeDistance = 5f; // m
    public float darkTimeMultiplier = 2f; // in dark zone detection takes 2x longer
    public float crouchTimeMultiplier = 1.5f;
    public float sprintTimeMultiplier = 0.7f;
    public float meterDrainPerSecond = 1f;

    [Header("Noise radii (m)")]
    public float noiseCrouch = 0f;
    public float noiseWalk = 4f;
    public float noiseSprint = 10f;
    public float noiseBodyDrop = 3f;

    [Header("Decoy")]
    public float decoySoundRadius = 12f; // m
    public float decoyConfusionTime = 4f; // s

    [Header("Security cameras")]
    public float cameraConeAngle = 60f; // degrees
    public float cameraRange = 12f; // m
    public float cameraAlertRadius = 20f; // m

    [Header("Camera hacking minigame")]
    public float hackMemorizeTime = 3f; // s
    public float hackSolveTime = 12f; // s
    public int hackDifferences = 3;
    public float hackRetryDelay = 10f; // s

    [Header("Combat")]
    public float bladeDamage = 25f;
    public float takedownRange = 1.5f; // m
    public float clubSwingTime = 0.4f; // s
    public float weaponSwitchTime = 0.3f; // s
    public float knockoutDuration = 60f; // s until the enemy wakes
    public float pickpocketHoldTime = 1.5f; // s

    [Header("Sleep Dart")]
    public float dartRange = 20f; // m
    public float dartMissNoiseRadius = 4f;// m

    [Header("Whistleblower")]
    public float lostSightTime = 5f; // s
    public float alarmPullTime = 1.5f; // s
}
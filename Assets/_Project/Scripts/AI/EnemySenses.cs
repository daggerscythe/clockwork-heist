using UnityEngine;

// lets a guard see the player and fills its detection meter
public class EnemySenses : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform eyes;

    private const float HeadBelowTop = 0.2f; // m from the top of the player capsule to their eyes

    private PlayerStealth playerStealth;
    private CharacterController playerBody;
    private int sightMask; // things that block the view

    // 0 means unaware and 1 means the player is fully detected
    public float Detection { get; private set; }

    // find the player and build the mask
    private void Awake()
    {
        GameObject player = GameObject.FindWithTag("Player");
        playerStealth = player.GetComponent<PlayerStealth>();
        playerBody = player.GetComponent<CharacterController>();
        sightMask = ~LayerMask.GetMask("Player", "Enemy", "LightVolume", "Ignore Raycast");
    }

    // fill the meter while the player is seen and drain it when not
    private void Update()
    {
        if (CanSeePlayer(out float distance))
            Detection += Time.deltaTime / TimeToDetect(distance);
        else
            Detection -= settings.meterDrainPerSecond * Time.deltaTime;

        Detection = Mathf.Clamp01(Detection);
    }

    // true when the player is in range and their head or chest is in the cone with nothing in the way
    private bool CanSeePlayer(out float distance)
    {
        Vector3 feet = playerBody.transform.position;
        Vector3 head = feet + Vector3.up * (playerBody.height - HeadBelowTop);
        Vector3 chest = feet + Vector3.up * (playerBody.height / 2f);

        distance = Vector3.Distance(eyes.position, chest);
        if (distance > settings.visionMaxDistance) return false;

        return IsVisible(head) || IsVisible(chest);
    }

    // one point counts when it is inside the cone and the line to it is clear
    private bool IsVisible(Vector3 point)
    {
        if (Vector3.Angle(eyes.forward, point - eyes.position) > settings.visionConeAngle / 2f) return false;
        return !Physics.Linecast(eyes.position, point, sightMask, QueryTriggerInteraction.Ignore);
    }

    // s to fill the meter at this distance after the player stealth multiplier
    private float TimeToDetect(float distance)
    {
        // flat inside the close distance then a straight line out to max range
        float t = Mathf.InverseLerp(settings.closeDistance, settings.visionMaxDistance, distance);
        float baseTime = Mathf.Lerp(settings.detectTimeClose, settings.detectTimeFar, t);
        return baseTime * playerStealth.DetectTimeMultiplier;
    }

    // draws the edges of the vision cone in the scene view when selected
    private void OnDrawGizmosSelected()
    {
        if (eyes == null || settings == null) return;

        float half = settings.visionConeAngle / 2f;
        Vector3 reach = eyes.forward * settings.visionMaxDistance;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(eyes.position, Quaternion.AngleAxis(half, eyes.up) * reach);
        Gizmos.DrawRay(eyes.position, Quaternion.AngleAxis(-half, eyes.up) * reach);
        Gizmos.DrawRay(eyes.position, Quaternion.AngleAxis(half, eyes.right) * reach);
        Gizmos.DrawRay(eyes.position, Quaternion.AngleAxis(-half, eyes.right) * reach);
    }
}
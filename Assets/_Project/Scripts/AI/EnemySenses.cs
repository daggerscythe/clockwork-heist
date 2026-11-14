using System.Collections.Generic;
using UnityEngine;

// lets a guard see the player and bodies and fills its detection meter
public class EnemySenses : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform eyes;

    private const float HeadBelowTop = 0.2f; // m from the top of the player capsule to their eyes
    private const float BodyHeight = 0.4f; // m above the floor a lying body is looked at

    private PlayerStealth playerStealth;
    private CharacterController playerBody;
    private int sightMask; // things that block the view
    private readonly HashSet<EnemyHealth> knownBodies = new HashSet<EnemyHealth>(); // bodies this guard already reacted to

    // 0 means unaware and 1 means the player is fully detected
    public float Detection { get; private set; }

    // true on frames the guard can see the player
    public bool SeesPlayer { get; private set; }

    // where the player was standing the last time this guard saw them
    public Vector3 LastSeenPosition { get; private set; }

    // a downed guard seen for the first time this frame or null
    public EnemyHealth NewBody { get; private set; }

    // find the player and build the mask
    private void Awake()
    {
        GameObject player = GameObject.FindWithTag("Player");
        playerStealth = player.GetComponent<PlayerStealth>();
        playerBody = player.GetComponent<CharacterController>();
        sightMask = ~LayerMask.GetMask("Player", "Enemy", "LightVolume", "Ignore Raycast");
    }

    // a guard that wakes back up starts with an empty meter
    private void OnEnable()
    {
        Detection = 0f;
        SeesPlayer = false;
        NewBody = null;
    }

    // fill the meter while the player is seen and drain it when not and look for bodies
    private void Update()
    {
        SeesPlayer = CanSeePlayer(out float distance);

        if (SeesPlayer)
        {
            LastSeenPosition = playerBody.transform.position;
            Detection += Time.deltaTime / TimeToDetect(distance);
        }
        else
        {
            Detection -= settings.meterDrainPerSecond * Time.deltaTime;
        }

        Detection = Mathf.Clamp01(Detection);
        NewBody = FindNewBody();
    }

    // a hit from the player fills the meter and gives away where they are
    public void NoticePlayer()
    {
        Detection = 1f;
        LastSeenPosition = playerBody.transform.position;
    }

    // true when a point is inside the vision cone no matter what is in the way
    public bool InCone(Vector3 point)
    {
        return Vector3.Angle(eyes.forward, point - eyes.position) <= settings.visionConeAngle / 2f;
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
        return InCone(point) && !Physics.Linecast(eyes.position, point, sightMask, QueryTriggerInteraction.Ignore);
    }

    // looks for a knocked out or dead guard in view that this guard has not reacted to yet
    private EnemyHealth FindNewBody()
    {
        foreach (EnemyHealth other in EnemyHealth.All)
        {
            if (!other.IsDown || other.IsHidden || knownBodies.Contains(other)) continue;

            Vector3 point = other.transform.position + Vector3.up * BodyHeight;
            if (Vector3.Distance(eyes.position, point) > settings.visionMaxDistance || !IsVisible(point)) continue;

            knownBodies.Add(other);
            return other;
        }
        return null;
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
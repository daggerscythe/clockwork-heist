using System.Collections.Generic;
using UnityEngine;

// a wall camera that sweeps watches for the player calls nearby guards and can be hacked
public class SecurityCamera : MonoBehaviour, ICheckpointState
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform head; // part that turns side to side
    [SerializeField] private MeshFilter lens; // where it looks from and where the see through cone is drawn
    [SerializeField] private Camera feedCamera; // draws the feed for the hack
    [SerializeField] private Transform[] props; // things in view the hack can change and each needs a collider
    [SerializeField] private float tilt = 30f; // degrees the head looks down
    [SerializeField] private float sweepAngle = 45f; // degrees to each side of where it was placed facing
    [SerializeField] private float sweepTime = 8f; // s for one sweep there and back
    [SerializeField] private Color calmColor = new Color(1f, 1f, 1f, 0.08f); // cone while it sees nothing
    [SerializeField] private Color spottingColor = new Color(1f, 0.85f, 0.2f, 0.2f); // cone with a full meter
    [SerializeField] private Color reportingColor = new Color(1f, 0.2f, 0.2f, 0.25f); // cone while it calls guards
    [SerializeField] private Color hackedColor = new Color(0.3f, 1f, 0.4f, 0.1f); // cone once hacked

    private const float HeadBelowTop = 0.2f; // m from the top of the player capsule to their eyes
    private const int ConeSides = 24; // points around the end of the cone

    // every active camera so the haze can check them
    public static readonly List<SecurityCamera> All = new List<SecurityCamera>();

    private PlayerStealth playerStealth;
    private CharacterController playerBody;
    private Material coneMaterial; // this camera's own copy so cameras color their cones apart
    private int sightMask; // things that block the view
    private int enemyMask; // only guards
    private float sweepClock; // s into the sweep
    private float reportEndTime; // game time the camera sweeps again after calling guards
    private Vector3 lastSeenPosition; // player feet the last time it saw them
    private bool savedHacked; // true if it was hacked at the last checkpoint

    // 0 means it has not noticed the player and 1 means it calls the guards
    public float Detection { get; private set; }

    // true on frames the player is in view
    public bool SeesPlayer { get; private set; }

    // true once hacked so it can never see the player again
    public bool IsHacked { get; private set; }

    // game time another hack try is allowed after a failure
    public float RetryTime { get; private set; }

    // true when the retry wait is over
    public bool CanRetry => Time.time >= RetryTime;

    // the camera that draws the hack feed
    public Camera FeedCamera => feedCamera;

    // things in view the hack can change
    public Transform[] Props => props;

    // find the player build the cone and aim the head
    private void Awake()
    {
        GameObject player = GameObject.FindWithTag("Player");
        playerStealth = player.GetComponent<PlayerStealth>();
        playerBody = player.GetComponent<CharacterController>();
        sightMask = ~LayerMask.GetMask("Player", "Enemy", "LightVolume", "Ignore Raycast", "Interactable");
        enemyMask = LayerMask.GetMask("Enemy");

        lens.mesh = BuildCone();
        coneMaterial = lens.GetComponent<MeshRenderer>().material;
        coneMaterial.color = calmColor;
        feedCamera.enabled = false;
        AimHead();
    }

    // join the list while in the world
    private void OnEnable()
    {
        All.Add(this);
    }

    // leave the list when the floor unloads
    private void OnDisable()
    {
        All.Remove(this);
    }

    // looks for the player then fills the meter or sweeps and calls guards once the meter is full
    private void Update()
    {
        // while the player holds its feed the camera is theirs so it sees nothing and holds still
        if (feedCamera.enabled)
        {
            SeesPlayer = false;
            Detection = 0f;
            return;
        }

        SeesPlayer = CanSeePlayer(out float distance);

        if (IsHacked)
        {
            Sweep();
            coneMaterial.color = hackedColor;
            return;
        }

        // holds still on the spot it reported for a moment
        if (Time.time < reportEndTime) return;

        if (SeesPlayer)
        {
            lastSeenPosition = playerBody.transform.position;
            Detection += Time.deltaTime / TimeToDetect(distance);
        }
        else
        {
            Detection -= settings.meterDrainPerSecond * Time.deltaTime;
            Sweep();
        }

        Detection = Mathf.Clamp01(Detection);
        coneMaterial.color = Color.Lerp(calmColor, spottingColor, Detection);
        if (Detection >= 1f) Report();
    }

    // a won hack blinds the camera to the player for good
    public void Hack()
    {
        IsHacked = true;
        Detection = 0f;
        reportEndTime = 0f;
    }

    // a lost hack makes the player wait before trying this camera again
    public void FailHack()
    {
        RetryTime = Time.time + settings.hackRetryDelay;
    }

    // turns to the middle of the sweep and starts drawing the feed
    public void StartFeed(RenderTexture target)
    {
        sweepClock = 0f;
        AimHead();
        feedCamera.targetTexture = target;
        feedCamera.enabled = true;
    }

    // stops drawing the feed
    public void StopFeed()
    {
        feedCamera.enabled = false;
        feedCamera.targetTexture = null;
    }

    // remember if it was hacked at the checkpoint
    public void SaveState()
    {
        savedHacked = IsHacked;
    }

    // back to hacked or not as at the checkpoint with an empty meter and no wait
    public void RestoreState()
    {
        IsHacked = savedHacked;
        Detection = 0f;
        reportEndTime = 0f;
        RetryTime = 0f;
    }

    // guards in range come to check where the player was seen but still have to spot them themselves
    private void Report()
    {
        Detection = 0f;
        reportEndTime = Time.time + settings.cameraReportTime;
        coneMaterial.color = reportingColor;

        Collider[] hits = Physics.OverlapSphere(transform.position, settings.cameraAlertRadius, enemyMask, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            EnemyAI guard = hit.GetComponentInParent<EnemyAI>();
            if (guard != null) guard.Investigate(lastSeenPosition);
        }
    }

    // moves the sweep clock on and turns the head to match
    private void Sweep()
    {
        sweepClock += Time.deltaTime;
        AimHead();
    }

    // a sine wave turn so the head slows down at each end
    private void AimHead()
    {
        float yaw = sweepAngle * Mathf.Sin(sweepClock * 2f * Mathf.PI / sweepTime);
        head.localRotation = Quaternion.Euler(tilt, yaw, 0f);
    }

    // true when the player is in range and their head or chest is in the cone with nothing in the way
    private bool CanSeePlayer(out float distance)
    {
        Vector3 feet = playerBody.transform.position;
        Vector3 playerHead = feet + Vector3.up * (playerBody.height - HeadBelowTop);
        Vector3 chest = feet + Vector3.up * (playerBody.height / 2f);

        distance = Vector3.Distance(lens.transform.position, chest);
        if (distance > settings.cameraRange) return false;

        return IsVisible(playerHead) || IsVisible(chest);
    }

    // one point counts when it is inside the cone and the line to it is clear
    private bool IsVisible(Vector3 point)
    {
        Vector3 eye = lens.transform.position;
        bool inCone = Vector3.Angle(lens.transform.forward, point - eye) <= settings.cameraConeAngle / 2f;
        return inCone && !Physics.Linecast(eye, point, sightMask, QueryTriggerInteraction.Ignore);
    }

    // s to fill the meter at this distance after the player stealth multiplier
    private float TimeToDetect(float distance)
    {
        float t = Mathf.InverseLerp(settings.closeDistance, settings.cameraRange, distance);
        float baseTime = Mathf.Lerp(settings.detectTimeClose, settings.detectTimeFar, t);
        return baseTime * playerStealth.DetectTimeMultiplier;
    }

    // a cone with its tip at the lens whose edges are as long as the camera range
    private Mesh BuildCone()
    {
        float half = settings.cameraConeAngle / 2f * Mathf.Deg2Rad;
        float radius = settings.cameraRange * Mathf.Sin(half);
        float length = settings.cameraRange * Mathf.Cos(half);

        // point 0 is the tip at the lens and the rest make the ring at the far end
        Vector3[] vertices = new Vector3[ConeSides + 1];
        int[] triangles = new int[ConeSides * 3];
        for (int i = 0; i < ConeSides; i++)
        {
            float angle = i * 2f * Mathf.PI / ConeSides;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, length);
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % ConeSides + 1;
        }

        Mesh mesh = new Mesh { name = "CameraCone", vertices = vertices, triangles = triangles };
        mesh.RecalculateBounds();
        return mesh;
    }

    // draws the middle and both ends of the sweep in the scene view when selected
    private void OnDrawGizmosSelected()
    {
        if (lens == null || settings == null) return;

        Gizmos.color = Color.yellow;
        foreach (float yaw in new[] { -sweepAngle, 0f, sweepAngle })
        {
            Vector3 direction = transform.rotation * Quaternion.Euler(tilt, yaw, 0f) * Vector3.forward;
            Gizmos.DrawRay(lens.transform.position, direction * settings.cameraRange);
        }
    }
}
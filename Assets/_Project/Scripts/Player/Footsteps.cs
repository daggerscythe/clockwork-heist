using UnityEngine;

// plays footsteps for the floor under the feet as the player or a guard moves
public class Footsteps : MonoBehaviour
{
    [SerializeField] private PlayerController player; // set on the player and left empty on guards

    private const float FootLift = 0.3f; // m above the feet where the floor check starts
    private const float FloorCheck = 0.6f; // m down from there to look for the floor
    private const float TeleportDistance = 1f; // m moved in one frame that counts as a teleport not a step
    private const float LandAfter = 0.3f; // s in the air before touching down counts as a landing
    private const float GuardRunSpeed = 3.5f; // m/s above which a guard is running
    private const float PitchSpread = 0.05f; // each step is pitched up or down by up to this much

    private int floorMask; // floors walls and props
    private Vector3 lastPosition;
    private float distance; // m moved since the last step
    private float airTime; // s the player has been off the ground

    // build the mask
    private void Awake()
    {
        floorMask = ~LayerMask.GetMask("Player", "Enemy", "LightVolume", "Ignore Raycast", "Interactable");
    }

    // start counting from here after being carried dropped or switched back on
    private void OnEnable()
    {
        lastPosition = transform.position;
        distance = 0f;
    }

    // adds up the distance moved along the ground and steps once every step length
    private void Update()
    {
        if (Time.deltaTime <= 0f) return;

        Vector3 moved = transform.position - lastPosition;
        moved.y = 0f;
        lastPosition = transform.position;

        // the player only steps on the ground and lands after a short fall
        if (player != null)
        {
            if (!player.IsGrounded)
            {
                airTime += Time.deltaTime;
                return;
            }
            if (airTime > LandAfter) Sfx.Flat(Sfx.Bank.land);
            airTime = 0f;
        }

        // a teleport or a warp is not a step
        float step = moved.magnitude;
        if (step > TeleportDistance) return;

        bool running = player != null ? player.IsSprinting : step / Time.deltaTime > GuardRunSpeed;
        distance += step;
        if (distance < (running ? Sfx.Bank.runStepLength : Sfx.Bank.walkStepLength)) return;

        distance = 0f;
        Step(running);
    }

    // one footstep for the floor underfoot flat for the player and placed in the world for a guard
    private void Step(bool running)
    {
        Surface kind = Surface.Marble;
        if (Physics.Raycast(transform.position + Vector3.up * FootLift, Vector3.down, out RaycastHit hit, FloorCheck, floorMask, QueryTriggerInteraction.Ignore))
        {
            FloorSurface floor = hit.collider.GetComponentInParent<FloorSurface>();
            if (floor != null) kind = floor.Kind;
        }

        AudioClip clip = Sfx.Pick(Sfx.Bank.StepsFor(kind));
        float pitch = 1f + Random.Range(-PitchSpread, PitchSpread);

        if (player == null)
        {
            float guardVolume = running ? Sfx.Bank.runStepVolume : Sfx.Bank.walkStepVolume;
            Sfx.At(clip, transform.position, Sfx.Bank.sfxRange, guardVolume, pitch);
            return;
        }

        float volume = Sfx.Bank.walkStepVolume;
        if (player.IsCrouching) volume = Sfx.Bank.crouchStepVolume;
        else if (running) volume = Sfx.Bank.runStepVolume;
        Sfx.Flat(clip, volume, pitch);
    }
}
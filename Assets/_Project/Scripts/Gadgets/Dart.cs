using UnityEngine;

// a sleep dart in flight that drops a little over distance and knocks out the first standing guard it hits
public class Dart : MonoBehaviour
{
    [SerializeField] private GameSettings settings;

    private const float MaxFlightTime = 3f; // s before a dart that hit nothing is removed
    private const float StayTime = 5f; // s a dart stays stuck where it landed

    private Vector3 start; // where it left the wrist
    private Vector3 launchVelocity; // m/s at launch
    private float flightTime; // s since launch
    private bool landed;
    private int hitMask; // guards and anything that stops a dart

    // build the mask
    private void Awake()
    {
        hitMask = ~LayerMask.GetMask("Player", "ShadowVolume", "Ignore Raycast");
    }

    // sends the dart off from a point at a launch velocity
    public void Launch(Vector3 from, Vector3 velocity)
    {
        start = from;
        launchVelocity = velocity;
        transform.SetPositionAndRotation(from, Quaternion.LookRotation(velocity));
    }

    // moves along the arc and checks the stretch it covered this frame for a hit
    private void Update()
    {
        if (landed) return;

        flightTime += Time.deltaTime;
        Vector3 next = PointAt(flightTime);

        if (Physics.Linecast(transform.position, next, out RaycastHit hit, hitMask, QueryTriggerInteraction.Ignore))
        {
            Land(hit);
            return;
        }

        transform.SetPositionAndRotation(next, Quaternion.LookRotation(VelocityAt(flightTime)));
        if (flightTime >= MaxFlightTime) Destroy(gameObject);
    }

    // where the dart is a number of s after launch
    private Vector3 PointAt(float time)
    {
        return start + launchVelocity * time + Vector3.down * (0.5f * settings.dartGravity * time * time);
    }

    // which way and how fast the dart is going a number of s after launch
    private Vector3 VelocityAt(float time)
    {
        return launchVelocity + Vector3.down * (settings.dartGravity * time);
    }

    // knocks out a standing guard or sticks in whatever it hit with a small noise
    private void Land(RaycastHit hit)
    {
        landed = true;
        transform.position = hit.point;

        // the dart goes into the guard so it is gone straight away
        EnemyHealth target = hit.collider.GetComponentInParent<EnemyHealth>();
        if (target != null && target.IsAlive)
        {
            Sfx.At(Sfx.Bank.dartHitBody, hit.point);
            target.KnockOut();
            Destroy(gameObject);
            return;
        }

        Sfx.At(Sfx.Bank.dartHitSurface, hit.point);
        Noise.Make(hit.point, settings.dartMissNoiseRadius);
        Destroy(gameObject, StayTime);
    }
}
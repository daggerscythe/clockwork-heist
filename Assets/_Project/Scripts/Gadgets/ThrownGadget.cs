using UnityEngine;

// flies along an arc and lands on the first thing it hits
public abstract class ThrownGadget : MonoBehaviour
{
    [SerializeField] protected GameSettings settings;

    private const float FlightLimit = 5f; // s of flying without a hit before it counts as out of the level

    protected int solidMask; // things a gadget can hit
    protected GameObject playerObject;
    private PlayerInventory inventory;
    private Vector3 start;
    private Vector3 velocity; // m/s at the moment of the throw
    private float flightTime; // s since the throw
    private bool isFlying;

    // where a throw is a given time after leaving the hand
    public static Vector3 PointAt(Vector3 from, Vector3 throwVelocity, float gravity, float time)
    {
        return from + throwVelocity * time + Vector3.down * (0.5f * gravity * time * time);
    }

    // which gadget this is so it can be given back
    protected abstract Ability Kind { get; }

    // build the mask and find the player
    protected virtual void Awake()
    {
        solidMask = ~LayerMask.GetMask("Player", "ShadowVolume", "Ignore Raycast");
        playerObject = GameObject.FindWithTag("Player");
        inventory = playerObject.GetComponent<PlayerInventory>();
    }

    // starts the flight from a point with a speed and direction
    public void Launch(Vector3 from, Vector3 throwVelocity)
    {
        start = from;
        velocity = throwVelocity;
        flightTime = 0f;
        isFlying = true;
        transform.position = from;
    }

    // moves along the arc and checks the stretch flown this frame for a hit
    private void Update()
    {
        if (!isFlying) return;

        Vector3 from = transform.position;
        flightTime += Time.deltaTime;
        Vector3 to = PointAt(start, velocity, settings.projectileGravity, flightTime);

        if (Physics.Linecast(from, to, out RaycastHit hit, solidMask, QueryTriggerInteraction.Ignore))
        {
            isFlying = false;
            transform.position = hit.point;
            Land(hit);
            return;
        }

        transform.position = to;

        // never hit anything so it left the level and the player gets it back
        if (flightTime > FlightLimit)
        {
            inventory.Add(Kind, 1);
            Sfx.Flat(Sfx.Bank.pickupGadget);
            Destroy(gameObject);
        }
    }

    // what this gadget does where it lands
    protected abstract void Land(RaycastHit hit);
}
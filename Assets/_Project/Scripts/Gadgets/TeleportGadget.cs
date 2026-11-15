using UnityEngine;

// teleports the player where it lands on a dark surface or sticks there to be picked up again
public class TeleportGadget : ThrownGadget
{
    [SerializeField] private Collider pickupCollider; // off while flying so e can only find it after it lands

    private PlayerController player;
    private CharacterController playerBody;

    // given back to the inventory as a teleport gadget
    protected override Ability Kind => Ability.TeleportGadget;

    // grab the player parts after the base finds the player
    protected override void Awake()
    {
        base.Awake();
        player = playerObject.GetComponent<PlayerController>();
        playerBody = playerObject.GetComponent<CharacterController>();
    }

    // true when the hit is a teleportable surface the player capsule fits against and gives the capsule center
    public static bool CanTeleportTo(RaycastHit hit, CharacterController body, int mask, out Vector3 center)
    {
        bool fits = TeleportSpot.Find(hit, body.radius, body.height, mask, out center);
        return fits && IsTeleportable(hit);
    }

    // on a dark surface the player teleports and anywhere else it sticks and waits
    protected override void Land(RaycastHit hit)
    {
        if (IsTeleportable(hit))
        {
            // used up either way and the player only moves when the capsule fits
            if (CanTeleportTo(hit, playerBody, solidMask, out Vector3 center))
                player.TeleportTo(center - Vector3.up * (playerBody.height / 2f));
            Destroy(gameObject);
            return;
        }

        // ride along on a guard and otherwise stay put
        EnemyHealth guard = hit.collider.GetComponentInParent<EnemyHealth>();
        if (guard != null) transform.SetParent(guard.transform, true);

        // e can pick it back up and it disappears if left alone too long
        pickupCollider.enabled = true;
        Destroy(gameObject, settings.stuckGadgetDespawnSeconds);
    }
}
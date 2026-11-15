using UnityEngine;

// teleports the player wherever it lands if they fit there or sticks there to be picked up again
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

    // teleports onto any surface the player fits against but sticks to guards and to spots that are too tight
    protected override void Land(RaycastHit hit)
    {
        EnemyHealth guard = hit.collider.GetComponentInParent<EnemyHealth>();
        if (guard == null && TeleportSpot.Find(hit, playerBody.radius, playerBody.height, solidMask, out Vector3 center))
        {
            player.TeleportTo(center - Vector3.up * (playerBody.height / 2f));
            Destroy(gameObject);
            return;
        }

        // ride along on a guard and otherwise stay put
        if (guard != null) transform.SetParent(guard.transform, true);

        // e can pick it back up and it disappears if left alone too long
        pickupCollider.enabled = true;
        Destroy(gameObject, settings.stuckGadgetDespawnSeconds);
    }
}
using UnityEngine;

// gadgets lying in the level that e picks up
public class GadgetPickup : MonoBehaviour
{
    [SerializeField] private Ability gadget = Ability.Decoy;
    [SerializeField] private int amount = 1; // how many the player gets

    // which gadget this gives
    public Ability Gadget => gadget;

    // hands the gadgets to the player and removes the pickup
    public void Take(PlayerInventory inventory)
    {
        inventory.Add(gadget, amount);
        Destroy(gameObject);
    }
}
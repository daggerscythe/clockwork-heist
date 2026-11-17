using UnityEngine;

// gadgets lying in the level that e picks up
public class GadgetPickup : MonoBehaviour, ICheckpointState
{
    [SerializeField] private Ability gadget = Ability.Decoy;
    [SerializeField] private int amount = 1; // how many the player gets

    private bool wasThere; // true when it was still lying there at the last checkpoint

    // which gadget this gives
    public Ability Gadget => gadget;

    // hands the gadgets to the player and hides the pickup so a checkpoint can bring it back
    public void Take(PlayerInventory inventory)
    {
        inventory.Add(gadget, amount);
        gameObject.SetActive(false);
    }

    // remember if it was still there at the checkpoint
    public void SaveState()
    {
        wasThere = gameObject.activeSelf;
    }

    // show it again if it was there at the checkpoint
    public void RestoreState()
    {
        gameObject.SetActive(wasThere);
    }
}
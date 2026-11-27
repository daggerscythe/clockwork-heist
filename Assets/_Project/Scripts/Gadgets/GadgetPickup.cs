using UnityEngine;

// gadgets lying in the level that e picks up
public class GadgetPickup : MonoBehaviour, ICheckpointState
{
    [SerializeField] private Ability gadget = Ability.Decoy;
    [SerializeField] private int amount = 1; // how many the player gets
    [SerializeField, TextArea] private string note; // tutorial note shown the first time it is taken or empty for none

    private bool wasThere; // true when it was still lying there at the last checkpoint
    private bool noteShown; // the note only comes up once

    // which gadget this gives
    public Ability Gadget => gadget;

    // hands the gadgets to the player and hides the pickup so a checkpoint can bring it back
    public void Take(PlayerInventory inventory)
    {
        Sfx.Flat(Sfx.Bank.pickupGadget);
        inventory.Add(gadget, amount);
        gameObject.SetActive(false);

        // how to use the gadget right when the player has it
        if (note == "" || noteShown) return;
        noteShown = true;
        FindAnyObjectByType<HUD>().ShowNote(note);
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
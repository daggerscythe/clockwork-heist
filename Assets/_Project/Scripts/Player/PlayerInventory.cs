using UnityEngine;

// holds what the player has collected
public class PlayerInventory : MonoBehaviour, ICheckpointState
{
    private readonly int[] gadgets = new int[5]; // one count per ability and the blink one is never used
    private readonly int[] savedGadgets = new int[5]; // counts at the last checkpoint
    private int savedCoins; // coins at the last checkpoint

    // coins to spend in the shop
    public int Coins { get; private set; }

    // adds coins from loot
    public void AddCoins(int amount)
    {
        Coins += amount;
    }

    // adds gadgets from a pickup
    public void Add(Ability gadget, int amount)
    {
        gadgets[(int)gadget] += amount;
    }

    // true when the player can use a gadget right now
    public bool Has(Ability gadget)
    {
        return GameOptions.UnlimitedGadgets || gadgets[(int)gadget] > 0;
    }

    // the count as text for the wheel and hud with the infinity sign while the cheat is on
    public string CountText(Ability gadget)
    {
        return GameOptions.UnlimitedGadgets ? "\u221E" : gadgets[(int)gadget].ToString();
    }

    // takes one away and returns false if there were none and the cheat keeps the count as it is
    public bool TryUse(Ability gadget)
    {
        if (GameOptions.UnlimitedGadgets) return true;
        if (gadgets[(int)gadget] <= 0) return false;
        gadgets[(int)gadget]--;
        return true;
    }

    // remember coins and gadgets at the checkpoint
    public void SaveState()
    {
        savedCoins = Coins;
        gadgets.CopyTo(savedGadgets, 0);
    }

    // back to the coins and gadgets from the checkpoint
    public void RestoreState()
    {
        Coins = savedCoins;
        savedGadgets.CopyTo(gadgets, 0);
    }
}
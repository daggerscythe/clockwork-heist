using UnityEngine;

// holds what the player has collected
public class PlayerInventory : MonoBehaviour
{
    private readonly int[] gadgets = new int[5]; // one count per ability and the blink one is never used

    // coins to spend in the shop
    public int Coins { get; private set; }

    // adds coins from loot
    public void AddCoins(int amount)
    {
        Coins += amount;
    }

    // how many of a gadget the player holds
    public int Count(Ability gadget)
    {
        return gadgets[(int)gadget];
    }

    // adds gadgets from a pickup
    public void Add(Ability gadget, int amount)
    {
        gadgets[(int)gadget] += amount;
    }

    // takes one away and returns false if there were none
    public bool TryUse(Ability gadget)
    {
        if (gadgets[(int)gadget] <= 0) return false;
        gadgets[(int)gadget]--;
        return true;
    }
}
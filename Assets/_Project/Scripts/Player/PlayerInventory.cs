using UnityEngine;

// holds what the player has collected
public class PlayerInventory : MonoBehaviour
{
    // coins to spend in the shop
    public int Coins { get; private set; }

    // adds coins from loot
    public void AddCoins(int amount)
    {
        Coins += amount;
    }
}
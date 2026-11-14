using UnityEngine;

// coins a guard carries or a pile lying in the level
public class Loot : MonoBehaviour
{
    [SerializeField] private int coins = 10; // coins inside
    [SerializeField] private bool removeWhenEmpty; // tick for coin piles so they disappear once taken

    // true while there is anything left to take
    public bool HasCoins => coins > 0;

    // hands over every coin and removes a pile once it is empty
    public int TakeAll()
    {
        int taken = coins;
        coins = 0;
        if (removeWhenEmpty) Destroy(gameObject);
        return taken;
    }
}
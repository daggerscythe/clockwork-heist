using UnityEngine;

// coins a guard carries or a pile lying in the level
public class Loot : MonoBehaviour, ICheckpointState
{
    [SerializeField] private int coins = 10; // coins inside
    [SerializeField] private bool removeWhenEmpty; // tick for coin piles so they disappear once taken

    private int savedCoins; // coins at the last checkpoint

    // true while there is anything left to take
    public bool HasCoins => coins > 0;

    // hands over every coin and hides a pile once it is empty
    public int TakeAll()
    {
        int taken = coins;
        coins = 0;
        if (removeWhenEmpty) gameObject.SetActive(false);
        return taken;
    }

    // remember the coins at the checkpoint
    public void SaveState()
    {
        savedCoins = coins;
    }

    // coins come back and a pile shows again if it had any at the checkpoint
    public void RestoreState()
    {
        coins = savedCoins;
        if (removeWhenEmpty) gameObject.SetActive(coins > 0);
    }
}
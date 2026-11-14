using TMPro;
using UnityEngine;
using UnityEngine.UI;

// shows the player info on screen
public class HUD : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerInteract interact;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private GameObject holdBar;
    [SerializeField] private Image holdFill;

    // keep everything matching the player
    private void Update()
    {
        // shrink the fill from the right so the sliced edges stay clean
        healthFill.rectTransform.anchorMax = new Vector2(playerHealth.HealthFraction, 1f);

        coinsText.text = "Coins " + inventory.Coins;
        promptText.text = interact.Prompt;

        // the hold bar only shows while pickpocketing
        holdBar.SetActive(interact.HoldFraction > 0f);
        holdFill.rectTransform.anchorMax = new Vector2(interact.HoldFraction, 1f);
    }
}
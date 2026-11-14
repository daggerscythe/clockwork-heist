using UnityEngine;
using UnityEngine.UI;

// shows the player info on screen
public class HUD : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image healthFill;

    // keep the health bar width matching the player
    private void Update()
    {
        // shrink the fill from the right so the sliced edges stay clean
        healthFill.rectTransform.anchorMax = new Vector2(playerHealth.HealthFraction, 1f);
    }
}
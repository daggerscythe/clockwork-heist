using UnityEngine;

// tracks the player health and ends the run when it runs out
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private GameSettings settings;

    // current health points
    public float Health { get; private set; }

    // health from 0 to 1 for the health bar
    public float HealthFraction => Health / settings.playerMaxHealth;

    // start at full health
    private void Awake()
    {
        Restore();
    }

    // lose health and end the run at zero
    public void TakeDamage(float amount)
    {
        if (Health <= 0f) return;
        Health = Mathf.Max(0f, Health - amount);
        if (Health <= 0f) GameManager.Instance.GameOver("You died");
    }

    // back to full health after a respawn
    public void Restore()
    {
        Health = settings.playerMaxHealth;
    }
}
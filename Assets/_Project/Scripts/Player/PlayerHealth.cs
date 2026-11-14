using UnityEngine;
using UnityEngine.SceneManagement;

// tracks the player health and restarts the level when it runs out
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
        Health = settings.playerMaxHealth;
    }

    // lose health and restart the level at zero
    public void TakeDamage(float amount)
    {
        Health = Mathf.Max(0f, Health - amount);
        if (Health <= 0f)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
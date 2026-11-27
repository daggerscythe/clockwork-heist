using UnityEngine;
using UnityEngine.SceneManagement;

// the way out of a floor that clears it when the player walks in
[RequireComponent(typeof(Collider))]
public class LevelExit : MonoBehaviour
{
    [SerializeField] private string nextScene = "MainMenu"; // scene loaded once the floor is cleared
    [SerializeField] private string clearedText = "Floor cleared"; // tip shown while it loads
    [SerializeField] private float loadDelay = 2f; // real s before the next scene loads

    private float loadTime = -1f; // real time the next scene loads or below 0 until the player arrives

    // the player walking in takes the controls away and starts the wait
    private void OnTriggerEnter(Collider other)
    {
        if (loadTime >= 0f || !other.CompareTag("Player") || GameManager.Instance.IsOver) return;
        loadTime = Time.unscaledTime + loadDelay;
        GameManager.Instance.SetPlayerInput(false);
        FindAnyObjectByType<HUD>().ShowHint(clearedText);
    }

    // loads the next scene once the wait is over
    private void Update()
    {
        if (loadTime >= 0f && Time.unscaledTime >= loadTime) SceneManager.LoadScene(nextScene);
    }
}
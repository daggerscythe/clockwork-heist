using UnityEngine;

// a trigger the player walks through to save where they come back after a game over
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint; // where and which way the player comes back

    private bool reached; // only the first pass counts

    // the player walking in saves the spot once
    private void OnTriggerEnter(Collider other)
    {
        if (reached || !other.CompareTag("Player")) return;
        reached = true;
        GameManager.Instance.SetCheckpoint(spawnPoint.position, spawnPoint.rotation);
    }
}
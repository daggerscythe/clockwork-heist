using UnityEngine;

// shows the combat note the first time a guard spots the player
public class SpottedNote : MonoBehaviour
{
    [SerializeField, TextArea] private string note; // what the card says

    private bool shown; // the note only comes up once

    // waits for the first guard that goes alerted on the player
    private void Update()
    {
        if (shown) return;
        foreach (EnemyHealth guard in EnemyHealth.All)
        {
            if (!guard.GetComponent<EnemyAI>().IsAlerted) continue;
            shown = true;
            FindAnyObjectByType<HUD>().ShowNote(note);
            return;
        }
    }
}
using UnityEngine;

// a trigger that shows a tip on the hud the first time the player walks in and keeps it as a note
[RequireComponent(typeof(Collider))]
public class HintTrigger : MonoBehaviour
{
    [SerializeField, TextArea] private string hint; // what the tip says
    [SerializeField] private HintTrigger[] twins; // other triggers with the same tip that count as shown too

    private bool shown; // each tip only shows once

    // the player walking in shows the tip
    private void OnTriggerEnter(Collider other)
    {
        if (shown || !other.CompareTag("Player")) return;
        shown = true;
        foreach (HintTrigger twin in twins) twin.shown = true;
        FindAnyObjectByType<HUD>().ShowNote(hint);
    }
}
using UnityEngine;

// the steps of a floor's tutorial in order which the arrow leads the player through
public class Tutorial : MonoBehaviour, ICheckpointState
{
    private TutorialStep[] steps; // the children in hierarchy order
    private Transform player;
    private int current; // the step the arrow leads to
    private int savedCurrent; // the step at the last checkpoint

    // the step the arrow leads to or null once the tutorial is over or turned off
    public TutorialStep Current => GameOptions.TutorialOff || current >= steps.Length ? null : steps[current];

    // collect the steps and find the player
    private void Awake()
    {
        steps = GetComponentsInChildren<TutorialStep>();
        player = GameObject.FindWithTag("Player").transform;
    }

    // moves past finished steps and jumps ahead when the player already took a later pickup
    private void Update()
    {
        for (int i = steps.Length - 1; i > current; i--)
        {
            if (steps[i].IsTake && steps[i].IsDone(player.position))
            {
                current = i + 1;
                break;
            }
        }
        while (current < steps.Length && steps[current].IsDone(player.position)) current++;
    }

    // remember the step at the checkpoint
    public void SaveState()
    {
        savedCurrent = current;
    }

    // lead to the same step again after a respawn
    public void RestoreState()
    {
        current = savedCurrent;
    }
}
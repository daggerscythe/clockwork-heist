using UnityEngine;

// the thing a floor is about like the vault keycard that e takes
public class ObjectivePickup : MonoBehaviour, ICheckpointState
{
    [SerializeField] private string displayName = "vault keycard"; // name in the prompt
    [SerializeField, TextArea] private string takenHint; // tip shown once it is taken

    private bool wasTaken; // true when it was already taken at the last checkpoint

    // true once the player has it
    public bool IsTaken { get; private set; }

    // name in the prompt
    public string DisplayName => displayName;

    // gives it to the player with the pickup sound and the objective sting
    public void Take()
    {
        IsTaken = true;
        gameObject.SetActive(false);
        Sfx.Flat(Sfx.Bank.objectivePickup);
        Sfx.Flat(Sfx.Bank.objectiveSting);
        if (takenHint != "") FindAnyObjectByType<HUD>().ShowHint(takenHint);
    }

    // remember if it was taken at the checkpoint
    public void SaveState()
    {
        wasTaken = IsTaken;
    }

    // put it back on the desk if it was still there at the checkpoint
    public void RestoreState()
    {
        IsTaken = wasTaken;
        gameObject.SetActive(!IsTaken);
    }
}
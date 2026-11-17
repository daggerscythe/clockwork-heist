// anything that goes back to how it was when the player reached the last checkpoint
public interface ICheckpointState
{
    // remember the current state
    void SaveState();

    // go back to the remembered state
    void RestoreState();
}
using UnityEngine;

// a hinged door that e swings open and that can stay locked until the player has an objective
public class Door : MonoBehaviour, ICheckpointState
{
    [SerializeField] private Transform[] leaves; // parts that turn on their hinge and every second one turns the other way
    [SerializeField] private float openAngle = 95f; // degrees the leaves turn and negative swings them the other way
    [SerializeField] private float openTime = 0.6f; // s to swing fully open
    [SerializeField] private ObjectivePickup key; // objective that unlocks it or empty for a door that is never locked
    [SerializeField] private string lockedText = "Locked"; // prompt while the player doesn't have the key

    private Quaternion[] closedRotations; // each leaf as it was placed
    private float openAmount; // 0 closed to 1 open
    private bool wasOpen; // true when it was open at the last checkpoint

    // true once it has been opened
    public bool IsOpen { get; private set; }

    // the prompt while it can't be opened or null when it can
    public string LockedReason => key != null && !key.IsTaken ? lockedText : null;

    // remember how the leaves were placed
    private void Awake()
    {
        closedRotations = new Quaternion[leaves.Length];
        for (int i = 0; i < leaves.Length; i++) closedRotations[i] = leaves[i].localRotation;
    }

    // starts the swing with a creak
    public void Open()
    {
        IsOpen = true;
        Sfx.At(Sfx.Bank.doorOpen, transform.position);
    }

    // swings the leaves open with an ease in and out
    private void Update()
    {
        if (!IsOpen || openAmount >= 1f) return;
        openAmount = Mathf.Min(1f, openAmount + Time.deltaTime / openTime);
        Turn(Mathf.SmoothStep(0f, 1f, openAmount));
    }

    // turns every leaf the given part of the way open
    private void Turn(float amount)
    {
        for (int i = 0; i < leaves.Length; i++)
        {
            float angle = (i % 2 == 0 ? openAngle : -openAngle) * amount;
            leaves[i].localRotation = closedRotations[i] * Quaternion.Euler(0f, angle, 0f);
        }
    }

    // remember if it was open at the checkpoint
    public void SaveState()
    {
        wasOpen = IsOpen;
    }

    // snap open or shut the way it was at the checkpoint
    public void RestoreState()
    {
        IsOpen = wasOpen;
        openAmount = IsOpen ? 1f : 0f;
        Turn(openAmount);
    }
}
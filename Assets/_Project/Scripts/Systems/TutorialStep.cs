using UnityEngine;

// one place the tutorial arrow leads to either a spot to reach or something to pick up
public class TutorialStep : MonoBehaviour
{
    [SerializeField] private GameObject takeTarget; // pickup or objective that finishes the step once taken or empty for a spot to reach
    [SerializeField] private float reachRadius = 1.5f; // m from this spot the player must come when there is nothing to take
    [SerializeField] private float arrowHeight = 1f; // m above the step the arrow points at

    // true for a step that ends by taking something
    public bool IsTake => takeTarget != null;

    // where the arrow points
    public Vector3 ArrowPoint => (takeTarget != null ? takeTarget.transform.position : transform.position) + Vector3.up * arrowHeight;

    // true once the thing is taken or the player stands close enough
    public bool IsDone(Vector3 player)
    {
        if (takeTarget != null) return !takeTarget.activeInHierarchy;
        return Vector3.Distance(player, transform.position) <= reachRadius;
    }
}
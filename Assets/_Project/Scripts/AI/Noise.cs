using UnityEngine;

// sends a sound to every guard close enough to hear it
public static class Noise
{
    // each guard inside the radius goes to check out the spot
    public static void Make(Vector3 position, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(position, radius, LayerMask.GetMask("Enemy"), QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            EnemyAI guard = hit.GetComponentInParent<EnemyAI>();
            if (guard != null) guard.Investigate(position);
        }
    }
}
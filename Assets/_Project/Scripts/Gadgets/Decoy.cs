using UnityEngine;

// a ghost copy of the player that keeps calling guards over until one hits it
public class Decoy : MonoBehaviour
{
    [SerializeField] private GameSettings settings;

    private const float CallInterval = 1f; // s between calls so guards that walk into range come too

    private int enemyMask; // only guards
    private float nextCallTime; // game time of the next call

    // build the mask
    private void Awake()
    {
        enemyMask = LayerMask.GetMask("Enemy");
    }

    // calls every guard in range over and again every interval
    private void Update()
    {
        if (Time.time < nextCallTime) return;
        nextCallTime = Time.time + CallInterval;

        Collider[] hits = Physics.OverlapSphere(transform.position, settings.decoySoundRadius, enemyMask, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            EnemyAI guard = hit.GetComponentInParent<EnemyAI>();
            if (guard != null) guard.AttackDecoy(this);
        }
    }

    // a guard hit it so it vanishes
    public void Dissipate()
    {
        Sfx.At(Sfx.Bank.decoyDissipate, transform.position);
        Destroy(gameObject);
    }
}
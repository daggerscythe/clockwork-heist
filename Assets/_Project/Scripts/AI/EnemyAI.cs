using UnityEngine;
using UnityEngine.AI;

// walks a guard around its patrol route or leaves it standing at its watch spot
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform[] patrolPoints; // leave empty for a guard that stands still
    [SerializeField] private float waitAtPoint = 2f; // s to pause at each patrol point

    private NavMeshAgent agent;
    private int pointIndex;
    private bool isWaiting;
    private float waitUntil; // game time when the guard walks on

    // grab the agent and set the walking speed
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = settings.enemyWalkSpeed;
    }

    // start walking to the first point
    private void Start()
    {
        if (patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[0].position);
    }

    // a guard with no points just keeps its spot
    private void Update()
    {
        if (patrolPoints.Length > 0) Patrol();
    }

    // walk to the current point then wait there a moment before the next one
    private void Patrol()
    {
        // still on the way
        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance) return;

        // just arrived so start the pause
        if (!isWaiting)
        {
            isWaiting = true;
            waitUntil = Time.time + waitAtPoint;
            return;
        }

        // pause is over so go to the next point and loop back to the first after the last
        if (Time.time < waitUntil) return;
        isWaiting = false;
        pointIndex = (pointIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[pointIndex].position);
    }

    // draws the patrol route in the scene view when selected
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Vector3 from = patrolPoints[i].position;
            Vector3 to = patrolPoints[(i + 1) % patrolPoints.Length].position;
            Gizmos.DrawLine(from, to);
            Gizmos.DrawWireSphere(from, 0.2f);
        }
    }
}
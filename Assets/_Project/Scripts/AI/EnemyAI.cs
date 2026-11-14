using UnityEngine;
using UnityEngine.AI;

// guard brain that patrols checks noises chases attacks and searches
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemySenses))]
public class EnemyAI : MonoBehaviour
{
    private enum State { Patrol, Suspicious, Alerted, Confused }

    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform[] patrolPoints; // leave empty for a guard that stands still
    [SerializeField] private float waitAtPoint = 2f; // s to pause at each patrol point

    private NavMeshAgent agent;
    private EnemySenses senses;
    private PlayerHealth playerHealth;
    private State state;
    private int enemyMask; // only guards
    private int pointIndex;
    private bool isWaiting;
    private float waitUntil; // game time when the guard walks on
    private bool onAlert; // true when the guard knows something is wrong so it chases the player at first sight
    private float searchDuration; // s the next or current look around lasts
    private float searchEndTime; // game time when looking around stops
    private float nextAttackTime; // game time the next hit can land
    private Vector3 postPosition; // where the guard was placed
    private Quaternion postRotation; // which way the guard was placed facing

    // true while the guard is chasing or fighting the player
    public bool IsAlerted => state == State.Alerted;

    // true while the guard is going about its normal routine
    public bool IsCalm => state == State.Patrol;

    // grab components and remember the post
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        senses = GetComponent<EnemySenses>();
        playerHealth = GameObject.FindWithTag("Player").GetComponent<PlayerHealth>();
        enemyMask = LayerMask.GetMask("Enemy");
        postPosition = transform.position;
        postRotation = transform.rotation;
    }

    // every guard starts on its routine
    private void Start()
    {
        StartPatrol();
    }

    // check for the player and bodies first then run the current state
    private void Update()
    {
        // a full meter alerts any guard and a guard on alert chases at first sight
        bool spotted = senses.Detection >= 1f || (onAlert && senses.SeesPlayer);
        if (state != State.Alerted && spotted) StartAlert();

        // a body nobody hid raises the alarm unless the guard is already chasing
        if (state != State.Alerted && senses.NewBody != null) ReactToBody(senses.NewBody.transform.position);

        switch (state)
        {
            case State.Patrol: Patrol(); break;
            case State.Suspicious: Suspicious(); break;
            case State.Alerted: Alerted(); break;
            case State.Confused: Confused(); break;
        }
    }

    // a noise or a shout sends a guard that is awake and not chasing to check the spot
    public void Investigate(Vector3 point)
    {
        if (!enabled || state == State.Alerted) return;
        state = State.Suspicious;
        searchDuration = settings.confusionTime;
        MoveTo(point, settings.enemyWalkSpeed);
    }

    // gets up calm and looks around before going back to the routine
    public void WakeUp()
    {
        onAlert = false;
        StartSearch(settings.wakeSearchTime);
    }

    // go back to the route or the post and calm down
    private void StartPatrol()
    {
        state = State.Patrol;
        onAlert = false;
        isWaiting = false;
        Vector3 target = patrolPoints.Length > 0 ? patrolPoints[pointIndex].position : postPosition;
        MoveTo(target, settings.enemyWalkSpeed);
    }

    // walk the route with a pause at each point or stand at the post
    private void Patrol()
    {
        if (!Arrived()) return;

        // a stationary guard turns back to the way it was placed
        if (patrolPoints.Length == 0)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, postRotation, agent.angularSpeed * Time.deltaTime);
            return;
        }

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
        MoveTo(patrolPoints[pointIndex].position, settings.enemyWalkSpeed);
    }

    // walk to the noise or run to the body then look around there
    private void Suspicious()
    {
        if (Arrived()) StartSearch(searchDuration);
    }

    // a body puts the guard on alert so it shouts runs over and searches around it
    private void ReactToBody(Vector3 point)
    {
        onAlert = true;
        Shout(point);
        state = State.Suspicious;
        searchDuration = settings.bodySearchTime;
        MoveTo(point, settings.enemyRunSpeed);
    }

    // shout to nearby guards and start the chase
    private void StartAlert()
    {
        state = State.Alerted;
        onAlert = true;
        agent.isStopped = false;
        agent.speed = settings.enemyRunSpeed;
        nextAttackTime = Time.time + settings.enemyAttackInterval;
        Shout(senses.LastSeenPosition);
    }

    // nearby guards come to check the point but still need to spot the player themselves
    private void Shout(Vector3 point)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, settings.alertShoutRadius, enemyMask, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            EnemyAI guard = hit.GetComponentInParent<EnemyAI>();
            if (guard != null && guard != this) guard.Investigate(point);
        }
    }

    // chase while the player is seen and hit them when close or run to where they were last seen
    private void Alerted()
    {
        Vector3 target = senses.LastSeenPosition;

        if (!senses.SeesPlayer)
        {
            // the path already leads to the last seen spot so just keep going and search there
            agent.isStopped = false;
            if (Arrived()) StartSearch(settings.confusionTime);
            return;
        }

        agent.SetDestination(target);
        bool inReach = Vector3.Distance(transform.position, target) <= settings.enemyAttackRange;
        agent.isStopped = inReach;

        // still running so the first hit waits a full interval after reaching the player
        if (!inReach)
        {
            nextAttackTime = Time.time + settings.enemyAttackInterval;
            return;
        }

        FaceTowards(target);
        if (Time.time >= nextAttackTime)
        {
            playerHealth.TakeDamage(settings.enemyHitDamage);
            nextAttackTime = Time.time + settings.enemyAttackInterval;
        }
    }

    // stop and look around for a while
    private void StartSearch(float duration)
    {
        state = State.Confused;
        searchDuration = duration;
        searchEndTime = Time.time + duration;
        agent.ResetPath();
    }

    // turn one full circle over the search time then go back to the routine
    private void Confused()
    {
        transform.Rotate(0f, 360f / searchDuration * Time.deltaTime, 0f);
        if (Time.time >= searchEndTime) StartPatrol();
    }

    // un-pause the agent and send it somewhere at a speed
    private void MoveTo(Vector3 point, float speed)
    {
        agent.isStopped = false;
        agent.speed = speed;
        agent.SetDestination(point);
    }

    // true when the agent has reached where it was going
    private bool Arrived()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }

    // turns the guard on the spot toward a point
    private void FaceTowards(Vector3 point)
    {
        Vector3 flat = point - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), agent.angularSpeed * Time.deltaTime);
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
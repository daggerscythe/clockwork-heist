using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// guard health and being knocked out killed carried hidden and woken up
[RequireComponent(typeof(EnemyAI))]
public class EnemyHealth : MonoBehaviour, ICheckpointState
{
    private enum Condition { Alive, KnockedOut, Dead }

    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform model; // guard model and body collider that hide in a stash
    [SerializeField] private Transform body; // capsule collider that lies down when the guard goes down
    [SerializeField] private GameObject detectionMeter;

    private const float LyingHeight = 0.4f; // m the lying body collider sits above the floor
    private const float StandUpSearch = 2f; // m to look for walkable floor when getting up

    // every active guard so others can look for bodies
    public static readonly List<EnemyHealth> All = new List<EnemyHealth>();

    private EnemyAI ai;
    private EnemySenses senses;
    private GuardAnimator guardAnimator;
    private NavMeshAgent agent;
    private Condition condition;
    private float health; // health points
    private float wakeLeft; // s until a knocked out guard gets up
    private Condition savedCondition; // how the guard was at the last checkpoint
    private Vector3 savedPosition; // where a downed guard lay at the last checkpoint
    private Quaternion savedRotation; // how a downed guard lay at the last checkpoint
    private bool savedHidden; // true if he was in a stash at the last checkpoint
    private float savedWakeLeft; // s a knocked out guard had left at the last checkpoint
    private Vector3 bodyStandPosition; // where the body collider sits while standing

    // true while standing and able to act
    public bool IsAlive => condition == Condition.Alive;

    // true for knocked out or dead guards which count as bodies
    public bool IsDown => condition != Condition.Alive;

    // true only for knocked out guards that will wake up
    public bool IsKnockedOut => condition == Condition.KnockedOut;

    // true while stuffed in a stash where guards cannot see it
    public bool IsHidden { get; private set; }

    // 1 right after the knock out and 0 when it wakes
    public float WakeFraction => wakeLeft / settings.knockoutDuration;

    // grab the other guard parts and start at full health
    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
        senses = GetComponent<EnemySenses>();
        guardAnimator = GetComponent<GuardAnimator>();
        agent = GetComponent<NavMeshAgent>();
        health = settings.enemyMaxHealth;
        bodyStandPosition = body.localPosition;
    }

    // join the list while in the world
    private void OnEnable()
    {
        All.Add(this);
    }

    // leave the list while carried or destroyed
    private void OnDisable()
    {
        All.Remove(this);
    }

    // counts down to waking up and this stops while carried because the guard is turned off
    private void Update()
    {
        if (!IsKnockedOut) return;
        wakeLeft -= Time.deltaTime;
        if (wakeLeft <= 0f) WakeUp();
    }

    // true when the guard is not chasing and the point is outside its vision cone
    public bool IsUnaware(Vector3 point)
    {
        return IsAlive && !ai.IsAlerted && !senses.InCone(point);
    }

    // a blade hit in open combat which also shows the guard where the player is
    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;
        health -= amount;
        if (health <= 0f) Kill();
        else senses.NoticePlayer();
    }

    // dead for good with no timer
    public void Kill()
    {
        GoDown(Condition.Dead);
    }

    // out cold until the timer runs out
    public void KnockOut()
    {
        wakeLeft = settings.knockoutDuration;
        GoDown(Condition.KnockedOut);
    }

    // picked up by the player so the whole guard leaves the world for now
    public void PickUp()
    {
        gameObject.SetActive(false);
    }

    // put back on the floor at a spot
    public void Drop(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        gameObject.SetActive(true);
    }

    // put into a stash where no guard can see it but the timer keeps going
    public void Hide(Vector3 position, Quaternion rotation)
    {
        Drop(position, rotation);
        IsHidden = true;
        model.gameObject.SetActive(false);
    }

    // remembers how this guard was when the player reached the checkpoint
    public void SaveState()
    {
        savedCondition = condition;
        savedPosition = transform.position;
        savedRotation = transform.rotation;
        savedHidden = IsHidden;
        savedWakeLeft = wakeLeft;
    }

    // a guard standing at the checkpoint gets up fresh at his post and a downed one lies back where he was
    public void RestoreState()
    {
        gameObject.SetActive(true);
        guardAnimator.Snap();

        if (savedCondition == Condition.Alive)
        {
            health = settings.enemyMaxHealth;
            StandUp();
            ai.ResetToPost();
            return;
        }

        transform.SetPositionAndRotation(savedPosition, savedRotation);
        wakeLeft = savedWakeLeft;
        GoDown(savedCondition);
        IsHidden = savedHidden;
        model.gameObject.SetActive(!savedHidden);
    }

    // turns the guard into a body lying on the floor while the guard animator plays the fall
    private void GoDown(Condition newCondition)
    {
        condition = newCondition;
        SetBrain(false);

        // the collider lies forward along the ground where the model falls
        body.localPosition = new Vector3(0f, LyingHeight, bodyStandPosition.y);
        body.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // gets up on his own and looks around
    private void WakeUp()
    {
        StandUp();
        ai.WakeUp();
    }

    // back on his feet on walkable floor with his brain on
    private void StandUp()
    {
        condition = Condition.Alive;
        IsHidden = false;
        model.gameObject.SetActive(true);
        body.localPosition = bodyStandPosition;
        body.localRotation = Quaternion.identity;

        // stand on the closest walkable floor so the agent can start again
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, StandUpSearch, NavMesh.AllAreas))
            transform.position = hit.position;

        SetBrain(true);
    }

    // turns the brain movement and meter on or off together
    private void SetBrain(bool on)
    {
        ai.enabled = on;
        senses.enabled = on;
        agent.enabled = on;
        detectionMeter.SetActive(on);
    }
}
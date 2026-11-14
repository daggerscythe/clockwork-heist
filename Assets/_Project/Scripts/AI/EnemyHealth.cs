using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// guard health and being knocked out killed carried hidden and woken up
[RequireComponent(typeof(EnemyAI))]
public class EnemyHealth : MonoBehaviour
{
    private enum Condition { Alive, KnockedOut, Dead }

    [SerializeField] private GameSettings settings;
    [SerializeField] private Transform model; // body and visor that lie down when the guard goes down
    [SerializeField] private GameObject detectionMeter;

    private const float LyingHeight = 0.4f; // m the lying body sits above the floor
    private const float StandUpSearch = 2f; // m to look for walkable floor when waking

    // every active guard so others can look for bodies
    public static readonly List<EnemyHealth> All = new List<EnemyHealth>();

    private EnemyAI ai;
    private EnemySenses senses;
    private NavMeshAgent agent;
    private Condition condition;
    private float health; // health points
    private float wakeLeft; // s until a knocked out guard gets up

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
        agent = GetComponent<NavMeshAgent>();
        health = settings.enemyMaxHealth;
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

    // turns the guard into a body lying on the floor
    private void GoDown(Condition newCondition)
    {
        condition = newCondition;
        SetBrain(false);
        model.localPosition = Vector3.up * LyingHeight;
        model.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // stands back up and starts searching
    private void WakeUp()
    {
        condition = Condition.Alive;
        IsHidden = false;
        model.gameObject.SetActive(true);
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;

        // stand on the closest walkable floor so the agent can start again
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, StandUpSearch, NavMesh.AllAreas))
            transform.position = hit.position;

        SetBrain(true);
        ai.WakeUp();
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
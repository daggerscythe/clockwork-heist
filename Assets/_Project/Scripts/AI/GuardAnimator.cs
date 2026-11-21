using UnityEngine;
using UnityEngine.AI;

// plays the guard model clip that matches what the guard is doing
[RequireComponent(typeof(EnemyAI))]
[RequireComponent(typeof(EnemyHealth))]
public class GuardAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator; // on the guard model

    private const float WalkSpeed = 0.2f; // m/s below this the guard counts as standing
    private const float RunSpeed = 3.5f; // m/s above this the guard counts as running
    private const float SwingTime = 0.6f; // s the attack clip lasts
    private const float BlendTime = 0.15f; // s to blend from one clip to the next
    private const float GetUpTime = 0.5f; // s to blend from lying down to standing
    private const string KnockOutClip = "KnockOut";

    private EnemyAI ai;
    private EnemyHealth health;
    private NavMeshAgent agent;
    private string current; // state playing now
    private bool snapNext; // the next state starts at once with no blend

    // grab the guard parts
    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
        health = GetComponent<EnemyHealth>();
        agent = GetComponent<NavMeshAgent>();
    }

    // a guard put back into the world takes his pose at once
    private void OnEnable()
    {
        Snap();
    }

    // the next pose is set straight away with no blend so a downed guard is already lying down
    public void Snap()
    {
        current = null;
        snapNext = true;
    }

    // switches state only when what the guard does changes
    private void Update()
    {
        // the model is switched off while the body is in a stash
        if (!animator.isActiveAndEnabled) return;

        string next = PickState();
        if (next == current) return;

        if (snapNext) animator.Play(next, 0, next == KnockOutClip ? 1f : 0f);
        else animator.CrossFadeInFixedTime(next, current == KnockOutClip ? GetUpTime : BlendTime);
        snapNext = false;
        current = next;
    }

    // the state for what the guard is doing with the most important first
    private string PickState()
    {
        if (health.IsDown) return KnockOutClip;
        if (ai.IsPulling) return "AlarmPull";
        if (Time.time - ai.LastSwingTime < SwingTime) return "Attack";
        if (ai.IsSearching) return "LookAround";

        float speed = agent.velocity.magnitude;
        if (speed > RunSpeed) return "Run";
        return speed > WalkSpeed ? "Walk" : "Idle";
    }
}
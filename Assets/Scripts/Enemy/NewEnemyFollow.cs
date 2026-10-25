using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NewEnemyFollow : MonoBehaviour
{
    [Header("Target & Movement")]
    public Transform player;
    public float detectRange = 10f;
    public float attackRange = 2f;
    private NavMeshAgent agent;
    private Animator animator;

    [Header("State Flags")]
    private bool hasRaged = false;
    private bool isAttacking = false;
    private bool isDead = false;

    private Coroutine randomMoveCoroutine;
    private Coroutine attackCoroutine;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        // 1. Quái di chuyển ngẫu nhiên ban đầu
        randomMoveCoroutine = StartCoroutine(RandomPatrolRoutine());
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectRange)
        {
            if (!hasRaged)
            {
                TriggerRageAndCharge();
            }
            else
            {
                if (agent.isStopped) return;

                if (distanceToPlayer <= attackRange)
                {
                    if (!isAttacking)
                    {
                        attackCoroutine = StartCoroutine(PerformAttackRoutine());
                    }
                }
                else if (!isAttacking)
                {
                    animator.SetInteger("MovementState", 0); // Animation Run
                    agent.SetDestination(player.position);
                }
            }
        }
    }

    IEnumerator RandomPatrolRoutine()
    {
        while (!hasRaged && !isDead)
        {
            Vector3 randomDirection = Random.insideUnitSphere * 8f + transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 8f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            int randomMove = Random.Range(0, 5); // 0: Run, 1: walk1, 2: walkBack, 3: StrafeLeft, 4: StrafeRight
            animator.SetInteger("MovementState", randomMove);

            yield return new WaitForSeconds(Random.Range(3f, 5f));
        }
    }

    void TriggerRageAndCharge()
    {
        hasRaged = true;

        if (randomMoveCoroutine != null)
        {
            StopCoroutine(randomMoveCoroutine);
        }

        agent.isStopped = true;
        animator.SetTrigger("RageTrigger");

        StartCoroutine(ResumeAgentAfterRage());
    }

    IEnumerator ResumeAgentAfterRage()
    {
        yield return new WaitForSeconds(1.5f);

        if (!isDead)
        {
            agent.isStopped = false;
            animator.SetInteger("MovementState", 0);
            agent.SetDestination(player.position);
        }
    }

    IEnumerator PerformAttackRoutine()
    {
        isAttacking = true;

        int randomAttackIndex = Random.Range(1, 5);
        animator.SetInteger("AttackIndex", randomAttackIndex);
        animator.SetTrigger("Attack");

        Vector3 dodgeOffset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
        agent.SetDestination(player.position + dodgeOffset);

        yield return new WaitForSeconds(1.2f);

        isAttacking = false;
    }

    public void PlayGetHitAnimation()
    {
        if (isDead) return;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }
        isAttacking = false;

        int randomHitIndex = Random.Range(1, 4);
        animator.SetInteger("GetHitIndex", randomHitIndex);
        animator.SetTrigger("GetHit");

        animator.SetInteger("MovementState", 0);

        if (player != null && hasRaged)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    public void HandleDeath()
    {
        isDead = true;
        StopAllCoroutines();
        if (agent != null) agent.isStopped = true;
        this.enabled = false;
    }

    public bool HasRaged() => hasRaged;
}
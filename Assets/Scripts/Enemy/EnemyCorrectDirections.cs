using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyCorrectDirections : MonoBehaviour
{
    [Header("Tham chiếu")]
    public NavMeshAgent agent;
    public Animator animator;
    public Transform player;

    [Header("Cấu hình Phát hiện")]
    public float detectRadius = 10f;             // Bán kính phát hiện người chơi
    public float loseRadiusMultiplier = 1.3f;    // Bán kính mất dấu người chơi

    [Header("Cấu hình Di chuyển")]
    public float moveSpeed = 2.0f;
    public float changeDirectionInterval = 2.0f; // Thời gian cho 1 nhịp di chuyển

    [HideInInspector]
    public bool isPlayerInZone = false;

    private EnemyCombat combatScript;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        combatScript = GetComponent<EnemyCombat>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.1f;
        }

        StartCoroutine(MainStateController());
    }

    void OnEnable()
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
        }
    }

    IEnumerator MainStateController()
    {
        while (true)
        {
            CheckPlayerDistance();

            if (!isPlayerInZone)
            {
                // 1. Ngoại vòng: Walk1 tuần tra
                yield return StartCoroutine(PatrolOutsideRoutine());
            }
            else
            {
                if (combatScript != null && combatScript.IsCombatActive)
                {
                    yield return null;
                    continue;
                }

                // 2. Trong vòng: Di chuyển/né đòn các hướng
                yield return StartCoroutine(SensingInsideRoutine());
            }

            yield return null;
        }
    }

    // ================= 1. NGOÀI VÒNG: TUẦN TRA =================
    IEnumerator PatrolOutsideRoutine()
    {
        if (animator != null) animator.SetBool("isRunning", false);
        SetMovementState(1); // 1 = Walk1

        if (agent != null)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = moveSpeed;
        }

        Vector3 optimalDestination = GetOptimalPatrolPoint();
        agent.SetDestination(optimalDestination);

        while (!isPlayerInZone && agent.isActiveAndEnabled)
        {
            CheckPlayerDistance();

            if (!agent.pathPending && (agent.remainingDistance <= 0.4f || agent.velocity.sqrMagnitude < 0.05f))
            {
                optimalDestination = GetOptimalPatrolPoint();
                agent.SetDestination(optimalDestination);
            }

            yield return null;
        }
    }

    // ================= 2. TRONG VÒNG: NÉ ĐÒN BỐN HƯỚNG =================
    IEnumerator SensingInsideRoutine()
    {
        if (animator != null) animator.SetBool("isRunning", false);

        if (agent != null)
        {
            agent.isStopped = false;
            agent.updateRotation = false;
            agent.speed = moveSpeed;
        }

        while (isPlayerInZone && (combatScript == null || !combatScript.IsCombatActive))
        {
            CheckPlayerDistance();
            if (!isPlayerInZone) break;

            SmoothLookAtPlayer(15f);

            int selectedState = ChooseSafeAnimationState(out Vector3 targetPos);

            SetMovementState(selectedState);
            agent.SetDestination(targetPos);

            float timer = 0f;

            while (timer < changeDirectionInterval && isPlayerInZone && (combatScript == null || !combatScript.IsCombatActive))
            {
                CheckPlayerDistance();
                SmoothLookAtPlayer(10f);

                if (!agent.pathPending && agent.remainingDistance <= 0.2f)
                {
                    break;
                }

                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    // ================= CÁC HÀM TÍNH TOÁN =================
    int ChooseSafeAnimationState(out Vector3 targetPos)
    {
        List<int> states = new List<int> { 2, 3, 4, 1 };
        float fullAnimDistance = moveSpeed * changeDirectionInterval;

        foreach (int state in states)
        {
            Vector3 dir = GetVectorByState(state);
            Vector3 desiredTarget = transform.position + dir * fullAnimDistance;

            NavMeshHit hit;
            bool isBlocked = NavMesh.Raycast(transform.position, desiredTarget, out hit, NavMesh.AllAreas);

            if (!isBlocked)
            {
                targetPos = desiredTarget;
                return state;
            }
        }

        targetPos = transform.position;
        return 1;
    }

    Vector3 GetOptimalPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 15f;
        randomDirection += transform.position;
        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, 15f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return transform.position;
    }

    public void SmoothLookAtPlayer(float turnSpeed)
    {
        if (player == null) return;

        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0;

        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }
    }

    void CheckPlayerDistance()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (!isPlayerInZone && distance <= detectRadius)
        {
            isPlayerInZone = true;
        }
        else if (isPlayerInZone && distance > detectRadius * loseRadiusMultiplier)
        {
            isPlayerInZone = false;
        }
    }

    Vector3 GetVectorByState(int state)
    {
        switch (state)
        {
            case 1: return transform.forward;   // Tiến
            case 2: return -transform.forward;  // Lùi
            case 3: return -transform.right;    // Trái
            case 4: return transform.right;     // Phải
            default: return transform.forward;
        }
    }

    public void SetMovementState(int state)
    {
        if (animator != null)
        {
            animator.SetInteger("MovementState", state);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
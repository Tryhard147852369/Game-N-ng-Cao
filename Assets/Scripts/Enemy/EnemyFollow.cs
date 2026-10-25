using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyFollow : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Sensing,
        Chasing,
        Attacking,
        Dead
    }

    [Header("1. Phạm vi")]
    public float detectAndChaseRadius = 12f;
    public float attackRadius = 2f;

    [Header("2. Tốc độ di chuyển")]
    public float patrolSpeed = 2.5f;
    public float chaseSpeed = 5f;
    public float randomPatrolRadius = 20f;

    [Header("3. Cài đặt Tấn công")]
    public float attackCooldown = 0.5f;
    public float attackBlendTime = 0.1f;

    [Header("3.1 Tốc độ gây Damage liên tục (Tự chỉnh trên Inspector)")]
    public float punchDamageInterval = 0.2f;
    public float biteDamageInterval = 0.15f;

    [Header("4. Trạng thái")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("5. Hitbox Attacks")]
    public EnemyHitbox punchHitbox;
    public EnemyHitbox biteHitbox;

    [Header("6. Cài đặt khi Chết")]
    public float destroyDelay = 2.0f;

    [Header("7. Hiển thị Phạm vi (Gizmos)")]
    public bool showGizmosAlways = true;
    public Color chaseRangeColor = new Color(0f, 0.7f, 1f, 1f);
    public Color attackRangeColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float gizmoYOffset = 0.05f;

    [Header("8. Cài đặt Âm thanh (Audio)")]
    public AudioSource audioSource;
    public AudioClip roarSound;

    [Tooltip("Thời gian trễ (giây) trước khi phát âm thanh tính từ lúc chuyển sang Roar. Để 0 nếu muốn phát ngay.")]
    public float audioStartDelay = 0f;

    [Tooltip("Mốc thời gian BẮT ĐẦU phát trong file audio (giây). Để 0 nếu muốn phát từ đầu.")]
    public float audioStartTime = 0f;

    [Tooltip("Mốc thời gian KẾT THÚC phát trong file audio (giây). Để 0 nếu muốn phát đến hết file.")]
    public float audioEndTime = 0f;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private EnemyHealth health;

    private float lastAttackTime;
    private bool isLockedStanding = false;
    private bool hasSensedPlayer = false;

    private bool sensingRunning = false;
    private bool attackRunning = false;
    private bool isDead = false;

    private Coroutine roarAudioRoutine;

    private const string WALK1 = "Creep|Walk1_Action 0";
    private const string WALK2 = "Creep|Walk2_Action";
    private const string SNIFF = "Creep|Sniff_Action";
    private const string ROAR = "Creep|Roar_Action";
    private const string BITE = "Creep|Bite_Action";
    private const string PUNCH = "Creep|Punch_Action";
    private const string DEATH = "Creep|Death_Action";

    #region ANIMATION EVENTS & AUDIO CONTROL
    public void EnablePunchHitbox()
    {
        if (punchHitbox != null && !isDead)
        {
            punchHitbox.damageInterval = punchDamageInterval;
            punchHitbox.EnableHitbox();
        }
    }

    public void DisablePunchHitbox()
    {
        if (punchHitbox != null) punchHitbox.DisableHitbox();
    }

    public void EnableBiteHitbox()
    {
        if (biteHitbox != null && !isDead)
        {
            biteHitbox.damageInterval = biteDamageInterval;
            biteHitbox.EnableHitbox();
        }
    }

    public void DisableBiteHitbox()
    {
        if (biteHitbox != null) biteHitbox.DisableHitbox();
    }

    public void PlayRoarSound()
    {
        if (roarAudioRoutine != null) StopCoroutine(roarAudioRoutine);
        roarAudioRoutine = StartCoroutine(PlayRoarAudioRoutine());
    }

    public void StopRoarSound()
    {
        if (roarAudioRoutine != null)
        {
            StopCoroutine(roarAudioRoutine);
            roarAudioRoutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private IEnumerator PlayRoarAudioRoutine()
    {
        // 1. Độ trễ phát âm thanh
        if (audioStartDelay > 0f)
        {
            yield return new WaitForSeconds(audioStartDelay);
        }

        if (audioSource != null && roarSound != null && !isDead)
        {
            audioSource.clip = roarSound;

            // 2. Tua tới mốc bắt đầu trong file audio
            if (audioStartTime > 0f && audioStartTime < roarSound.length)
            {
                audioSource.time = audioStartTime;
            }
            else
            {
                audioSource.time = 0f;
            }

            audioSource.Play();

            // 3. Tính độ dài cần phát và dừng khi chạm mốc audioEndTime
            float effectiveEndTime = (audioEndTime > audioStartTime && audioEndTime <= roarSound.length)
                ? audioEndTime
                : roarSound.length;

            float playDuration = effectiveEndTime - audioSource.time;

            if (playDuration > 0f)
            {
                yield return new WaitForSeconds(playDuration);
                audioSource.Stop();
            }
        }
    }
    #endregion

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Tìm EnemyHealth linh hoạt trên chính nó, con hoặc cha
        health = GetComponent<EnemyHealth>();
        if (health == null) health = GetComponentInChildren<EnemyHealth>();
        if (health == null) health = GetComponentInParent<EnemyHealth>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        if (animator != null)
            animator.applyRootMotion = false;

        DisablePunchHitbox();
        DisableBiteHitbox();

        ChangeState(EnemyState.Patrol);
        SetRandomDestination();
    }

    void Update()
    {
        if (isDead || currentState == EnemyState.Dead)
            return;

        if (player == null || !player.gameObject.activeInHierarchy)
        {
            StopEnemy();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (isLockedStanding)
        {
            ForceFreezeAgent();
            RotateTowardsPlayer();
            return;
        }

        // 1. TẤN CÔNG
        if (distance <= attackRadius && !sensingRunning)
        {
            if (currentState != EnemyState.Attacking)
            {
                ChangeState(EnemyState.Attacking);
            }

            ForceFreezeAgent();
            RotateTowardsPlayer();

            if (!attackRunning && Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackSequence());
            }
            return;
        }

        // 2. PHÁT HIỆN & TRUY ĐUỔI
        if (distance <= detectAndChaseRadius && !attackRunning)
        {
            if (!hasSensedPlayer && !sensingRunning)
            {
                StartCoroutine(PlaySensingSequenceThenChase());
                return;
            }

            if (!sensingRunning)
            {
                if (currentState != EnemyState.Chasing)
                {
                    ChangeState(EnemyState.Chasing);
                }
                UpdateChaseDestination();
            }
            return;
        }

        // 3. MẤT DẤU -> QUAY LẠI TUẦN TRA
        if (distance > detectAndChaseRadius && !attackRunning && !sensingRunning && !isDead)
        {
            if (currentState != EnemyState.Patrol)
            {
                hasSensedPlayer = false;
                SetInvulnerable(false);
                ChangeState(EnemyState.Patrol);
                SetRandomDestination();
            }

            ExecutePatrol();
        }
    }

    private void SetInvulnerable(bool state)
    {
        if (health != null)
        {
            health.isInvulnerable = state;
        }

        NewEnemyHealth newHealth = GetComponent<NewEnemyHealth>();
        if (newHealth == null) newHealth = GetComponentInChildren<NewEnemyHealth>();
        if (newHealth == null) newHealth = GetComponentInParent<NewEnemyHealth>();

        if (newHealth != null)
        {
            newHealth.isInvulnerable = state;
        }
    }

    void ChangeState(EnemyState newState)
    {
        if (isDead || currentState == EnemyState.Dead) return;

        currentState = newState;

        switch (currentState)
        {
            case EnemyState.Patrol:
                UnfreezeAgent();
                if (agent != null) agent.speed = patrolSpeed;
                PlayAnimationOnce(WALK1, 0.2f);
                break;

            case EnemyState.Chasing:
                isLockedStanding = false;
                UnfreezeAgent();
                if (agent != null) agent.speed = chaseSpeed;
                PlayAnimationOnce(WALK2, 0.2f);
                break;

            case EnemyState.Attacking:
                ForceFreezeAgent();
                break;

            case EnemyState.Sensing:
                ForceFreezeAgent();
                break;
        }
    }

    void UpdateChaseDestination()
    {
        if (player == null || isDead || agent == null || !agent.isActiveAndEnabled) return;

        if (Vector3.Distance(agent.destination, player.position) > 0.2f)
        {
            SafeSetDestination(player.position);
        }
    }

    // ================= CHUỖI SENSING (SNIFF & ROAR) =================
    IEnumerator PlaySensingSequenceThenChase()
    {
        sensingRunning = true;
        isLockedStanding = true;

        SetInvulnerable(true);

        ChangeState(EnemyState.Sensing);

        // 1. Chờ chạy HẾT 100% hoạt ảnh Sniff
        yield return StartCoroutine(PlayAnimationAndWait(SNIFF, 1.0f));
        if (isDead) yield break;

        // 2. Kích hoạt phát tiếng gầm theo các tùy chỉnh audio
        PlayRoarSound();

        // 3. Chờ chạy HẾT 100% hoạt ảnh Roar
        yield return StartCoroutine(PlayAnimationAndWait(ROAR, 1.0f));

        // 4. Ngắt âm thanh ngay khi kết thúc Roar
        StopRoarSound();

        if (isDead) yield break;

        hasSensedPlayer = true;
        isLockedStanding = false;
        sensingRunning = false;

        SetInvulnerable(false);

        ChangeState(EnemyState.Chasing);
    }

    IEnumerator AttackSequence()
    {
        attackRunning = true;

        DisablePunchHitbox();
        DisableBiteHitbox();
        ForceFreezeAgent();

        bool isPunch = Random.value > 0.5f;
        string attackAnimation = isPunch ? PUNCH : BITE;

        if (animator != null && animator.isActiveAndEnabled && !isDead)
        {
            animator.Play(attackAnimation, 0, 0f);
            animator.Update(0f);
        }

        yield return null;

        while (animator != null && animator.isActiveAndEnabled && !isDead)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);

            if (!current.IsName(attackAnimation) || current.normalizedTime >= 0.95f)
            {
                break;
            }

            ForceFreezeAgent();
            RotateTowardsPlayer();
            yield return null;
        }

        DisablePunchHitbox();
        DisableBiteHitbox();

        if (animator != null && !isDead)
        {
            animator.CrossFadeInFixedTime(WALK2, 0.1f);
        }

        lastAttackTime = Time.time;
        attackRunning = false;

        if (player != null && !isDead)
        {
            float currentDistance = Vector3.Distance(transform.position, player.position);
            if (currentDistance > attackRadius && currentDistance <= detectAndChaseRadius)
            {
                ChangeState(EnemyState.Chasing);
            }
        }
    }

    void ExecutePatrol()
    {
        if (agent == null || !agent.isActiveAndEnabled || isDead) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            SetRandomDestination();
        }
    }

    // ================= XỬ LÝ CHẾT =================
    public void TriggerDeath()
    {
        if (isDead) return;

        isDead = true;
        currentState = EnemyState.Dead;

        StopAllCoroutines();
        DisablePunchHitbox();
        DisableBiteHitbox();

        // Tắt âm thanh gầm nếu quái bị tiêu diệt giữa chừng
        StopRoarSound();

        SetInvulnerable(false);

        if (agent != null)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            agent.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        if (animator != null)
        {
            animator.speed = 1f;
            animator.Play(DEATH, 0, 0f);
            yield return null;

            while (animator != null && animator.isActiveAndEnabled)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(DEATH) && state.normalizedTime >= 0.98f)
                {
                    animator.speed = 0f;
                    break;
                }
                yield return null;
            }
        }

        yield return new WaitForSeconds(destroyDelay);

        Destroy(gameObject);
    }

    void PlayAnimationOnce(string animName, float transitionDuration = 0.2f)
    {
        if (animator == null || !animator.gameObject.activeInHierarchy || isDead) return;

        animator.CrossFadeInFixedTime(animName, transitionDuration);
    }

    IEnumerator PlayAnimationAndWait(string animationName, float targetNormalizedTime = 1.0f)
    {
        if (animator == null || !animator.gameObject.activeInHierarchy || isDead) yield break;

        animator.CrossFadeInFixedTime(animationName, 0.15f);
        yield return null;

        float timeout = 0f;
        while (animator != null && animator.isActiveAndEnabled && !isDead)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(animationName)) break;

            yield return null;
            timeout += Time.deltaTime;
            if (timeout > 0.5f) break;
        }

        while (animator != null && animator.isActiveAndEnabled && !isDead)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(animationName)) yield break;
            if (state.normalizedTime >= targetNormalizedTime) break;

            if (isLockedStanding)
            {
                RotateTowardsPlayer();
            }

            yield return null;
        }
    }

    void ForceFreezeAgent()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            if (agent.hasPath) agent.ResetPath();
        }
    }

    void UnfreezeAgent()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !isDead)
            agent.isStopped = false;
    }

    void SafeSetDestination(Vector3 destination)
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !isDead)
        {
            agent.SetDestination(destination);
        }
    }

    void RotateTowardsPlayer()
    {
        if (player == null || isDead) return;
        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }
    }

    void SetRandomDestination()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh || isDead) return;

        Vector3 randomDirection = Random.insideUnitSphere * randomPatrolRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, randomPatrolRadius, NavMesh.AllAreas))
        {
            SafeSetDestination(hit.position);
        }
    }

    void StopEnemy()
    {
        isLockedStanding = true;
        ForceFreezeAgent();
    }

    void OnDrawGizmos()
    {
        if (showGizmosAlways)
        {
            DrawGroundRanges();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmosAlways)
        {
            DrawGroundRanges();
        }
    }

    void DrawGroundRanges()
    {
        Vector3 groundCenter = transform.position + Vector3.up * gizmoYOffset;

        Gizmos.color = chaseRangeColor;
        DrawWireDisk(groundCenter, detectAndChaseRadius);

        Gizmos.color = attackRangeColor;
        DrawWireDisk(groundCenter, attackRadius);
    }

    void DrawWireDisk(Vector3 center, float radius)
    {
        int segments = 48;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}
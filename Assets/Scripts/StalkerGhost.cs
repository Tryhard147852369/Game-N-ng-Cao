using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class StalkerGhost : MonoBehaviour
{
    [Header("Target & Movement")]
    public Transform player;
    public float rushSpeed = 14f;
    public float detectDistance = 12f;
    public float jumpscareDistance = 2.5f; // Tăng khoảng cách kích hoạt

    [Header("Audio & Effects")]
    public AudioSource audioSource;
    public AudioClip jumpscareSound;
    public GameObject jumpscareUI;

    private NavMeshAgent agent;
    private bool isRushing = false;
    private bool hasTriggered = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        isRushing = false;
        hasTriggered = false;
        if (agent != null) agent.enabled = false;
    }

    void Update()
    {
        if (player == null || hasTriggered) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. Trạng thái nhìn lén
        if (!isRushing)
        {
            Vector3 lookDir = (player.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }

            if (distanceToPlayer <= detectDistance)
            {
                StartRush();
            }
        }
        // 2. Trạng thái lao tới
        else
        {
            if (agent != null && agent.enabled)
            {
                agent.SetDestination(player.position);
            }

            // Chạm đủ gần thì biến mất
            if (distanceToPlayer <= jumpscareDistance)
            {
                StartCoroutine(TriggerJumpscare());
            }
        }
    }

    void StartRush()
    {
        isRushing = true;
        if (agent != null)
        {
            agent.enabled = true;
            agent.speed = rushSpeed;
            agent.stoppingDistance = 0f; // Bắt buộc lao sát rạt
        }
    }

    // Bắt va chạm vật lý trực tiếp
    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && isRushing && other.CompareTag("Player"))
        {
            StartCoroutine(TriggerJumpscare());
        }
    }

    IEnumerator TriggerJumpscare()
    {
        hasTriggered = true;
        isRushing = false;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Âm thanh & Effect
        if (audioSource != null && jumpscareSound != null)
        {
            audioSource.PlayOneShot(jumpscareSound);
        }

        if (jumpscareUI != null) jumpscareUI.SetActive(true);

        yield return new WaitForSeconds(0.4f);

        if (jumpscareUI != null) jumpscareUI.SetActive(false);

        // Biến mất hoàn toàn
        gameObject.SetActive(false);
    }

    public void AppearAt(Vector3 newPosition)
    {
        transform.position = newPosition;
        gameObject.SetActive(true);
    }
}
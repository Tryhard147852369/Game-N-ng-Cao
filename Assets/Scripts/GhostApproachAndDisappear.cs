using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GhostApproachAndDisappear : MonoBehaviour
{
    [Header("1. Tham Số Người Chơi & Di Chuyển")]
    public Transform player;             // Kéo GameObject Player vào đây
    public float detectRange = 10f;      // Khoảng cách phát hiện để bắt đầu lao tới
    public float moveSpeed = 8f;         // Tốc độ lao về phía người chơi
    public float triggerDistance = 2.5f; // Khoảng cách đủ gần để bắt đầu nhấp nháy & biến mất
    public bool useNavMesh = false;      // Tích chọn nếu bạn dùng NavMeshAgent

    [Header("2. Hiệu Ứng Nhấp Nháy (Màn Hình / Quái)")]
    public GameObject blackScreenUI;    // Kéo BlackScreenUI (màn hình đen) vào đây
    public Renderer ghostRenderer;      // (Tùy chọn) Kéo MeshRenderer của quái vào nếu muốn quái tự nhấp nháy
    public int blinkCount = 4;          // Số lần nhấp nháy
    public float blinkInterval = 0.07f; // Tốc độ nhấp nháy (tính bằng giây)

    [Header("3. Âm Thanh (Tùy chọn)")]
    public AudioSource audioSource;
    public AudioClip scareSound;

    private NavMeshAgent agent;
    private bool isApproaching = false;
    private bool isTriggered = false;

    void Start()
    {
        if (useNavMesh)
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.speed = moveSpeed;
        }

        if (ghostRenderer == null)
        {
            ghostRenderer = GetComponentInChildren<Renderer>();
        }
    }

    void Update()
    {
        if (player == null || isTriggered) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Luôn xoay mặt nhìn về phía Player
        Vector3 lookTarget = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookTarget);

        // Trạng thái 1: Chưa tiếp cận -> Kiểm tra khoảng cách
        if (!isApproaching)
        {
            if (distance <= detectRange)
            {
                isApproaching = true;
            }
        }
        // Trạng thái 2: Đang di chuyển tiếp cận
        else
        {
            // Di chuyển bằng NavMesh hoặc di chuyển thường
            if (useNavMesh && agent != null && agent.enabled)
            {
                agent.SetDestination(player.position);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPositionFixY(player.position), moveSpeed * Time.deltaTime);
            }

            // Khi đã tới đủ gần người chơi -> Kích hoạt nhấp nháy & biến mất
            if (distance <= triggerDistance)
            {
                StartCoroutine(FlickerAndDisappearRoutine());
            }
        }
    }

    Vector3 targetPositionFixY(Vector3 target)
    {
        return new Vector3(target.x, transform.position.y, target.z);
    }

    IEnumerator FlickerAndDisappearRoutine()
    {
        isTriggered = true;

        // Dừng di chuyển
        if (useNavMesh && agent != null && agent.enabled)
        {
            agent.isStopped = true;
        }

        // Phát tiếng hù / giật mình
        if (audioSource != null && scareSound != null)
        {
            audioSource.PlayOneShot(scareSound);
        }

        // Vòng lặp nhấp nháy
        for (int i = 0; i < blinkCount; i++)
        {
            // Bật màn hình đen / Tắt quái
            if (blackScreenUI != null) blackScreenUI.SetActive(true);
            if (ghostRenderer != null) ghostRenderer.enabled = false;

            yield return new WaitForSeconds(blinkInterval);

            // Tắt màn hình đen / Bật quái
            if (blackScreenUI != null) blackScreenUI.SetActive(false);
            if (ghostRenderer != null) ghostRenderer.enabled = true;

            yield return new WaitForSeconds(blinkInterval);
        }

        // Đợi chớp đen khoảnh khắc cuối để giấu việc ẩn hẳn quái
        if (blackScreenUI != null) blackScreenUI.SetActive(true);
        yield return new WaitForSeconds(0.05f);

        // Biến mất hoàn toàn
        gameObject.SetActive(false);

        // Trả lại màn hình bình thường
        if (blackScreenUI != null) blackScreenUI.SetActive(false);
    }

    // Vẽ vòng tròn phạm vi trong tab Scene để dễ quan sát
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}
using System.Collections;
using UnityEngine;

public class GhostFlickerDisappear : MonoBehaviour
{
    [Header("Cấu hình Khoảng Cách")]
    public Transform player;            // Drag Player vào đây
    public float triggerDistance = 3f;  // Khoảng cách tới gần để kích hoạt nhấp nháy

    [Header("Cấu hình Hiệu Ứng UI")]
    public GameObject blackScreenUI;    // Drag Panel màu đen (BlackScreenUI) vào đây
    public int blinkCount = 3;          // Số lần màn hình chớp tắt
    public float blinkInterval = 0.08f; // Tốc độ nhấp nháy (giây)

    [Header("Âm Thanh (Tùy chọn)")]
    public AudioSource audioSource;
    public AudioClip glitchOrScareSound;

    private bool isTriggered = false;

    void Update()
    {
        if (isTriggered || player == null) return;

        // Tính khoảng cách giữa Player và Quái
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= triggerDistance)
        {
            StartCoroutine(FlickerAndDisappearRoutine());
        }
    }

    IEnumerator FlickerAndDisappearRoutine()
    {
        isTriggered = true;

        // Phát tiếng động nhấp nháy / dị thường (nếu có)
        if (audioSource != null && glitchOrScareSound != null)
        {
            audioSource.PlayOneShot(glitchOrScareSound);
        }

        // Vòng lặp nhấp nháy màn hình (Bật đen -> Tắt đen)
        for (int i = 0; i < blinkCount; i++)
        {
            if (blackScreenUI != null) blackScreenUI.SetActive(true);  // Tắt màn hình (tối đen)
            yield return new WaitForSeconds(blinkInterval);

            if (blackScreenUI != null) blackScreenUI.SetActive(false); // Bật lại màn hình
            yield return new WaitForSeconds(blinkInterval);
        }

        // Bật màn hình tối trong tích tắc để giấu việc quái biến mất
        if (blackScreenUI != null) blackScreenUI.SetActive(true);
        yield return new WaitForSeconds(0.05f);

        // Quái biến mất
        gameObject.SetActive(false);

        // Trả lại màn hình bình thường
        if (blackScreenUI != null) blackScreenUI.SetActive(false);
    }

    // Hiển thị vòng tròn khoảng cách trong tab Scene để dễ căn chỉnh
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}
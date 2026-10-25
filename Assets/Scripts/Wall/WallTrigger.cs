using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WallTrigger : MonoBehaviour
{
    [Header("UI Component References")]
    public GameObject victoryPanel;
    public TextMeshProUGUI messageText;

    [Header("--- ẨN CÁC UI KHÁC KHI THẮNG (Tùy chọn) ---")]
    [Tooltip("Kéo các UI/HUD cần ẩn khi hiện bảng Victory vào đây (ví dụ: Thanh máu, Tâm ngắm, Kho đồ...)")]
    public GameObject[] elementsToHide;

    [Header("Cấu hình Màu sắc")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.85f); // Nền đen mờ
    public Color textColor = new Color(1f, 0.84f, 0f, 1f);       // Chữ vàng kim

    [Header("Cấu hình Thông báo")]
    [TextArea(2, 5)]
    public string customMessage = "Bạn đã chiến thắng!! GG";
    public float autoHideDelay = 0f; // Để 0 nếu muốn bảng victory hiện vĩnh viễn

    [Header("--- CẤU HÌNH DỪNG GAME & ÂM THANH ---")]
    [Tooltip("Kéo đối tượng Basement_Wall_Var1 (chứa script WallPassAudio) vào đây")]
    public WallPassAudio basementWallAudio;

    [Tooltip("Thời gian (giây) âm thanh tiếp tục phát từ thời điểm chạm tường trước khi dừng hẳn.")]
    public float audioDuration = 5f;

    private Coroutine hideCoroutine;
    private Coroutine audioStopCoroutine;
    private bool isReadyToTrigger = false;

    private void Awake()
    {
        // Ẩn bảng thông báo ngay từ lúc khởi tạo Scene
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }

    private IEnumerator Start()
    {
        Time.timeScale = 1f;

        // Đợi 0.5 giây sau khi load game xong mới bật nhận va chạm
        yield return new WaitForSeconds(0.5f);
        isReadyToTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Nếu chưa hết 0.5s đầu game -> Bỏ qua va chạm
        if (!isReadyToTrigger) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("<color=green>[SUCCESS]</color> Player đã chạm vào tường! Hiển thị thông báo Victory và giữ âm thanh.");
            ShowMessage();
        }
    }

    public void ShowMessage()
    {
        // 1. DỪNG TOÀN BỘ HOẠT ĐỘNG GAME (PLAYER, QUÁI, VẬT LÝ DỪNG LẠI)
        Time.timeScale = 0f;

        // 2. ẨN TẤT CẢ CÁC UI KHÁC TRÊN MÀN HÌNH (NẾU CÓ CÀI ĐẶT)
        if (elementsToHide != null && elementsToHide.Length > 0)
        {
            foreach (GameObject element in elementsToHide)
            {
                if (element != null)
                {
                    element.SetActive(false);
                }
            }
        }

        // 3. XỬ LÝ ÂM THANH (TIẾP TỤC PHÁT VÀ ĐẾM NGƯỢC 5S ĐỂ TẮT)
        if (basementWallAudio != null)
        {
            // Kiểm tra: Nếu chưa phát thì mới bật, nếu ĐANG PHÁT THÌ KHÔNG PHÁT LẠI (tránh bị reset âm thanh)
            if (basementWallAudio.audioSource != null && !basementWallAudio.audioSource.isPlaying)
            {
                basementWallAudio.PlayPassSound();
            }

            // Bắt đầu đếm ngược 5 giây thời gian thực để dừng âm thanh
            if (audioStopCoroutine != null) StopCoroutine(audioStopCoroutine);
            audioStopCoroutine = StartCoroutine(StopAudioAfterDelayRoutine(audioDuration));
        }

        // 4. HIỂN THỊ UI VICTORY
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);

            Image panelImage = victoryPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = backgroundColor;
            }

            CanvasGroup cg = victoryPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            messageText.text = customMessage;
            messageText.color = textColor;
        }

        // 5. TỰ ĐỘNG ẨN VICTORY PANEL (NẾU AUTO HIDE DELAY > 0)
        if (autoHideDelay > 0f)
        {
            if (hideCoroutine != null) StopCoroutine(hideCoroutine);
            hideCoroutine = StartCoroutine(HideMessageRoutine());
        }
    }

    private IEnumerator StopAudioAfterDelayRoutine(float delay)
    {
        // WaitForSecondsRealtime đếm giây chính xác kể cả khi Time.timeScale = 0
        yield return new WaitForSecondsRealtime(delay);

        if (basementWallAudio != null)
        {
            basementWallAudio.StopPassSound();
        }
    }

    private IEnumerator HideMessageRoutine()
    {
        yield return new WaitForSecondsRealtime(autoHideDelay);

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }
}
using UnityEngine;
using TMPro;

public class ItemNotification : MonoBehaviour
{
    public static ItemNotification Instance;
    public TMP_Text notificationText;
    private float hideTimer;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Ẩn toàn bộ chữ ngay khi vừa load Scene
        if (notificationText != null)
        {
            notificationText.text = "";
        }
    }

    void Update()
    {
        if (hideTimer > 0)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0)
            {
                notificationText.text = "";
            }
        }
    }

    public void ShowNotification(string message, float timeDuration = 2f)
    {
        if (notificationText == null) return;
        notificationText.text = message;
        hideTimer = timeDuration;
    }

    public void ShowHint(string message)
    {
        if (notificationText == null) return;
        notificationText.text = message;
        hideTimer = 0.1f;
    }

    public void ClearHint()
    {
        if (notificationText == null) return;
        if (hideTimer <= 0)
        {
            notificationText.text = "";
        }
    }
}
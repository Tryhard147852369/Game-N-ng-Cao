using UnityEngine;

/// <summary>Fits this inventory only, without changing the Canvas or the other HUDs.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class GothicInventoryLayout : MonoBehaviour
{
    public RectTransform frame;
    public bool hotbar;
    private void OnEnable() { Fit(); }
    private void LateUpdate() { Fit(); }
    private void Fit()
    {
        var parent = transform as RectTransform;
        if (frame == null || parent == null) return;
        // Chừa khoảng trống bên dưới cho thanh trang bị nhanh.
        float scale = Mathf.Min(1, parent.rect.width / 900f, parent.rect.height / 840f);
        if (hotbar) scale = Mathf.Min(1, parent.rect.width / 1000f, parent.rect.height / 600f);
        scale = Mathf.Max(.1f, scale);
        var desired = Vector3.one * scale;
        if (frame.localScale != desired) frame.localScale = desired;
        if (!hotbar) frame.anchoredPosition = new Vector2(0, parent.rect.height * .11f);
    }
}

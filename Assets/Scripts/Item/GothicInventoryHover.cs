using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class GothicInventoryHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GothicInventoryGraphic frame;
    private bool hovered;

    private void Awake() { if (frame == null) frame = GetComponentInChildren<GothicInventoryGraphic>(true); }
    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; }
    private void Update()
    {
        if (frame == null) return;
        float target = hovered ? 1 : 0;
        if (Mathf.Abs(frame.highlight - target) < .001f) return;
        frame.highlight = Mathf.MoveTowards(frame.highlight, target, Time.unscaledDeltaTime * 7);
        frame.SetVerticesDirty();
    }
    private void OnDisable()
    {
        hovered = false;
        if (frame == null) return;
        frame.highlight = 0;
        frame.SetVerticesDirty();
    }
}

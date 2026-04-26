using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Converts screen-space pointer/touch events into normalised UV coordinates
/// (0,0 = bottom-left of image, 1,1 = top-right) and broadcasts them via event.
///
/// Attach alongside the RawImage that displays the coloring canvas.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class CanvasInputHandler : MonoBehaviour, IPointerClickHandler
{
    /// <summary>Fired whenever the user taps inside the canvas bounds.</summary>
    public event Action<Vector2> OnCanvasTapped;

    private RectTransform _rect;
    private Canvas        _parentCanvas;

    private void Awake()
    {
        _rect         = GetComponent<RectTransform>();
        _parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerClick(PointerEventData evt)
    {
        // Choose the right camera based on Canvas render mode
        Camera cam = (_parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _parentCanvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rect, evt.position, cam, out Vector2 local))
            return;

        Rect r  = _rect.rect;
        float nx = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float ny = Mathf.InverseLerp(r.yMin, r.yMax, local.y);

        // Clamp to guarantee we're inside the image
        if (nx < 0f || nx > 1f || ny < 0f || ny > 1f) return;

        OnCanvasTapped?.Invoke(new Vector2(nx, ny));
    }
}

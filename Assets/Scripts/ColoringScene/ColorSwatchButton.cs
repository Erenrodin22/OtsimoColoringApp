using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One coloured circle in the right-hand palette toolbar.
/// Handles its own visual state (normal / selected) via DOTween.
/// </summary>
public sealed class ColorSwatchButton : MonoBehaviour
{
    [SerializeField] private Image  _fill;          // main colour disc
    [SerializeField] private Image  _selectionRing; // outer ring shown when selected
    [SerializeField] private Button _button;

    private const float ScaleNormal   = 1.00f;
    private const float ScaleSelected = 1.22f;
    private const float ScaleBounce   = 1.38f;  // overshoot before settling

    public Color SwatchColor => _fill.color;

    // ── Setup ──────────────────────────────────────────────────────────────────

    public void Initialise(Color color, Action onTap)
    {
        _fill.color = color;
        _selectionRing.gameObject.SetActive(false);
        _button.onClick.AddListener(() => onTap?.Invoke());
    }

    // ── Selection state ────────────────────────────────────────────────────────

    public void SetSelected(bool selected, bool animate)
    {
        _selectionRing.gameObject.SetActive(selected);

        transform.DOKill(complete: false);

        if (!animate)
        {
            transform.localScale = Vector3.one * (selected ? ScaleSelected : ScaleNormal);
            return;
        }

        if (selected)
        {
            // Quick two-step bounce: overshoot → settle
            transform
                .DOScale(ScaleBounce, 0.10f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                    transform.DOScale(ScaleSelected, 0.09f).SetEase(Ease.InQuad));
        }
        else
        {
            transform
                .DOScale(ScaleNormal, 0.14f)
                .SetEase(Ease.OutQuad);
        }
    }
}

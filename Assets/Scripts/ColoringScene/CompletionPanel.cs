using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// "Great job!" celebration panel that slides / fades in when a level is finished.
/// All animations are DOTween; input is blocked via CanvasGroup.blocksRaycasts.
/// </summary>
public sealed class CompletionPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup    _group;
    [SerializeField] private RectTransform  _card;
    [SerializeField] private Image          _starIcon;
    [SerializeField] private TMP_Text       _headline;
    [SerializeField] private Button         _backToLevelsButton;

    public event Action OnBackRequested;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Start invisible and non-interactive
        _group.alpha          = 0f;
        _group.interactable   = false;
        _group.blocksRaycasts = false;
        _card.localScale      = Vector3.one * 0.4f;
        gameObject.SetActive(false);

        _backToLevelsButton.onClick.AddListener(() => OnBackRequested?.Invoke());
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void Show()
    {
        gameObject.SetActive(true);

        // Block all input immediately (spec requirement: no input during celebration)
        _group.blocksRaycasts = true;

        // Dim backdrop
        _group
            .DOFade(1f, 0.30f)
            .SetEase(Ease.OutQuad);

        // Card bounces in from small scale
        _card
            .DOScale(1.06f, 0.38f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
                _card.DOScale(1f, 0.14f).SetEase(Ease.InQuad)
                     .OnComplete(() => _group.interactable = true));

        // Star spins in with a satisfying whirl
        _starIcon.transform
            .DORotate(new Vector3(0f, 0f, 360f), 0.55f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic)
            .SetDelay(0.20f);

        // Headline punches in
        if (_headline != null)
        {
            _headline.transform.localScale = Vector3.zero;
            _headline.transform
                .DOScale(1f, 0.30f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.18f);
        }
    }
}

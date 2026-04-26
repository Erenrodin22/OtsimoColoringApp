using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen black overlay that fades in/out to mask scene transitions.
///
/// Place one instance on a DontDestroyOnLoad canvas (or on the root canvas
/// of each scene as the topmost element). Wire to <see cref="LevelGridView"/>
/// and <see cref="ColoringController"/> via Inspector.
///
/// The overlay starts transparent so the scene appears immediately on first load.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class SceneFader : MonoBehaviour
{
    [SerializeField] private float _fadeOutDuration = 0.25f;  // screen → black
    [SerializeField] private float _fadeInDuration = 0.30f;  // black → scene

    private CanvasGroup _group;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        _group.interactable = false;
    }

    private void Start()
    {
        // Fade the scene in when it loads
        FadeIn(null);
    }

    /// <summary>Fades to black then invokes <paramref name="onComplete"/>.</summary>
    public void FadeOut(Action onComplete)
    {
        _group.blocksRaycasts = true;
        _group
            .DOFade(1f, _fadeOutDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>Fades from black to transparent. Called automatically on scene start.</summary>
    public void FadeIn(Action onComplete)
    {
        _group.alpha = 1f;
        _group
            .DOFade(0f, _fadeInDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _group.blocksRaycasts = false;
                onComplete?.Invoke();
            });
    }
}
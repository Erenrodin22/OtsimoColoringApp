using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class LevelCell : MonoBehaviour
{
    [SerializeField] private RawImage _thumbnail;
    [SerializeField] private Image _completedBadge;
    [SerializeField] private TMP_Text _titleLabel;
    [SerializeField] private Button _button;
    [SerializeField] private CanvasGroup _group;

    private Action<LevelData> _onSelected;
    private LevelData _data;

    public void Setup(LevelData data, bool isCompleted, Action<LevelData> onSelected)
    {
        _data = data;
        _onSelected = onSelected;

        
        if (_thumbnail != null && data.LineArt != null)
            _thumbnail.texture = data.LineArt;

       
        if (_titleLabel != null)
            _titleLabel.text = data.DisplayName;

        
        if (_completedBadge != null)
            _completedBadge.gameObject.SetActive(isCompleted);

        
        if (_button != null)
            _button.onClick.AddListener(OnTapped);

        
        transform.localScale = Vector3.zero;
        float stagger = data.LevelNumber * 0.07f;

        transform
            .DOScale(1.08f, 0.28f)
            .SetDelay(stagger)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
                transform.DOScale(1f, 0.12f).SetEase(Ease.InQuad));
    }

    private void OnTapped()
    {
        if (_button != null)
            _button.interactable = false;

        transform
            .DOScale(0.90f, 0.07f)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
                transform
                    .DOScale(1f, 0.06f)
                    .OnComplete(() => _onSelected?.Invoke(_data)));
    }
}
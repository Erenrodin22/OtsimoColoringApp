using System;
using UnityEngine;

/// <summary>
/// Spawns and manages the six colour swatches in the right-hand toolbar.
/// Notifies subscribers when the active colour changes.
/// </summary>
public sealed class ColorPaletteView : MonoBehaviour
{
    [SerializeField] private ColorSwatchButton _swatchPrefab;
    [SerializeField] private Transform         _container;

    private ColorSwatchButton[] _swatches;
    private int                 _activeIndex = -1;

    /// <summary>Currently selected colour.</summary>
    public Color ActiveColor => _swatches?[_activeIndex].SwatchColor ?? Color.white;

    // ── Initialisation ─────────────────────────────────────────────────────────

    /// <param name="settings">Source of palette colours</param>
    /// <param name="savedIndex">Colour index persisted from the previous session</param>
    /// <param name="onChanged">Callback fired with (index, color) on selection change</param>
    public void Initialise(GameSettings settings, int savedIndex, Action<int, Color> onChanged)
    {
        Color[] palette = settings.PaletteColors;
        _swatches       = new ColorSwatchButton[palette.Length];

        for (int i = 0; i < palette.Length; i++)
        {
            ColorSwatchButton btn = Instantiate(_swatchPrefab, _container);
            int captured = i;   // close over the correct index
            btn.Initialise(palette[i], () => HandleSwatchTapped(captured, onChanged));
            _swatches[i] = btn;
        }

        // Restore last-used colour without playing the selection animation
        int startIndex = Mathf.Clamp(savedIndex, 0, _swatches.Length - 1);
        _activeIndex   = startIndex;
        _swatches[startIndex].SetSelected(true, animate: false);
    }

    // ── Selection logic ────────────────────────────────────────────────────────

    private void HandleSwatchTapped(int index, Action<int, Color> onChanged)
    {
        if (index == _activeIndex) return;

        _swatches[_activeIndex].SetSelected(false, animate: true);
        _activeIndex = index;
        _swatches[_activeIndex].SetSelected(true, animate: true);

        onChanged?.Invoke(index, _swatches[index].SwatchColor);
    }
}

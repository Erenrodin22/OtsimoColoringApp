using UnityEngine;

/// <summary>
/// Single source of truth for every magic number in the app.
/// Create via Assets → ColoringApp → Game Settings.
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "ColoringApp/Game Settings")]
public sealed class GameSettings : ScriptableObject
{
    // ── Palette ────────────────────────────────────────────────────────────────

    [Header("Color Palette (exactly 6 entries)")]
    [SerializeField] private Color[] _paletteColors =
    {
        new Color(0.929f, 0.110f, 0.141f),   // Crimson
        new Color(0.204f, 0.596f, 0.859f),   // Sky Blue
        new Color(0.180f, 0.800f, 0.443f),   // Mint
        new Color(1.000f, 0.843f, 0.000f),   // Sunflower
        new Color(0.698f, 0.404f, 0.851f),   // Lavender
        new Color(1.000f, 0.573f, 0.118f),   // Tangerine
    };

    // ── Flood-fill ─────────────────────────────────────────────────────────────

    [Header("Flood Fill")]
    [Tooltip("Luminance below this value is treated as outline (0 = black only, 0.5 = any mid-tone)")]
    [SerializeField, Range(0f, 0.5f)] private float _outlineThreshold = 0.25f;

    [Tooltip("Max colour distance between seed and neighbour for them to be connected")]
    [SerializeField, Range(0f, 0.5f)] private float _colorTolerance = 0.08f;

    // ── Completion ─────────────────────────────────────────────────────────────

    [Header("Completion")]
    [Tooltip("Fraction of non-outline pixels that must be painted before celebration fires")]
    [SerializeField, Range(0.5f, 1f)] private float _completionRatio = 0.90f;

    // ── Public API ─────────────────────────────────────────────────────────────

    public Color[] PaletteColors    => _paletteColors;
    public float   OutlineThreshold => _outlineThreshold;
    public float   ColorTolerance   => _colorTolerance;
    public float   CompletionRatio  => _completionRatio;
}

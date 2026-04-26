using UnityEngine;

/// <summary>
/// Data asset that describes one coloring level.
/// Drop a new asset in Resources/Levels/ to add a level — no code changes needed.
/// </summary>
[CreateAssetMenu(fileName = "Level_01", menuName = "ColoringApp/Level")]
public sealed class LevelData : ScriptableObject
{
    [SerializeField] private int       _levelNumber;
    [SerializeField] private string    _displayName = "Level";

    [Tooltip("The black-on-white line-art texture. Must be Read/Write enabled in Import Settings.")]
    [SerializeField] private Texture2D _lineArt;

    public int       LevelNumber => _levelNumber;
    public string    DisplayName => _displayName;
    public Texture2D LineArt     => _lineArt;
}

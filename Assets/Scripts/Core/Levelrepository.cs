using System.Linq;
using UnityEngine;

/// <summary>
/// Loads all <see cref="LevelData"/> assets placed inside any
/// <c>Resources/Levels/</c> folder at runtime, sorted by level number.
///
/// Usage: drop new LevelData assets into Resources/Levels/ — nothing else required.
/// </summary>
public static class LevelRepository
{
    private const string ResourcePath = "Levels";

    private static LevelData[] _cachedLevels;

    /// <summary>Returns all levels sorted ascending by <see cref="LevelData.LevelNumber"/>.</summary>
    public static LevelData[] All
    {
        get
        {
            if (_cachedLevels == null)
                _cachedLevels = Resources.LoadAll<LevelData>(ResourcePath)
                                         .OrderBy(l => l.LevelNumber)
                                         .ToArray();
            return _cachedLevels;
        }
    }

    /// <summary>
    /// Clears the in-memory cache. Call this in the Editor when assets change
    /// (the cache is rebuilt on next access anyway in a fresh play session).
    /// </summary>
    public static void InvalidateCache() => _cachedLevels = null;
}
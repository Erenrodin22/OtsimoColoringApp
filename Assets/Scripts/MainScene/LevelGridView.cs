using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Populates the level-selection grid in MainScene.
/// Levels are discovered automatically from <see cref="LevelRepository"/>
/// (Resources/Levels/) — no Inspector wiring needed when adding new levels.
/// </summary>
public sealed class LevelGridView : MonoBehaviour
{
    [SerializeField] private LevelCell  _cellPrefab;
    [SerializeField] private Transform  _gridRoot;
    [SerializeField] private SceneFader _fader;     // optional; null-safe

    private ProgressService _progress;

    private void Awake()
    {
        _progress = new ProgressService();
        DOTween.Init(recycleAllByDefault: true, useSafeMode: false);
    }

    private void Start()
    {
        LevelData[] levels = LevelRepository.All;

        if (levels.Length == 0)
        {
            Debug.LogWarning("[LevelGridView] No LevelData assets found in Resources/Levels/");
            return;
        }

        foreach (LevelData data in levels)
        {
            LevelCell cell = Instantiate(_cellPrefab, _gridRoot);
            cell.Setup(data, _progress.IsCompleted(data.LevelNumber), HandleLevelSelected);
        }
    }

    private void HandleLevelSelected(LevelData data)
    {
        LevelSelection.ActiveLevel = data;

        if (_fader != null)
            _fader.FadeOut(() => SceneManager.LoadScene("ColoringScene"));
        else
            SceneManager.LoadScene("ColoringScene");
    }
}
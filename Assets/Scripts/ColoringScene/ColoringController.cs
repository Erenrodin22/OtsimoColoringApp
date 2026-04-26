using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Top-level controller for ColoringScene.
/// Wires together all sub-systems via SerializeField references — no singletons.
///
/// Responsibilities:
///   • Bootstrap FloodFillProcessor and ColorPaletteView
///   • Route canvas tap events to the fill processor
///   • React to fill completion (persistence + celebration)
///   • Handle Back button (save progress, return to MainScene)
/// </summary>
public sealed class ColoringController : MonoBehaviour
{
    [Header("Sub-systems")]
    [SerializeField] private GameSettings       _settings;
    [SerializeField] private FloodFillProcessor _fillProcessor;
    [SerializeField] private ColorPaletteView   _paletteView;
    [SerializeField] private CompletionPanel    _completionPanel;
    [SerializeField] private CanvasInputHandler _canvasInput;

    [Header("UI")]
    [SerializeField] private Button _backButton;

    // ── State ──────────────────────────────────────────────────────────────────

    private ProgressService _progress;
    private LevelData       _level;
    private bool            _celebrating;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        _progress = new ProgressService();
        _level    = LevelSelection.ActiveLevel;

        if (_level == null || _level.LineArt == null)
        {
            Debug.LogError("[ColoringController] No valid level selected. Returning to MainScene.");
            SceneManager.LoadScene("MainScene");
            return;
        }

        DOTween.Init(recycleAllByDefault: true, useSafeMode: false);
    }

    private void Start()
    {
        // ── Fill processor ─────────────────────────────────────────────────────
        _fillProcessor.Initialise(_level.LevelNumber, _level.LineArt, _progress);
        _fillProcessor.OnFillCompleted += HandleFillProgress;

        // ── Palette ────────────────────────────────────────────────────────────
        int savedColor = _progress.GetColorIndex();
        _paletteView.Initialise(_settings, savedColor, HandleColorChanged);

        // ── Completion panel ───────────────────────────────────────────────────
        _completionPanel.OnBackRequested += HandleBackFromCompletion;

        // ── Input ──────────────────────────────────────────────────────────────
        _canvasInput.OnCanvasTapped += HandleCanvasTapped;
        _backButton.onClick.AddListener(HandleBackButton);
    }

    private void OnDestroy()
    {
        if (_fillProcessor   != null) _fillProcessor.OnFillCompleted -= HandleFillProgress;
        if (_canvasInput     != null) _canvasInput.OnCanvasTapped    -= HandleCanvasTapped;
        if (_completionPanel != null) _completionPanel.OnBackRequested -= HandleBackFromCompletion;
    }

    // ── Event handlers ─────────────────────────────────────────────────────────

    private void HandleCanvasTapped(Vector2 uv)
    {
        if (_celebrating || _fillProcessor.IsBusy) return;

        Color32 c = (Color32)_paletteView.ActiveColor;
        _fillProcessor.RequestFill(uv, c);
    }

    private void HandleColorChanged(int index, Color color)
    {
        _progress.SaveColorIndex(index);
    }

    private void HandleFillProgress(float paintedRatio)
    {
        if (!_celebrating && paintedRatio >= _settings.CompletionRatio)
            TriggerCelebration();
    }

    private void HandleBackButton()
    {
        if (_celebrating) return;
        PersistCanvasAndReturn();
    }

    private void HandleBackFromCompletion()
    {
        GoToMainScene();
    }

    // ── Celebration ────────────────────────────────────────────────────────────

    private void TriggerCelebration()
    {
        _celebrating = true;
        _progress.SetCompleted(_level.LevelNumber);
        _progress.SaveCanvas(_level.LevelNumber, _fillProcessor.CanvasTexture);
        _completionPanel.Show();
    }

    // ── Navigation ─────────────────────────────────────────────────────────────

    private void PersistCanvasAndReturn()
    {
        _progress.SaveCanvas(_level.LevelNumber, _fillProcessor.CanvasTexture);
        GoToMainScene();
    }

    /// <summary>Called by CompletionPanel "Back to Levels" button and the regular back button.</summary>
    public static void GoToMainScene()
        => SceneManager.LoadScene("MainScene");
}

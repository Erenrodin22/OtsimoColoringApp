using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the writable canvas texture and executes flood-fill operations.
///
/// Fill strategy:
///   1. BFS pixel collection runs on a background Thread (no main-thread stall).
///   2. Once the pixel list is ready, pixels are applied in one batch and
///      Texture2D.Apply() is called — keeping the per-frame spike minimal.
///
/// Outline detection always reads from <see cref="_srcPixels"/> (the original
/// source texture) so re-painting a region can never bleed through outlines.
/// </summary>
public sealed class FloodFillProcessor : MonoBehaviour
{
    [SerializeField] private RawImage    _display;
    [SerializeField] private GameSettings _settings;

    // ── Public surface ─────────────────────────────────────────────────────────

    public event Action<float> OnFillCompleted;   // receives painted-ratio 0..1
    public bool      IsBusy        { get; private set; }
    public Texture2D CanvasTexture => _canvas;

    // ── State ──────────────────────────────────────────────────────────────────

    private Texture2D _canvas;
    private Color32[] _srcPixels;      // original, never mutated
    private Color32[] _canvasPixels;   // current paint state, CPU-side
    private int _w, _h;
    private int _totalColorable;       // cached count of non-outline pixels

    // ── Direction vectors (no allocations per fill) ────────────────────────────

    private static readonly int[] Dx = { 1, -1, 0, 0 };
    private static readonly int[] Dy = { 0, 0, 1, -1 };

    // ── Initialisation ─────────────────────────────────────────────────────────

    public void Initialise(int levelNumber, Texture2D source, ProgressService progress)
    {
        _w = source.width;
        _h = source.height;

        // Cache source pixels — this array is the boundary oracle
        _srcPixels = source.GetPixels32();

        // Restore saved canvas or start from a clean copy of the source
        Texture2D saved = progress.LoadCanvas(levelNumber);
        _canvasPixels = saved != null
            ? saved.GetPixels32()
            : (Color32[])_srcPixels.Clone();

        // Build the GPU-side texture
        _canvas = new Texture2D(_w, _h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };
        _canvas.SetPixels32(_canvasPixels);
        _canvas.Apply(false);
        _display.texture = _canvas;

        // Pre-count colorable (non-outline) pixels so we don't recount every fill
        _totalColorable = 0;
        for (int i = 0; i < _srcPixels.Length; i++)
            if (!IsOutline(_srcPixels[i])) _totalColorable++;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <param name="uv">Normalised UV on the canvas image (0,0 = bottom-left)</param>
    /// <param name="fillColor">The colour to paint with</param>
    public void RequestFill(Vector2 uv, Color32 fillColor)
    {
        if (IsBusy) return;
        StartCoroutine(FillRoutine(uv, fillColor));
    }

    // ── Fill pipeline ──────────────────────────────────────────────────────────

    private IEnumerator FillRoutine(Vector2 uv, Color32 fillColor)
    {
        IsBusy = true;

        int startX   = Mathf.Clamp(Mathf.RoundToInt(uv.x * (_w - 1)), 0, _w - 1);
        int startY   = Mathf.Clamp(Mathf.RoundToInt(uv.y * (_h - 1)), 0, _h - 1);
        int startIdx = startY * _w + startX;

        // Abort early: tapped on an outline pixel
        if (IsOutline(_srcPixels[startIdx]))
        {
            IsBusy = false;
            yield break;
        }

        Color32 seedColor = _canvasPixels[startIdx];

        // Abort early: this region is already the target colour
        if (ColorDist(seedColor, fillColor) < 0.01f)
        {
            IsBusy = false;
            yield break;
        }

        // ── Phase 1: collect pixels on a background thread ─────────────────────

        List<int> hits    = null;
        bool      bgDone  = false;

        // Capture locals for closure — avoids capturing `this` in a thread
        Color32[] src        = _srcPixels;
        Color32[] canvas     = _canvasPixels;
        int       w          = _w;
        int       h          = _h;
        float     tolerance  = _settings.ColorTolerance;
        float     outThresh  = _settings.OutlineThreshold;
        Color32   seed       = seedColor;

        Task.Run(() =>
        {
            hits   = BreadthFirstCollect(startX, startY, seed, w, h, src, canvas, tolerance, outThresh);
            bgDone = true;
        });

        yield return new WaitUntil(() => bgDone);

        // ── Phase 2: apply results on the main thread ──────────────────────────

        for (int i = 0; i < hits.Count; i++)
            _canvasPixels[hits[i]] = fillColor;

        _canvas.SetPixels32(_canvasPixels);
        _canvas.Apply(false);

        // ── Phase 3: report completion ratio ──────────────────────────────────

        float ratio = ComputePaintedRatio();
        IsBusy = false;
        OnFillCompleted?.Invoke(ratio);
    }

    // ── BFS (runs on background thread — no Unity API calls allowed here) ─────

    private static List<int> BreadthFirstCollect(
        int     startX,  int startY,
        Color32 seed,
        int     w,       int h,
        Color32[] src,   Color32[] canvas,
        float   tolerance, float outlineThresh)
    {
        var result  = new List<int>(2048);
        var visited = new bool[w * h];
        var queue   = new Queue<int>(512);

        int si = startY * w + startX;
        queue.Enqueue(si);
        visited[si] = true;

        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            result.Add(idx);

            int cx = idx % w;
            int cy = idx / w;

            for (int d = 0; d < 4; d++)
            {
                int nx = cx + Dx[d];
                int ny = cy + Dy[d];

                // bounds check using unsigned comparison trick
                if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) continue;

                int ni = ny * w + nx;
                if (visited[ni]) continue;
                visited[ni] = true;

                // Boundary check against SOURCE texture — this is the key rule
                if (IsOutlineStatic(src[ni], outlineThresh)) continue;

                // Colour-similarity check against CANVAS state
                if (ColorDistStatic(canvas[ni], seed) > tolerance) continue;

                queue.Enqueue(ni);
            }
        }

        return result;
    }

    // ── Ratio calculation ──────────────────────────────────────────────────────

    private float ComputePaintedRatio()
    {
        if (_totalColorable == 0) return 0f;

        int painted = 0;
        for (int i = 0; i < _canvasPixels.Length; i++)
        {
            if (!IsOutline(_srcPixels[i]) && HasBeenPainted(_srcPixels[i], _canvasPixels[i]))
                painted++;
        }
        return (float)painted / _totalColorable;
    }

    /// <summary>A pixel is "painted" when canvas diverges from the original white.</summary>
    private static bool HasBeenPainted(Color32 original, Color32 canvas)
        => ColorDistStatic(original, canvas) > 0.05f;

    // ── Colour helpers ─────────────────────────────────────────────────────────

    private bool  IsOutline(Color32 c) => IsOutlineStatic(c, _settings.OutlineThreshold);

    private static bool IsOutlineStatic(Color32 c, float threshold)
    {
        if (c.a < 128) return false;                     // transparent = not outline
        float lum = (c.r + c.g + c.b) / (3f * 255f);
        return lum < threshold;
    }

    private static float ColorDistStatic(Color32 a, Color32 b)
    {
        float dr = (a.r - b.r) / 255f;
        float dg = (a.g - b.g) / 255f;
        float db = (a.b - b.b) / 255f;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }

    private static float ColorDist(Color32 a, Color32 b) => ColorDistStatic(a, b);
}

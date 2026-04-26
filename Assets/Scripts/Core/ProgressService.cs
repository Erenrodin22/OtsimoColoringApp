using System.IO;
using UnityEngine;

/// <summary>
/// Plain-C# service (no MonoBehaviour) responsible for all save/load operations.
/// Instantiated by whoever needs it via `new ProgressService()`.
/// </summary>
public sealed class ProgressService
{
    // ── PlayerPrefs keys ───────────────────────────────────────────────────────

    private const string CompletedKeyPrefix = "otsimo_done_";
    private const string ColorIndexKey      = "otsimo_color_idx";

    // ── Level completion ───────────────────────────────────────────────────────

    public bool IsCompleted(int levelNumber)
        => PlayerPrefs.GetInt(CompletedKeyPrefix + levelNumber, 0) == 1;

    public void SetCompleted(int levelNumber)
    {
        PlayerPrefs.SetInt(CompletedKeyPrefix + levelNumber, 1);
        PlayerPrefs.Save();
    }

    // ── Color preference ───────────────────────────────────────────────────────

    public int  GetColorIndex()           => PlayerPrefs.GetInt(ColorIndexKey, 0);
    public void SaveColorIndex(int index) { PlayerPrefs.SetInt(ColorIndexKey, index); PlayerPrefs.Save(); }

    // ── Canvas state ───────────────────────────────────────────────────────────

    /// <summary>Serialises the live canvas to a PNG on disk.</summary>
    public void SaveCanvas(int levelNumber, Texture2D canvas)
    {
        byte[] png  = canvas.EncodeToPNG();
        File.WriteAllBytes(CanvasPath(levelNumber), png);
    }

    /// <summary>Loads a previously saved canvas or returns null if none exists.</summary>
    public Texture2D LoadCanvas(int levelNumber)
    {
        string path = CanvasPath(levelNumber);
        if (!File.Exists(path)) return null;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };
        tex.LoadImage(File.ReadAllBytes(path));
        return tex;
    }

    // ── Full reset ─────────────────────────────────────────────────────────────

    public void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        foreach (string f in Directory.GetFiles(Application.persistentDataPath, "otsimo_canvas_*.png"))
            File.Delete(f);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string CanvasPath(int n)
        => Path.Combine(Application.persistentDataPath, $"otsimo_canvas_{n}.png");
}

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates three simple line-art PNG textures for testing.
/// Menu: ColoringApp → Generate Placeholder Art
///
/// Each image is 512×512, black outline on white background.
/// Replace with your real artwork before submission.
/// </summary>
public static class PlaceholderArtGenerator
{
    private const int Size = 512;
    private const string OutputFolder = "Assets/Resources/Levels/Art";

    [MenuItem("ColoringApp/Generate Placeholder Art")]
    private static void GenerateAll()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
            AssetDatabase.Refresh();
        }

        SaveTexture(DrawSun(),     "Level_01_LineArt");
        SaveTexture(DrawHouse(),   "Level_02_LineArt");
        SaveTexture(DrawFlower(),  "Level_03_LineArt");

        AssetDatabase.Refresh();

        // Force textures to be Read/Write enabled
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { OutputFolder });
        foreach (string guid in guids)
        {
            string path     = AssetDatabase.GUIDToAssetPath(guid);
            var    importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) continue;

            importer.isReadable     = true;
            importer.textureType    = TextureImporterType.Default;
            importer.mipmapEnabled  = false;
            importer.filterMode     = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        EditorUtility.DisplayDialog(
            "Placeholder Art Generated",
            $"Three line-art textures written to {OutputFolder}.\n\n" +
             "Import settings (Read/Write, no compression) have been applied automatically.",
            "OK");

        Debug.Log($"[PlaceholderArtGenerator] Art generated at {OutputFolder}");
    }

    // ── Drawing primitives ─────────────────────────────────────────────────────

    private static Texture2D DrawSun()
    {
        var tex = White();
        // Body circle
        DrawCircle(tex, 256, 256, 100, 6, Color.black);
        // Eye circles
        DrawCircle(tex, 220, 280, 18, 4, Color.black);
        DrawCircle(tex, 292, 280, 18, 4, Color.black);
        // Smile arc (approximated)
        DrawArc(tex, 256, 240, 50, 200f, 340f, 4, Color.black);
        // Rays
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            int x1 = 256 + Mathf.RoundToInt(Mathf.Cos(angle) * 115);
            int y1 = 256 + Mathf.RoundToInt(Mathf.Sin(angle) * 115);
            int x2 = 256 + Mathf.RoundToInt(Mathf.Cos(angle) * 155);
            int y2 = 256 + Mathf.RoundToInt(Mathf.Sin(angle) * 155);
            DrawLine(tex, x1, y1, x2, y2, 5, Color.black);
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawHouse()
    {
        var tex = White();
        // Walls
        DrawRect(tex, 120, 80, 272, 220, 6, Color.black);
        // Roof (triangle)
        DrawLine(tex, 100, 300, 256, 130, 6, Color.black);
        DrawLine(tex, 412, 300, 256, 130, 6, Color.black);
        DrawLine(tex, 100, 300, 412, 300, 6, Color.black);
        // Door
        DrawRect(tex, 210, 80, 92, 130, 5, Color.black);
        // Window
        DrawRect(tex, 140, 160, 70, 70, 4, Color.black);
        DrawLine(tex, 175, 160, 175, 230, 3, Color.black);
        DrawLine(tex, 140, 195, 210, 195, 3, Color.black);
        // Chimney
        DrawRect(tex, 310, 185, 40, 80, 5, Color.black);
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawFlower()
    {
        var tex = White();
        // Stem
        DrawLine(tex, 256, 90, 256, 280, 6, Color.black);
        // Leaf left
        DrawEllipse(tex, 200, 200, 50, 25, 6, Color.black, -30f);
        // Leaf right
        DrawEllipse(tex, 312, 200, 50, 25, 6, Color.black, 30f);
        // Center
        DrawCircle(tex, 256, 340, 45, 6, Color.black);
        // Petals
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            int cx = 256 + Mathf.RoundToInt(Mathf.Cos(a) * 72);
            int cy = 340 + Mathf.RoundToInt(Mathf.Sin(a) * 72);
            DrawCircle(tex, cx, cy, 32, 5, Color.black);
        }
        tex.Apply();
        return tex;
    }

    // ── Pixel-level drawing helpers ────────────────────────────────────────────

    private static Texture2D White()
    {
        var tex   = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        var white = new Color32[Size * Size];
        for (int i = 0; i < white.Length; i++) white[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(white);
        return tex;
    }

    private static void DrawCircle(Texture2D tex, int cx, int cy, int r, int thickness, Color c)
    {
        for (int t = 0; t < thickness; t++)
        {
            int rr = r - t;
            for (float a = 0; a < 360f; a += 0.3f)
            {
                int x = cx + Mathf.RoundToInt(Mathf.Cos(a * Mathf.Deg2Rad) * rr);
                int y = cy + Mathf.RoundToInt(Mathf.Sin(a * Mathf.Deg2Rad) * rr);
                SetSafe(tex, x, y, c);
            }
        }
    }

    private static void DrawArc(Texture2D tex, int cx, int cy, int r, float startDeg, float endDeg, int thickness, Color c)
    {
        for (int t = 0; t < thickness; t++)
        {
            int rr = r - t;
            for (float a = startDeg; a <= endDeg; a += 0.4f)
            {
                int x = cx + Mathf.RoundToInt(Mathf.Cos(a * Mathf.Deg2Rad) * rr);
                int y = cy + Mathf.RoundToInt(Mathf.Sin(a * Mathf.Deg2Rad) * rr);
                SetSafe(tex, x, y, c);
            }
        }
    }

    private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, int thickness, Color c)
    {
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int steps = Mathf.Max(dx, dy);
        if (steps == 0) return;
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            int   x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            int   y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            for (int ox = -thickness / 2; ox <= thickness / 2; ox++)
            for (int oy = -thickness / 2; oy <= thickness / 2; oy++)
                SetSafe(tex, x + ox, y + oy, c);
        }
    }

    private static void DrawRect(Texture2D tex, int x, int y, int w, int h, int thickness, Color c)
    {
        DrawLine(tex, x,     y,     x + w, y,     thickness, c);
        DrawLine(tex, x + w, y,     x + w, y + h, thickness, c);
        DrawLine(tex, x + w, y + h, x,     y + h, thickness, c);
        DrawLine(tex, x,     y + h, x,     y,     thickness, c);
    }

    private static void DrawEllipse(Texture2D tex, int cx, int cy, int rx, int ry, int thickness, Color c, float rotDeg = 0f)
    {
        float rot = rotDeg * Mathf.Deg2Rad;
        for (int t = 0; t < thickness; t++)
        {
            for (float a = 0; a < 360f; a += 0.5f)
            {
                float rad = a * Mathf.Deg2Rad;
                float lx  = (rx - t) * Mathf.Cos(rad);
                float ly  = (ry - t) * Mathf.Sin(rad);
                int   px  = cx + Mathf.RoundToInt(lx * Mathf.Cos(rot) - ly * Mathf.Sin(rot));
                int   py  = cy + Mathf.RoundToInt(lx * Mathf.Sin(rot) + ly * Mathf.Cos(rot));
                SetSafe(tex, px, py, c);
            }
        }
    }

    private static void SetSafe(Texture2D tex, int x, int y, Color c)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size) return;
        tex.SetPixel(x, y, c);
    }

    private static void SaveTexture(Texture2D tex, string name)
    {
        string path = $"{OutputFolder}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}
#endif
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds MainScene and ColoringScene from scratch via editor scripting.
/// All GameObjects, components, and critical SerializeField references are
/// wired programmatically — no manual Inspector work needed.
///
/// Menu: ColoringApp → Build Scenes
///
/// NOTE: After running this, open each scene and assign:
///   • LevelCell prefab   → LevelGridView._cellPrefab
///   • ColorSwatch prefab → ColorPaletteView._swatchPrefab
///   • GameSettings asset → FloodFillProcessor._settings, ColorPaletteView (via ColoringController._settings)
/// These require prefab assets that only exist after you've created them manually.
/// </summary>
public static class SceneBuilder
{
    private const string ScenesFolder  = "Assets/Scenes";
    private const int    RefWidth      = 1080;
    private const int    RefHeight     = 1920;

    // ── Entry points ──────────────────────────────────────────────────────────

    [MenuItem("ColoringApp/Build Scenes/Build Both Scenes")]
    private static void BuildBoth()
    {
        EnsureScenesFolder();
        BuildMainScene();
        BuildColoringScene();
        AddScenesToBuildSettings();

        EditorUtility.DisplayDialog(
            "Scenes Built",
            "MainScene and ColoringScene have been created in Assets/Scenes/.\n\n" +
            "Remaining manual steps:\n" +
            "• Assign LevelCell prefab in LevelGridView\n" +
            "• Assign ColorSwatchButton prefab in ColorPaletteView\n" +
            "• Assign GameSettings ScriptableObject to ColoringController and FloodFillProcessor",
            "Got it");
    }

    [MenuItem("ColoringApp/Build Scenes/Build MainScene Only")]
    private static void BuildMainSceneOnly() { EnsureScenesFolder(); BuildMainScene(); }

    [MenuItem("ColoringApp/Build Scenes/Build ColoringScene Only")]
    private static void BuildColoringSceneOnly() { EnsureScenesFolder(); BuildColoringScene(); }

    // ── Main Scene ────────────────────────────────────────────────────────────

    private static void BuildMainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Canvas ──
        var canvasGO = CreateCanvas("MainCanvas");
        var canvas   = canvasGO.GetComponent<Canvas>();

        // ── Background ──
        var bg = CreateUIImage(canvasGO.transform, "Background");
        bg.color = new Color(0.98f, 0.94f, 0.88f);   // warm cream
        StretchFull(bg.GetComponent<RectTransform>());

        // ── Title ──
        var titleGO = new GameObject("TitleLabel", typeof(RectTransform));
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(20, -160);
        titleRect.offsetMax = new Vector2(-20, -40);

        // ── Scroll View ──
        var scroll      = CreateScrollView(canvasGO.transform, out Transform content);
        var scrollRect  = scroll.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(20, 40);
        scrollRect.offsetMax = new Vector2(-20, -180);

        // GridLayoutGroup on content
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(460, 520);
        grid.spacing         = new Vector2(20, 24);
        grid.padding         = new RectOffset(20, 20, 20, 20);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment  = TextAnchor.UpperCenter;

        // ContentSizeFitter so grid expands vertically
        var csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Fader overlay (must be last child so it renders on top) ──
        var faderGO = CreateFaderOverlay(canvasGO.transform);

        // ── LevelGridView ──
        var managerGO = new GameObject("LevelGridManager");
        var gridView  = managerGO.AddComponent<LevelGridView>();

        // Wire what we can — prefab assignment still manual
        SerializedObject so = new SerializedObject(gridView);
        so.FindProperty("_gridRoot").objectReferenceValue = content;
        so.FindProperty("_fader").objectReferenceValue    = faderGO.GetComponent<SceneFader>();
        so.ApplyModifiedPropertiesWithoutUndo();

        // ── EventSystem ──
        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/MainScene.unity");
        Debug.Log("[SceneBuilder] MainScene built and saved.");
    }

    // ── Coloring Scene ────────────────────────────────────────────────────────

    private static void BuildColoringScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Canvas ──
        var canvasGO = CreateCanvas("ColoringCanvas");

        // ── Background ──
        var bg = CreateUIImage(canvasGO.transform, "Background");
        bg.color = new Color(0.95f, 0.95f, 0.97f);
        StretchFull(bg.GetComponent<RectTransform>());

        // ── Canvas image (line art + input) ──
        var canvasImageGO = new GameObject("ColoringCanvas_Image",
            typeof(RectTransform), typeof(RawImage), typeof(CanvasInputHandler));
        canvasImageGO.transform.SetParent(canvasGO.transform, false);
        var canvasRect = canvasImageGO.GetComponent<RectTransform>();
        canvasRect.anchorMin = new Vector2(0.05f, 0.1f);
        canvasRect.anchorMax = new Vector2(0.78f, 0.92f);
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        // Preserve aspect ratio
        canvasImageGO.GetComponent<RawImage>().color = Color.white;
        var aspectFitter = canvasImageGO.AddComponent<AspectRatioFitter>();
        aspectFitter.aspectMode  = AspectRatioFitter.AspectMode.FitInParent;
        aspectFitter.aspectRatio = 1f;   // updated at runtime once texture loads

        // ── Palette toolbar ──
        var paletteGO = new GameObject("PaletteToolbar", typeof(RectTransform), typeof(VerticalLayoutGroup));
        paletteGO.transform.SetParent(canvasGO.transform, false);
        var paletteRect = paletteGO.GetComponent<RectTransform>();
        paletteRect.anchorMin = new Vector2(0.80f, 0.1f);
        paletteRect.anchorMax = new Vector2(1.00f, 0.92f);
        paletteRect.offsetMin = new Vector2(0, 0);
        paletteRect.offsetMax = new Vector2(-10, 0);

        var paletteLayout       = paletteGO.GetComponent<VerticalLayoutGroup>();
        paletteLayout.spacing   = 18f;
        paletteLayout.childAlignment             = TextAnchor.MiddleCenter;
        paletteLayout.childControlHeight         = false;
        paletteLayout.childControlWidth          = false;
        paletteLayout.childForceExpandHeight     = false;
        paletteLayout.childForceExpandWidth      = false;
        paletteLayout.padding                    = new RectOffset(8, 8, 8, 8);

        var colorPaletteView = paletteGO.AddComponent<ColorPaletteView>();

        // ── Back button ──
        var backGO   = CreateButton(canvasGO.transform, "BackButton", "← Back");
        var backRect = backGO.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0, 1);
        backRect.anchorMax = new Vector2(0, 1);
        backRect.pivot     = new Vector2(0, 1);
        backRect.anchoredPosition = new Vector2(20, -20);
        backRect.sizeDelta        = new Vector2(160, 70);

        // ── Completion panel ──
        var completionGO = BuildCompletionPanel(canvasGO.transform);

        // ── Fader overlay ──
        var faderGO = CreateFaderOverlay(canvasGO.transform);

        // ── Managers ──
        var managersGO = new GameObject("Managers");

        var fillProcessor = managersGO.AddComponent<FloodFillProcessor>();
        var coloringCtrl  = managersGO.AddComponent<ColoringController>();

        // Wire serialized fields (GameSettings must be assigned manually)
        var ctrlSO = new SerializedObject(coloringCtrl);
        ctrlSO.FindProperty("_fillProcessor").objectReferenceValue   = fillProcessor;
        ctrlSO.FindProperty("_paletteView").objectReferenceValue     = colorPaletteView;
        ctrlSO.FindProperty("_completionPanel").objectReferenceValue = completionGO.GetComponent<CompletionPanel>();
        ctrlSO.FindProperty("_canvasInput").objectReferenceValue     = canvasImageGO.GetComponent<CanvasInputHandler>();
        ctrlSO.FindProperty("_backButton").objectReferenceValue      = backGO.GetComponent<Button>();
        ctrlSO.ApplyModifiedPropertiesWithoutUndo();

        var fillSO = new SerializedObject(fillProcessor);
        fillSO.FindProperty("_display").objectReferenceValue = canvasImageGO.GetComponent<RawImage>();
        fillSO.ApplyModifiedPropertiesWithoutUndo();

        // ── EventSystem ──
        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/ColoringScene.unity");
        Debug.Log("[SceneBuilder] ColoringScene built and saved.");
    }

    // ── Completion panel builder ──────────────────────────────────────────────

    private static GameObject BuildCompletionPanel(Transform parent)
    {
        var panelGO = new GameObject("CompletionPanel", typeof(RectTransform), typeof(CanvasGroup));
        panelGO.transform.SetParent(parent, false);
        StretchFull(panelGO.GetComponent<RectTransform>());

        // Semi-transparent backdrop
        var backdrop = CreateUIImage(panelGO.transform, "Backdrop");
        backdrop.color = new Color(0, 0, 0, 0.55f);
        StretchFull(backdrop.GetComponent<RectTransform>());

        // Card
        var cardGO   = new GameObject("Card", typeof(RectTransform));
        cardGO.transform.SetParent(panelGO.transform, false);
        var cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.anchorMin       = new Vector2(0.1f, 0.28f);
        cardRect.anchorMax       = new Vector2(0.9f, 0.72f);
        cardRect.offsetMin       = Vector2.zero;
        cardRect.offsetMax       = Vector2.zero;
        var cardImage            = cardGO.AddComponent<Image>();
        cardImage.color          = new Color(1f, 0.98f, 0.92f);
        cardImage.raycastTarget  = true;

        // Star icon (using a simple image placeholder)
        var starGO    = CreateUIImage(cardGO.transform, "StarIcon");
        var starRect  = starGO.GetComponent<RectTransform>();
        starRect.anchorMin       = new Vector2(0.5f, 0.62f);
        starRect.anchorMax       = new Vector2(0.5f, 0.62f);
        starRect.pivot           = new Vector2(0.5f, 0.5f);
        starRect.anchoredPosition = Vector2.zero;
        starRect.sizeDelta        = new Vector2(110, 110);
        starGO.GetComponent<Image>().color = new Color(1f, 0.85f, 0f);

        // "Back to Levels" button
        var btnGO   = CreateButton(cardGO.transform, "BackToLevelsButton", "Back to Levels");
        var btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.anchorMin       = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax       = new Vector2(0.5f, 0.5f);
        btnRect.pivot           = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0, -80);
        btnRect.sizeDelta        = new Vector2(280, 75);

        // Wire CompletionPanel component
        var cp = panelGO.AddComponent<CompletionPanel>();
        var cpSO = new SerializedObject(cp);
        cpSO.FindProperty("_group").objectReferenceValue          = panelGO.GetComponent<CanvasGroup>();
        cpSO.FindProperty("_card").objectReferenceValue           = cardGO.GetComponent<RectTransform>();
        cpSO.FindProperty("_starIcon").objectReferenceValue       = starGO.GetComponent<Image>();
        cpSO.FindProperty("_backToLevelsButton").objectReferenceValue = btnGO.GetComponent<Button>();
        cpSO.ApplyModifiedPropertiesWithoutUndo();

        panelGO.SetActive(false);
        return panelGO;
    }

    // ── Helper factory methods ─────────────────────────────────────────────────

    private static GameObject CreateCanvas(string name)
    {
        var go      = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas  = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
        scaler.matchWidthOrHeight  = 0.5f;

        return go;
    }

    private static Image CreateUIImage(Transform parent, string name)
    {
        var go    = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    private static GameObject CreateButton(Transform parent, string name, string label)
    {
        var go     = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.20f, 0.60f, 0.86f);

        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        StretchFull(textGO.GetComponent<RectTransform>());

        return go;
    }

    private static GameObject CreateScrollView(Transform parent, out Transform content)
    {
        var go         = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        var viewport    = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(go.transform, false);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);

        var contentGO   = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewport.transform, false);
        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot     = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var scrollRect          = go.GetComponent<ScrollRect>();
        scrollRect.content      = contentRect;
        scrollRect.viewport     = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal   = false;
        scrollRect.vertical     = true;
        scrollRect.scrollSensitivity = 30f;

        content = contentGO.transform;
        return go;
    }

    private static GameObject CreateFaderOverlay(Transform parent)
    {
        var go    = new GameObject("FadeOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(SceneFader));
        go.transform.SetParent(parent, false);
        StretchFull(go.GetComponent<RectTransform>());
        go.GetComponent<Image>().color         = Color.black;
        go.GetComponent<Image>().raycastTarget = false;
        return go;
    }

    private static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }

    private static void EnsureScenesFolder()
    {
        if (!AssetDatabase.IsValidFolder(ScenesFolder))
        {
            Directory.CreateDirectory(ScenesFolder);
            AssetDatabase.Refresh();
        }
    }

    private static void AddScenesToBuildSettings()
    {
        var scenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesFolder}/MainScene.unity",    true),
            new EditorBuildSettingsScene($"{ScenesFolder}/ColoringScene.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[SceneBuilder] Build Settings updated — MainScene (index 0), ColoringScene (index 1).");
    }
}
#endif
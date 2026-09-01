using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 미니게임 카드 선택 씬을 프로젝트의 메인 메뉴 UI 기준으로 생성합니다.
/// </summary>
public static class MinigameSelectionSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MinigameSelection.unity";
    private const string BackgroundSpritePath = "Assets/Sprites/UI/Backgrounds/Temp_UI_Background_Beach.png";
    private const string RoundedSpritePath = "Assets/Sprites/UI/Image_RoundedRect_64.png";
    private const string FontPath = "Assets/Fonts/Galmuri11/Galmuri11-Bold SDF.asset";
    private const int CardCount = 13;
    private const float CardSpacing = 470f;

    [MenuItem("Wildlife Sports Day/Build Minigame Selection Scene")]
    public static void Build()
    {
        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
        Sprite roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (backgroundSprite == null || roundedSprite == null || font == null)
        {
            Debug.LogError("미니게임 선택 씬에 필요한 UI 에셋을 찾을 수 없습니다.");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCamera();
        Canvas canvas = CreateCanvas(camera);
        CreateBackground(canvas.transform, backgroundSprite);
        CreateOverlay(canvas.transform);
        CreateText(canvas.transform, "Text_Title", "다음 종목 추첨 중!", font, 78, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(1320f, 120f), Color.white, TextAlignmentOptions.Center);
        CreateText(canvas.transform, "Text_Subtitle", "카드가 멈춘 곳의 종목으로 바로 출발합니다", font, 31, new Vector2(0.5f, 0.81f), new Vector2(0.5f, 0.81f), new Vector2(1500f, 70f), new Color(0.88f, 0.95f, 1f, 1f), TextAlignmentOptions.Center);

        RectTransform viewport = CreateRouletteViewport(canvas.transform, roundedSprite);
        RectTransform cardTrack = CreateCardTrack(viewport);
        MinigameSelectionCard[] cards = CreateCards(cardTrack, roundedSprite, font);
        CreateSelectionGuide(viewport, font);
        CreateText(canvas.transform, "Text_Footer", "운도 실력이다!", font, 36, new Vector2(0.5f, 0.13f), new Vector2(0.5f, 0.13f), new Vector2(1000f, 80f), new Color(1f, 0.9f, 0.52f, 1f), TextAlignmentOptions.Center);

        GameObject controllerObject = new("MinigameSelectionController");
        MinigameSelectionController controller = controllerObject.AddComponent<MinigameSelectionController>();
        SerializedObject controllerSerialized = new(controller);
        controllerSerialized.FindProperty("_cardTrack").objectReferenceValue = cardTrack;
        SerializedProperty cardsProperty = controllerSerialized.FindProperty("_cards");
        cardsProperty.arraySize = cards.Length;
        for (int i = 0; i < cards.Length; i++)
        {
            cardsProperty.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
        }

        controllerSerialized.FindProperty("_spinDurationSeconds").floatValue = 4f;
        controllerSerialized.FindProperty("_selectionHoldSeconds").floatValue = 0.35f;
        controllerSerialized.FindProperty("_cardSpacing").floatValue = CardSpacing;
        controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"미니게임 선택 씬을 생성했습니다: {ScenePath}");
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.32f, 0.54f, 1f);
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static Canvas CreateCanvas(Camera camera)
    {
        GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 100f;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560f, 1440f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        return canvas;
    }

    private static void CreateBackground(Transform parent, Sprite backgroundSprite)
    {
        Image background = CreateImage(parent, "Image_Background", backgroundSprite, Color.white);
        Stretch(background.rectTransform);
        background.raycastTarget = false;
    }

    private static void CreateOverlay(Transform parent)
    {
        Image overlay = CreateImage(parent, "Image_BackgroundShade", null, new Color(0.02f, 0.09f, 0.18f, 0.4f));
        Stretch(overlay.rectTransform);
        overlay.raycastTarget = false;
    }

    private static RectTransform CreateRouletteViewport(Transform parent, Sprite roundedSprite)
    {
        Image frame = CreateImage(parent, "Panel_RouletteFrame", roundedSprite, new Color(0.03f, 0.1f, 0.2f, 0.82f));
        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(2280f, 660f);
        frameRect.anchoredPosition = new Vector2(0f, 20f);

        GameObject viewportObject = new("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(frame.transform, false);
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask mask = viewportObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;
        RectTransform viewportRect = viewportImage.rectTransform;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(46f, 46f);
        viewportRect.offsetMax = new Vector2(-46f, -46f);
        return viewportRect;
    }

    private static RectTransform CreateCardTrack(Transform parent)
    {
        GameObject trackObject = new("CardTrack", typeof(RectTransform));
        trackObject.transform.SetParent(parent, false);
        RectTransform track = trackObject.GetComponent<RectTransform>();
        track.anchorMin = new Vector2(0.5f, 0.5f);
        track.anchorMax = new Vector2(0.5f, 0.5f);
        track.pivot = new Vector2(0.5f, 0.5f);
        track.sizeDelta = new Vector2(1f, 560f);
        return track;
    }

    private static MinigameSelectionCard[] CreateCards(Transform parent, Sprite roundedSprite, TMP_FontAsset font)
    {
        MinigameSelectionCard[] cards = new MinigameSelectionCard[CardCount];
        int centerIndex = CardCount / 2;
        for (int i = 0; i < CardCount; i++)
        {
            GameObject cardObject = new($"Card_{i + 1:00}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            cardObject.transform.SetParent(parent, false);
            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(420f, 540f);
            cardRect.anchoredPosition = new Vector2((i - centerIndex) * CardSpacing, 0f);

            Image panel = cardObject.GetComponent<Image>();
            panel.sprite = roundedSprite;
            panel.type = Image.Type.Sliced;

            Image selectionFrame = CreateImage(cardObject.transform, "Image_Selection", roundedSprite, new Color(1f, 0.78f, 0.18f, 0.28f));
            selectionFrame.type = Image.Type.Sliced;
            Stretch(selectionFrame.rectTransform, -18f);
            selectionFrame.raycastTarget = false;

            Image accent = CreateImage(cardObject.transform, "Image_Accent", null, Color.white);
            RectTransform accentRect = accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.sizeDelta = new Vector2(-64f, 14f);
            accentRect.anchoredPosition = new Vector2(0f, -44f);

            CreateText(cardObject.transform, "Text_Random", "RANDOM MINIGAME", font, 20, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(330f, 40f), new Color(0.72f, 0.84f, 0.94f, 1f), TextAlignmentOptions.Center, new Vector2(0f, -82f));
            TMP_Text title = CreateText(cardObject.transform, "Text_Title", "미니게임", font, 40, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(340f, 150f), Color.white, TextAlignmentOptions.Center);
            TMP_Text description = CreateText(cardObject.transform, "Text_Description", "종목 설명", font, 25, new Vector2(0.5f, 0.31f), new Vector2(0.5f, 0.31f), new Vector2(340f, 140f), new Color(0.86f, 0.92f, 0.96f, 1f), TextAlignmentOptions.Center);
            CreateText(cardObject.transform, "Text_Go", "준비!", font, 30, new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), new Vector2(280f, 60f), new Color(1f, 0.86f, 0.35f, 1f), TextAlignmentOptions.Center);

            MinigameSelectionCard card = cardObject.AddComponent<MinigameSelectionCard>();
            SerializedObject cardSerialized = new(card);
            cardSerialized.FindProperty("_panelImage").objectReferenceValue = panel;
            cardSerialized.FindProperty("_accentImage").objectReferenceValue = accent;
            cardSerialized.FindProperty("_selectionFrameImage").objectReferenceValue = selectionFrame;
            cardSerialized.FindProperty("_titleText").objectReferenceValue = title;
            cardSerialized.FindProperty("_descriptionText").objectReferenceValue = description;
            cardSerialized.FindProperty("_canvasGroup").objectReferenceValue = cardObject.GetComponent<CanvasGroup>();
            cardSerialized.ApplyModifiedPropertiesWithoutUndo();
            cards[i] = card;
        }

        return cards;
    }

    private static void CreateSelectionGuide(Transform parent, TMP_FontAsset font)
    {
        Image topGuide = CreateImage(parent, "Image_SelectTop", null, new Color(1f, 0.78f, 0.18f, 1f));
        RectTransform topRect = topGuide.rectTransform;
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.sizeDelta = new Vector2(18f, 74f);
        topRect.anchoredPosition = new Vector2(0f, 18f);

        Image bottomGuide = CreateImage(parent, "Image_SelectBottom", null, new Color(1f, 0.78f, 0.18f, 1f));
        RectTransform bottomRect = bottomGuide.rectTransform;
        bottomRect.anchorMin = new Vector2(0.5f, 0f);
        bottomRect.anchorMax = new Vector2(0.5f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.sizeDelta = new Vector2(18f, 74f);
        bottomRect.anchoredPosition = new Vector2(0f, -18f);

        CreateText(parent, "Text_Select", "▼ 선택 ▼", font, 27, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(240f, 50f), new Color(1f, 0.85f, 0.28f, 1f), TextAlignmentOptions.Center, new Vector2(0f, 48f));
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, TMP_FontAsset font, float size, Vector2 anchorMin, Vector2 anchorMax, Vector2 dimensions, Color color, TextAlignmentOptions alignment, Vector2? position = null)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        textComponent.font = font;
        textComponent.text = text;
        textComponent.fontSize = size;
        textComponent.color = color;
        textComponent.alignment = alignment;
        textComponent.textWrappingMode = TextWrappingModes.Normal;
        textComponent.overflowMode = TextOverflowModes.Ellipsis;
        textComponent.raycastTarget = false;

        RectTransform rect = textComponent.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = dimensions;
        rect.anchoredPosition = position ?? Vector2.zero;
        return textComponent;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rectTransform, float inset = 0f)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(inset, inset);
        rectTransform.offsetMax = new Vector2(-inset, -inset);
    }

    private static void ConfigureBuildSettings()
    {
        List<string> paths = new()
        {
            "Assets/Scenes/LoginScene.unity",
            "Assets/Scenes/MainScene.unity",
            ScenePath,
        };

        List<SceneAsset> minigameScenes = MinigameDefinitionSceneReferenceUtility.GetReferencedScenes();
        for (int i = 0; i < minigameScenes.Count; i++)
        {
            paths.Add(AssetDatabase.GetAssetPath(minigameScenes[i]));
        }

        List<EditorBuildSettingsScene> scenes = new(paths.Count);
        for (int i = 0; i < paths.Count; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(paths[i]) != null)
            {
                scenes.Add(new EditorBuildSettingsScene(paths[i], true));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}

using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 키보드 연타 북극곰 미니게임의 트랙, HUD, 머티리얼 참조를 한 번에 구성합니다.
/// </summary>
public static class KeyboardMashPolarBearSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Minigames/Minigame_KeyboardMashPolarBear.unity";
    private const string MaterialFolderPath = "Assets/Materials/Minigames/KeyboardMashPolarBear";
    private const string FontPath = "Assets/Fonts/Galmuri11/Galmuri11-Bold SDF.asset";
    private const string RoundedSpritePath = "Assets/Sprites/UI/Image_RoundedRect_64.png";
    private const string DefinitionPath = "Assets/Resources/Minigames/KeyboardMashPolarBear.asset";

    [MenuItem("Wildlife Sports Day/Build Keyboard Mash Polar Bear Scene")]
    public static void Build()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        if (font == null || roundedSprite == null)
        {
            Debug.LogError("북극곰 미니게임에 필요한 폰트 또는 UI 스프라이트를 찾을 수 없습니다.");
            return;
        }

        UpdateDefinition();
        MaterialLibrary materials = MaterialLibrary.LoadOrCreate();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCamera();
        CreateLighting();
        CreateEnvironment(materials);
        Transform runner = CreateRunner(materials);
        KeyboardMashPolarBearController controller = CreateController(runner, camera.transform);
        CreateHud(camera, controller, font, roundedSprite);
        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("키보드 연타 북극곰 미니게임 씬을 생성했습니다.");
    }

    private static void UpdateDefinition()
    {
        MinigameDefinition definition = AssetDatabase.LoadAssetAtPath<MinigameDefinition>(DefinitionPath);
        if (definition == null)
        {
            Debug.LogWarning("북극곰 미니게임 정의 에셋을 찾지 못해 인트로 문구를 갱신하지 않았습니다.");
            return;
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("_minigameName").stringValue = "북극곰 트랙 질주!";
        serialized.FindProperty("_description").stringValue = "F키를 연타해 북극곰을 트랙 끝까지 달리게 하세요.";
        serialized.FindProperty("_objective").stringValue = "6초 안에 F키를 많이 눌러 더 먼 거리까지 이동하기";
        serialized.FindProperty("_controlGuide").stringValue = "F 키 연타";
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
    }

    /// <summary>
    /// 씬을 다시 생성한 직후 시작 구도 미리보기를 저장합니다.
    /// </summary>
    public static void BuildAndRenderPreview()
    {
        Build();
        RenderPreview();
    }

    /// <summary>
    /// 저장된 씬의 시작 카메라 구도를 PNG로 렌더해 시각 검수에 사용합니다.
    /// </summary>
    public static void RenderPreview()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Camera camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("북극곰 미니게임의 Main Camera를 찾을 수 없습니다.");
            return;
        }

        const int width = 1920;
        const int height = 1080;
        RenderTexture renderTexture = new(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new(width, height, TextureFormat.RGBA32, false);
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        camera.targetTexture = renderTexture;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        RenderTexture.active = renderTexture;
        image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        image.Apply();
        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;

        string outputPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Temp", "KeyboardMashPolarBearPreview.png");
        File.WriteAllBytes(outputPath, image.EncodeToPNG());
        Object.DestroyImmediate(renderTexture);
        Object.DestroyImmediate(image);
        Debug.Log($"북극곰 미니게임 미리보기 저장: {outputPath}");
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(UniversalAdditionalCameraData));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.35f, 0.73f, 0.95f);
        camera.fieldOfView = 52f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 300f;
        camera.transform.position = new Vector3(0.5f, 7f, -15f);
        camera.transform.rotation = Quaternion.Euler(20f, 25f, 0f);
        return camera;
    }

    private static void CreateLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.51f, 0.76f, 0.92f);
        RenderSettings.ambientEquatorColor = new Color(0.6f, 0.67f, 0.48f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.32f, 0.22f);

        GameObject lightObject = new("Sun", typeof(Light));
        Light sun = lightObject.GetComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.82f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(48f, -36f, 0f);
    }

    private static void CreateEnvironment(MaterialLibrary materials)
    {
        Transform root = new GameObject("Track Environment").transform;
        CreatePrimitive(PrimitiveType.Cube, "Grass Field", root, new Vector3(40f, -0.55f, 0f), new Vector3(150f, 1f, 42f), materials.Grass);
        CreatePrimitive(PrimitiveType.Cube, "Running Track", root, new Vector3(40f, 0f, 0f), new Vector3(116f, 0.24f, 13.2f), materials.Track);
        CreatePrimitive(PrimitiveType.Cube, "Front Curb", root, new Vector3(40f, 0.18f, -6.7f), new Vector3(116f, 0.22f, 0.28f), materials.Curb);
        CreatePrimitive(PrimitiveType.Cube, "Back Curb", root, new Vector3(40f, 0.18f, 6.7f), new Vector3(116f, 0.22f, 0.28f), materials.Curb);

        for (int lane = -1; lane <= 1; lane++)
        {
            CreatePrimitive(PrimitiveType.Cube, $"Lane Line {lane + 2}", root, new Vector3(40f, 0.14f, lane * 3.1f), new Vector3(116f, 0.025f, 0.1f), materials.Lane);
        }

        for (int x = 0; x <= 80; x += 10)
        {
            CreateDistanceMarker(root, materials, x);
        }

        CreateStartFinishLine(root, materials, "Start Line", 0f, materials.StartLine);
        CreateStartFinishLine(root, materials, "Finish Line", 80f, materials.FinishLine);
        CreateFinishGate(root, materials);
        CreateBleachers(root, materials);
        CreateScenery(root, materials);
    }

    private static void CreateDistanceMarker(Transform parent, MaterialLibrary materials, int distance)
    {
        Transform marker = new GameObject($"Distance Marker {distance:000}m").transform;
        marker.SetParent(parent, false);
        CreatePrimitive(PrimitiveType.Cylinder, "Post", marker, new Vector3(distance, 1.2f, 7.6f), new Vector3(0.14f, 1.2f, 0.14f), materials.Metal);
        CreatePrimitive(PrimitiveType.Cube, "Sign", marker, new Vector3(distance, 2.35f, 7.6f), new Vector3(1.6f, 0.8f, 0.12f), materials.Marker);
    }

    private static void CreateStartFinishLine(Transform parent, MaterialLibrary materials, string name, float x, Material darkMaterial)
    {
        Transform lineRoot = new GameObject(name).transform;
        lineRoot.SetParent(parent, false);
        for (int row = -6; row <= 6; row++)
        {
            for (int column = 0; column < 2; column++)
            {
                bool useDarkTile = (row + column) % 2 == 0;
                CreatePrimitive(PrimitiveType.Cube, $"Tile {row}_{column}", lineRoot, new Vector3(x + (column - 0.5f) * 0.5f, 0.16f, row + 0.5f), new Vector3(0.5f, 0.04f, 1f), useDarkTile ? darkMaterial : materials.Lane);
            }
        }
    }

    private static void CreateFinishGate(Transform parent, MaterialLibrary materials)
    {
        Transform gate = new GameObject("Finish Gate").transform;
        gate.SetParent(parent, false);
        CreatePrimitive(PrimitiveType.Cylinder, "Left Pillar", gate, new Vector3(80f, 3f, -6f), new Vector3(0.35f, 3f, 0.35f), materials.FinishLine);
        CreatePrimitive(PrimitiveType.Cylinder, "Right Pillar", gate, new Vector3(80f, 3f, 6f), new Vector3(0.35f, 3f, 0.35f), materials.FinishLine);
        CreatePrimitive(PrimitiveType.Cube, "Finish Banner", gate, new Vector3(80f, 5.8f, 0f), new Vector3(0.4f, 1.4f, 12.6f), materials.Curb);
    }

    private static void CreateBleachers(Transform parent, MaterialLibrary materials)
    {
        Transform bleachers = new GameObject("Blue Bleachers").transform;
        bleachers.SetParent(parent, false);
        for (int x = -10; x <= 100; x += 14)
        {
            for (int row = 0; row < 3; row++)
            {
                CreatePrimitive(PrimitiveType.Cube, $"Seat {x}_{row}", bleachers, new Vector3(x, 0.75f + row * 0.62f, 12.5f + row * 0.52f), new Vector3(10f, 0.52f, 1.3f), materials.Bleacher);
            }
        }
    }

    private static void CreateScenery(Transform parent, MaterialLibrary materials)
    {
        Transform scenery = new GameObject("Field Decorations").transform;
        scenery.SetParent(parent, false);
        for (int x = -5; x <= 90; x += 12)
        {
            CreatePrimitive(PrimitiveType.Cylinder, $"Tree Trunk {x}", scenery, new Vector3(x + 5f, 1.2f, 18f), new Vector3(0.38f, 1.2f, 0.38f), materials.Trunk);
            CreatePrimitive(PrimitiveType.Sphere, $"Tree Crown {x}", scenery, new Vector3(x + 5f, 3.1f, 18f), new Vector3(2.4f, 2.6f, 2.4f), materials.Tree);
        }
    }

    private static Transform CreateRunner(MaterialLibrary materials)
    {
        GameObject runner = CreatePrimitive(PrimitiveType.Capsule, "Polar Bear Placeholder", null, new Vector3(0f, 1.15f, 0f), new Vector3(1.8f, 1.25f, 1.25f), materials.PolarBear);
        runner.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        return runner.transform;
    }

    private static KeyboardMashPolarBearController CreateController(Transform runner, Transform cameraTransform)
    {
        GameObject controllerObject = new("KeyboardMashPolarBearController");
        KeyboardMashPolarBearController controller = controllerObject.AddComponent<KeyboardMashPolarBearController>();
        SerializedObject serialized = new(controller);
        serialized.FindProperty("_runnerRoot").objectReferenceValue = runner;
        serialized.FindProperty("_followCamera").objectReferenceValue = cameraTransform;
        serialized.FindProperty("_cameraFollowOffset").vector3Value = new Vector3(0.5f, 5.85f, -15f);
        serialized.FindProperty("_cameraFollowEulerAngles").vector3Value = new Vector3(20f, 25f, 0f);
        serialized.FindProperty("_roundDurationSeconds").floatValue = 6f;
        serialized.FindProperty("_distancePerMash").floatValue = 4f;
        serialized.FindProperty("_cameraSwayPositionAmount").floatValue = 0.06f;
        serialized.FindProperty("_cameraSwayRotationAmount").floatValue = 0.45f;
        serialized.FindProperty("_cameraSwayDecaySpeed").floatValue = 3.5f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    private static void CreateHud(Camera camera, KeyboardMashPolarBearController controller, TMP_FontAsset font, Sprite roundedSprite)
    {
        GameObject canvasObject = new("Polar Bear HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 0.4f;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image topPanel = CreateImage(canvas.transform, "Top Panel", roundedSprite, new Color(0.03f, 0.12f, 0.22f, 0.88f));
        topPanel.type = Image.Type.Sliced;
        SetAnchors(topPanel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1560f, 190f), new Vector2(0f, -36f));
        TMP_Text title = CreateScreenText(canvas.transform, "Title", "북극곰 트랙 질주!", font, 52, Color.white, TextAlignmentOptions.Center);
        SetAnchors(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(820f, 64f), new Vector2(0f, -66f));
        TMP_Text description = CreateScreenText(canvas.transform, "Description", "F키를 빠르게 연타해 결승선을 향해 달리세요!", font, 24, new Color(0.86f, 0.94f, 1f), TextAlignmentOptions.Center);
        SetAnchors(description.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 68f), new Vector2(0f, -124f));

        Image timerPanel = CreatePanel(canvas.transform, "Timer Panel", roundedSprite, new Vector2(0f, 1f), new Vector2(310f, 78f), new Vector2(185f, -270f));
        TMP_Text timer = CreateScreenText(timerPanel.transform, "Timer", "남은 시간  6.0초", font, 28, new Color(1f, 0.88f, 0.32f), TextAlignmentOptions.Center);
        Stretch(timer.rectTransform, new Vector2(12f, 8f));

        Image progressPanel = CreatePanel(canvas.transform, "Progress Panel", roundedSprite, new Vector2(1f, 1f), new Vector2(330f, 118f), new Vector2(-185f, -290f));
        TMP_Text progress = CreateScreenText(progressPanel.transform, "Progress", "거리  0m / 80m\n연타  0회", font, 25, Color.white, TextAlignmentOptions.Center);
        Stretch(progress.rectTransform, new Vector2(14f, 8f));

        Image gradePanel = CreatePanel(canvas.transform, "Grade Panel", roundedSprite, new Vector2(0.5f, 0f), new Vector2(370f, 76f), new Vector2(0f, 95f));
        TMP_Text grade = CreateScreenText(gradePanel.transform, "Grade", "☆☆☆  출발!", font, 30, new Color(1f, 0.88f, 0.32f), TextAlignmentOptions.Center);
        Stretch(grade.rectTransform, new Vector2(16f, 8f));

        SerializedObject serialized = new(controller);
        serialized.FindProperty("_titleText").objectReferenceValue = title;
        serialized.FindProperty("_descriptionText").objectReferenceValue = description;
        serialized.FindProperty("_progressText").objectReferenceValue = progress;
        serialized.FindProperty("_timerText").objectReferenceValue = timer;
        serialized.FindProperty("_gradeText").objectReferenceValue = grade;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Image CreatePanel(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 size, Vector2 position)
    {
        Image panel = CreateImage(parent, name, sprite, new Color(0.05f, 0.16f, 0.28f, 0.9f));
        panel.type = Image.Type.Sliced;
        SetAnchors(panel.rectTransform, anchor, anchor, size, position);
        return panel;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<EventSystem>().sendNavigationEvents = true;
    }

    private static GameObject CreatePrimitive(PrimitiveType primitiveType, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, true);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        return gameObject;
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

    private static TMP_Text CreateScreenText(Transform parent, string name, string value, TMP_FontAsset font, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect, Vector2 inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = inset;
        rect.offsetMax = -inset;
    }

    private sealed class MaterialLibrary
    {
        public Material Grass { get; private set; }
        public Material Track { get; private set; }
        public Material Lane { get; private set; }
        public Material Curb { get; private set; }
        public Material Marker { get; private set; }
        public Material StartLine { get; private set; }
        public Material FinishLine { get; private set; }
        public Material Bleacher { get; private set; }
        public Material Metal { get; private set; }
        public Material Trunk { get; private set; }
        public Material Tree { get; private set; }
        public Material PolarBear { get; private set; }

        public static MaterialLibrary LoadOrCreate()
        {
            EnsureFolder(MaterialFolderPath);
            return new MaterialLibrary
            {
                Grass = GetOrCreate("M_Grass", new Color(0.24f, 0.56f, 0.25f)),
                Track = GetOrCreate("M_Track_Red", new Color(0.68f, 0.18f, 0.13f)),
                Lane = GetOrCreate("M_Lane_White", new Color(0.96f, 0.94f, 0.85f)),
                Curb = GetOrCreate("M_Curb_Yellow", new Color(0.96f, 0.67f, 0.12f)),
                Marker = GetOrCreate("M_Marker_Blue", new Color(0.05f, 0.32f, 0.7f)),
                StartLine = GetOrCreate("M_Start_Green", new Color(0.14f, 0.62f, 0.28f)),
                FinishLine = GetOrCreate("M_Finish_Navy", new Color(0.04f, 0.1f, 0.2f)),
                Bleacher = GetOrCreate("M_Bleacher_Blue", new Color(0.1f, 0.36f, 0.72f)),
                Metal = GetOrCreate("M_Metal_Dark", new Color(0.18f, 0.25f, 0.3f), 0.8f),
                Trunk = GetOrCreate("M_Tree_Trunk", new Color(0.35f, 0.18f, 0.08f)),
                Tree = GetOrCreate("M_Tree_Leaf", new Color(0.1f, 0.48f, 0.17f)),
                PolarBear = GetOrCreate("M_PolarBear_Placeholder", new Color(0.94f, 0.97f, 1f)),
            };
        }

        private static Material GetOrCreate(string materialName, Color color, float metallic = 0f)
        {
            string path = $"{MaterialFolderPath}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string targetFolder)
        {
            string[] parts = targetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}

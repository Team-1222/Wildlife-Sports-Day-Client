using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 노션 기획에 등록된 미구현 미니게임의 씬과 정의 에셋을 공통 플레이스홀더로 생성합니다.
/// </summary>
public static class MinigameScaffoldSceneBuilder
{
    private const string SceneFolderPath = "Assets/Scenes/Minigames";
    private const string DefinitionFolderPath = "Assets/Resources/Minigames";
    private const string FontPath = "Assets/Fonts/Galmuri11/Galmuri11-Bold SDF.asset";

    private static readonly MinigameScaffold[] Scaffolds =
    {
        new("BiteSalmon", "연어를 깨무세요", "곰이 폭포를 거슬러 뛰는 연어를 박자에 맞춰 낚아채는 리듬 게임입니다.", "8박 동안 목표 마릿수의 연어를 깨물기", "지정 키 또는 클릭", MinigameViewType.SideView, MinigameCharacterType.GrizzlyBear, MinigameControlType.Rhythm),
        new("FollowSpider", "따라하세요", "거미가 빛나는 발판 순서를 재현하는 리듬·기억 게임입니다.", "제시된 발판 순서를 끝까지 재현하기", "방향키 또는 8방향 입력", MinigameViewType.TopView, MinigameCharacterType.Spider, MinigameControlType.SequenceInput),
        new("AvoidHookSalmon", "낚싯바늘을 피하세요", "심해 연어가 내려오는 낚싯줄과 바늘을 피하는 생존 게임입니다.", "제한 시간 동안 바늘에 걸리지 않고 생존하기", "상하 이동과 짧은 대시", MinigameViewType.SideView, MinigameCharacterType.Salmon, MinigameControlType.AvoidObjects),
        new("AvoidPigeonHawk", "비둘기를 피하세요", "야생 매가 하늘에서 돌진하는 비둘기 떼를 피하는 회피 게임입니다.", "제한 시간 동안 비둘기와 충돌하지 않고 비행하기", "상하 이동과 짧은 부스트", MinigameViewType.SideView, MinigameCharacterType.Hawk, MinigameControlType.AvoidObjects),
        new("InfectBat", "감염시키세요", "박쥐가 도시 상공을 날며 가상의 바이러스를 시민들에게 퍼뜨리는 포인터 게임입니다.", "제한 시간 안에 목표 인원을 감염시키기", "마우스로 박쥐 이동", MinigameViewType.SideView, MinigameCharacterType.Bat, MinigameControlType.Aim),
        new("RhythmElephant", "박자에 맞춰 밟으세요", "코끼리가 사바나의 발광 표적을 박자에 맞춰 밟는 리듬 액션 게임입니다.", "제한 시간 안에 목표 점수 달성하기", "좌·우 발 입력", MinigameViewType.ThirdPerson, MinigameCharacterType.Elephant, MinigameControlType.Rhythm),
        new("GuessAnimalIdentity", "동물의 정체를 맞추세요", "탐정 너구리가 단서로 야생동물의 정체를 맞히는 초고속 퀴즈입니다.", "제한 시간 안에 목표 정답 수 달성하기", "선택지 클릭 또는 숫자 키", MinigameViewType.UiOnly, MinigameCharacterType.Raccoon, MinigameControlType.ClickMash),
        new("HideAcorns", "도토리를 숨기세요", "다람쥐가 도토리를 모아 들키지 않게 저장 장소에 숨기는 수집 게임입니다.", "제한 시간 안에 목표 개수의 도토리 숨기기", "이동 후 숨기기 입력", MinigameViewType.ThirdPerson, MinigameCharacterType.Squirrel, MinigameControlType.Movement),
        new("ClimbMountainGoat", "꼭대기로 올라가세요", "산양이 무너지는 절벽 발판을 피해 정상 깃발까지 오르는 수직 등반 게임입니다.", "제한 시간 안에 정상 깃발에 닿기", "좌우 이동, 점프, 벽면 재점프", MinigameViewType.SideView, MinigameCharacterType.MountainGoat, MinigameControlType.Movement),
        new("CrossRoadCheetah", "길을 건너세요", "치타가 차량과 강을 넘어 반대편 안전지대로 가는 아케이드 게임입니다.", "반대편 안전지대에 도착하기", "방향키 또는 WASD", MinigameViewType.ThirdPerson, MinigameCharacterType.Cheetah, MinigameControlType.Movement),
        new("StepTargetRhino", "표적을 밟으세요", "코뿔소가 잠깐 나타나는 목표 발판을 밟아 점수를 쌓는 반응 게임입니다.", "제한 시간 안에 목표 점수 달성하기", "방향키 또는 WASD", MinigameViewType.TopView, MinigameCharacterType.Rhinoceros, MinigameControlType.Movement),
        new("WallRunLizard", "벽을 타고 질주하세요", "도마뱀이 벽에 닿을 때까지 직선 질주하며 출구를 찾는 액션 게임입니다.", "가시와 추격 장애물을 피해 출구에 도착하기", "상하좌우로 질주 방향 선택", MinigameViewType.TopView, MinigameCharacterType.Lizard, MinigameControlType.Movement),
        new("ShapeBeaver", "모양을 따라 만드세요", "비버가 잠깐 본 목표 조형물을 제한된 조각으로 재구성하는 관찰 퍼즐입니다.", "제한 시간 안에 목표 모양 완성하기", "조각 드래그와 회전", MinigameViewType.TopView, MinigameCharacterType.Beaver, MinigameControlType.Puzzle),
        new("PetCat", "고양이를 쓰다듬으세요", "고양이의 표정과 털 방향을 읽고 기분 좋게 쓰다듬는 제스처 게임입니다.", "제한 시간 안에 친밀도 게이지 채우기", "털 방향을 따라 마우스 드래그", MinigameViewType.FirstPerson, MinigameCharacterType.Cat, MinigameControlType.Drag),
        new("DigBadger", "굴을 파세요", "오소리가 돌과 단단한 흙을 피해 목표 지점까지 통로를 연결하는 굴착 게임입니다.", "제한 시간 안에 안전한 굴을 목표 지점까지 연결하기", "방향 입력과 돌파 입력", MinigameViewType.SideView, MinigameCharacterType.Badger, MinigameControlType.Movement),
        new("FeedBoar", "먹이를 먹으세요", "멧돼지가 상태에 맞는 먹이를 골라 포만 게이지를 채우는 선택 게임입니다.", "제한 시간 안에 포만 게이지 채우기", "먹이 선택", MinigameViewType.SideView, MinigameCharacterType.Boar, MinigameControlType.ClickMash),
        new("TugOfWarOtter", "줄다리기에서 이기세요", "수달 선수가 상대 동물 팀과의 결승 줄다리기에서 이기는 대결 게임입니다.", "중앙 깃발을 내 쪽 결승선까지 끌기", "좌·우 키 번갈아 연타", MinigameViewType.SideView, MinigameCharacterType.Otter, MinigameControlType.ButtonMash),
        new("BirdStrikePigeon", "버드스트라이크를 일으키세요", "비둘기 무리를 조종해 가상의 공항 상공을 지나는 비행기에 돌진시키는 타이밍 게임입니다.", "제한 시간 안에 지정 횟수만큼 비행기와 충돌하기", "마우스 또는 방향키", MinigameViewType.SideView, MinigameCharacterType.Pigeon, MinigameControlType.Movement),
        new("WeaveWebSpider", "거미줄을 치세요", "거미가 나뭇가지 사이에 거미줄을 연결해 먹이를 잡는 배치 퍼즐 게임입니다.", "제한 시간 안에 목표 수의 곤충을 잡을 거미줄 만들기", "연결 지점 선택", MinigameViewType.TopView, MinigameCharacterType.Spider, MinigameControlType.Puzzle),
        new("ThrowFruitMonkey", "열매를 던지세요", "원숭이가 나무 위에서 날아다니는 표적을 향해 열매를 던지는 조준 게임입니다.", "제한 시간 안에 목표 점수만큼 표적 맞히기", "마우스 조준과 클릭", MinigameViewType.SideView, MinigameCharacterType.Monkey, MinigameControlType.Aim),
        new("TrackScentWolf", "냄새를 추적하세요", "늑대가 숲에 남은 냄새 흔적을 따라 목적지를 찾는 탐색 게임입니다.", "제한 시간 안에 냄새의 근원에 도착하기", "이동과 냄새 맡기", MinigameViewType.ThirdPerson, MinigameCharacterType.Wolf, MinigameControlType.Movement),
        new("MoveBranchGiraffe", "나뭇가지를 옮기세요", "기린이 강물의 나뭇가지를 옮겨 임시 댐의 빈틈을 메우는 운반 게임입니다.", "제한 시간 안에 댐의 빈틈을 모두 메우기", "이동, 물기, 놓기", MinigameViewType.ThirdPerson, MinigameCharacterType.Giraffe, MinigameControlType.Movement),
        new("HerdFishDolphin", "물고기 떼를 몰아가세요", "돌고래가 산호초 사이의 물고기 떼를 안전 구역으로 모는 유도 게임입니다.", "제한 시간 안에 모든 물고기를 안전 구역으로 이동시키기", "이동 키", MinigameViewType.TopView, MinigameCharacterType.Dolphin, MinigameControlType.Movement),
        new("RollSnowballArcticFox", "눈덩이를 굴리세요", "북극여우가 눈덩이를 목표 크기로 키워 깃발 지점까지 보내는 경로 퍼즐입니다.", "목표 크기의 눈덩이를 깃발에 보내기", "이동 키와 방향 전환", MinigameViewType.ThirdPerson, MinigameCharacterType.ArcticFox, MinigameControlType.Movement),
        new("RollMudBoar", "진흙탕을 구르세요", "멧돼지가 비탈진 진흙길을 굴러 내려가 결승 진흙탕에 도착하는 물리 액션 게임입니다.", "속도를 유지한 채 결승 진흙탕에 도착하기", "좌우 이동과 점프", MinigameViewType.SideView, MinigameCharacterType.Boar, MinigameControlType.Movement),
    };

    [MenuItem("Wildlife Sports Day/Build Minigame Scaffold Scenes")]
    public static void BuildAll()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError("미니게임 스캐폴드에 필요한 폰트를 찾을 수 없습니다.");
            return;
        }

        for (int i = 0; i < Scaffolds.Length; i++)
        {
            CreateDefinitionIfMissing(Scaffolds[i]);
            CreateSceneIfMissing(Scaffolds[i], font);
        }

        MinigameDefinitionSceneReferenceUtility.AssignMissingReferences();
        NormalizeExistingGenericScaffoldScenes(font);
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"미니게임 스캐폴드 {Scaffolds.Length}개를 확인했습니다.");
    }

    private static void CreateDefinitionIfMissing(MinigameScaffold scaffold)
    {
        string definitionPath = $"{DefinitionFolderPath}/{scaffold.Id}.asset";
        if (AssetDatabase.LoadAssetAtPath<MinigameDefinition>(definitionPath) != null)
        {
            return;
        }

        MinigameDefinition definition = ScriptableObject.CreateInstance<MinigameDefinition>();
        definition.name = scaffold.Id;
        SerializedObject serialized = new(definition);
        serialized.FindProperty("_minigameName").stringValue = scaffold.Name;
        serialized.FindProperty("_description").stringValue = scaffold.Description;
        serialized.FindProperty("_objective").stringValue = scaffold.Objective;
        serialized.FindProperty("_controlGuide").stringValue = scaffold.ControlGuide;
        serialized.FindProperty("_viewType").enumValueIndex = (int)scaffold.ViewType;
        serialized.FindProperty("_characterType").enumValueIndex = (int)scaffold.CharacterType;
        serialized.FindProperty("_controlType").enumValueIndex = (int)scaffold.ControlType;
        serialized.FindProperty("_difficulty").enumValueIndex = (int)MinigameDifficultyType.Normal;
        serialized.FindProperty("_isRandomSelectionEnabled").boolValue = false;
        serialized.FindProperty("_score").intValue = 300;
        serialized.FindProperty("_targetCount").intValue = 1;
        serialized.FindProperty("_sceneName").stringValue = $"Minigame_{scaffold.Id}";
        serialized.FindProperty("_oneStarTargetCount").intValue = 1;
        serialized.FindProperty("_twoStarTargetCount").intValue = 2;
        serialized.FindProperty("_threeStarTargetCount").intValue = 3;
        serialized.FindProperty("_oneStarScore").intValue = 100;
        serialized.FindProperty("_twoStarScore").intValue = 200;
        serialized.FindProperty("_threeStarScore").intValue = 300;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(definition, definitionPath);
    }

    private static void CreateSceneIfMissing(MinigameScaffold scaffold, TMP_FontAsset font)
    {
        string scenePath = $"{SceneFolderPath}/Minigame_{scaffold.Id}.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCamera();
        Canvas canvas = CreateCanvas(camera);
        CreateBackdrop(canvas.transform);
        CreateScaffoldLabels(canvas.transform, scaffold, font);
        Button completeButton = CreateCompleteButton(canvas.transform, font);
        MinigameSceneController controller = new GameObject("MinigameSceneController").AddComponent<MinigameSceneController>();
        AssignControllerReferences(controller, canvas.transform, completeButton);
        CreateEventSystem();
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    /// <summary>
    /// 이전에 UI 없이 생성된 공통 컨트롤러 씬에 현재 스캐폴드 UI를 보강합니다.
    /// </summary>
    private static void NormalizeExistingGenericScaffoldScenes(TMP_FontAsset font)
    {
        MinigameDefinition[] definitions = Resources.LoadAll<MinigameDefinition>("Minigames");
        List<ExistingSceneScaffold> existingScenes = new(definitions.Length);
        for (int i = 0; i < definitions.Length; i++)
        {
            MinigameDefinition definition = definitions[i];
            if (definition == null || definition.SceneAsset == null)
            {
                continue;
            }

            existingScenes.Add(new ExistingSceneScaffold(
                AssetDatabase.GetAssetPath(definition.SceneAsset),
                definition.DisplayName,
                definition.Description,
                definition.Objective,
                definition.ControlGuide));
        }

        for (int i = 0; i < existingScenes.Count; i++)
        {
            ExistingSceneScaffold scaffold = existingScenes[i];
            Scene scene = EditorSceneManager.OpenScene(scaffold.ScenePath, OpenSceneMode.Single);
            MinigameSceneController controller = UnityEngine.Object.FindFirstObjectByType<MinigameSceneController>();
            if (controller == null || UnityEngine.Object.FindFirstObjectByType<Canvas>() != null)
            {
                continue;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = CreateCamera();
            }

            Canvas canvas = CreateCanvas(camera);
            CreateBackdrop(canvas.transform);
            CreateScaffoldLabels(canvas.transform, scaffold.MinigameName, scaffold.Description, scaffold.Objective, scaffold.ControlGuide, font);
            Button completeButton = CreateCompleteButton(canvas.transform, font);
            AssignControllerReferences(controller, canvas.transform, completeButton);
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.1f, 0.18f);
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

    private static void CreateBackdrop(Transform parent)
    {
        Image backdrop = CreateImage(parent, "ScaffoldBackdrop", new Color(0.04f, 0.1f, 0.18f));
        Stretch(backdrop.rectTransform, Vector2.zero);
        Image stage = CreateImage(parent, "GameplayRoot", new Color(0.08f, 0.2f, 0.31f));
        Stretch(stage.rectTransform, new Vector2(180f, 250f));
        stage.rectTransform.offsetMax = new Vector2(-180f, -340f);
    }

    private static void CreateScaffoldLabels(Transform parent, MinigameScaffold scaffold, TMP_FontAsset font)
    {
        CreateScaffoldLabels(parent, scaffold.Name, scaffold.Description, scaffold.Objective, scaffold.ControlGuide, font);
    }

    private static void CreateScaffoldLabels(Transform parent, string minigameName, string description, string objective, string controlGuide, TMP_FontAsset font)
    {
        CreateText(parent, "Text_Scaffold", "MINIGAME SCAFFOLD", font, 28f, new Vector2(0.5f, 0.87f), new Vector2(1700f, 60f), new Color(0.45f, 0.78f, 1f), TextAlignmentOptions.Center);
        CreateText(parent, "Text_Title", minigameName, font, 74f, new Vector2(0.5f, 0.76f), new Vector2(1900f, 120f), Color.white, TextAlignmentOptions.Center);
        CreateText(parent, "Text_Description", description, font, 34f, new Vector2(0.5f, 0.62f), new Vector2(1800f, 150f), new Color(0.84f, 0.92f, 0.98f), TextAlignmentOptions.Center);
        CreateText(parent, "Text_Objective", $"목표: {objective}", font, 31f, new Vector2(0.5f, 0.43f), new Vector2(1750f, 90f), new Color(1f, 0.85f, 0.36f), TextAlignmentOptions.Center);
        CreateText(parent, "Text_Control", $"조작: {controlGuide}", font, 29f, new Vector2(0.5f, 0.36f), new Vector2(1750f, 80f), new Color(0.78f, 0.9f, 0.98f), TextAlignmentOptions.Center);
        CreateText(parent, "Text_Progress", "전용 게임플레이는 아직 구현 전입니다.", font, 27f, new Vector2(0.5f, 0.25f), new Vector2(1750f, 70f), new Color(0.7f, 0.78f, 0.86f), TextAlignmentOptions.Center);
    }

    private static Button CreateCompleteButton(Transform parent, TMP_FontAsset font)
    {
        GameObject buttonObject = new("Button_CompletePlaceholder", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.63f, 0.38f);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.12f);
        rect.sizeDelta = new Vector2(540f, 110f);
        CreateText(buttonObject.transform, "Text", "스캐폴드 완료 처리", font, 33f, new Vector2(0.5f, 0.5f), new Vector2(500f, 80f), Color.white, TextAlignmentOptions.Center);
        return buttonObject.GetComponent<Button>();
    }

    private static void AssignControllerReferences(MinigameSceneController controller, Transform canvas, Button completeButton)
    {
        SerializedObject serialized = new(controller);
        serialized.FindProperty("_titleText").objectReferenceValue = canvas.Find("Text_Title").GetComponent<TMP_Text>();
        serialized.FindProperty("_descriptionText").objectReferenceValue = canvas.Find("Text_Description").GetComponent<TMP_Text>();
        serialized.FindProperty("_progressText").objectReferenceValue = canvas.Find("Text_Progress").GetComponent<TMP_Text>();
        serialized.FindProperty("_completeButton").objectReferenceValue = completeButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, TMP_FontAsset font, float fontSize, Vector2 anchor, Vector2 size, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = anchor;
        text.rectTransform.sizeDelta = size;
        return text;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect, Vector2 inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = inset;
        rect.offsetMax = -inset;
    }

    private static void ConfigureBuildSettings()
    {
        List<string> scenePaths = new()
        {
            "Assets/Scenes/LoginScene.unity",
            "Assets/Scenes/MainScene.unity",
            "Assets/Scenes/MinigameSelection.unity",
        };
        List<SceneAsset> minigameScenes = MinigameDefinitionSceneReferenceUtility.GetReferencedScenes();
        for (int i = 0; i < minigameScenes.Count; i++)
        {
            scenePaths.Add(AssetDatabase.GetAssetPath(minigameScenes[i]));
        }
        List<EditorBuildSettingsScene> buildScenes = new(scenePaths.Count);
        for (int i = 0; i < scenePaths.Count; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePaths[i]) != null)
            {
                buildScenes.Add(new EditorBuildSettingsScene(scenePaths[i], true));
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
    }

    private readonly struct MinigameScaffold
    {
        public MinigameScaffold(string id, string name, string description, string objective, string controlGuide, MinigameViewType viewType, MinigameCharacterType characterType, MinigameControlType controlType)
        {
            Id = id;
            Name = name;
            Description = description;
            Objective = objective;
            ControlGuide = controlGuide;
            ViewType = viewType;
            CharacterType = characterType;
            ControlType = controlType;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string Objective { get; }
        public string ControlGuide { get; }
        public MinigameViewType ViewType { get; }
        public MinigameCharacterType CharacterType { get; }
        public MinigameControlType ControlType { get; }
    }

    private readonly struct ExistingSceneScaffold
    {
        public ExistingSceneScaffold(string scenePath, string minigameName, string description, string objective, string controlGuide)
        {
            ScenePath = scenePath;
            MinigameName = minigameName;
            Description = description;
            Objective = objective;
            ControlGuide = controlGuide;
        }

        public string ScenePath { get; }
        public string MinigameName { get; }
        public string Description { get; }
        public string Objective { get; }
        public string ControlGuide { get; }
    }
}

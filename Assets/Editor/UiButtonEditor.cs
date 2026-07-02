using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(UiButton))]
public sealed class UiButtonEditor : ButtonEditor
{
    private SerializedProperty _audioSource;
    private SerializedProperty _pointerEnterSound;
    private SerializedProperty _pointerDownSound;
    private SerializedProperty _clickSound;
    private SerializedProperty _useInteractionMotion;
    private SerializedProperty _motionTarget;
    private SerializedProperty _useUnscaledMotionTime;
    private SerializedProperty _interactionMotion;
    private SerializedProperty _clickAction;
    private SerializedProperty _currentScreen;
    private SerializedProperty _targetScreen;
    private SerializedProperty _transitionMode;
    private SerializedProperty _useTransitionAnimation;
    private SerializedProperty _blockClickDuringAction;
    private SerializedProperty _sceneName;
    private SerializedProperty _loadSceneMode;
    private SerializedProperty _targetGameObject;
    private SerializedProperty _setActiveValue;
    private SerializedProperty _url;
    private SerializedProperty _actionCompleted;

    protected override void OnEnable()
    {
        base.OnEnable();

        _audioSource = serializedObject.FindProperty("_audioSource");
        _pointerEnterSound = serializedObject.FindProperty("_pointerEnterSound");
        _pointerDownSound = serializedObject.FindProperty("_pointerDownSound");
        _clickSound = serializedObject.FindProperty("_clickSound");
        _useInteractionMotion = serializedObject.FindProperty("_useInteractionMotion");
        _motionTarget = serializedObject.FindProperty("_motionTarget");
        _useUnscaledMotionTime = serializedObject.FindProperty("_useUnscaledMotionTime");
        _interactionMotion = serializedObject.FindProperty("_interactionMotion");
        _clickAction = serializedObject.FindProperty("_clickAction");
        _currentScreen = serializedObject.FindProperty("_currentScreen");
        _targetScreen = serializedObject.FindProperty("_targetScreen");
        _transitionMode = serializedObject.FindProperty("_transitionMode");
        _useTransitionAnimation = serializedObject.FindProperty("_useTransitionAnimation");
        _blockClickDuringAction = serializedObject.FindProperty("_blockClickDuringAction");
        _sceneName = serializedObject.FindProperty("_sceneName");
        _loadSceneMode = serializedObject.FindProperty("_loadSceneMode");
        _targetGameObject = serializedObject.FindProperty("_targetGameObject");
        _setActiveValue = serializedObject.FindProperty("_setActiveValue");
        _url = serializedObject.FindProperty("_url");
        _actionCompleted = serializedObject.FindProperty("_actionCompleted");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        float previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = Mathf.Max(previousLabelWidth, 190f);

        try
        {
            Layout.Note(
                "Unity Button을 확장한 공통 UI 버튼입니다.\n" +
                "사운드, 버튼 모션, 클릭 동작을 Inspector에서 설정합니다.");

            base.OnInspectorGUI();

            DrawSoundSettings();
            DrawMotionSettings();
            DrawClickActionSettings();
            DrawEvents();
        }
        finally
        {
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSoundSettings()
    {
        Layout.Section("사운드", () =>
        {
            EditorGUILayout.PropertyField(_audioSource, new GUIContent("AudioSource", "비어 있으면 부모에서 AudioSource를 찾습니다."));
            EditorGUILayout.PropertyField(_pointerEnterSound, new GUIContent("Pointer Enter 사운드"));
            EditorGUILayout.PropertyField(_pointerDownSound, new GUIContent("Pointer Down 사운드"));
            EditorGUILayout.PropertyField(_clickSound, new GUIContent("Click 사운드"));
        });
    }

    private void DrawMotionSettings()
    {
        Layout.Section("상호작용 모션", () =>
        {
            EditorGUILayout.PropertyField(_useInteractionMotion, new GUIContent("모션 사용"));

            if (!_useInteractionMotion.boolValue)
            {
                Layout.Note("버튼 상호작용 모션을 사용하지 않습니다.");
                return;
            }

            EditorGUILayout.PropertyField(_motionTarget, new GUIContent("모션 대상", "비어 있으면 버튼 자신의 RectTransform을 사용합니다."));
            EditorGUILayout.PropertyField(_useUnscaledMotionTime, new GUIContent("Unscaled Time 사용"));

            SerializedProperty duration = _interactionMotion.FindPropertyRelative("_duration");
            SerializedProperty ease = _interactionMotion.FindPropertyRelative("_ease");
            SerializedProperty hoverScale = _interactionMotion.FindPropertyRelative("_hoverScale");
            SerializedProperty pressedScale = _interactionMotion.FindPropertyRelative("_pressedScale");
            SerializedProperty disabledAlpha = _interactionMotion.FindPropertyRelative("_disabledAlpha");
            SerializedProperty hoverOffset = _interactionMotion.FindPropertyRelative("_hoverOffset");
            SerializedProperty pressedOffset = _interactionMotion.FindPropertyRelative("_pressedOffset");
            SerializedProperty useClickPunch = _interactionMotion.FindPropertyRelative("_useClickPunch");
            SerializedProperty clickPunchScale = _interactionMotion.FindPropertyRelative("_clickPunchScale");
            SerializedProperty clickPunchDuration = _interactionMotion.FindPropertyRelative("_clickPunchDuration");

            EditorGUILayout.PropertyField(duration, new GUIContent("전환 시간"));
            EditorGUILayout.PropertyField(ease, new GUIContent("Ease"));
            EditorGUILayout.PropertyField(hoverScale, new GUIContent("Hover Scale"));
            EditorGUILayout.PropertyField(pressedScale, new GUIContent("Press Scale"));
            EditorGUILayout.PropertyField(disabledAlpha, new GUIContent("비활성 Alpha"));
            DrawVector2(hoverOffset, "Hover 위치 오프셋");
            DrawVector2(pressedOffset, "Press 위치 오프셋");

            Layout.Subsection("클릭", () =>
            {
                EditorGUILayout.PropertyField(useClickPunch, new GUIContent("Click Punch 사용"));

                if (useClickPunch.boolValue)
                {
                    DrawVector3(clickPunchScale, "Click Punch Scale");
                    EditorGUILayout.PropertyField(clickPunchDuration, new GUIContent("Click Punch 시간"));
                }
            });
        });
    }

    private void DrawClickActionSettings()
    {
        Layout.Section("클릭 시 동작", () =>
        {
            EditorGUILayout.PropertyField(_clickAction, new GUIContent("동작 타입", "클릭 성공 시 실행할 버튼 동작입니다."));

            UiButton.ButtonActionType action = (UiButton.ButtonActionType)_clickAction.enumValueIndex;
            if (action == UiButton.ButtonActionType.None)
            {
                Layout.Note("클릭 동작을 사용하지 않습니다.");
                return;
            }

            switch (action)
            {
                case UiButton.ButtonActionType.OpenScreen:
                    EditorGUILayout.PropertyField(_targetScreen, new GUIContent("대상 화면", "열 UiScreen을 직접 연결합니다."));
                    DrawTransitionOptions();
                    break;
                case UiButton.ButtonActionType.CloseScreen:
                    EditorGUILayout.PropertyField(_currentScreen, new GUIContent("현재 화면", "닫을 UiScreen을 직접 연결합니다."));
                    DrawTransitionOptions();
                    break;
                case UiButton.ButtonActionType.ChangeScreen:
                    EditorGUILayout.PropertyField(_currentScreen, new GUIContent("현재 화면", "닫거나 전환할 시작 화면입니다."));
                    EditorGUILayout.PropertyField(_targetScreen, new GUIContent("대상 화면", "열 UiScreen을 직접 연결합니다."));
                    EditorGUILayout.PropertyField(_transitionMode, new GUIContent("전환 방식"));
                    DrawTransitionOptions();
                    break;
                case UiButton.ButtonActionType.LoadScene:
                    EditorGUILayout.PropertyField(_sceneName, new GUIContent("씬 이름"));
                    EditorGUILayout.PropertyField(_loadSceneMode, new GUIContent("로드 모드"));
                    break;
                case UiButton.ButtonActionType.ReloadScene:
                    Layout.Note($"현재 활성 씬을 다시 로드합니다. 현재 씬: {SceneManager.GetActiveScene().name}");
                    break;
                case UiButton.ButtonActionType.QuitGame:
                    Layout.Note("플레이어 빌드에서는 Application.Quit을 실행합니다.");
                    break;
                case UiButton.ButtonActionType.SetGameObjectActive:
                    EditorGUILayout.PropertyField(_targetGameObject, new GUIContent("대상 GameObject"));
                    EditorGUILayout.PropertyField(_setActiveValue, new GUIContent("Active 값"));
                    break;
                case UiButton.ButtonActionType.OpenUrl:
                    EditorGUILayout.PropertyField(_url, new GUIContent("URL"));
                    break;
            }

            EditorGUILayout.PropertyField(_blockClickDuringAction, new GUIContent("동작 중 중복 클릭 차단"));
        });
    }

    private void DrawTransitionOptions()
    {
        EditorGUILayout.PropertyField(_useTransitionAnimation, new GUIContent("전환 애니메이션 사용"));
    }

    private void DrawEvents()
    {
        Layout.Section("이벤트", () =>
        {
            EditorGUILayout.PropertyField(_actionCompleted, new GUIContent("동작 완료 이벤트"));
        });
    }

    private static void DrawVector2(SerializedProperty property, string label)
    {
        Layout.Subsection(label, () =>
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("x"), new GUIContent("X"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("y"), new GUIContent("Y"));
            }
        });
    }

    private static void DrawVector3(SerializedProperty property, string label)
    {
        Layout.Subsection(label, () =>
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("x"), new GUIContent("X"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("y"), new GUIContent("Y"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("z"), new GUIContent("Z"));
            }
        });
    }

    private static class Layout
    {
        private const string SectionFoldoutPrefix = "WildlifeSportsDay.UiButtonEditor.Section.";

        private static readonly GUIStyle SectionStyle = new(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 10),
            margin = new RectOffset(0, 0, 6, 8),
        };

        private static readonly GUIStyle SubsectionStyle = new(EditorStyles.helpBox)
        {
            padding = new RectOffset(8, 8, 6, 8),
            margin = new RectOffset(0, 0, 4, 6),
        };

        public static void Section(string title, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(SectionStyle);
            bool isExpanded = DrawSectionFoldout(title);
            if (isExpanded)
            {
                EditorGUILayout.Space(2f);
                drawContent?.Invoke();
            }

            EditorGUILayout.EndVertical();
        }

        public static void Note(string text)
        {
            EditorGUILayout.HelpBox(text, MessageType.None);
        }

        public static void Subsection(string title, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(SubsectionStyle);
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            drawContent?.Invoke();
            EditorGUILayout.EndVertical();
        }

        private static bool DrawSectionFoldout(string title)
        {
            string key = $"{SectionFoldoutPrefix}{title}";
            bool isExpanded = EditorPrefs.GetBool(key, true);

            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            rect.x += 8f;
            rect.width -= 8f;
            isExpanded = EditorGUI.Foldout(rect, isExpanded, title, true, EditorStyles.foldout);
            EditorPrefs.SetBool(key, isExpanded);
            return isExpanded;
        }
    }
}

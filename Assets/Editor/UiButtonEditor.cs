using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(UiButton))]
public sealed class UiButtonEditor : ButtonEditor
{
    private SerializedProperty _audioSource;
    private SerializedProperty _pointerEnterSound;
    private SerializedProperty _pointerDownSound;
    private SerializedProperty _clickSound;
    private SerializedProperty _useInteractionAnimations;
    private SerializedProperty _pointerEnterAnimation;
    private SerializedProperty _pointerExitAnimation;
    private SerializedProperty _pointerDownAnimation;
    private SerializedProperty _pointerUpAnimation;
    private SerializedProperty _clickAnimation;
    private SerializedProperty _disabledAnimation;
    private SerializedProperty _screenAction;
    private SerializedProperty _currentScreen;
    private SerializedProperty _targetScreen;
    private SerializedProperty _transitionMode;
    private SerializedProperty _useTransitionAnimation;
    private SerializedProperty _blockClickDuringTransition;
    private SerializedProperty _transitionCompleted;

    protected override void OnEnable()
    {
        base.OnEnable();

        _audioSource = serializedObject.FindProperty("_audioSource");
        _pointerEnterSound = serializedObject.FindProperty("_pointerEnterSound");
        _pointerDownSound = serializedObject.FindProperty("_pointerDownSound");
        _clickSound = serializedObject.FindProperty("_clickSound");
        _useInteractionAnimations = serializedObject.FindProperty("_useInteractionAnimations");
        _pointerEnterAnimation = serializedObject.FindProperty("_pointerEnterAnimation");
        _pointerExitAnimation = serializedObject.FindProperty("_pointerExitAnimation");
        _pointerDownAnimation = serializedObject.FindProperty("_pointerDownAnimation");
        _pointerUpAnimation = serializedObject.FindProperty("_pointerUpAnimation");
        _clickAnimation = serializedObject.FindProperty("_clickAnimation");
        _disabledAnimation = serializedObject.FindProperty("_disabledAnimation");
        _screenAction = serializedObject.FindProperty("_screenAction");
        _currentScreen = serializedObject.FindProperty("_currentScreen");
        _targetScreen = serializedObject.FindProperty("_targetScreen");
        _transitionMode = serializedObject.FindProperty("_transitionMode");
        _useTransitionAnimation = serializedObject.FindProperty("_useTransitionAnimation");
        _blockClickDuringTransition = serializedObject.FindProperty("_blockClickDuringTransition");
        _transitionCompleted = serializedObject.FindProperty("_transitionCompleted");
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
                "포인터 이벤트 애니메이션, 사운드, 직접 참조한 UiScreen 화면 동작을 Inspector에서 설정합니다.");

            base.OnInspectorGUI();

            DrawSoundSettings();
            DrawInteractionAnimationSettings();
            DrawScreenActionSettings();
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

    private void DrawInteractionAnimationSettings()
    {
        Layout.Section("상호작용 애니메이션", () =>
        {
            EditorGUILayout.PropertyField(_useInteractionAnimations, new GUIContent("애니메이션 사용"));

            if (!_useInteractionAnimations.boolValue)
            {
                Layout.Note("버튼 상호작용 애니메이션을 사용하지 않습니다.");
                return;
            }

            Layout.Subsection("포인터 이벤트", () =>
            {
                EditorGUILayout.PropertyField(_pointerEnterAnimation, new GUIContent("Pointer Enter", "마우스가 들어오면 PlayShow를 실행합니다."));
                EditorGUILayout.PropertyField(_pointerExitAnimation, new GUIContent("Pointer Exit", "마우스가 나가면 PlayHide를 실행합니다."));
                EditorGUILayout.PropertyField(_pointerDownAnimation, new GUIContent("Pointer Down", "누르는 순간 PlayShow를 실행합니다."));
                EditorGUILayout.PropertyField(_pointerUpAnimation, new GUIContent("Pointer Up", "떼는 순간 PlayHide를 실행합니다."));
                EditorGUILayout.PropertyField(_clickAnimation, new GUIContent("Click", "클릭 성공 시 PlayShow를 실행합니다."));
                EditorGUILayout.PropertyField(_disabledAnimation, new GUIContent("비활성화", "Interactable이 꺼질 때 PlayShow를 실행합니다."));
            });

            Layout.Warning(
                "버튼 루트는 Raycast 판정용으로 고정하고, 실제 Scale/Move 애니메이션은 Visual 자식에 붙이는 것을 권장합니다.\n" +
                "Visual 자식의 Image/Text는 Raycast Target을 끄는 것이 안전합니다.\n" +
                "Pointer Exit는 연결된 UiAnimation의 Hide 설정을 사용합니다.");
        });
    }

    private void DrawScreenActionSettings()
    {
        Layout.Section("클릭 시 화면 전환", () =>
        {
            EditorGUILayout.PropertyField(_screenAction, new GUIContent("동작 타입", "클릭 성공 시 실행할 화면 동작입니다."));

            UiButton.ScreenAction action = (UiButton.ScreenAction)_screenAction.enumValueIndex;
            if (action == UiButton.ScreenAction.None)
            {
                Layout.Note("화면 전환을 사용하지 않습니다.");
                return;
            }

            if (action == UiButton.ScreenAction.Close || action == UiButton.ScreenAction.Change)
            {
                EditorGUILayout.PropertyField(_currentScreen, new GUIContent("현재 화면", "닫거나 전환할 시작 화면입니다."));
            }

            if (action == UiButton.ScreenAction.Open || action == UiButton.ScreenAction.Change)
            {
                EditorGUILayout.PropertyField(_targetScreen, new GUIContent("대상 화면", "열 UiScreen을 직접 연결합니다."));
            }

            if (action == UiButton.ScreenAction.Change)
            {
                EditorGUILayout.PropertyField(_transitionMode, new GUIContent("전환 방식"));
            }

            EditorGUILayout.PropertyField(_useTransitionAnimation, new GUIContent("전환 애니메이션 사용"));
            EditorGUILayout.PropertyField(_blockClickDuringTransition, new GUIContent("전환 중 중복 클릭 차단"));
            Layout.Note("Open은 대상 화면, Close는 현재 화면, Change는 현재 화면과 대상 화면이 필요합니다.");
        });
    }

    private void DrawEvents()
    {
        Layout.Section("이벤트", () =>
        {
            EditorGUILayout.PropertyField(_transitionCompleted, new GUIContent("전환 완료 이벤트"));
        });
    }

    private static class Layout
    {
        private const string SectionFoldoutPrefix = "WildlifeSportsDay.UiButtonEditor.Section.";

        private static readonly GUIStyle SectionStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 10),
            margin = new RectOffset(0, 0, 6, 8),
        };

        private static readonly GUIStyle SubsectionStyle = new GUIStyle(EditorStyles.helpBox)
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

        public static void Subsection(string title, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(SubsectionStyle);
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            drawContent?.Invoke();
            EditorGUILayout.EndVertical();
        }

        public static void Note(string text)
        {
            EditorGUILayout.HelpBox(text, MessageType.None);
        }

        public static void Warning(string text)
        {
            EditorGUILayout.HelpBox(text, MessageType.Warning);
        }
    }
}

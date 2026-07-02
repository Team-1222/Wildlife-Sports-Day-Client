using UnityEditor;
using UnityEngine;
using DG.Tweening;

[CustomEditor(typeof(UiAnimation))]
public sealed class UiAnimationEditor : Editor
{
    private static bool _showAdvancedEase;
    private const float EaseButtonHeight = 24f;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        float previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = Mathf.Max(previousLabelWidth, 190f);

        try
        {
            Layout.Note(
                "UI 요소 하나의 Fade, Scale, Move 등장/퇴장 애니메이션을 관리합니다.\n" +
                "실행 타이밍과 순서는 UiScreen에서 제어하는 것을 권장합니다.");

            SerializedProperty useSameSettings = serializedObject.FindProperty("_useSameSettingsForShowAndHide");
            DrawAnimationModeSettings(useSameSettings);

            if (useSameSettings.boolValue)
            {
                DrawAnimationSettings("등장/퇴장 공통 애니메이션", "_showSettings", null, true);
                DrawCompletionEvents();
            }
            else
            {
                DrawAnimationSettings("등장 애니메이션", "_showSettings", "_showCompleted", true);
                DrawAnimationSettings("퇴장 애니메이션", "_hideSettings", "_hideCompleted", false);
            }

            DrawExecutionSettings();
            DrawRuntimeButtons();
        }
        finally
        {
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawExecutionSettings()
    {
        Layout.Section("공통 실행 설정", () =>
        {
            Layout.Note("Fade, Scale, Move 중 하나 이상 켜진 등장/퇴장 애니메이션에 공통으로 적용됩니다.");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_duration"), new GUIContent("지속 시간", "애니메이션이 진행되는 시간입니다."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_delay"), new GUIContent("시작 지연", "애니메이션 시작 전 대기 시간입니다."));
            DrawEasePresetSelector(serializedObject.FindProperty("_ease"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_useUnscaledTime"), new GUIContent("시간 정지 무시", "Time.timeScale이 0이어도 UI 애니메이션을 실행합니다."));
        });
    }

    private void DrawAnimationModeSettings(SerializedProperty useSameSettings)
    {
        Layout.Section("애니메이션 설정 방식", () =>
        {
            EditorGUILayout.PropertyField(useSameSettings, new GUIContent("등장/퇴장 설정 같이 사용", "켜면 등장 설정 하나만 입력하고 퇴장은 같은 설정을 반대로 재생합니다."));
            EditorGUILayout.Space(3f);

            if (useSameSettings.boolValue)
            {
                Layout.Note("퇴장은 같은 Fade/Scale/Move 값을 반대로 재생합니다. 예: Right 80 등장은 오른쪽에서 들어오고, 퇴장은 오른쪽으로 나갑니다.");
            }
        });
    }

    private void DrawCompletionEvents()
    {
        Layout.Section("완료 이벤트", () =>
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_showCompleted"), new GUIContent("등장 완료 이벤트"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_hideCompleted"), new GUIContent("퇴장 완료 이벤트"));
        });
    }

    private void DrawAnimationSettings(string title, string settingsPath, string eventPath, bool isShow)
    {
        SerializedProperty settings = serializedObject.FindProperty(settingsPath);

        Layout.Section(title, () =>
        {
            Layout.Note(
                isShow
                    ? "PlayShow 호출 시 사용할 시작값과 도착값입니다."
                    : "PlayHide 호출 시 사용할 시작값과 도착값입니다.");

            DrawFadeSettings(settings);
            DrawScaleSettings(settings);
            DrawMoveSettings(settings);

            if (!string.IsNullOrEmpty(eventPath))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(eventPath), new GUIContent("완료 이벤트", "애니메이션 완료 후 호출됩니다."));
            }
        });
    }

    private static void DrawFadeSettings(SerializedProperty settings)
    {
        Layout.Subsection("Fade", () =>
        {
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("_useFade"), new GUIContent("Fade 사용"));

            if (!settings.FindPropertyRelative("_useFade").boolValue)
            {
                return;
            }

            EditorGUILayout.PropertyField(settings.FindPropertyRelative("_fromAlpha"), new GUIContent("시작 Alpha"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("_toAlpha"), new GUIContent("종료 Alpha"));
        });
    }

    private static void DrawScaleSettings(SerializedProperty settings)
    {
        Layout.Subsection("Scale", () =>
        {
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("_useScale"), new GUIContent("Scale 사용"));

            if (!settings.FindPropertyRelative("_useScale").boolValue)
            {
                return;
            }

            EditorGUILayout.PropertyField(settings.FindPropertyRelative("_scaleFromSavedState"), new GUIContent("저장된 기본 Scale에서 시작"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("_fromScale"), new GUIContent("시작 Scale"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("_toScale"), new GUIContent("종료 Scale"));
            Layout.Warning("버튼 Hover용 Scale 애니메이션은 버튼 루트가 아니라 Visual 자식에 붙이는 것을 권장합니다.");
        });
    }

    private static void DrawMoveSettings(SerializedProperty settings)
    {
        Layout.Subsection("Move", () =>
        {
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("_useMove"), new GUIContent("Move 사용"));

            if (!settings.FindPropertyRelative("_useMove").boolValue)
            {
                return;
            }

            EditorGUILayout.PropertyField(settings.FindPropertyRelative("_moveFromSavedState"), new GUIContent("저장된 기본 위치 기준"));
            SerializedProperty movePreset = settings.FindPropertyRelative("_movePreset");
            EditorGUILayout.PropertyField(movePreset, new GUIContent("이동 방향 프리셋"));

            UiAnimation.MovePreset preset = (UiAnimation.MovePreset)movePreset.intValue;
            if (preset == UiAnimation.MovePreset.Custom)
            {
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("_fromPosition"), new GUIContent("시작 위치"));
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("_toPosition"), new GUIContent("종료 위치"));
            }
            else
            {
                DrawMoveDistance(settings.FindPropertyRelative("_offset"), preset);
            }

            Layout.Warning("Raycast 대상에 Move를 적용하면 Pointer Enter/Exit가 반복될 수 있습니다. 실제 이동은 Visual 자식에 적용하는 것이 안전합니다.");
        });
    }

    private static void DrawMoveDistance(SerializedProperty offset, UiAnimation.MovePreset preset)
    {
        Vector2 value = offset.vector2Value;
        bool useHorizontalDistance = preset == UiAnimation.MovePreset.Left || preset == UiAnimation.MovePreset.Right;
        float distance = useHorizontalDistance ? Mathf.Abs(value.x) : Mathf.Abs(value.y);

        EditorGUI.BeginChangeCheck();
        distance = EditorGUILayout.FloatField(new GUIContent("이동 거리", "선택한 방향으로 이동할 거리입니다."), distance);
        if (EditorGUI.EndChangeCheck())
        {
            distance = Mathf.Max(0f, distance);
            offset.vector2Value = useHorizontalDistance
                ? new Vector2(distance, 0f)
                : new Vector2(0f, distance);
        }
    }

    private void DrawRuntimeButtons()
    {
        Layout.Section("테스트", () =>
        {
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                UiAnimation animation = (UiAnimation)target;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("등장 실행"))
                {
                    animation.PlayShow();
                }

                if (GUILayout.Button("퇴장 실행"))
                {
                    animation.PlayHide();
                }

                if (GUILayout.Button("기본 상태 복구"))
                {
                    animation.RestoreInitialState();
                }
                EditorGUILayout.EndHorizontal();
            }
        });
    }

    private static void DrawEasePresetSelector(SerializedProperty ease)
    {
        Ease selectedEase = GetSelectedEase(ease);

        Layout.Subsection("Ease", () =>
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("현재 선택", GUILayout.Width(72f));
            GUILayout.Space(8f);
            EditorGUILayout.SelectableLabel(ObjectNames.NicifyVariableName(selectedEase.ToString()), EditorStyles.helpBox, GUILayout.Height(20f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3f);
            EditorGUILayout.HelpBox(GetEaseDescription(selectedEase), MessageType.None);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("자주 쓰는 프리셋", EditorStyles.miniBoldLabel);
            DrawEasePresetRow(ease, Ease.Linear, Ease.OutQuad, Ease.OutCubic);
            DrawEasePresetRow(ease, Ease.InOutCubic, Ease.OutBack, Ease.OutBounce);
            DrawEasePresetRow(ease, Ease.OutElastic);

            EditorGUILayout.Space(4f);
            Rect foldoutRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            foldoutRect.x += 8f;
            foldoutRect.width -= 8f;
            _showAdvancedEase = EditorGUI.Foldout(foldoutRect, _showAdvancedEase, "고급: DOTween Ease 전체 목록", true);
            if (_showAdvancedEase)
            {
                EditorGUILayout.LabelField("전체 Ease", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(ease, GUIContent.none);
            }
        });
    }

    private static void DrawEasePresetRow(SerializedProperty ease, params Ease[] presets)
    {
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < presets.Length; i++)
        {
            Ease preset = presets[i];
            bool isSelected = GetSelectedEase(ease) == preset;
            string label = isSelected
                ? $"{ObjectNames.NicifyVariableName(preset.ToString())} ✓"
                : ObjectNames.NicifyVariableName(preset.ToString());

            using (new EditorGUI.DisabledScope(isSelected))
            {
                if (GUILayout.Button(label, GUILayout.Height(EaseButtonHeight)))
                {
                    SetEase(ease, preset);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private static string GetEaseDescription(Ease ease)
    {
        return ease switch
        {
            Ease.Linear => "일정한 속도로 움직입니다.",
            Ease.InSine => "천천히 시작해 점점 빨라집니다.",
            Ease.OutSine => "빠르게 시작해 끝에서 부드럽게 느려집니다.",
            Ease.InOutSine => "시작과 끝은 부드럽고 중간은 빠르게 움직입니다.",
            Ease.InQuad => "초반 가속이 비교적 부드럽게 들어갑니다.",
            Ease.OutQuad => "끝으로 갈수록 부드럽게 감속합니다.",
            Ease.InOutQuad => "부드럽게 가속했다가 부드럽게 감속합니다.",
            Ease.InCubic => "초반에는 느리고 뒤로 갈수록 강하게 빨라집니다.",
            Ease.OutCubic => "빠르게 시작한 뒤 강하게 부드러워집니다.",
            Ease.InOutCubic => "중앙을 기준으로 가속과 감속이 뚜렷합니다.",
            Ease.InQuart => "초반 정지가 길고 후반 가속이 강합니다.",
            Ease.OutQuart => "초반 이동이 빠르고 끝에서 길게 감속합니다.",
            Ease.InOutQuart => "가속과 감속이 강한 부드러운 전환입니다.",
            Ease.InQuint => "아주 천천히 시작한 뒤 강하게 가속합니다.",
            Ease.OutQuint => "아주 빠르게 시작한 뒤 길게 감속합니다.",
            Ease.InOutQuint => "가속과 감속이 매우 강한 전환입니다.",
            Ease.InExpo => "거의 멈춘 상태에서 시작해 급격히 빨라집니다.",
            Ease.OutExpo => "급격히 움직인 뒤 끝에서 천천히 멈춥니다.",
            Ease.InOutExpo => "시작과 끝은 매우 느리고 중간에서 급격히 움직입니다.",
            Ease.InCirc => "원형 곡선처럼 시작이 무겁게 들어갑니다.",
            Ease.OutCirc => "원형 곡선처럼 끝에서 자연스럽게 멈춥니다.",
            Ease.InOutCirc => "원형 곡선 느낌의 부드러운 왕복 가감속입니다.",
            Ease.InBack => "살짝 반대로 당겼다가 시작합니다.",
            Ease.OutBack => "목표를 살짝 지나쳤다가 돌아옵니다.",
            Ease.InOutBack => "시작과 끝에서 살짝 튕기는 느낌이 납니다.",
            Ease.InElastic => "시작 전에 탄성 있게 흔들리며 들어갑니다.",
            Ease.OutElastic => "도착 후 탄성 있게 흔들리며 멈춥니다.",
            Ease.InOutElastic => "시작과 끝 모두 탄성 흔들림이 있습니다.",
            Ease.InBounce => "끝의 바운스를 거꾸로 재생한 느낌입니다.",
            Ease.OutBounce => "도착 지점에서 튕기며 멈춥니다.",
            Ease.InOutBounce => "시작과 끝 모두 튕기는 느낌이 있습니다.",
            Ease.Flash => "짧은 깜빡임처럼 급격히 반복됩니다.",
            Ease.InFlash => "초반에 깜빡임이 몰립니다.",
            Ease.OutFlash => "후반에 깜빡임이 몰립니다.",
            Ease.InOutFlash => "시작과 끝에 깜빡임이 분산됩니다.",
            _ => "DOTween의 선택된 Ease 곡선을 사용합니다.",
        };
    }

    private static Ease GetSelectedEase(SerializedProperty ease)
    {
        return (Ease)ease.intValue;
    }

    private static void SetEase(SerializedProperty ease, Ease value)
    {
        ease.intValue = (int)value;
    }

    private static class Layout
    {
        private const string SectionFoldoutPrefix = "WildlifeSportsDay.UiAnimationEditor.Section.";

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

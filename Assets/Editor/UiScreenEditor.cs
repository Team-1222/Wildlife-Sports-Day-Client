using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UiScreen))]
public sealed class UiScreenEditor : Editor
{
    private const string TargetFoldoutPrefix = "WildlifeSportsDay.UiScreenEditor.TargetFoldout.";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        float previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = Mathf.Max(previousLabelWidth, 190f);

        try
        {
            UiScreen screen = (UiScreen)target;

            Layout.Note(
                "Canvas 또는 Panel 단위 화면을 열고 닫는 컴포넌트입니다.\n" +
                "버튼은 이 컴포넌트를 직접 참조해 화면을 열고 닫습니다.");

            DrawRuntimeState(screen);
            DrawScreenSettings();
            DrawAnimationSettings();
            DrawChildAnimationCommonSettings(screen);
            DrawAnimationList();
            DrawEvents();
            DrawTools(screen);
        }
        finally
        {
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawRuntimeState(UiScreen screen)
    {
        Layout.Section("현재 상태", () =>
        {
            EditorGUILayout.LabelField("화면 상태", screen.State.ToString());
            EditorGUILayout.LabelField("전환 중", screen.IsTransitioning ? "예" : "아니오");
        });
    }

    private void DrawScreenSettings()
    {
        Layout.Section("화면 설정", () =>
        {
            SerializedProperty startOpened = serializedObject.FindProperty("_startOpened");
            EditorGUILayout.PropertyField(startOpened, new GUIContent("시작 시 열린 상태", "첫 화면처럼 시작부터 보이고 입력 가능한 화면이면 켭니다."));

            if (startOpened.boolValue)
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("_playOpenAnimationOnStart"),
                    new GUIContent("시작 시 등장 애니메이션 실행", "Play 시작 시 Show 시작 상태를 적용한 뒤 Open 애니메이션을 실행합니다."));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_deactivateOnClosed"), new GUIContent("닫힘 시 오브젝트 비활성화", "닫힌 화면을 GameObject 비활성화 상태로 만듭니다."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_blockInputDuringTransition"), new GUIContent("전환 중 입력 차단", "Opening/Closing 중 클릭을 막습니다."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_collectChildAnimationsOnAwake"), new GUIContent("Awake 때 하위 애니메이션 자동 수집", "이 화면이 직접 소유한 하위 UiAnimation을 실행 목록에 등록합니다."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_collectChildScreensOnAwake"), new GUIContent("Awake 때 하위 화면 자동 수집", "자식 UiScreen을 하나의 실행 단위로 등록합니다. 자식 화면 내부 애니메이션은 해당 자식 화면이 관리합니다."));
            Layout.Warning(
                "시작 화면은 '시작 시 열린 상태'를 켜야 처음부터 보이고 입력 가능합니다.\n" +
                "Closed 상태는 alpha 0, 입력 차단, 옵션에 따라 오브젝트 비활성화로 처리됩니다.");
        });
    }

    private void DrawAnimationSettings()
    {
        Layout.Section("하위 애니메이션 실행", () =>
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_reverseCloseOrder"), new GUIContent("퇴장 시 순서 반전"));
            SerializedProperty waitForPreviousTarget = serializedObject.FindProperty("_waitForPreviousTarget");
            EditorGUILayout.PropertyField(waitForPreviousTarget, new GUIContent("이전 항목 완료 후 다음 실행", "켜면 등록된 항목이 끝난 뒤 다음 항목을 실행합니다. 꺼두면 기존처럼 간격만 두고 겹쳐 실행합니다."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_animationInterval"), new GUIContent("실행 간격", "기존 방식에서는 시작 간격입니다. 완료 후 실행 방식에서는 다음 항목을 시작하기 전 대기 시간입니다."));

            if (waitForPreviousTarget.boolValue)
            {
                Layout.Note("켜짐: 이전 Animation/Screen 실행 예상 시간이 끝난 뒤, 실행 간격만큼 기다리고 다음 항목을 실행합니다.");
            }
            else
            {
                Layout.Note("꺼짐: 모든 항목을 등록 순서대로 실행 간격만큼 늦게 시작합니다. 각 항목은 서로 겹쳐 실행될 수 있습니다.");
            }
        });
    }

    private void DrawChildAnimationCommonSettings(UiScreen screen)
    {
        Layout.Section("하위 애니메이션 공통 설정", () =>
        {
            SerializedProperty useSyncSource = serializedObject.FindProperty("_syncChildAnimationsFromSource");
            EditorGUILayout.PropertyField(useSyncSource, new GUIContent("기준 애니메이션으로 통일", "켜면 기준 UiAnimation 하나를 골라 하위 UiAnimation 설정을 일괄 복사할 수 있습니다."));

            if (!useSyncSource.boolValue)
            {
                Layout.Note("꺼짐: 하위 UiAnimation 값을 자동으로 바꾸지 않습니다.");
                return;
            }

            SerializedProperty syncSource = serializedObject.FindProperty("_animationSyncSource");
            EditorGUILayout.PropertyField(syncSource, new GUIContent("기준 UiAnimation", "이 애니메이션의 실행값과 Fade/Scale/Move 설정을 하위 UiAnimation에 복사합니다."));

            UiAnimation source = syncSource.objectReferenceValue as UiAnimation;
            int targetCount = source != null ? screen.GetChildAnimationsExcept(source).Count : 0;
            Layout.Note(
                "복사 대상: 지속 시간, 시작 지연, Ease, 시간 정지 무시, 등장/퇴장 설정 방식, Show/Hide의 Fade/Scale/Move 전체 설정\n" +
                "프리셋 Move는 방향과 거리만 복사하고, 각 대상의 기본 위치 기준으로 적용합니다. Custom Move만 From/To 위치를 복사합니다.\n" +
                "제외 대상: 기준 UiAnimation 자기 자신, 완료 이벤트");
            Layout.Note($"적용 대상: 하위 UiAnimation {targetCount}개");

            using (new EditorGUI.DisabledScope(source == null || targetCount == 0))
            {
                if (GUILayout.Button("기준 값으로 하위 UiAnimation 통일"))
                {
                    serializedObject.ApplyModifiedProperties();

                    List<UiAnimation> animations = screen.GetChildAnimationsExcept(source);
                    Object[] undoTargets = new Object[animations.Count];
                    for (int i = 0; i < animations.Count; i++)
                    {
                        undoTargets[i] = animations[i];
                    }

                    Undo.RecordObjects(undoTargets, "Copy UI animation settings from source");
                    int appliedCount = screen.ApplyAnimationSyncSourceToChildAnimations();
                    for (int i = 0; i < animations.Count; i++)
                    {
                        EditorUtility.SetDirty(animations[i]);
                    }

                    EditorUtility.DisplayDialog("애니메이션 설정 통일", $"하위 UiAnimation {appliedCount}개를 기준 값으로 통일했습니다.", "확인");
                }
            }
        });
    }

    private void DrawAnimationList()
    {
        Layout.Section("실행 목록", () =>
        {
            SerializedProperty targets = serializedObject.FindProperty("_animationTargets");

            DrawAnimationTargets(targets);

            Layout.Warning(
                "Screen 항목은 해당 UiScreen의 Open/Close만 호출합니다. 내부 애니메이션은 자식 UiScreen에서 설정하세요.\n" +
                "부모 UiScreen이 자식 UiScreen을 실행 목록에 넣으면, 자식 화면 내부 UiAnimation은 부모가 직접 수집하지 않습니다.\n" +
                "팝업은 작은 패널만 만들지 말고 전체 화면 Raycast blocker를 함께 두어야 뒤 UI 클릭을 막을 수 있습니다.");
        });
    }

    private static void DrawAnimationTargets(SerializedProperty targets)
    {
        EditorGUILayout.LabelField(new GUIContent("Animation / Screen 실행 목록", "UiAnimation 또는 자식 UiScreen을 순서대로 실행합니다."), EditorStyles.boldLabel);

        for (int i = 0; i < targets.arraySize; i++)
        {
            SerializedProperty target = targets.GetArrayElementAtIndex(i);
            SerializedProperty type = target.FindPropertyRelative("_type");
            SerializedProperty animation = target.FindPropertyRelative("_animation");
            SerializedProperty screen = target.FindPropertyRelative("_screen");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4f);
            Rect foldoutRect = new Rect(headerRect.x + 12f, headerRect.y + 2f, headerRect.width - 70f, EditorGUIUtility.singleLineHeight);
            Rect deleteRect = new Rect(headerRect.xMax - 52f, headerRect.y + 1f, 48f, EditorGUIUtility.singleLineHeight + 2f);

            string key = $"{TargetFoldoutPrefix}{targets.serializedObject.targetObject.GetInstanceID()}.{i}";
            bool isExpanded = EditorPrefs.GetBool(key, false);
            string typeName = type.enumValueIndex >= 0 && type.enumValueIndex < type.enumDisplayNames.Length
                ? type.enumDisplayNames[type.enumValueIndex]
                : "Unknown";
            isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, $"Element {i} - {typeName}", true, EditorStyles.foldout);
            EditorPrefs.SetBool(key, isExpanded);

            if (GUI.Button(deleteRect, "삭제"))
            {
                targets.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndVertical();
                break;
            }

            if (!isExpanded)
            {
                EditorGUILayout.EndVertical();
                continue;
            }

            EditorGUILayout.PropertyField(type, new GUIContent("실행 대상"));

            if (type.enumValueIndex == (int)UiScreen.AnimationTargetType.Screen)
            {
                EditorGUILayout.PropertyField(screen, new GUIContent("UiScreen", "순차 실행 시 이 화면의 Open/Close를 호출합니다."));
                Layout.Note("Screen 항목은 이 화면 안쪽의 UiAnimation 값을 여기서 직접 설정하지 않습니다.");
            }
            else
            {
                EditorGUILayout.PropertyField(animation, new GUIContent("UiAnimation", "순차 실행 시 이 애니메이션의 Show/Hide를 호출합니다."));
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Animation 추가"))
        {
            AddAnimationTarget(targets, UiScreen.AnimationTargetType.Animation);
        }

        if (GUILayout.Button("Screen 추가"))
        {
            AddAnimationTarget(targets, UiScreen.AnimationTargetType.Screen);
        }
        EditorGUILayout.EndHorizontal();
    }

    private static void AddAnimationTarget(SerializedProperty targets, UiScreen.AnimationTargetType type)
    {
        int index = targets.arraySize;
        targets.InsertArrayElementAtIndex(index);

        SerializedProperty target = targets.GetArrayElementAtIndex(index);
        target.FindPropertyRelative("_type").enumValueIndex = (int)type;
        target.FindPropertyRelative("_animation").objectReferenceValue = null;
        target.FindPropertyRelative("_screen").objectReferenceValue = null;
    }

    private void DrawEvents()
    {
        Layout.Section("이벤트", () =>
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_opened"), new GUIContent("열림 완료 이벤트"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_closed"), new GUIContent("닫힘 완료 이벤트"));
        });
    }

    private static void DrawTools(UiScreen screen)
    {
        Layout.Section("도구", () =>
        {
            if (GUILayout.Button("하위 UiAnimation 다시 수집"))
            {
                Undo.RecordObject(screen, "Collect child UI animation targets");
                screen.CollectChildAnimations();
                EditorUtility.SetDirty(screen);
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open 테스트"))
                {
                    screen.Open();
                }

                if (GUILayout.Button("Close 테스트"))
                {
                    screen.Close();
                }
                EditorGUILayout.EndHorizontal();
            }
        });
    }

    private static class Layout
    {
        private const string SectionFoldoutPrefix = "WildlifeSportsDay.UiScreenEditor.Section.";

        private static readonly GUIStyle SectionStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 10),
            margin = new RectOffset(0, 0, 6, 8),
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

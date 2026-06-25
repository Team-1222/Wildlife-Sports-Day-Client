using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UiScreen))]
public sealed class UiScreenEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        UiScreen screen = (UiScreen)target;

        Layout.Note(
            "Canvas 또는 Panel 단위 화면을 열고 닫는 컴포넌트입니다.\n" +
            "버튼은 이 컴포넌트를 직접 참조해 화면을 열고 닫습니다.");

        DrawRuntimeState(screen);
        DrawScreenSettings();
        DrawAnimationSettings();
        DrawAnimationList();
        DrawEvents();
        DrawTools(screen);

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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_collectChildAnimationsOnAwake"), new GUIContent("Awake 때 하위 애니메이션 자동 수집"));
            Layout.Warning(
                "시작 화면은 '시작 시 열린 상태'를 켜야 처음부터 보이고 입력 가능합니다.\n" +
                "Closed 상태는 alpha 0, 입력 차단, 옵션에 따라 오브젝트 비활성화로 처리됩니다.");
        });
    }

    private void DrawAnimationSettings()
    {
        Layout.Section("하위 애니메이션 실행", () =>
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_openPlayMode"), new GUIContent("등장 실행 방식"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_closePlayMode"), new GUIContent("퇴장 실행 방식"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_reverseCloseOrder"), new GUIContent("퇴장 시 순서 반전"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_animationInterval"), new GUIContent("순차 실행 간격"));
            Layout.Note("순차 등장은 Open 전에 각 UiAnimation의 Show 시작 상태를 먼저 적용한 뒤 실행됩니다.");
        });
    }

    private void DrawAnimationList()
    {
        Layout.Section("등록된 애니메이션", () =>
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_animations"), new GUIContent("UiAnimation 목록"), true);
            Layout.Warning("팝업은 작은 패널만 만들지 말고 전체 화면 Raycast blocker를 함께 두어야 뒤 UI 클릭을 막을 수 있습니다.");
        });
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
                Undo.RecordObject(screen, "Collect child UiAnimations");
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
        private static readonly GUIStyle SectionStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 10),
            margin = new RectOffset(0, 0, 6, 8),
        };

        public static void Section(string title, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(SectionStyle);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);
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

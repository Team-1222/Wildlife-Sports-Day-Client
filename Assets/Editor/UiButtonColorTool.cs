using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public sealed class UiButtonColorTool : EditorWindow
{
    private static readonly Color LoginRed = new(0.83137256f, 0.2627451f, 0.18431373f);
    private static readonly Color SignUpBlue = new(0.13725491f, 0.40392157f, 0.80784315f);
    private static readonly Color BackYellow = new(0.9372549f, 0.68235296f, 0.07058824f);
    private static readonly Color GuestGray = new(0.78f, 0.78f, 0.78f);
    private static readonly Color GuestDarkGray = new(0.62f, 0.62f, 0.62f);

    private readonly List<Button> _sceneButtons = new();
    private Vector2 _scrollPosition;
    private Button _targetButton;
    private Color _baseColor = GuestGray;
    private float _brightnessOffset;

    [MenuItem("Tools/Button Color Auto Setup")]
    public static void Open()
    {
        GetWindow<UiButtonColorTool>("Button Color Auto Setup");
    }

    private void OnEnable()
    {
        RefreshButtons();
        SetTargetFromSelection();
    }

    private void OnSelectionChange()
    {
        SetTargetFromSelection();
        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawTarget();
        DrawBaseColorPresets();
        DrawColorControls();
        DrawSceneButtons();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("씬 버튼 새로고침", GUILayout.Height(28f)))
        {
            RefreshButtons();
        }

        if (GUILayout.Button("선택 오브젝트 사용", GUILayout.Height(28f)))
        {
            SetTargetFromSelection();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTarget()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("대상 버튼", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUI.BeginChangeCheck();
            _targetButton = (Button)EditorGUILayout.ObjectField("Button", _targetButton, typeof(Button), true);
            if (EditorGUI.EndChangeCheck())
            {
                ReadColorFromTarget();
            }

            if (_targetButton == null)
            {
                EditorGUILayout.HelpBox("버튼 오브젝트를 선택하거나 아래 목록에서 버튼을 고르세요.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("현재 오브젝트", _targetButton.name);
        }
    }

    private void DrawBaseColorPresets()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("기본색 프리셋", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.HelpBox("프리셋은 기본색만 바꿉니다. 상태 컬러는 아래 자동 세팅 버튼을 눌렀을 때만 적용됩니다.", MessageType.None);
            DrawPresetRow("로그인 빨강", LoginRed, "회원가입 파랑", SignUpBlue);
            DrawPresetRow("뒤로 노랑", BackYellow, "게스트 회색", GuestGray);

            EditorGUILayout.BeginHorizontal();
            DrawPresetButton("게스트 진회색", GuestDarkGray);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawPresetRow(string firstLabel, Color firstColor, string secondLabel, Color secondColor)
    {
        EditorGUILayout.BeginHorizontal();
        DrawPresetButton(firstLabel, firstColor);
        DrawPresetButton(secondLabel, secondColor);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPresetButton(string label, Color color)
    {
        Color previousBackground = GUI.backgroundColor;
        GUI.backgroundColor = color;

        if (GUILayout.Button(label, GUILayout.Height(32f)))
        {
            _baseColor = color;
            _brightnessOffset = 0f;
        }

        GUI.backgroundColor = previousBackground;
    }

    private void DrawColorControls()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("직접 조정", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _baseColor = EditorGUILayout.ColorField("기본색", _baseColor);
            _brightnessOffset = EditorGUILayout.Slider("밝기 조절", _brightnessOffset, -0.35f, 0.35f);

            DrawStatePreview(GetAdjustedColor(_baseColor));

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("기본색 + 자동 상태 적용", GUILayout.Height(32f)))
            {
                ApplyAutoSetupToTarget(readCurrentColor: false);
            }

            if (GUILayout.Button("현재 버튼 색 기준 자동 세팅", GUILayout.Height(32f)))
            {
                ReadColorFromTarget();
                ApplyAutoSetupToTarget(readCurrentColor: false);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("현재 버튼 색 읽기만", GUILayout.Height(26f)))
            {
                ReadColorFromTarget();
            }
        }
    }

    private void DrawStatePreview(Color normalColor)
    {
        Color highlightedColor = GetHighlightedColor(normalColor);
        Color pressedColor = GetPressedColor(normalColor);
        Color selectedColor = highlightedColor;

        EditorGUILayout.LabelField("자동 계산 미리보기", EditorStyles.miniBoldLabel);
        DrawColorPreview("Normal", normalColor);
        DrawColorPreview("Highlighted", highlightedColor);
        DrawColorPreview("Pressed", pressedColor);
        DrawColorPreview("Selected", selectedColor);
    }

    private static void DrawColorPreview(string label, Color color)
    {
        Rect rect = GUILayoutUtility.GetRect(1f, 22f, GUILayout.ExpandWidth(true));
        Rect labelRect = new(rect.x, rect.y, 92f, rect.height);
        Rect colorRect = new(rect.x + 96f, rect.y + 2f, rect.width - 96f, rect.height - 4f);
        EditorGUI.LabelField(labelRect, label);
        EditorGUI.DrawRect(colorRect, color);
    }

    private void DrawSceneButtons()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("씬 버튼 목록", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (_sceneButtons.Count == 0)
            {
                EditorGUILayout.HelpBox("현재 열린 씬에서 Button을 찾지 못했습니다.", MessageType.Warning);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MinHeight(160f));

            for (int i = 0; i < _sceneButtons.Count; i++)
            {
                Button button = _sceneButtons[i];
                if (button == null)
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(button, typeof(Button), true);

                if (GUILayout.Button("선택", GUILayout.Width(58f)))
                {
                    _targetButton = button;
                    Selection.activeGameObject = button.gameObject;
                    ReadColorFromTarget();
                }

                if (GUILayout.Button("적용", GUILayout.Width(58f)))
                {
                    _targetButton = button;
                    ApplyAutoSetupToTarget(readCurrentColor: false);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void RefreshButtons()
    {
        _sceneButtons.Clear();

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.scene.IsValid())
            {
                _sceneButtons.Add(buttons[i]);
            }
        }
    }

    private void SetTargetFromSelection()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            return;
        }

        Button button = selected.GetComponent<Button>();
        if (button == null)
        {
            button = selected.GetComponentInParent<Button>();
        }

        if (button == null)
        {
            return;
        }

        _targetButton = button;
        ReadColorFromTarget();
    }

    private void ReadColorFromTarget()
    {
        if (_targetButton == null)
        {
            return;
        }

        Image image = _targetButton.GetComponent<Image>();
        _baseColor = image != null ? image.color : _targetButton.colors.normalColor;
        _brightnessOffset = 0f;
    }

    private void ApplyAutoSetupToTarget(bool readCurrentColor)
    {
        if (_targetButton == null)
        {
            EditorUtility.DisplayDialog("Button Color Auto Setup", "적용할 버튼이 없습니다.", "확인");
            return;
        }

        if (readCurrentColor)
        {
            ReadColorFromTarget();
        }

        Color color = GetAdjustedColor(_baseColor);

        Undo.RecordObject(_targetButton, "Auto Setup Button Color");

        ColorBlock colors = _targetButton.colors;
        colors.normalColor = color;
        colors.highlightedColor = GetHighlightedColor(color);
        colors.pressedColor = GetPressedColor(color);
        colors.selectedColor = colors.highlightedColor;
        _targetButton.colors = colors;
        EditorUtility.SetDirty(_targetButton);

        Image image = _targetButton.GetComponent<Image>();
        if (image != null)
        {
            Undo.RecordObject(image, "Auto Setup Button Image Color");
            image.color = color;
            EditorUtility.SetDirty(image);
        }

        EditorSceneManager.MarkSceneDirty(_targetButton.gameObject.scene);
    }

    private Color GetAdjustedColor(Color color)
    {
        float offset = _brightnessOffset;
        return new Color(
            Mathf.Clamp01(color.r + offset),
            Mathf.Clamp01(color.g + offset),
            Mathf.Clamp01(color.b + offset),
            color.a);
    }

    private static Color GetHighlightedColor(Color color)
    {
        return Color.Lerp(color, Color.white, 0.22f);
    }

    private static Color GetPressedColor(Color color)
    {
        return Color.Lerp(color, Color.black, 0.2f);
    }
}

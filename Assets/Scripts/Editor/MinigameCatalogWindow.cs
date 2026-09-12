using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 미니게임 정의의 씬 연결과 빌드 등록 상태를 한 화면에서 확인합니다.
/// </summary>
public sealed class MinigameCatalogWindow : EditorWindow
{
    private const float RowHeight = 34f;
    private const float RowSpacing = 1f;
    private const string SearchFieldControlName = "MinigameCatalogSearchField";

    private string _searchText = string.Empty;
    private Vector2 _scrollPosition;

    private static GUIStyle _rowTitleStyle;
    private static GUIStyle _rowIdentifierStyle;

    [MenuItem("Tools/Minigame Catalog")]
    private static void Open()
    {
        MinigameCatalogWindow window = GetWindow<MinigameCatalogWindow>();
        window.titleContent = new GUIContent("Minigame Catalog");
        window.minSize = new Vector2(620f, 420f);
    }

    private void OnGUI()
    {
        EnsureStyles();

        MinigameCatalog catalog = FindCatalog();
        if (catalog == null)
        {
            EditorGUILayout.HelpBox("MinigameCatalog 에셋을 찾을 수 없습니다.", MessageType.Error);
            return;
        }

        DrawFilters();
        DrawDefinitions(catalog);
    }

    private void DrawFilters()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("검색", GUILayout.Width(32f));
        GUI.SetNextControlName(SearchFieldControlName);
        _searchText = EditorGUILayout.TextField(_searchText, GUILayout.MinWidth(220f));
        Input.imeCompositionMode = GUI.GetNameOfFocusedControl() == SearchFieldControlName
            ? IMECompositionMode.On
            : IMECompositionMode.Auto;
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("새로고침", GUILayout.Width(60f)))
        {
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void OnDisable()
    {
        Input.imeCompositionMode = IMECompositionMode.Auto;
    }

    private void DrawDefinitions(MinigameCatalog catalog)
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        int visibleIndex = 0;
        for (int i = 0; i < catalog.Definitions.Count; i++)
        {
            MinigameDefinition definition = catalog.Definitions[i];
            if (definition == null || !MatchesFilters(definition))
            {
                continue;
            }

            DrawDefinition(definition, visibleIndex);
            visibleIndex++;
        }

        EditorGUILayout.EndScrollView();
    }

    private bool MatchesFilters(MinigameDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(_searchText) &&
            definition.DisplayName.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0 &&
            definition.name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        return true;
    }

    private static void DrawDefinition(MinigameDefinition definition, int visibleIndex)
    {
        string scenePath = definition.SceneAsset == null ? string.Empty : AssetDatabase.GetAssetPath(definition.SceneAsset);
        Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(RowHeight), GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rowRect, GetRowBackgroundColor(visibleIndex));

        const float HorizontalInset = 10f;
        const float ActionButtonWidth = 42f;
        const float ButtonSpacing = 4f;
        float identifierWidth = Mathf.Clamp(rowRect.width * 0.28f, 130f, 240f);
        float titleWidth = rowRect.width - (HorizontalInset * 2f) - identifierWidth - (ActionButtonWidth * 3f) - (ButtonSpacing * 3f);

        Rect titleRect = new(rowRect.x + HorizontalInset, rowRect.y, titleWidth, rowRect.height);
        Rect identifierRect = new(titleRect.xMax, rowRect.y, identifierWidth, rowRect.height);
        Rect selectButtonRect = new(identifierRect.xMax + ButtonSpacing, rowRect.y + 5f, ActionButtonWidth, rowRect.height - 10f);
        Rect openButtonRect = new(selectButtonRect.xMax + ButtonSpacing, rowRect.y + 5f, ActionButtonWidth, rowRect.height - 10f);
        Rect playButtonRect = new(openButtonRect.xMax + ButtonSpacing, rowRect.y + 5f, ActionButtonWidth, rowRect.height - 10f);

        string tooltip = $"정의: {AssetDatabase.GetAssetPath(definition)}\n씬: {(string.IsNullOrEmpty(scenePath) ? "미지정" : scenePath)}";
        GUI.Label(titleRect, new GUIContent(definition.DisplayName, tooltip), _rowTitleStyle);
        GUI.Label(identifierRect, new GUIContent(definition.name, tooltip), _rowIdentifierStyle);

        if (GUI.Button(selectButtonRect, "SO"))
        {
            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
        }

        using (new EditorGUI.DisabledScope(definition.SceneAsset == null))
        {
            if (GUI.Button(openButtonRect, "씬"))
            {
                AssetDatabase.OpenAsset(definition.SceneAsset);
            }

            if (GUI.Button(playButtonRect, "실행"))
            {
                StartPlaytest(definition);
            }
        }

        GUILayout.Space(RowSpacing);
    }

    private static void EnsureStyles()
    {
        if (_rowTitleStyle != null)
        {
            return;
        }

        Color primaryTextColor = EditorGUIUtility.isProSkin ? new Color(0.92f, 0.94f, 0.96f) : new Color(0.15f, 0.17f, 0.2f);
        Color secondaryTextColor = EditorGUIUtility.isProSkin ? new Color(0.62f, 0.68f, 0.74f) : new Color(0.34f, 0.4f, 0.46f);

        _rowTitleStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 12,
            clipping = TextClipping.Ellipsis,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = primaryTextColor },
        };
        _rowIdentifierStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            clipping = TextClipping.Ellipsis,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = secondaryTextColor },
        };
    }

    private static Color GetRowBackgroundColor(int visibleIndex)
    {
        if (EditorGUIUtility.isProSkin)
        {
            return visibleIndex % 2 == 0 ? new Color(0.16f, 0.16f, 0.17f) : new Color(0.18f, 0.18f, 0.19f);
        }

        return visibleIndex % 2 == 0 ? new Color(0.94f, 0.94f, 0.95f) : new Color(0.89f, 0.9f, 0.91f);
    }

    /// <summary>
    /// 현재 씬의 저장 여부를 확인한 뒤, 선택한 미니게임만 세션과 함께 바로 재생합니다.
    /// </summary>
    private static void StartPlaytest(MinigameDefinition definition)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || definition.SceneAsset == null)
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string scenePath = AssetDatabase.GetAssetPath(definition.SceneAsset);
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            Debug.LogError($"Minigame playtest scene asset path was not found: {definition.DisplayName}");
            return;
        }

        MinigamePlaytestBootstrap.Request(definition);
        EditorSceneManager.OpenScene(scenePath);
        EditorApplication.isPlaying = true;
    }

    private static MinigameCatalog FindCatalog()
    {
        string[] catalogGuids = AssetDatabase.FindAssets("t:MinigameCatalog");
        if (catalogGuids.Length != 1)
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<MinigameCatalog>(AssetDatabase.GUIDToAssetPath(catalogGuids[0]));
    }
}

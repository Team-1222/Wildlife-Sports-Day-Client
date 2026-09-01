using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 미니게임 정의의 상태와 씬 등록 상태를 한 화면에서 관리합니다.
/// </summary>
public sealed class MinigameCatalogWindow : EditorWindow
{
    private string _searchText = string.Empty;
    private bool _showAllStatuses = true;
    private bool _showOnlyIssues;
    private MinigameDevelopmentStatus _statusFilter;
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Minigame Catalog")]
    private static void Open()
    {
        MinigameCatalogWindow window = GetWindow<MinigameCatalogWindow>();
        window.titleContent = new GUIContent("Minigame Catalog");
        window.minSize = new Vector2(620f, 420f);
    }

    private void OnGUI()
    {
        MinigameCatalog catalog = FindCatalog();
        if (catalog == null)
        {
            EditorGUILayout.HelpBox("MinigameCatalog 에셋을 찾을 수 없습니다.", MessageType.Error);
            return;
        }

        DrawFilters();
        DrawSummary(catalog);
        DrawDefinitions(catalog);
    }

    private void DrawFilters()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _searchText = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.MinWidth(180f));
        _showAllStatuses = GUILayout.Toggle(_showAllStatuses, "전체 상태", EditorStyles.toolbarButton, GUILayout.Width(80f));
        using (new EditorGUI.DisabledScope(_showAllStatuses))
        {
            _statusFilter = (MinigameDevelopmentStatus)EditorGUILayout.EnumPopup(_statusFilter, EditorStyles.toolbarPopup, GUILayout.Width(110f));
        }

        _showOnlyIssues = GUILayout.Toggle(_showOnlyIssues, "문제만", EditorStyles.toolbarButton, GUILayout.Width(60f));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(60f)))
        {
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
    }

    private static void DrawSummary(MinigameCatalog catalog)
    {
        int planningCount = 0;
        int scaffoldedCount = 0;
        int playableCount = 0;
        int completeCount = 0;

        for (int i = 0; i < catalog.Definitions.Count; i++)
        {
            MinigameDefinition definition = catalog.Definitions[i];
            if (definition == null)
            {
                continue;
            }

            switch (definition.DevelopmentStatus)
            {
                case MinigameDevelopmentStatus.Planning:
                    planningCount++;
                    break;
                case MinigameDevelopmentStatus.Scaffolded:
                    scaffoldedCount++;
                    break;
                case MinigameDevelopmentStatus.Playable:
                    playableCount++;
                    break;
                case MinigameDevelopmentStatus.Complete:
                    completeCount++;
                    break;
            }
        }

        EditorGUILayout.HelpBox($"전체 {catalog.Definitions.Count}개  |  기획 {planningCount}  |  틀 {scaffoldedCount}  |  플레이 가능 {playableCount}  |  완료 {completeCount}", MessageType.Info);
    }

    private void DrawDefinitions(MinigameCatalog catalog)
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        for (int i = 0; i < catalog.Definitions.Count; i++)
        {
            MinigameDefinition definition = catalog.Definitions[i];
            if (definition == null || !MatchesFilters(definition))
            {
                continue;
            }

            DrawDefinition(definition);
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

        if (!_showAllStatuses && definition.DevelopmentStatus != _statusFilter)
        {
            return false;
        }

        return !_showOnlyIssues || !HasIssue(definition);
    }

    private static void DrawDefinition(MinigameDefinition definition)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(definition.DisplayName, EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        MinigameDevelopmentStatus status = (MinigameDevelopmentStatus)EditorGUILayout.EnumPopup(definition.DevelopmentStatus, GUILayout.Width(120f));
        if (status != definition.DevelopmentStatus)
        {
            SetDevelopmentStatus(definition, status);
        }

        EditorGUILayout.EndHorizontal();

        string scenePath = definition.SceneAsset == null ? string.Empty : AssetDatabase.GetAssetPath(definition.SceneAsset);
        bool isInBuildSettings = IsInBuildSettings(scenePath);
        EditorGUILayout.LabelField("SO", AssetDatabase.GetAssetPath(definition));
        EditorGUILayout.LabelField("씬", string.IsNullOrEmpty(scenePath) ? "미지정" : scenePath);
        EditorGUILayout.LabelField("빌드 설정", isInBuildSettings ? "등록됨" : "미등록");

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("SO 선택"))
        {
            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
        }

        using (new EditorGUI.DisabledScope(definition.SceneAsset == null))
        {
            if (GUILayout.Button("씬 열기"))
            {
                AssetDatabase.OpenAsset(definition.SceneAsset);
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private static bool HasIssue(MinigameDefinition definition)
    {
        return definition.SceneAsset == null || !IsInBuildSettings(AssetDatabase.GetAssetPath(definition.SceneAsset));
    }

    private static bool IsInBuildSettings(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            return false;
        }

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (string.Equals(scenes[i].path, scenePath, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void SetDevelopmentStatus(MinigameDefinition definition, MinigameDevelopmentStatus status)
    {
        SerializedObject serializedDefinition = new(definition);
        serializedDefinition.FindProperty("_developmentStatus").enumValueIndex = (int)status;
        serializedDefinition.ApplyModifiedProperties();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
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

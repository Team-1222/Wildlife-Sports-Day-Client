#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디터에서 특정 미니게임 씬을 직접 재생할 때 필요한 게임 세션을 초기화합니다.
/// </summary>
public static class MinigamePlaytestBootstrap
{
    private const string DefinitionPathKey = "WildlifeSportsDay.MinigamePlaytest.DefinitionPath";
    private const float PlaytestDurationSeconds = 120f;

    /// <summary>
    /// 다음 Play Mode 진입 때 지정한 미니게임 정의를 선택하도록 예약합니다.
    /// </summary>
    public static void Request(MinigameDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        string definitionPath = AssetDatabase.GetAssetPath(definition);
        if (string.IsNullOrWhiteSpace(definitionPath))
        {
            Debug.LogError($"Minigame playtest definition asset path was not found: {definition.name}");
            return;
        }

        EditorPrefs.SetString(DefinitionPathKey, definitionPath);
    }

    /// <summary>
    /// 씬 오브젝트가 활성화되기 전에 예약된 미니게임 세션을 구성합니다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void StartRequestedPlaytest()
    {
        string definitionPath = EditorPrefs.GetString(DefinitionPathKey, string.Empty);
        EditorPrefs.DeleteKey(DefinitionPathKey);

        if (string.IsNullOrWhiteSpace(definitionPath))
        {
            return;
        }

        MinigameDefinition definition = AssetDatabase.LoadAssetAtPath<MinigameDefinition>(definitionPath);
        if (definition == null)
        {
            Debug.LogError($"Minigame playtest definition could not be loaded: {definitionPath}");
            return;
        }

        GameLoopSession.Start(PlaytestDurationSeconds);
        GameLoopSession.SelectMinigame(definition);
    }
}
#endif

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 미니게임 정의와 씬 에셋의 에디터 참조를 동기화하고 빌드 대상 씬을 제공합니다.
/// </summary>
public static class MinigameDefinitionSceneReferenceUtility
{
    private const string DefinitionResourcePath = "Minigames";

    /// <summary>
    /// 참조가 비어 있는 정의에만 같은 이름의 씬 에셋을 연결합니다.
    /// </summary>
    public static void AssignMissingReferences()
    {
        MinigameDefinition[] definitions = Resources.LoadAll<MinigameDefinition>(DefinitionResourcePath);
        for (int i = 0; i < definitions.Length; i++)
        {
            MinigameDefinition definition = definitions[i];
            if (definition == null || definition.SceneAsset != null || string.IsNullOrWhiteSpace(definition.SceneName))
            {
                continue;
            }

            string[] sceneGuids = AssetDatabase.FindAssets($"{definition.SceneName} t:Scene");
            List<SceneAsset> matchingScenes = new();
            for (int sceneIndex = 0; sceneIndex < sceneGuids.Length; sceneIndex++)
            {
                SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(sceneGuids[sceneIndex]));
                if (scene != null && string.Equals(scene.name, definition.SceneName, StringComparison.Ordinal))
                {
                    matchingScenes.Add(scene);
                }
            }

            if (matchingScenes.Count != 1)
            {
                Debug.LogWarning($"미니게임 정의 '{definition.name}'의 씬 참조를 연결하지 못했습니다. 같은 이름의 씬이 정확히 하나 필요합니다.", definition);
                continue;
            }

            SerializedObject serialized = new(definition);
            serialized.FindProperty("_sceneAsset").objectReferenceValue = matchingScenes[0];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 모든 미니게임 정의가 직접 참조하는 씬을 경로 순서대로 반환합니다.
    /// </summary>
    public static List<SceneAsset> GetReferencedScenes()
    {
        MinigameDefinition[] definitions = Resources.LoadAll<MinigameDefinition>(DefinitionResourcePath);
        HashSet<SceneAsset> uniqueScenes = new();
        for (int i = 0; i < definitions.Length; i++)
        {
            MinigameDefinition definition = definitions[i];
            if (definition == null || definition.SceneAsset == null)
            {
                if (definition != null)
                {
                    Debug.LogWarning($"미니게임 정의 '{definition.name}'에 씬 에셋 참조가 없어 빌드 설정에서 제외합니다.", definition);
                }

                continue;
            }

            uniqueScenes.Add(definition.SceneAsset);
        }

        List<SceneAsset> scenes = new(uniqueScenes);
        scenes.Sort((left, right) => string.CompareOrdinal(AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right)));
        return scenes;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임에서 사용할 미니게임 정의 SO를 한 곳에서 관리합니다.
/// </summary>
[CreateAssetMenu(fileName = "MinigameCatalog", menuName = "Wildlife Sports Day/Minigame Catalog")]
public sealed class MinigameCatalog : ScriptableObject
{
    [SerializeField] private List<MinigameDefinition> _definitions = new();

    public IReadOnlyList<MinigameDefinition> Definitions => _definitions;
}

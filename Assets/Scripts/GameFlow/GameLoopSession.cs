using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 게임 런의 상태를 보관하고, 랜덤 미니게임 선택과 점수 정산을 담당합니다.
/// </summary>
public static class GameLoopSession
{
    public const string RandomMinigameSceneName = "RandomMinigame";
    public const string MinigameSelectionSceneName = "MinigameSelection";
    public const string ResultSceneName = "MainScene";

    private const float DefaultDurationSeconds = 120f;
    private const string MinigameResourcePath = "Minigames";

    private static int _lastTickFrame = -1;

    public static bool IsActive { get; private set; }
    public static bool IsResultRequested { get; private set; }
    public static float RemainingSeconds { get; private set; }
    public static int Score { get; private set; }
    public static int TotalStars { get; private set; }
    public static int ClearedMinigameCount { get; private set; }
    public static int LastAwardedStars { get; private set; }
    public static string LastPlayedMinigameName { get; private set; } = string.Empty;
    public static string CurrentMinigameName { get; private set; } = string.Empty;
    public static string CurrentMinigameDescription { get; private set; } = string.Empty;
    public static int CurrentMinigameScore { get; private set; }
    public static MinigameDefinition CurrentMinigameDefinition { get; private set; }
    public static bool IsPlayingMinigame => !string.IsNullOrEmpty(CurrentMinigameName);

    /// <summary>
    /// 새 게임 런을 시작하고 첫 미니게임을 뽑을 선택 씬 이름을 반환합니다.
    /// </summary>
    public static string StartRunAndGetFirstScene()
    {
        Start(DefaultDurationSeconds);
        GameHudController.ShowAfterNextSceneLoaded();
        return MinigameSelectionSceneName;
    }

    /// <summary>
    /// 전체 게임 제한시간과 누적 점수 상태를 초기화합니다.
    /// </summary>
    public static void Start(float durationSeconds)
    {
        IsActive = true;
        IsResultRequested = false;
        RemainingSeconds = durationSeconds;
        _lastTickFrame = -1;
        Score = 0;
        TotalStars = 0;
        ClearedMinigameCount = 0;
        LastAwardedStars = 0;
        LastPlayedMinigameName = string.Empty;
        ClearCurrentMinigame();
    }

    /// <summary>
    /// 코드에서 직접 넘긴 정보로 현재 미니게임 상태를 설정합니다.
    /// </summary>
    public static void SelectMinigame(string name, string description, int score, float durationSeconds)
    {
        CurrentMinigameDefinition = null;
        CurrentMinigameName = name;
        CurrentMinigameDescription = description;
        CurrentMinigameScore = score;
    }

    /// <summary>
    /// 미니게임 정의 에셋을 기준으로 현재 미니게임 상태를 설정합니다.
    /// </summary>
    public static void SelectMinigame(MinigameDefinition definition)
    {
        if (definition == null)
        {
            ClearCurrentMinigame();
            return;
        }

        CurrentMinigameDefinition = definition;
        CurrentMinigameName = definition.DisplayName;
        CurrentMinigameDescription = definition.Description;
        CurrentMinigameScore = definition.Score;
    }

    /// <summary>
    /// 전체 게임 제한시간만 감소시키고, 시간이 끝나면 결과 상태로 전환합니다.
    /// </summary>
    public static void Tick(float deltaTime)
    {
        if (!IsActive || IsResultRequested)
        {
            return;
        }

        if (_lastTickFrame == Time.frameCount)
        {
            return;
        }

        _lastTickFrame = Time.frameCount;
        RemainingSeconds = Mathf.Max(0f, RemainingSeconds - deltaTime);
        if (RemainingSeconds <= 0f)
        {
            RequestResult();
        }
    }

    /// <summary>
    /// 현재 미니게임을 3성 클리어로 처리합니다.
    /// </summary>
    public static void CompleteCurrentMinigame()
    {
        CompleteCurrentMinigame(3);
    }

    /// <summary>
    /// 현재 미니게임의 별 등급을 점수로 정산하고 다음 미니게임을 받을 수 있게 비웁니다.
    /// </summary>
    public static void CompleteCurrentMinigame(int stars)
    {
        if (!IsPlayingMinigame)
        {
            return;
        }

        int awardedStars = Mathf.Clamp(stars, 0, 3);
        Score += GetScoreForStars(awardedStars);
        TotalStars += awardedStars;
        LastAwardedStars = awardedStars;
        LastPlayedMinigameName = CurrentMinigameName;
        if (awardedStars > 0)
        {
            ClearedMinigameCount++;
        }

        ClearCurrentMinigame();
    }

    /// <summary>
    /// 현재 미니게임을 실패 처리하고 점수 없이 다음 미니게임을 받을 수 있게 비웁니다.
    /// </summary>
    public static void FailCurrentMinigame()
    {
        LastAwardedStars = 0;
        LastPlayedMinigameName = CurrentMinigameName;
        ClearCurrentMinigame();
    }

    /// <summary>
    /// 전체 게임 런을 종료하고 결과 화면으로 넘어갈 준비를 합니다.
    /// </summary>
    public static void RequestResult()
    {
        IsActive = false;
        IsResultRequested = true;
        RemainingSeconds = 0f;
        ClearCurrentMinigame();
        GameHudController.DestroyHud();
    }

    /// <summary>
    /// 다음 미니게임을 뽑을 선택 씬 또는 결과 화면 씬 이름을 반환합니다.
    /// </summary>
    public static string GetNextSceneNameOrResult()
    {
        if (!IsActive || IsResultRequested || RemainingSeconds <= 0f)
        {
            RequestResult();
            return ResultSceneName;
        }

        return MinigameSelectionSceneName;
    }

    /// <summary>
    /// 카드 선택 연출에서 사용할 다음 랜덤 미니게임을 하나 확정합니다.
    /// </summary>
    public static bool TrySelectRandomMinigame(out MinigameDefinition selectedDefinition)
    {
        selectedDefinition = null;
        if (!IsActive || IsResultRequested || !SelectNextRandomMinigame())
        {
            return false;
        }

        selectedDefinition = CurrentMinigameDefinition;
        return selectedDefinition != null;
    }

    /// <summary>
    /// 카드 연출에서 확정된 미니게임 씬 또는 결과 화면 씬 이름을 반환합니다.
    /// </summary>
    public static string GetCurrentMinigameSceneNameOrResult()
    {
        if (!IsActive || IsResultRequested || CurrentMinigameDefinition == null || string.IsNullOrWhiteSpace(CurrentMinigameDefinition.SceneName))
        {
            RequestResult();
            return ResultSceneName;
        }

        return CurrentMinigameDefinition.SceneName;
    }

    /// <summary>
    /// 현재 미니게임 선택 상태만 비웁니다.
    /// </summary>
    public static void ClearCurrentMinigame()
    {
        CurrentMinigameName = string.Empty;
        CurrentMinigameDescription = string.Empty;
        CurrentMinigameScore = 0;
        CurrentMinigameDefinition = null;
    }

    /// <summary>
    /// 별 개수를 현재 미니게임 정의에 맞는 점수로 변환합니다.
    /// </summary>
    private static int GetScoreForStars(int stars)
    {
        if (CurrentMinigameDefinition != null)
        {
            return CurrentMinigameDefinition.GetScoreForStars(stars);
        }

        return Mathf.RoundToInt(CurrentMinigameScore * (stars / 3f));
    }

    /// <summary>
    /// Resources의 미니게임 정의 중 하나를 랜덤으로 선택합니다.
    /// </summary>
    private static bool SelectNextRandomMinigame()
    {
        MinigameDefinition[] definitions = Resources.LoadAll<MinigameDefinition>(MinigameResourcePath);
        if (definitions == null || definitions.Length <= 0)
        {
            return false;
        }

        List<MinigameDefinition> pool = new(definitions.Length);
        for (int i = 0; i < definitions.Length; i++)
        {
            if (definitions[i] != null &&
                definitions[i].IsRandomSelectionEnabled &&
                !string.IsNullOrWhiteSpace(definitions[i].SceneName))
            {
                pool.Add(definitions[i]);
            }
        }

        if (pool.Count <= 0)
        {
            return false;
        }

        if (pool.Count > 1)
        {
            List<MinigameDefinition> filteredPool = pool.FindAll(definition => definition.DisplayName != LastPlayedMinigameName);
            if (filteredPool.Count > 0)
            {
                pool = filteredPool;
            }
        }

        SelectMinigame(pool[Random.Range(0, pool.Count)]);
        return true;
    }
}

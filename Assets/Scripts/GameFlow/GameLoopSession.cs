using UnityEngine;

public static class GameLoopSession
{
    public static bool IsActive { get; private set; }
    public static bool IsResultRequested { get; private set; }
    public static float RemainingSeconds { get; private set; }
    public static int Score { get; private set; }
    public static int ClearedMinigameCount { get; private set; }
    public static string CurrentMinigameName { get; private set; } = string.Empty;
    public static string CurrentMinigameDescription { get; private set; } = string.Empty;
    public static int CurrentMinigameScore { get; private set; }
    public static float CurrentMinigameDurationSeconds { get; private set; }
    public static float CurrentMinigameElapsedSeconds { get; private set; }
    public static bool IsPlayingMinigame => !string.IsNullOrEmpty(CurrentMinigameName);
    public static float CurrentMinigameRemainingSeconds => Mathf.Max(0f, CurrentMinigameDurationSeconds - CurrentMinigameElapsedSeconds);

    public static void Start(float durationSeconds)
    {
        IsActive = true;
        IsResultRequested = false;
        RemainingSeconds = durationSeconds;
        Score = 0;
        ClearedMinigameCount = 0;
        ClearCurrentMinigame();
    }

    public static void SelectMinigame(string name, string description, int score, float durationSeconds)
    {
        CurrentMinigameName = name;
        CurrentMinigameDescription = description;
        CurrentMinigameScore = score;
        CurrentMinigameDurationSeconds = durationSeconds;
        CurrentMinigameElapsedSeconds = 0f;
    }

    public static void Tick(float deltaTime)
    {
        if (!IsActive || IsResultRequested)
        {
            return;
        }

        RemainingSeconds = Mathf.Max(0f, RemainingSeconds - deltaTime);
        if (IsPlayingMinigame)
        {
            CurrentMinigameElapsedSeconds += deltaTime;
        }

        if (RemainingSeconds <= 0f)
        {
            RequestResult();
        }
    }

    public static void CompleteCurrentMinigame()
    {
        if (!IsPlayingMinigame)
        {
            return;
        }

        Score += CurrentMinigameScore;
        ClearedMinigameCount++;
        ClearCurrentMinigame();
    }

    public static void RequestResult()
    {
        IsResultRequested = true;
        RemainingSeconds = 0f;
        ClearCurrentMinigame();
    }

    public static void ClearCurrentMinigame()
    {
        CurrentMinigameName = string.Empty;
        CurrentMinigameDescription = string.Empty;
        CurrentMinigameScore = 0;
        CurrentMinigameDurationSeconds = 0f;
        CurrentMinigameElapsedSeconds = 0f;
    }
}

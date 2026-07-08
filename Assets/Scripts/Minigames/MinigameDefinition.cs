using UnityEngine;

/// <summary>
/// 랜덤 선택 가능한 미니게임의 표시 정보, 씬 이름, 별 목표와 보상 점수를 정의합니다.
/// </summary>
[CreateAssetMenu(fileName = "MinigameDefinition", menuName = "Wildlife Sports Day/Minigame Definition")]
public sealed class MinigameDefinition : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string _minigameName;
    [SerializeField, TextArea] private string _description;
    [SerializeField, TextArea] private string _objective;
    [SerializeField] private string _controlGuide;

    [Header("분류")]
    [SerializeField] private MinigameViewType _viewType = MinigameViewType.SideView;
    [SerializeField] private MinigameCharacterType _characterType = MinigameCharacterType.None;
    [SerializeField] private MinigameControlType _controlType = MinigameControlType.None;
    [SerializeField] private MinigameDifficultyType _difficulty = MinigameDifficultyType.Normal;

    [Header("진행")]
    [SerializeField] private bool _isRandomSelectionEnabled = true;
    [SerializeField, Min(0)] private int _score = 100;
    [SerializeField, Min(1)] private int _targetCount = 1;
    [SerializeField] private string _sceneName = "GameScene";

    [Header("별 등급")]
    [SerializeField, Min(1)] private int _oneStarTargetCount = 1;
    [SerializeField, Min(1)] private int _twoStarTargetCount = 2;
    [SerializeField, Min(1)] private int _threeStarTargetCount = 3;
    [SerializeField, Min(0)] private int _oneStarScore = 30;
    [SerializeField, Min(0)] private int _twoStarScore = 60;
    [SerializeField, Min(0)] private int _threeStarScore = 100;

    [Header("표시 에셋")]
    [SerializeField] private Sprite _titleImage;
    [SerializeField] private Sprite _previewImage;

    public string MinigameName => _minigameName;
    public string Description => _description;
    public string Objective => _objective;
    public string ControlGuide => _controlGuide;
    public MinigameViewType ViewType => _viewType;
    public MinigameCharacterType CharacterType => _characterType;
    public MinigameControlType ControlType => _controlType;
    public MinigameDifficultyType Difficulty => _difficulty;
    public bool IsRandomSelectionEnabled => _isRandomSelectionEnabled;
    public int Score => _score;
    public int TargetCount => _targetCount;
    public string SceneName => _sceneName;
    public int OneStarTargetCount => _oneStarTargetCount;
    public int TwoStarTargetCount => _twoStarTargetCount;
    public int ThreeStarTargetCount => _threeStarTargetCount;
    public int OneStarScore => _oneStarScore;
    public int TwoStarScore => _twoStarScore;
    public int ThreeStarScore => _threeStarScore;
    public Sprite TitleImage => _titleImage;
    public Sprite PreviewImage => _previewImage;

    public string DisplayName => string.IsNullOrWhiteSpace(_minigameName) ? name : _minigameName;
    public string DifficultyLabel => _difficulty switch
    {
        MinigameDifficultyType.Easy => "쉬움",
        MinigameDifficultyType.Normal => "보통",
        MinigameDifficultyType.Hard => "어려움",
        _ => "보통",
    };

    /// <summary>
    /// 달성 횟수를 별 등급 목표와 비교해 0~3성으로 변환합니다.
    /// </summary>
    public int GetStarGrade(int achievedCount)
    {
        if (achievedCount >= _threeStarTargetCount)
        {
            return 3;
        }

        if (achievedCount >= _twoStarTargetCount)
        {
            return 2;
        }

        if (achievedCount >= _oneStarTargetCount)
        {
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// 별 등급에 대응하는 보상 점수를 반환합니다.
    /// </summary>
    public int GetScoreForStars(int stars)
    {
        return stars switch
        {
            3 => _threeStarScore,
            2 => _twoStarScore,
            1 => _oneStarScore,
            _ => 0,
        };
    }

    /// <summary>
    /// 인스펙터 값이 역순으로 들어가지 않도록 별 목표와 점수를 보정합니다.
    /// </summary>
    private void OnValidate()
    {
        _twoStarTargetCount = Mathf.Max(_oneStarTargetCount, _twoStarTargetCount);
        _threeStarTargetCount = Mathf.Max(_twoStarTargetCount, _threeStarTargetCount);
        _twoStarScore = Mathf.Max(_oneStarScore, _twoStarScore);
        _threeStarScore = Mathf.Max(_twoStarScore, _threeStarScore);
    }
}

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameLoopController : MonoBehaviour
{
    [Serializable]
    public sealed class MinigameDefinition
    {
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private string _difficulty = "보통";
        [SerializeField, Min(0)] private int _score = 100;
        [SerializeField, Min(0.1f)] private float _playSeconds = 8f;

        public string Name => _name;
        public string Description => _description;
        public string Difficulty => _difficulty;
        public int Score => _score;
        public float PlaySeconds => _playSeconds;

        public MinigameDefinition(string name, string description, string difficulty, int score, float playSeconds)
        {
            _name = name;
            _description = description;
            _difficulty = difficulty;
            _score = score;
            _playSeconds = playSeconds;
        }
    }

    private enum LoopState
    {
        Selecting,
        Result,
    }

    [Header("게임 설정")]
    [SerializeField] private bool _startAutomatically = true;
    [SerializeField, Min(1f)] private float _gameDurationSeconds = 120f;
    [SerializeField, Min(1)] private int _candidateCount = 3;
    [SerializeField] private string _minigameSceneName = "GameScene";
    [SerializeField] private List<MinigameDefinition> _minigames = new()
    {
        new MinigameDefinition("연어 깨물기", "곰이 타이밍에 맞춰 폭포를 오르는 연어를 깨무는 미니게임", "쉬움", 100, 8f),
        new MinigameDefinition("뱀 미로 탈출", "처음 3초 동안 본 지도를 기억하고 정글 사원 미로를 탈출하는 미니게임", "보통", 180, 10f),
        new MinigameDefinition("플라밍고 중심잡기", "연못 위에서 균형을 잃지 않고 버티는 미니게임", "어려움", 280, 12f),
    };

    [Header("버튼")]
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _completeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private List<Button> _candidateButtons = new();

    [Header("패널")]
    [SerializeField] private GameObject _root;
    [SerializeField] private GameObject _selectionPanel;
    [SerializeField] private GameObject _playingPanel;
    [SerializeField] private GameObject _resultPanel;

    [Header("텍스트")]
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private List<TMP_Text> _candidateLabels = new();
    [SerializeField] private List<TMP_Text> _candidateTitleTexts = new();
    [SerializeField] private List<TMP_Text> _candidateDifficultyTexts = new();
    [SerializeField] private List<TMP_Text> _candidateRewardTexts = new();
    [SerializeField] private List<TMP_Text> _candidateSelectButtonLabels = new();
    [SerializeField] private TMP_Text _playingTitleText;
    [SerializeField] private TMP_Text _playingDescriptionText;
    [SerializeField] private TMP_Text _playingProgressText;
    [SerializeField] private TMP_Text _resultText;

    private readonly List<MinigameDefinition> _currentCandidates = new();
    private readonly List<UnityAction> _candidateButtonActions = new();

    private LoopState _state = LoopState.Selecting;

    private void Awake()
    {
        SetPanelActive(_playingPanel, false);

        if (!GameLoopSession.IsActive && _startAutomatically)
        {
            StartGame();
            return;
        }

        if (GameLoopSession.IsResultRequested)
        {
            ShowResult();
            return;
        }

        ShowSelection();
    }

    private void OnEnable()
    {
        AddButtonListeners();
    }

    private void OnDisable()
    {
        RemoveButtonListeners();
    }

    private void Update()
    {
        if (_state != LoopState.Selecting)
        {
            return;
        }

        GameLoopSession.Tick(Time.deltaTime);
        if (GameLoopSession.IsResultRequested)
        {
            ShowResult();
            return;
        }

        UpdateHud();
    }

    public void StartGame()
    {
        GameLoopSession.Start(_gameDurationSeconds);
        ShowSelection();
    }

    private void AddButtonListeners()
    {
        if (_startButton != null)
        {
            _startButton.onClick.AddListener(StartGame);
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.AddListener(StartGame);
        }

        for (int i = 0; i < _candidateButtons.Count; i++)
        {
            int candidateIndex = i;
            Button candidateButton = _candidateButtons[i];
            if (candidateButton != null)
            {
                UnityAction action = () => StartMinigame(candidateIndex);
                candidateButton.onClick.AddListener(action);
                _candidateButtonActions.Add(action);
                continue;
            }

            _candidateButtonActions.Add(null);
        }
    }

    private void RemoveButtonListeners()
    {
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(StartGame);
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveListener(StartGame);
        }

        for (int i = 0; i < _candidateButtons.Count; i++)
        {
            Button candidateButton = _candidateButtons[i];
            if (candidateButton != null && i < _candidateButtonActions.Count && _candidateButtonActions[i] != null)
            {
                candidateButton.onClick.RemoveListener(_candidateButtonActions[i]);
            }
        }

        _candidateButtonActions.Clear();
    }

    private void ShowSelection()
    {
        _state = LoopState.Selecting;
        SetPanelActive(_root, true);
        SetPanelActive(_selectionPanel, true);
        SetPanelActive(_resultPanel, false);
        PickCandidates();
        ApplyCandidatesToButtons();
        UpdateHud();
    }

    private void StartMinigame(int candidateIndex)
    {
        if (_state != LoopState.Selecting || candidateIndex < 0 || candidateIndex >= _currentCandidates.Count)
        {
            return;
        }

        MinigameDefinition minigame = _currentCandidates[candidateIndex];
        GameLoopSession.SelectMinigame(minigame.Name, minigame.Description, minigame.Score, minigame.PlaySeconds);
        SceneManager.LoadScene(_minigameSceneName, LoadSceneMode.Single);
    }

    private void ShowResult()
    {
        _state = LoopState.Result;
        SetPanelActive(_root, true);

        string resultMessage = $"결과\n점수: {GameLoopSession.Score}\n클리어: {GameLoopSession.ClearedMinigameCount}개";
        if (_resultPanel != null)
        {
            SetPanelActive(_selectionPanel, false);
            SetPanelActive(_resultPanel, true);

            if (_resultText != null)
            {
                _resultText.text = resultMessage;
            }
        }
        else
        {
            SetPanelActive(_selectionPanel, true);
            for (int i = 0; i < _candidateButtons.Count; i++)
            {
                if (_candidateButtons[i] != null)
                {
                    _candidateButtons[i].gameObject.SetActive(i == 0);
                }
            }

            if (_candidateLabels.Count > 0 && _candidateLabels[0] != null)
            {
                _candidateLabels[0].text = $"<size=56><color=#1A130D>{resultMessage}</color></size>";
            }
        }

        UpdateHud();
    }

    private void PickCandidates()
    {
        _currentCandidates.Clear();
        List<MinigameDefinition> pool = new(_minigames);
        int targetCount = Mathf.Min(_candidateCount, _candidateButtons.Count, pool.Count);

        while (_currentCandidates.Count < targetCount && pool.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            _currentCandidates.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    private void ApplyCandidatesToButtons()
    {
        for (int i = 0; i < _candidateButtons.Count; i++)
        {
            bool hasCandidate = i < _currentCandidates.Count;
            Button candidateButton = _candidateButtons[i];
            if (candidateButton != null)
            {
                candidateButton.gameObject.SetActive(hasCandidate);
                Transform cardTransform = candidateButton.transform.parent;
                if (cardTransform != null && (_selectionPanel == null || cardTransform != _selectionPanel.transform))
                {
                    cardTransform.gameObject.SetActive(hasCandidate);
                }
            }

            if (!hasCandidate)
            {
                continue;
            }

            MinigameDefinition candidate = _currentCandidates[i];
            ApplyCandidateText(i, candidate);
        }
    }

    private void ApplyCandidateText(int index, MinigameDefinition candidate)
    {
        TMP_Text titleText = GetText(_candidateTitleTexts, index);
        TMP_Text fallbackLabel = GetText(_candidateLabels, index);
        if (titleText != null)
        {
            titleText.text = candidate.Name;
        }
        else if (fallbackLabel != null)
        {
            fallbackLabel.text = FormatCandidateLabel(candidate);
        }

        string difficultyColor = candidate.Difficulty switch
        {
            "쉬움" => "#35A853",
            "보통" => "#D8861D",
            "어려움" => "#C8322B",
            _ => "#20180F",
        };

        TMP_Text difficultyText = GetText(_candidateDifficultyTexts, index);
        if (difficultyText != null)
        {
            difficultyText.text = $"난이도: <color={difficultyColor}>{candidate.Difficulty}</color>";
        }

        TMP_Text rewardText = GetText(_candidateRewardTexts, index);
        if (rewardText != null)
        {
            rewardText.text = $"보상: <color=#1E4CB5>{candidate.Score}점</color>";
        }

        TMP_Text selectButtonLabel = GetText(_candidateSelectButtonLabels, index);
        if (selectButtonLabel != null)
        {
            selectButtonLabel.text = "선택";
        }
    }

    private static string FormatCandidateLabel(MinigameDefinition candidate)
    {
        string difficultyColor = candidate.Difficulty switch
        {
            "쉬움" => "#35A853",
            "보통" => "#D8861D",
            "어려움" => "#C8322B",
            _ => "#20180F",
        };

        return
            $"<size=44><color=#FFFFFF>{candidate.Name}</color></size>\n\n\n\n\n\n\n\n\n" +
            $"<size=34><color=#1A130D>난이도: <color={difficultyColor}>{candidate.Difficulty}</color>\n" +
            $"----------------\n보상: <color=#1E4CB5>{candidate.Score}점</color></color></size>\n\n" +
            "<size=48><color=#FFFFFF>선택</color></size>";
    }

    private static TMP_Text GetText(List<TMP_Text> texts, int index)
    {
        return index >= 0 && index < texts.Count ? texts[index] : null;
    }

    private void UpdateHud()
    {
        int remaining = Mathf.CeilToInt(GameLoopSession.RemainingSeconds);
        int minutes = remaining / 60;
        int seconds = remaining % 60;
        if (_timerText != null)
        {
            _timerText.text = $"남은 시간 {minutes:00}:{seconds:00}";
        }

        if (_scoreText != null)
        {
            _scoreText.text = $"현재 점수 {GameLoopSession.Score}점";
        }
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }
}

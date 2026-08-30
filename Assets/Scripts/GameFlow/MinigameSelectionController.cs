using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 카드가 가로로 지나간 뒤 다음 미니게임을 무작위로 확정하는 선택 씬 흐름을 제어합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MinigameSelectionController : MonoBehaviour
{
    [SerializeField] private RectTransform _cardTrack;
    [SerializeField] private MinigameSelectionCard[] _cards;
    [SerializeField, Min(0.1f)] private float _spinDurationSeconds = 1.2f;
    [SerializeField, Min(0f)] private float _selectionHoldSeconds = 0.35f;
    [SerializeField, Min(1f)] private float _cardSpacing = 470f;

    private readonly List<MinigameDefinition> _availableDefinitions = new();
    private MinigameDefinition _selectedDefinition;
    private bool _isFinished;

    /// <summary>
    /// 선택 가능한 정의를 카드에 배치하고 룰렛 연출을 시작합니다.
    /// </summary>
    private void Start()
    {
        if (!GameLoopSession.IsActive || GameLoopSession.IsResultRequested)
        {
            LoadResultScene();
            return;
        }

        if (!GameLoopSession.TrySelectRandomMinigame(out _selectedDefinition))
        {
            GameLoopSession.RequestResult();
            LoadResultScene();
            return;
        }

        CollectAvailableDefinitions();
        if (_availableDefinitions.Count == 0 || _cards == null || _cards.Length == 0 || _cardTrack == null)
        {
            GameLoopSession.RequestResult();
            LoadResultScene();
            return;
        }

        PopulateCards();
        StartCoroutine(SpinRoutine());
    }

    /// <summary>
    /// Resources에 등록된 모든 미니게임 정의를 카드 연출용으로 수집합니다.
    /// </summary>
    private void CollectAvailableDefinitions()
    {
        _availableDefinitions.Clear();
        MinigameDefinition[] definitions = Resources.LoadAll<MinigameDefinition>("Minigames");
        for (int i = 0; i < definitions.Length; i++)
        {
            MinigameDefinition definition = definitions[i];
            if (definition != null && !string.IsNullOrWhiteSpace(definition.SceneName))
            {
                _availableDefinitions.Add(definition);
            }
        }
    }

    /// <summary>
    /// 이동 중 표시할 카드 시퀀스를 만들고 마지막 중앙 카드에 선택 결과를 배치합니다.
    /// </summary>
    private void PopulateCards()
    {
        int finalCardIndex = _cards.Length / 2;
        for (int i = 0; i < _cards.Length; i++)
        {
            MinigameDefinition definition = i == finalCardIndex
                ? _selectedDefinition
                : _availableDefinitions[Random.Range(0, _availableDefinitions.Count)];

            _cards[i].SetDefinition(definition);
            _cards[i].SetHighlighted(i == 0);
        }

        _cardTrack.anchoredPosition = new Vector2(finalCardIndex * _cardSpacing, _cardTrack.anchoredPosition.y);
    }

    /// <summary>
    /// 카드 트랙을 감속시키며 이동한 뒤 중앙의 미니게임을 확정하고 해당 씬을 로드합니다.
    /// </summary>
    private IEnumerator SpinRoutine()
    {
        int finalCardIndex = _cards.Length / 2;
        float startX = finalCardIndex * _cardSpacing;
        float elapsedSeconds = 0f;

        while (elapsedSeconds < _spinDurationSeconds)
        {
            elapsedSeconds += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedSeconds / _spinDurationSeconds);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            _cardTrack.anchoredPosition = new Vector2(Mathf.Lerp(startX, 0f, easedProgress), _cardTrack.anchoredPosition.y);
            RefreshHighlight();
            yield return null;
        }

        _cardTrack.anchoredPosition = new Vector2(0f, _cardTrack.anchoredPosition.y);
        _cards[finalCardIndex].SetHighlighted(true);
        yield return new WaitForSecondsRealtime(_selectionHoldSeconds);

        if (_isFinished)
        {
            yield break;
        }

        _isFinished = true;
        SceneTransitionController.LoadScene(GameLoopSession.GetCurrentMinigameSceneNameOrResult(), LoadSceneMode.Single);
    }

    /// <summary>
    /// 중앙 선택선에 가장 가까운 카드 하나를 강조합니다.
    /// </summary>
    private void RefreshHighlight()
    {
        int nearestCardIndex = 0;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < _cards.Length; i++)
        {
            float distance = Mathf.Abs(_cards[i].transform.localPosition.x + _cardTrack.anchoredPosition.x);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCardIndex = i;
            }
        }

        for (int i = 0; i < _cards.Length; i++)
        {
            _cards[i].SetHighlighted(i == nearestCardIndex);
        }
    }

    /// <summary>
    /// 선택 흐름을 진행할 수 없을 때 메인 씬으로 돌아갑니다.
    /// </summary>
    private static void LoadResultScene()
    {
        if (!SceneTransitionController.IsTransitioning)
        {
            SceneTransitionController.LoadScene(GameLoopSession.ResultSceneName, LoadSceneMode.Single);
        }
    }
}

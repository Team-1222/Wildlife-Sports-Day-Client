using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// F키 연타 횟수로 배경 트랙을 이동시키고, 도달 거리로 별 등급을 계산하는 미니게임 컨트롤러입니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class KeyboardMashPolarBearController : MinigameControllerBase
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private Button _completeButton;
    [SerializeField] private Key _buttonMashKey = Key.F;
    [SerializeField] private Transform _runnerRoot;
    [SerializeField] private Transform _movingTrackRoot;
    [SerializeField] private bool _useTrackInitialPositionAsStart = true;
    [SerializeField] private Vector3 _trackStartPosition = Vector3.zero;
    [SerializeField] private Vector3 _trackFinishPosition = new(-40f, 0f, 0f);
    [SerializeField, Min(0.1f)] private float _moveLerpSpeed = 14f;

    private int _buttonMashCount;
    private Vector3 _targetPosition;
    private float _targetProgress;

    /// <summary>
    /// 미니게임 씬 진입 시 진행 상태와 트랙 위치를 시작 상태로 맞춥니다.
    /// </summary>
    protected override void OnMinigameStarted()
    {
        _buttonMashCount = 0;
        SetCompleteButtonActive(false);
        ApplySceneTrackStartPosition();
        SetTrackProgress(0f, true);
        RefreshTexts();
    }

    /// <summary>
    /// 입력/트랙 이동/표시 갱신처럼 북극곰 미니게임 전용 갱신을 처리합니다.
    /// </summary>
    protected override void TickMinigame()
    {
        TickButtonMash();
        MoveTrack();
        RefreshTexts();
    }

    /// <summary>
    /// 연타 키 입력을 누적하고 3성 목표에 도달하면 미니게임을 완료합니다.
    /// </summary>
    private void TickButtonMash()
    {
        MinigameDefinition definition = Definition;
        if (definition == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        KeyControl keyControl = keyboard[_buttonMashKey];
        if (keyControl == null || !keyControl.wasPressedThisFrame)
        {
            return;
        }

        _buttonMashCount++;
        SetTrackProgress(GetProgress01(definition), false);
        if (_buttonMashCount >= definition.ThreeStarTargetCount)
        {
            SetTrackProgress(1f, true);
            CompleteMinigame(GetDistanceStarGrade(definition));
        }
    }

    /// <summary>
    /// 현재 이동 거리와 별 등급 정보를 씬 UI에 반영합니다.
    /// </summary>
    private void RefreshTexts()
    {
        MinigameDefinition definition = Definition;
        if (definition == null)
        {
            return;
        }

        if (_titleText != null)
        {
            _titleText.text = definition.DisplayName;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text =
                $"{definition.Description}\n" +
                $"목표: {definition.Objective}\n" +
                $"별: {definition.OneStarTargetCount}/{definition.TwoStarTargetCount}/{definition.ThreeStarTargetCount}회\n" +
                $"조작: {definition.ControlGuide}";
        }

        if (_progressText != null)
        {
            int distancePercent = Mathf.RoundToInt(GetTrackProgress01() * 100f);
            int stars = GetDistanceStarGrade(definition);
            _progressText.text = $"이동 거리: {distancePercent}%\n별: {stars}/3";
        }
    }

    /// <summary>
    /// 씬에 배치된 트랙의 현재 위치를 시작점으로 사용합니다.
    /// </summary>
    private void ApplySceneTrackStartPosition()
    {
        if (!_useTrackInitialPositionAsStart || _movingTrackRoot == null)
        {
            return;
        }

        Vector3 travelOffset = _trackFinishPosition - _trackStartPosition;
        _trackStartPosition = _movingTrackRoot.position;
        _trackFinishPosition = _trackStartPosition + travelOffset;
    }

    /// <summary>
    /// 목표 진행률을 트랙 위치로 변환하고, 필요하면 즉시 이동시킵니다.
    /// </summary>
    private void SetTrackProgress(float progress, bool snap)
    {
        _targetProgress = Mathf.Clamp01(progress);
        _targetPosition = Vector3.Lerp(_trackStartPosition, _trackFinishPosition, _targetProgress);
        if (snap && _movingTrackRoot != null)
        {
            _movingTrackRoot.position = _targetPosition;
        }
    }

    /// <summary>
    /// 트랙을 목표 위치까지 부드럽게 이동시킵니다.
    /// </summary>
    private void MoveTrack()
    {
        if (_movingTrackRoot == null)
        {
            return;
        }

        _movingTrackRoot.position = Vector3.Lerp(_movingTrackRoot.position, _targetPosition, Time.deltaTime * _moveLerpSpeed);
    }

    /// <summary>
    /// 현재 연타 횟수를 3성 목표 기준의 0~1 진행률로 변환합니다.
    /// </summary>
    private float GetProgress01(MinigameDefinition definition)
    {
        if (definition == null || definition.ThreeStarTargetCount <= 0)
        {
            return 0f;
        }

        return _buttonMashCount / (float)definition.ThreeStarTargetCount;
    }

    /// <summary>
    /// 실제 트랙 위치를 기준으로 현재 이동 진행률을 계산합니다.
    /// </summary>
    private float GetTrackProgress01()
    {
        if (_movingTrackRoot == null)
        {
            return _targetProgress;
        }

        Vector3 travel = _trackFinishPosition - _trackStartPosition;
        float travelSqrMagnitude = travel.sqrMagnitude;
        if (travelSqrMagnitude <= 0.001f)
        {
            return 0f;
        }

        float projectedDistance = Vector3.Dot(_movingTrackRoot.position - _trackStartPosition, travel) / travelSqrMagnitude;
        return Mathf.Clamp01(projectedDistance);
    }

    /// <summary>
    /// 현재 이동 진행률을 미니게임 정의의 별 목표값과 비교해 별 등급을 계산합니다.
    /// </summary>
    private int GetDistanceStarGrade(MinigameDefinition definition)
    {
        if (definition == null || definition.ThreeStarTargetCount <= 0)
        {
            return 0;
        }

        float progress = GetTrackProgress01();
        float oneStarProgress = definition.OneStarTargetCount / (float)definition.ThreeStarTargetCount;
        float twoStarProgress = definition.TwoStarTargetCount / (float)definition.ThreeStarTargetCount;

        if (progress >= 1f)
        {
            return 3;
        }

        if (progress >= twoStarProgress)
        {
            return 2;
        }

        if (progress >= oneStarProgress)
        {
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// 임시 완료 버튼의 표시 여부를 바꿉니다.
    /// </summary>
    private void SetCompleteButtonActive(bool active)
    {
        if (_completeButton != null)
        {
            _completeButton.gameObject.SetActive(active);
        }
    }

}

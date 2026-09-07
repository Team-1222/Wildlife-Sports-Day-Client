using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// F키 연타로 북극곰과 카메라를 전진시키고, 제한 시간 안의 도달 거리로 별 등급을 계산하는 미니게임 컨트롤러입니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class KeyboardMashPolarBearController : MinigameControllerBase
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _gradeText;
    [SerializeField] private Button _completeButton;
    [SerializeField] private Key _buttonMashKey = Key.F;
    [SerializeField] private Transform _runnerRoot;
    [SerializeField] private Transform _followCamera;
    [SerializeField] private Vector3 _cameraFollowOffset = new(0.5f, 5.85f, -15f);
    [SerializeField] private Vector3 _cameraFollowEulerAngles = new(20f, 25f, 0f);
    [SerializeField, Min(0.1f)] private float _roundDurationSeconds = 6f;
    [SerializeField, Min(0.1f)] private float _distancePerMash = 4f;
    [SerializeField, Min(0.1f)] private float _moveLerpSpeed = 14f;
    [SerializeField, Min(0.1f)] private float _cameraLerpSpeed = 7f;
    [SerializeField, Min(0f)] private float _cameraSwayPositionAmount = 0.06f;
    [SerializeField, Min(0f)] private float _cameraSwayRotationAmount = 0.45f;
    [SerializeField, Min(0.01f)] private float _cameraSwayDecaySpeed = 3.5f;

    private int _buttonMashCount;
    private Vector3 _runnerStartPosition;
    private Vector3 _runnerTargetPosition;
    private float _roundRemainingSeconds;
    private float _cameraSwayStrength;

    protected override void OnMinigameStarted()
    {
        _buttonMashCount = 0;
        _roundRemainingSeconds = _roundDurationSeconds;
        SetCompleteButtonActive(false);
        ResetRunnerAndCamera();
        RefreshTexts();
    }

    protected override void TickMinigame()
    {
        TickButtonMash();
        if (!GameLoopSession.IsPlayingMinigame)
        {
            return;
        }

        MoveRunnerAndCamera();
        TickRoundTimer();
        if (GameLoopSession.IsPlayingMinigame)
        {
            RefreshTexts();
        }
    }

    private void TickButtonMash()
    {
        MinigameDefinition definition = Definition;
        if (definition == null || Keyboard.current == null)
        {
            return;
        }

        KeyControl keyControl = Keyboard.current[_buttonMashKey];
        if (keyControl == null || !keyControl.wasPressedThisFrame)
        {
            return;
        }

        _buttonMashCount++;
        AddCameraSway();
        SetRunnerProgress(GetProgress01(definition), false);
        if (_buttonMashCount >= definition.ThreeStarTargetCount)
        {
            SetRunnerProgress(1f, true);
            CompleteMinigame(3);
        }
    }

    private void TickRoundTimer()
    {
        _roundRemainingSeconds = Mathf.Max(0f, _roundRemainingSeconds - Time.deltaTime);
        if (_roundRemainingSeconds > 0f)
        {
            return;
        }

        int stars = GetDistanceStarGrade(Definition);
        if (stars > 0)
        {
            CompleteMinigame(stars);
            return;
        }

        FailMinigame();
    }

    private void RefreshTexts()
    {
        MinigameDefinition definition = Definition;
        if (definition == null)
        {
            return;
        }

        if (_titleText != null)
        {
            _titleText.text = "빨리 달리세요";
        }

        if (_descriptionText != null)
        {
            _descriptionText.text =
                "F키를 빠르게 연타해 결승선을 향해 달리세요!\n" +
                $"{definition.OneStarTargetCount} / {definition.TwoStarTargetCount} / {definition.ThreeStarTargetCount}회 = 1 / 2 / 3성";
        }

        float distance = GetRunnerProgress01() * definition.ThreeStarTargetCount * _distancePerMash;
        float targetDistance = definition.ThreeStarTargetCount * _distancePerMash;
        int stars = GetDistanceStarGrade(definition);
        if (_progressText != null)
        {
            _progressText.text = $"거리  {distance:0}m / {targetDistance:0}m\n연타  {_buttonMashCount}회";
        }

        if (_timerText != null)
        {
            _timerText.text = $"남은 시간  {_roundRemainingSeconds:0.0}초";
        }

        if (_gradeText != null)
        {
            _gradeText.text = stars switch
            {
                3 => "★★★  완주!",
                2 => "★★☆  질주 중!",
                1 => "★☆☆  조금 더!",
                _ => "☆☆☆  출발!",
            };
        }
    }

    private void ResetRunnerAndCamera()
    {
        if (_runnerRoot == null)
        {
            return;
        }

        _runnerStartPosition = _runnerRoot.position;
        _runnerTargetPosition = _runnerStartPosition;
        _cameraSwayStrength = 0f;
        if (_followCamera != null)
        {
            _followCamera.position = _runnerStartPosition + _cameraFollowOffset;
            _followCamera.rotation = Quaternion.Euler(_cameraFollowEulerAngles);
        }
    }

    private void SetRunnerProgress(float progress, bool snap)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        float maxDistance = Definition != null ? Definition.ThreeStarTargetCount * _distancePerMash : 0f;
        _runnerTargetPosition = _runnerStartPosition + (Vector3.right * maxDistance * clampedProgress);
        if (snap && _runnerRoot != null)
        {
            _runnerRoot.position = _runnerTargetPosition;
        }
    }

    private void MoveRunnerAndCamera()
    {
        if (_runnerRoot == null)
        {
            return;
        }

        _runnerRoot.position = Vector3.Lerp(_runnerRoot.position, _runnerTargetPosition, Time.deltaTime * _moveLerpSpeed);
        if (_followCamera == null)
        {
            return;
        }

        Vector3 cameraTargetPosition = _runnerRoot.position + _cameraFollowOffset;
        _followCamera.position = Vector3.Lerp(_followCamera.position, cameraTargetPosition, Time.deltaTime * _cameraLerpSpeed);
        _followCamera.rotation = Quaternion.Euler(_cameraFollowEulerAngles);
        ApplyCameraSway();
    }

    private void AddCameraSway()
    {
        _cameraSwayStrength = Mathf.Min(1f, _cameraSwayStrength + 0.28f);
    }

    private void ApplyCameraSway()
    {
        if (_cameraSwayStrength <= 0f)
        {
            return;
        }

        float smoothStrength = Mathf.SmoothStep(0f, 1f, _cameraSwayStrength);
        float phase = Time.time * 5.5f;
        float horizontal = Mathf.Sin(phase) * _cameraSwayPositionAmount * smoothStrength;
        float vertical = Mathf.Cos(phase * 0.7f) * _cameraSwayPositionAmount * 0.35f * smoothStrength;
        _followCamera.position += (_followCamera.right * horizontal) + (_followCamera.up * vertical);
        _followCamera.rotation *= Quaternion.Euler(-vertical * _cameraSwayRotationAmount * 8f, horizontal * _cameraSwayRotationAmount * 7f, 0f);
        _cameraSwayStrength = Mathf.MoveTowards(_cameraSwayStrength, 0f, _cameraSwayDecaySpeed * Time.deltaTime);
    }

    private float GetProgress01(MinigameDefinition definition)
    {
        if (definition == null || definition.ThreeStarTargetCount <= 0)
        {
            return 0f;
        }

        return _buttonMashCount / (float)definition.ThreeStarTargetCount;
    }

    private float GetRunnerProgress01()
    {
        if (_runnerRoot == null || Definition == null)
        {
            return GetProgress01(Definition);
        }

        float maxDistance = Definition.ThreeStarTargetCount * _distancePerMash;
        if (maxDistance <= 0.001f)
        {
            return 0f;
        }

        return Mathf.Clamp01(Vector3.Dot(_runnerRoot.position - _runnerStartPosition, Vector3.right) / maxDistance);
    }

    private int GetDistanceStarGrade(MinigameDefinition definition)
    {
        if (definition == null || definition.ThreeStarTargetCount <= 0)
        {
            return 0;
        }

        float progress = GetRunnerProgress01();
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

        return progress >= oneStarProgress ? 1 : 0;
    }

    private void SetCompleteButtonActive(bool active)
    {
        if (_completeButton != null)
        {
            _completeButton.gameObject.SetActive(active);
        }
    }
}

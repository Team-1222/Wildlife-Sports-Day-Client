using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// F키 연타로 북극곰과 카메라를 전진시키고, 결승선 통과 시간으로 점수를 계산하는 미니게임 컨트롤러입니다.
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
    [SerializeField] private Animator _runnerAnimator;
    [SerializeField] private RuntimeAnimatorController _runnerAnimatorController;
    [SerializeField] private Transform _followCamera;
    [SerializeField] private Vector3 _cameraFollowOffset = new(0.5f, 5.85f, -15f);
    [SerializeField] private Vector3 _cameraFollowEulerAngles = new(20f, 25f, 0f);
    [SerializeField, Min(0.1f)] private float _roundDurationSeconds = 6f;
    [SerializeField, Min(0.1f)] private float _threePointCompletionSeconds = 2.5f;
    [SerializeField, Min(0.1f)] private float _twoPointCompletionSeconds = 4.5f;
    [SerializeField, Min(0.1f)] private float _distancePerMash = 1.4f;
    [SerializeField, Min(0.1f)] private float _runnerMoveSpeed = 16f;
    [SerializeField, Min(0.1f)] private float _walkSpeedAtNormalAnimation = 4f;
    [SerializeField, Min(0.1f)] private float _maximumAnimationSpeed = 4.5f;
    [SerializeField, Min(0.1f)] private float _animationAccelerationPerSecond = 24f;
    [SerializeField, Min(0.1f)] private float _animationDecelerationPerSecond = 5f;
    [SerializeField, Min(0.1f)] private float _animationReturnSpeed = 0.65f;
    [SerializeField, Min(0f)] private float _cameraSwayPositionAmount = 0.06f;
    [SerializeField, Min(0f)] private float _cameraSwayRotationAmount = 0.45f;
    [SerializeField, Min(0.01f)] private float _cameraSwayDecaySpeed = 3.5f;

    private int _buttonMashCount;
    private Vector3 _runnerStartPosition;
    private Vector3 _runnerTargetPosition;
    private float _roundRemainingSeconds;
    private float _cameraSwayStrength;
    private float _animationSpeed;
    private bool _isRunnerAnimationAtRestPose;
    private bool _isWaitingForAnimationLoopEnd;
    private int _animationStopStartLoop;

    protected override void OnMinigameStarted()
    {
        _buttonMashCount = 0;
        _roundRemainingSeconds = _roundDurationSeconds;
        _animationSpeed = 0f;
        _isWaitingForAnimationLoopEnd = false;
        SetUpRunnerAnimation();
        ResetRunnerAnimationToFirstFrame();
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
        if (HasReachedFinishLine())
        {
            CompleteMinigame(GetCompletionPoint(GetElapsedSeconds()));
            return;
        }

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
        SetRunnerProgress(GetProgress01(definition));
    }

    private void TickRoundTimer()
    {
        _roundRemainingSeconds = Mathf.Max(0f, _roundRemainingSeconds - Time.deltaTime);
        if (_roundRemainingSeconds > 0f)
        {
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
                $"{_threePointCompletionSeconds:0.0}초 / {_twoPointCompletionSeconds:0.0}초 / {_roundDurationSeconds:0.0}초 이내 완주 = 3 / 2 / 1점";
        }

        float distance = GetRunnerProgress01() * definition.ThreeStarTargetCount * _distancePerMash;
        float targetDistance = definition.ThreeStarTargetCount * _distancePerMash;
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
            _gradeText.text = "결승선까지 질주!";
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

    private void SetRunnerProgress(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        float maxDistance = Definition != null ? Definition.ThreeStarTargetCount * _distancePerMash : 0f;
        _runnerTargetPosition = _runnerStartPosition + (Vector3.right * maxDistance * clampedProgress);
    }

    private void MoveRunnerAndCamera()
    {
        if (_runnerRoot == null)
        {
            return;
        }

        Vector3 previousRunnerPosition = _runnerRoot.position;
        _runnerRoot.position = Vector3.MoveTowards(
            previousRunnerPosition,
            _runnerTargetPosition,
            _runnerMoveSpeed * Time.deltaTime);
        float runnerSpeed = Vector3.Distance(_runnerRoot.position, previousRunnerPosition) / Time.deltaTime;
        UpdateRunnerAnimation(runnerSpeed);

        if (_followCamera == null)
        {
            return;
        }

        Vector3 cameraTargetPosition = _runnerRoot.position + _cameraFollowOffset;
        _followCamera.position = cameraTargetPosition;
        _followCamera.rotation = Quaternion.Euler(_cameraFollowEulerAngles);
        ApplyCameraSway();
    }

    private bool HasReachedFinishLine()
    {
        MinigameDefinition definition = Definition;
        if (_runnerRoot == null || definition == null || _buttonMashCount < definition.ThreeStarTargetCount)
        {
            return false;
        }

        return Vector3.Distance(_runnerRoot.position, _runnerTargetPosition) <= 0.001f;
    }

    private void UpdateRunnerAnimation(float runnerSpeed)
    {
        if (runnerSpeed <= 0.001f)
        {
            CompleteCurrentAnimationLoop();
            return;
        }

        _isRunnerAnimationAtRestPose = false;
        _isWaitingForAnimationLoopEnd = false;
        float targetAnimationSpeed = Mathf.Clamp(
            runnerSpeed / _walkSpeedAtNormalAnimation,
            0f,
            _maximumAnimationSpeed);
        float animationSpeedChangeRate = targetAnimationSpeed > _animationSpeed
            ? _animationAccelerationPerSecond
            : _animationDecelerationPerSecond;
        _animationSpeed = Mathf.MoveTowards(
            _animationSpeed,
            targetAnimationSpeed,
            animationSpeedChangeRate * Time.deltaTime);
        SetRunnerAnimationSpeed();
    }

    private void CompleteCurrentAnimationLoop()
    {
        if (_isRunnerAnimationAtRestPose)
        {
            return;
        }

        if (!_isWaitingForAnimationLoopEnd)
        {
            _isWaitingForAnimationLoopEnd = true;
            _animationStopStartLoop = GetAnimatorLoopCount();
        }

        _animationSpeed = Mathf.MoveTowards(
            _animationSpeed,
            _animationReturnSpeed,
            _animationDecelerationPerSecond * Time.deltaTime);
        SetRunnerAnimationSpeed();

        if (GetAnimatorLoopCount() > _animationStopStartLoop)
        {
            _animationSpeed = 0f;
            StopRunnerAnimationAtLoopStart();
        }
    }

    private void SetRunnerAnimationSpeed()
    {
        if (_runnerAnimator != null)
        {
            _runnerAnimator.speed = _animationSpeed;
        }
    }

    private void ResetRunnerAnimationToFirstFrame()
    {
        if (_runnerAnimator == null)
        {
            return;
        }

        _runnerAnimator.Rebind();
        _runnerAnimator.Play(0, 0, 0f);
        _runnerAnimator.Update(0f);
        _runnerAnimator.speed = 0f;
        _isRunnerAnimationAtRestPose = true;
        _isWaitingForAnimationLoopEnd = false;
    }

    private void StopRunnerAnimationAtLoopStart()
    {
        if (_runnerAnimator == null)
        {
            return;
        }

        _runnerAnimator.speed = 0f;
        _isRunnerAnimationAtRestPose = true;
        _isWaitingForAnimationLoopEnd = false;
    }

    private int GetAnimatorLoopCount()
    {
        return _runnerAnimator != null
            ? Mathf.FloorToInt(_runnerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime)
            : 0;
    }

    private void SetUpRunnerAnimation()
    {
        if (_runnerAnimator == null && _runnerRoot != null)
        {
            _runnerAnimator = _runnerRoot.GetComponentInChildren<Animator>();
        }

        if (_runnerAnimator != null && _runnerAnimatorController != null)
        {
            _runnerAnimator.runtimeAnimatorController = _runnerAnimatorController;
        }
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

    private float GetElapsedSeconds()
    {
        return _roundDurationSeconds - _roundRemainingSeconds;
    }

    private int GetCompletionPoint(float elapsedSeconds)
    {
        if (elapsedSeconds <= _threePointCompletionSeconds)
        {
            return 3;
        }

        if (elapsedSeconds <= _twoPointCompletionSeconds)
        {
            return 2;
        }

        return 1;
    }

    private void SetCompleteButtonActive(bool active)
    {
        if (_completeButton != null)
        {
            _completeButton.gameObject.SetActive(active);
        }
    }
}

using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class UiScreen : MonoBehaviour
{
    public enum ScreenState
    {
        Closed,
        Opening,
        Opened,
        Closing,
    }

    public enum AnimationPlayMode
    {
        Simultaneous,
        RegisteredOrder,
    }

    public enum ScreenTransitionMode
    {
        CloseThenOpen,
        Simultaneous,
        Instant,
    }

    public enum AnimationTargetType
    {
        Animation,
        Screen,
    }

    [Serializable]
    private sealed class AnimationTarget
    {
        [SerializeField] private AnimationTargetType _type;
        [SerializeField] private UiAnimation _animation;
        [SerializeField] private UiScreen _screen;

        public AnimationTargetType Type => _type;
        public UiAnimation Animation => _animation;
        public UiScreen Screen => _screen;

        private AnimationTarget()
        {
        }

        public AnimationTarget(UiAnimation animation)
        {
            _type = AnimationTargetType.Animation;
            _animation = animation;
        }

        public AnimationTarget(UiScreen screen)
        {
            _type = AnimationTargetType.Screen;
            _screen = screen;
        }
    }

    [SerializeField] private bool _startOpened;
    [SerializeField] private bool _playOpenAnimationOnStart;
    [SerializeField] private bool _deactivateOnClosed = true;
    [SerializeField] private bool _blockInputDuringTransition = true;
    [SerializeField] private bool _collectChildAnimationsOnAwake = true;
    [SerializeField] private bool _collectChildScreensOnAwake = true;

#pragma warning disable CS0414
    [SerializeField, HideInInspector] private AnimationPlayMode _openPlayMode = AnimationPlayMode.Simultaneous;
    [SerializeField, HideInInspector] private AnimationPlayMode _closePlayMode = AnimationPlayMode.Simultaneous;
#pragma warning restore CS0414
    [SerializeField] private bool _reverseCloseOrder = true;
    [SerializeField] private bool _waitForPreviousTarget;
    [SerializeField, Min(0f)] private float _animationInterval = 0.05f;
    [SerializeField] private List<AnimationTarget> _animationTargets = new();
    [SerializeField] private List<UiAnimation> _animations = new();

    [SerializeField] private bool _syncChildAnimationsFromSource;
    [SerializeField] private UiAnimation _animationSyncSource;

    [SerializeField] private UnityEvent _opened;
    [SerializeField] private UnityEvent _closed;

    private CanvasGroup _canvasGroup;
    private Sequence _sequence;
    private bool _isOpening;
    private bool _startStateApplied;

    public ScreenState State { get; private set; } = ScreenState.Closed;
    public bool IsTransitioning => State == ScreenState.Opening || State == ScreenState.Closing;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        if (_collectChildAnimationsOnAwake || _collectChildScreensOnAwake)
        {
            CollectChildAnimations();
        }
    }

    private void OnEnable()
    {
        ApplyStartStateIfNeeded();
    }

    private void OnDisable()
    {
        KillSequence();
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    public void CollectChildAnimations()
    {
        _animations.Clear();
        _animationTargets.Clear();

        CollectTargetsInHierarchy(transform);
    }

    public List<UiAnimation> GetChildAnimations()
    {
        List<UiAnimation> animations = new();
        GetComponentsInChildren(true, animations);
        animations.RemoveAll(animation => animation == null || animation.gameObject == gameObject);
        return animations;
    }

    public List<UiAnimation> GetChildAnimationsExcept(UiAnimation excludedAnimation)
    {
        List<UiAnimation> animations = GetChildAnimations();
        animations.RemoveAll(animation => animation == excludedAnimation);
        return animations;
    }

    public int ApplyAnimationSyncSourceToChildAnimations()
    {
        if (_animationSyncSource == null)
        {
            return 0;
        }

        List<UiAnimation> animations = GetChildAnimationsExcept(_animationSyncSource);
        for (int i = 0; i < animations.Count; i++)
        {
            animations[i].CopySettingsFrom(_animationSyncSource);
        }

        return animations.Count;
    }

    public Tween Open(bool useAnimation = true)
    {
        if (State == ScreenState.Opening || State == ScreenState.Opened)
        {
            return _sequence;
        }

        _isOpening = true;
        gameObject.SetActive(true);
        _isOpening = false;

        KillSequence();
        State = ScreenState.Opening;
        ApplyVisibleState();
        SetInputEnabled(!_blockInputDuringTransition);

        if (!useAnimation)
        {
            CompleteOpen();
            return null;
        }

        ApplyShowStartStates();
        _sequence = BuildSequence(false, true)
            .OnComplete(CompleteOpen);

        return _sequence;
    }

    public Tween Close(bool useAnimation = true)
    {
        if (State == ScreenState.Closing || State == ScreenState.Closed)
        {
            return _sequence;
        }

        KillSequence();
        State = ScreenState.Closing;
        SetInputEnabled(false);

        if (!useAnimation)
        {
            CompleteClose();
            return null;
        }

        _sequence = BuildSequence(_reverseCloseOrder, false)
            .OnComplete(CompleteClose);

        return _sequence;
    }

    public Tween ChangeTo(UiScreen targetScreen, ScreenTransitionMode transitionMode = ScreenTransitionMode.CloseThenOpen, bool useAnimation = true)
    {
        if (targetScreen == null || targetScreen == this)
        {
            return null;
        }

        switch (transitionMode)
        {
            case ScreenTransitionMode.Simultaneous:
                Tween closeTween = Close(useAnimation);
                Tween openTween = targetScreen.Open(useAnimation);
                return CreateTransitionWaitTween(closeTween, openTween, true);

            case ScreenTransitionMode.Instant:
                Close(false);
                targetScreen.Open(false);
                return null;

            default:
                Tween closeThenOpen = Close(useAnimation);
                float closeDuration = GetTweenDuration(closeThenOpen);
                float openDuration = useAnimation ? targetScreen.GetEstimatedOpenDuration() : 0f;
                Sequence waitSequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
                waitSequence.AppendInterval(closeDuration);
                waitSequence.AppendCallback(() => targetScreen.Open(useAnimation));
                waitSequence.AppendInterval(openDuration);
                return waitSequence;
        }
    }

    private Sequence BuildSequence(bool reverseOrder, bool isOpen)
    {
        List<AnimationTarget> orderedTargets = GetOrderedTargets(reverseOrder);
        Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        if (orderedTargets.Count == 0)
        {
            sequence.AppendInterval(0f);
            return sequence;
        }

        if (_waitForPreviousTarget)
        {
            float startTime = 0f;
            for (int i = 0; i < orderedTargets.Count; i++)
            {
                AnimationTarget target = orderedTargets[i];
                sequence.InsertCallback(startTime, () => PlayTarget(target, isOpen));
                startTime += GetTargetDuration(target, isOpen);

                if (i < orderedTargets.Count - 1)
                {
                    startTime += _animationInterval;
                }
            }
        }
        else
        {
            for (int i = 0; i < orderedTargets.Count; i++)
            {
                AnimationTarget target = orderedTargets[i];
                sequence.InsertCallback(_animationInterval * i, () => PlayTarget(target, isOpen));
            }
        }

        sequence.AppendInterval(GetSequenceDuration(orderedTargets, isOpen));
        return sequence;
    }

    private List<AnimationTarget> GetOrderedTargets(bool reverseOrder)
    {
        List<AnimationTarget> orderedTargets = new();

        if (_animationTargets.Count > 0)
        {
            orderedTargets.AddRange(_animationTargets);
            orderedTargets.RemoveAll(target => target == null || !HasValidTarget(target));
        }
        else
        {
            List<UiAnimation> fallbackAnimations = GetOrderedAnimations(reverseOrder: false);
            for (int i = 0; i < fallbackAnimations.Count; i++)
            {
                orderedTargets.Add(new AnimationTarget(fallbackAnimations[i]));
            }
        }

        if (reverseOrder)
        {
            orderedTargets.Reverse();
        }

        return orderedTargets;
    }

    private List<UiAnimation> GetOrderedAnimations(bool reverseOrder)
    {
        List<UiAnimation> orderedAnimations = new(_animations);
        orderedAnimations.RemoveAll(animation => animation == null);

        if (reverseOrder)
        {
            orderedAnimations.Reverse();
        }

        return orderedAnimations;
    }

    private void ApplyShowStartStates()
    {
        List<AnimationTarget> targets = GetOrderedTargets(false);
        for (int i = 0; i < targets.Count; i++)
        {
            AnimationTarget target = targets[i];
            if (target.Type == AnimationTargetType.Animation && target.Animation != null)
            {
                target.Animation.ApplyShowStartState();
            }
        }
    }

    private void CompleteOpen()
    {
        State = ScreenState.Opened;
        ApplyVisibleState();
        SetInputEnabled(true);
        _opened?.Invoke();
    }

    private void CompleteClose()
    {
        State = ScreenState.Closed;
        ApplyClosedState();
        _closed?.Invoke();
    }

    private void SetInputEnabled(bool enabled)
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        _canvasGroup.interactable = enabled;
        _canvasGroup.blocksRaycasts = enabled;
    }

    private void ApplyStartStateIfNeeded()
    {
        if (_startStateApplied || _isOpening)
        {
            return;
        }

        _startStateApplied = true;

        if (_startOpened)
        {
            if (_playOpenAnimationOnStart)
            {
                State = ScreenState.Closed;
                Open(true);
                return;
            }

            CompleteOpen();
            return;
        }

        State = ScreenState.Closed;
        ApplyClosedState();
    }

    private void ApplyVisibleState()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        _canvasGroup.alpha = 1f;
    }

    private void ApplyClosedState()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        KillSequence();
        _canvasGroup.alpha = 0f;
        SetInputEnabled(false);

        if (_deactivateOnClosed)
        {
            gameObject.SetActive(false);
        }
    }

    private void KillSequence()
    {
        if (_sequence != null && _sequence.IsActive())
        {
            _sequence.Kill();
        }

        _sequence = null;
    }

    private static void PlayAnimation(UiAnimation animation, bool isOpen)
    {
        if (animation == null)
        {
            return;
        }

        if (isOpen)
        {
            animation.PlayShow();
        }
        else
        {
            animation.PlayHide();
        }
    }

    private static void PlayTarget(AnimationTarget target, bool isOpen)
    {
        if (target == null)
        {
            return;
        }

        if (target.Type == AnimationTargetType.Screen)
        {
            if (isOpen)
            {
                target.Screen?.Open();
            }
            else
            {
                target.Screen?.Close();
            }

            return;
        }

        PlayAnimation(target.Animation, isOpen);
    }

    private static Tween CreateTransitionWaitTween(Tween firstTween, Tween secondTween, bool useLongestDuration)
    {
        float firstDuration = GetTweenDuration(firstTween);
        float secondDuration = GetTweenDuration(secondTween);
        float duration = useLongestDuration ? Mathf.Max(firstDuration, secondDuration) : firstDuration + secondDuration;

        return duration > 0f ? DOVirtual.DelayedCall(duration, () => { }) : null;
    }

    private static float GetTweenDuration(Tween tween)
    {
        return tween != null && tween.IsActive() ? tween.Duration(true) : 0f;
    }

    private float GetEstimatedOpenDuration()
    {
        List<AnimationTarget> orderedTargets = GetOrderedTargets(false);
        return GetSequenceDuration(orderedTargets, true);
    }

    private float GetEstimatedCloseDuration()
    {
        List<AnimationTarget> orderedTargets = GetOrderedTargets(_reverseCloseOrder);
        return GetSequenceDuration(orderedTargets, false);
    }

    private static float GetAnimationDuration(UiAnimation animation)
    {
        return animation != null ? animation.Duration + animation.Delay : 0f;
    }

    private float GetSequenceDuration(IReadOnlyList<AnimationTarget> targets, bool isOpen)
    {
        if (_waitForPreviousTarget)
        {
            return GetSequentialDuration(targets, isOpen);
        }

        float duration = 0f;

        for (int i = 0; i < targets.Count; i++)
        {
            float startTime = _animationInterval * i;
            duration = Mathf.Max(duration, startTime + GetTargetDuration(targets[i], isOpen));
        }

        return duration;
    }

    private float GetSequentialDuration(IReadOnlyList<AnimationTarget> targets, bool isOpen)
    {
        float duration = 0f;

        for (int i = 0; i < targets.Count; i++)
        {
            duration += GetTargetDuration(targets[i], isOpen);

            if (i < targets.Count - 1)
            {
                duration += _animationInterval;
            }
        }

        return duration;
    }

    private static float GetTargetDuration(AnimationTarget target, bool isOpen)
    {
        if (target == null)
        {
            return 0f;
        }

        if (target.Type == AnimationTargetType.Screen)
        {
            if (target.Screen == null)
            {
                return 0f;
            }

            return isOpen ? target.Screen.GetEstimatedOpenDuration() : target.Screen.GetEstimatedCloseDuration();
        }

        return GetAnimationDuration(target.Animation);
    }

    private static bool HasValidTarget(AnimationTarget target)
    {
        return target.Type == AnimationTargetType.Screen ? target.Screen != null : target.Animation != null;
    }

    private void CollectTargetsInHierarchy(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (_collectChildScreensOnAwake && child.TryGetComponent(out UiScreen childScreen) && childScreen != this)
            {
                _animationTargets.Add(new AnimationTarget(childScreen));
                continue;
            }

            if (_collectChildAnimationsOnAwake && child.TryGetComponent(out UiAnimation animation))
            {
                _animations.Add(animation);
                _animationTargets.Add(new AnimationTarget(animation));
            }

            CollectTargetsInHierarchy(child);
        }
    }
}

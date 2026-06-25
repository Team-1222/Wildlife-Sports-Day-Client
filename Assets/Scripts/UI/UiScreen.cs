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

    [SerializeField] private bool _startOpened;
    [SerializeField] private bool _playOpenAnimationOnStart;
    [SerializeField] private bool _deactivateOnClosed = true;
    [SerializeField] private bool _blockInputDuringTransition = true;
    [SerializeField] private bool _collectChildAnimationsOnAwake = true;

    [SerializeField] private AnimationPlayMode _openPlayMode = AnimationPlayMode.Simultaneous;
    [SerializeField] private AnimationPlayMode _closePlayMode = AnimationPlayMode.Simultaneous;
    [SerializeField] private bool _reverseCloseOrder = true;
    [SerializeField, Min(0f)] private float _animationInterval = 0.05f;
    [SerializeField] private List<UiAnimation> _animations = new();

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

        if (_collectChildAnimationsOnAwake)
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
        GetComponentsInChildren(true, _animations);
        _animations.RemoveAll(animation => animation == null || animation.gameObject == gameObject);
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
        _sequence = BuildSequence(_openPlayMode, false, true)
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

        _sequence = BuildSequence(_closePlayMode, _reverseCloseOrder, false)
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

    private Sequence BuildSequence(AnimationPlayMode playMode, bool reverseOrder, bool isOpen)
    {
        List<UiAnimation> orderedAnimations = GetOrderedAnimations(playMode, reverseOrder);
        Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        if (orderedAnimations.Count == 0)
        {
            sequence.AppendInterval(0f);
            return sequence;
        }

        if (playMode == AnimationPlayMode.Simultaneous)
        {
            foreach (UiAnimation animation in orderedAnimations)
            {
                sequence.AppendCallback(() => PlayAnimation(animation, isOpen));
            }

            sequence.AppendInterval(GetLongestAnimationDuration(orderedAnimations));
            return sequence;
        }

        for (int i = 0; i < orderedAnimations.Count; i++)
        {
            UiAnimation animation = orderedAnimations[i];
            sequence.AppendCallback(() => PlayAnimation(animation, isOpen));
            sequence.AppendInterval(GetAnimationDuration(animation));

            if (_animationInterval > 0f && i < orderedAnimations.Count - 1)
            {
                sequence.AppendInterval(_animationInterval);
            }
        }

        return sequence;
    }

    private List<UiAnimation> GetOrderedAnimations(AnimationPlayMode playMode, bool reverseOrder)
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
        for (int i = 0; i < _animations.Count; i++)
        {
            if (_animations[i] != null)
            {
                _animations[i].ApplyShowStartState();
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
        List<UiAnimation> orderedAnimations = GetOrderedAnimations(_openPlayMode, false);

        if (_openPlayMode == AnimationPlayMode.Simultaneous)
        {
            return GetLongestAnimationDuration(orderedAnimations);
        }

        float duration = 0f;

        for (int i = 0; i < orderedAnimations.Count; i++)
        {
            duration += GetAnimationDuration(orderedAnimations[i]);

            if (_animationInterval > 0f && i < orderedAnimations.Count - 1)
            {
                duration += _animationInterval;
            }
        }

        return duration;
    }

    private static float GetLongestAnimationDuration(IReadOnlyList<UiAnimation> animations)
    {
        float duration = 0f;

        for (int i = 0; i < animations.Count; i++)
        {
            duration = Mathf.Max(duration, GetAnimationDuration(animations[i]));
        }

        return duration;
    }

    private static float GetAnimationDuration(UiAnimation animation)
    {
        return animation != null ? animation.Duration + animation.Delay : 0f;
    }
}

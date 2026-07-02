using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class UiAnimation : MonoBehaviour
{
    [Serializable]
    private sealed class AnimationSettings
    {
        [SerializeField] private bool _useFade;
        [SerializeField] private float _fromAlpha;
        [SerializeField] private float _toAlpha = 1f;

        [SerializeField] private bool _useScale;
        [SerializeField] private bool _scaleFromSavedState = true;
        [SerializeField] private Vector3 _fromScale = Vector3.zero;
        [SerializeField] private Vector3 _toScale = Vector3.one;

        [SerializeField] private bool _useMove;
        [SerializeField] private bool _moveFromSavedState = true;
        [SerializeField] private MovePreset _movePreset = MovePreset.Custom;
        [SerializeField] private Vector2 _fromPosition;
        [SerializeField] private Vector2 _toPosition;
        [SerializeField] private Vector2 _offset = new(0f, 80f);

        public bool UseFade => _useFade;
        public float FromAlpha => _fromAlpha;
        public float ToAlpha => _toAlpha;
        public bool UseScale => _useScale;
        public bool ScaleFromSavedState => _scaleFromSavedState;
        public Vector3 FromScale => _fromScale;
        public Vector3 ToScale => _toScale;
        public bool UseMove => _useMove;
        public bool MoveFromSavedState => _moveFromSavedState;
        public MovePreset MovePreset => _movePreset;
        public Vector2 FromPosition => _fromPosition;
        public Vector2 ToPosition => _toPosition;
        public Vector2 Offset => _offset;

        public void CopyFrom(AnimationSettings source)
        {
            if (source == null)
            {
                return;
            }

            _useFade = source._useFade;
            _fromAlpha = source._fromAlpha;
            _toAlpha = source._toAlpha;

            _useScale = source._useScale;
            _scaleFromSavedState = source._scaleFromSavedState;
            _fromScale = source._fromScale;
            _toScale = source._toScale;

            _useMove = source._useMove;
            _movePreset = source._movePreset;
            _offset = source._offset;

            if (source._movePreset == MovePreset.Custom)
            {
                _moveFromSavedState = source._moveFromSavedState;
                _fromPosition = source._fromPosition;
                _toPosition = source._toPosition;
                return;
            }

            _moveFromSavedState = true;
        }
    }

    public enum MovePreset
    {
        Left,
        Right,
        Up,
        Down,
        Custom,
    }

    [SerializeField, Min(0f)] private float _duration = 0.25f;
    [SerializeField, Min(0f)] private float _delay;
    [SerializeField] private Ease _ease = Ease.OutQuad;
    [SerializeField] private bool _useUnscaledTime = true;
    [SerializeField] private bool _useSameSettingsForShowAndHide = true;

    [SerializeField] private AnimationSettings _showSettings = new();
    [SerializeField] private UnityEvent _showCompleted;

    [SerializeField] private AnimationSettings _hideSettings = new();
    [SerializeField] private UnityEvent _hideCompleted;

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Sequence _sequence;
    private Vector2 _savedAnchoredPosition;
    private Vector3 _savedScale;
    private float _savedAlpha = 1f;
    private bool _hasSavedState;

    public bool IsPlaying => _sequence != null && _sequence.IsActive() && _sequence.IsPlaying();
    public float Duration => _duration;
    public float Delay => _delay;

    public void CopySettingsFrom(UiAnimation source)
    {
        if (source == null || source == this)
        {
            return;
        }

        _duration = source._duration;
        _delay = source._delay;
        _ease = source._ease;
        _useUnscaledTime = source._useUnscaledTime;
        _useSameSettingsForShowAndHide = source._useSameSettingsForShowAndHide;
        _showSettings.CopyFrom(source._showSettings);
        _hideSettings.CopyFrom(source._hideSettings);
    }

    private void Awake()
    {
        CacheComponents();
        SaveInitialState();
    }

    private void OnDisable()
    {
        Kill();
    }

    private void OnDestroy()
    {
        Kill();
    }

    public Tween PlayShow()
    {
        return Play(_showSettings, _showCompleted, false);
    }

    public Tween PlayHide()
    {
        return _useSameSettingsForShowAndHide
            ? Play(_showSettings, _hideCompleted, true)
            : Play(_hideSettings, _hideCompleted, false);
    }

    public void ApplyShowStartState()
    {
        CacheComponents();

        if (!_hasSavedState)
        {
            SaveInitialState();
        }

        Kill();
        ApplyStartState(_showSettings);
    }

    public void SaveInitialState()
    {
        CacheComponents();

        _savedAnchoredPosition = _rectTransform.anchoredPosition;
        _savedScale = _rectTransform.localScale;
        _savedAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;
        _hasSavedState = true;
    }

    public void RestoreInitialState()
    {
        if (!_hasSavedState)
        {
            SaveInitialState();
        }

        Kill();
        _rectTransform.anchoredPosition = _savedAnchoredPosition;
        _rectTransform.localScale = _savedScale;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = _savedAlpha;
        }
    }

    public void Complete()
    {
        if (_sequence != null && _sequence.IsActive())
        {
            _sequence.Complete();
        }
    }

    public void Kill()
    {
        if (_sequence != null && _sequence.IsActive())
        {
            _sequence.Kill();
        }

        _sequence = null;
    }

    private Tween Play(AnimationSettings settings, UnityEvent completed, bool reverse)
    {
        CacheComponents();

        if (!_hasSavedState)
        {
            SaveInitialState();
        }

        Kill();

        _sequence = DOTween.Sequence()
            .SetUpdate(_useUnscaledTime)
            .SetDelay(_delay)
            .SetEase(_ease)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        ApplyStartState(settings, reverse);
        AppendTweens(settings, reverse);

        _sequence.OnComplete(() => completed?.Invoke());
        return _sequence;
    }

    private void ApplyStartState(AnimationSettings settings, bool reverse = false)
    {
        if (settings.UseFade)
        {
            EnsureCanvasGroup();
            _canvasGroup.alpha = reverse ? settings.ToAlpha : settings.FromAlpha;
        }

        if (settings.UseScale)
        {
            _rectTransform.localScale = reverse
                ? settings.ToScale
                : (settings.ScaleFromSavedState ? _savedScale : settings.FromScale);
        }

        if (settings.UseMove)
        {
            _rectTransform.anchoredPosition = ResolveMovePosition(settings, true, reverse);
        }
    }

    private void AppendTweens(AnimationSettings settings, bool reverse)
    {
        bool hasTween = false;

        if (settings.UseFade)
        {
            EnsureCanvasGroup();
            _sequence.Join(_canvasGroup.DOFade(reverse ? settings.FromAlpha : settings.ToAlpha, _duration));
            hasTween = true;
        }

        if (settings.UseScale)
        {
            Vector3 targetScale = reverse
                ? (settings.ScaleFromSavedState ? _savedScale : settings.FromScale)
                : settings.ToScale;
            _sequence.Join(_rectTransform.DOScale(targetScale, _duration));
            hasTween = true;
        }

        if (settings.UseMove)
        {
            _sequence.Join(_rectTransform.DOAnchorPos(ResolveMovePosition(settings, false, reverse), _duration));
            hasTween = true;
        }

        if (!hasTween)
        {
            _sequence.AppendInterval(0f);
        }
    }

    private Vector2 ResolveMovePosition(AnimationSettings settings, bool isStart, bool reverse = false)
    {
        if (settings.MovePreset == MovePreset.Custom)
        {
            return reverse
                ? (isStart ? settings.ToPosition : settings.FromPosition)
                : (isStart ? settings.FromPosition : settings.ToPosition);
        }

        Vector2 basePosition = settings.MoveFromSavedState ? _savedAnchoredPosition : settings.ToPosition;
        Vector2 offset = settings.MovePreset switch
        {
            MovePreset.Left => Vector2.left * Mathf.Abs(settings.Offset.x),
            MovePreset.Right => Vector2.right * Mathf.Abs(settings.Offset.x),
            MovePreset.Up => Vector2.up * Mathf.Abs(settings.Offset.y),
            MovePreset.Down => Vector2.down * Mathf.Abs(settings.Offset.y),
            _ => settings.Offset,
        };

        return reverse
            ? (isStart ? basePosition : basePosition + offset)
            : (isStart ? basePosition + offset : basePosition);
    }

    private void CacheComponents()
    {
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
}

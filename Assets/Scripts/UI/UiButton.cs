using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[AddComponentMenu("UI/Ui Button")]
public sealed class UiButton : Button
{
    public enum ButtonActionType
    {
        None,
        OpenScreen,
        CloseScreen,
        ChangeScreen,
        LoadScene,
        ReloadScene,
        QuitGame,
        SetGameObjectActive,
        OpenUrl,
        InvokeEvent,
    }

    [Serializable]
    private sealed class InteractionMotionSettings
    {
        [SerializeField, Min(0f)] private float _duration = 0.12f;
        [SerializeField] private Ease _ease = Ease.OutQuad;
        [SerializeField, Min(0f)] private float _hoverScale = 1.05f;
        [SerializeField, Min(0f)] private float _pressedScale = 0.96f;
        [SerializeField, Range(0f, 1f)] private float _disabledAlpha = 0.45f;
        [SerializeField] private Vector2 _hoverOffset;
        [SerializeField] private Vector2 _pressedOffset = new(0f, -2f);
        [SerializeField] private bool _useClickPunch = true;
        [SerializeField] private Vector3 _clickPunchScale = new(0.08f, 0.08f, 0f);
        [SerializeField, Min(0f)] private float _clickPunchDuration = 0.18f;

        public float Duration => _duration;
        public Ease Ease => _ease;
        public float HoverScale => _hoverScale;
        public float PressedScale => _pressedScale;
        public float DisabledAlpha => _disabledAlpha;
        public Vector2 HoverOffset => _hoverOffset;
        public Vector2 PressedOffset => _pressedOffset;
        public bool UseClickPunch => _useClickPunch;
        public Vector3 ClickPunchScale => _clickPunchScale;
        public float ClickPunchDuration => _clickPunchDuration;
    }

    [SerializeField, HideInInspector] private AudioSource _audioSource;
    [SerializeField, HideInInspector] private AudioClip _pointerEnterSound;
    [SerializeField, HideInInspector] private AudioClip _pointerDownSound;
    [SerializeField, HideInInspector] private AudioClip _clickSound;

    [SerializeField, HideInInspector] private bool _useInteractionMotion = true;
    [SerializeField, HideInInspector] private RectTransform _motionTarget;
    [SerializeField, HideInInspector] private bool _useUnscaledMotionTime = true;
    [SerializeField, HideInInspector] private InteractionMotionSettings _interactionMotion = new();

    [SerializeField, HideInInspector] private ButtonActionType _clickAction = ButtonActionType.None;
    [SerializeField, HideInInspector] private UiScreen _currentScreen;
    [SerializeField, HideInInspector] private UiScreen _targetScreen;
    [SerializeField, HideInInspector] private UiScreen.ScreenTransitionMode _transitionMode = UiScreen.ScreenTransitionMode.CloseThenOpen;
    [SerializeField, HideInInspector] private bool _useTransitionAnimation = true;
    [SerializeField, HideInInspector] private bool _blockClickDuringAction = true;
    [SerializeField, HideInInspector] private string _sceneName;
    [SerializeField, HideInInspector] private LoadSceneMode _loadSceneMode = LoadSceneMode.Single;
    [SerializeField, HideInInspector] private GameObject _targetGameObject;
    [SerializeField, HideInInspector] private bool _setActiveValue = true;
    [SerializeField, HideInInspector] private string _url;
    [SerializeField, HideInInspector] private UnityEvent _actionCompleted;

    private Tween _interactionMotionTween;
    private Tween _clickMotionTween;
    private CanvasGroup _motionCanvasGroup;
    private Vector2 _savedMotionPosition;
    private Vector3 _savedMotionScale = Vector3.one;
    private float _savedMotionAlpha = 1f;
    private float _interactionScale = 1f;
    private float _interactionAlpha = 1f;
    private Vector2 _interactionOffset;
    private Vector3 _clickScaleOffset;
    private bool _hasSavedMotionState;
    private bool _isPointerInside;
    private bool _isPointerDown;
    private bool _isActionRunning;

    protected override void Awake()
    {
        base.Awake();

        if (_audioSource == null)
        {
            _audioSource = GetComponentInParent<AudioSource>();
        }

        SaveMotionStateIfNeeded();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SaveMotionStateIfNeeded();
        ApplyCurrentMotionState(instant: true);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        RestoreInteractionMotion();
        _isPointerInside = false;
        _isPointerDown = false;
        _isActionRunning = false;
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);

        if (!IsInteractable())
        {
            return;
        }

        _isPointerInside = true;
        PlaySound(_pointerEnterSound);
        ApplyCurrentMotionState(instant: false);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        if (!IsInteractable())
        {
            return;
        }

        _isPointerInside = false;
        _isPointerDown = false;
        ApplyCurrentMotionState(instant: false);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);

        if (!IsInteractable())
        {
            return;
        }

        _isPointerDown = true;
        PlaySound(_pointerDownSound);
        ApplyCurrentMotionState(instant: false);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        if (!IsInteractable())
        {
            return;
        }

        _isPointerDown = false;
        ApplyCurrentMotionState(instant: false);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (_blockClickDuringAction && _isActionRunning)
        {
            return;
        }

        base.OnPointerClick(eventData);

        if (!IsInteractable())
        {
            return;
        }

        PlaySound(_clickSound);
        PlayClickMotion();
        ExecuteClickAction();
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        ApplyCurrentMotionState(instant);
    }

    private void ApplyCurrentMotionState(bool instant)
    {
        if (!_useInteractionMotion || !Application.isPlaying)
        {
            return;
        }

        SaveMotionStateIfNeeded();

        RectTransform target = GetMotionTarget();
        if (target == null)
        {
            return;
        }

        float scaleMultiplier = 1f;
        Vector2 offset = Vector2.zero;
        float alpha = _savedMotionAlpha;

        if (!IsInteractable())
        {
            alpha = _interactionMotion.DisabledAlpha;
        }
        else if (_isPointerDown)
        {
            scaleMultiplier = _interactionMotion.PressedScale;
            offset = _interactionMotion.PressedOffset;
        }
        else if (_isPointerInside)
        {
            scaleMultiplier = _interactionMotion.HoverScale;
            offset = _interactionMotion.HoverOffset;
        }

        if (instant)
        {
            _interactionScale = scaleMultiplier;
            _interactionOffset = offset;
            _interactionAlpha = alpha;
            ApplyComposedMotion();
            return;
        }

        KillInteractionMotion();

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(_useUnscaledMotionTime)
            .SetEase(_interactionMotion.Ease)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        sequence.Join(DOTween.To(() => _interactionScale, value =>
        {
            _interactionScale = value;
            ApplyComposedMotion();
        }, scaleMultiplier, _interactionMotion.Duration));

        sequence.Join(DOTween.To(() => _interactionOffset, value =>
        {
            _interactionOffset = value;
            ApplyComposedMotion();
        }, offset, _interactionMotion.Duration));

        sequence.Join(DOTween.To(() => _interactionAlpha, value =>
        {
            _interactionAlpha = value;
            ApplyComposedMotion();
        }, alpha, _interactionMotion.Duration));

        _interactionMotionTween = sequence;
    }

    private void PlayClickMotion()
    {
        if (!_useInteractionMotion || !_interactionMotion.UseClickPunch || GetMotionTarget() == null)
        {
            return;
        }

        KillClickMotion();

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(_useUnscaledMotionTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        sequence.Append(DOTween.To(() => _clickScaleOffset, value =>
        {
            _clickScaleOffset = value;
            ApplyComposedMotion();
        }, _interactionMotion.ClickPunchScale, _interactionMotion.ClickPunchDuration * 0.45f).SetEase(Ease.OutQuad));

        sequence.Append(DOTween.To(() => _clickScaleOffset, value =>
        {
            _clickScaleOffset = value;
            ApplyComposedMotion();
        }, Vector3.zero, _interactionMotion.ClickPunchDuration * 0.55f).SetEase(Ease.OutBack));

        sequence.OnComplete(() =>
        {
            _clickMotionTween = null;
            _clickScaleOffset = Vector3.zero;
            ApplyComposedMotion();
        });

        _clickMotionTween = sequence;
    }

    private void SaveMotionStateIfNeeded()
    {
        if (_hasSavedMotionState)
        {
            return;
        }

        RectTransform target = GetMotionTarget();
        if (target == null)
        {
            return;
        }

        _savedMotionPosition = target.anchoredPosition;
        _savedMotionScale = target.localScale;
        _motionCanvasGroup = target.GetComponent<CanvasGroup>();
        _savedMotionAlpha = _motionCanvasGroup != null ? _motionCanvasGroup.alpha : 1f;
        _interactionAlpha = _savedMotionAlpha;
        _hasSavedMotionState = true;
    }

    private void RestoreInteractionMotion()
    {
        if (!_useInteractionMotion || !_hasSavedMotionState)
        {
            return;
        }

        RectTransform target = GetMotionTarget();
        if (target == null)
        {
            return;
        }

        KillInteractionMotion();
        KillClickMotion();
        _interactionScale = 1f;
        _interactionAlpha = _savedMotionAlpha;
        _interactionOffset = Vector2.zero;
        _clickScaleOffset = Vector3.zero;
        target.anchoredPosition = _savedMotionPosition;
        target.localScale = _savedMotionScale;

        if (_motionCanvasGroup != null)
        {
            _motionCanvasGroup.alpha = _savedMotionAlpha;
        }
    }

    private void ApplyComposedMotion()
    {
        RectTransform target = GetMotionTarget();
        if (target == null || !_hasSavedMotionState)
        {
            return;
        }

        target.localScale = (_savedMotionScale * _interactionScale) + _clickScaleOffset;
        target.anchoredPosition = _savedMotionPosition + _interactionOffset;

        if (_interactionAlpha < _savedMotionAlpha || EnsureMotionCanvasGroupIfExists())
        {
            EnsureMotionCanvasGroup();
            _motionCanvasGroup.alpha = _interactionAlpha;
        }
    }

    private void ExecuteClickAction()
    {
        Tween screenTween = _clickAction switch
        {
            ButtonActionType.OpenScreen => _targetScreen != null ? _targetScreen.Open(_useTransitionAnimation) : null,
            ButtonActionType.CloseScreen => _currentScreen != null ? _currentScreen.Close(_useTransitionAnimation) : null,
            ButtonActionType.ChangeScreen => _currentScreen != null ? _currentScreen.ChangeTo(_targetScreen, _transitionMode, _useTransitionAnimation) : null,
            _ => null,
        };

        if (screenTween != null)
        {
            _isActionRunning = true;
            StartCoroutine(WaitForAction(screenTween));
            return;
        }

        ExecuteInstantClickAction();
    }

    private void ExecuteInstantClickAction()
    {
        switch (_clickAction)
        {
            case ButtonActionType.LoadScene:
                LoadConfiguredScene();
                return;
            case ButtonActionType.ReloadScene:
                _actionCompleted?.Invoke();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            case ButtonActionType.QuitGame:
                _actionCompleted?.Invoke();
                Application.Quit();
                return;
            case ButtonActionType.SetGameObjectActive:
                if (_targetGameObject != null)
                {
                    _targetGameObject.SetActive(_setActiveValue);
                }
                break;
            case ButtonActionType.OpenUrl:
                if (!string.IsNullOrWhiteSpace(_url))
                {
                    Application.OpenURL(_url);
                }
                break;
        }

        _actionCompleted?.Invoke();
    }

    private void LoadConfiguredScene()
    {
        if (string.IsNullOrWhiteSpace(_sceneName))
        {
            _actionCompleted?.Invoke();
            return;
        }

        _actionCompleted?.Invoke();
        SceneManager.LoadScene(_sceneName, _loadSceneMode);
    }

    private IEnumerator WaitForAction(Tween actionTween)
    {
        yield return actionTween.WaitForCompletion();

        _isActionRunning = false;
        _actionCompleted?.Invoke();
    }

    private void KillInteractionMotion()
    {
        if (_interactionMotionTween != null && _interactionMotionTween.IsActive())
        {
            _interactionMotionTween.Kill();
        }

        _interactionMotionTween = null;
    }

    private void KillClickMotion()
    {
        if (_clickMotionTween != null && _clickMotionTween.IsActive())
        {
            _clickMotionTween.Kill();
        }

        _clickMotionTween = null;
    }

    private RectTransform GetMotionTarget()
    {
        return _motionTarget != null ? _motionTarget : transform as RectTransform;
    }

    private bool EnsureMotionCanvasGroupIfExists()
    {
        if (_motionCanvasGroup != null)
        {
            return true;
        }

        RectTransform target = GetMotionTarget();
        if (target == null)
        {
            return false;
        }

        _motionCanvasGroup = target.GetComponent<CanvasGroup>();
        return _motionCanvasGroup != null;
    }

    private void EnsureMotionCanvasGroup()
    {
        if (EnsureMotionCanvasGroupIfExists())
        {
            return;
        }

        RectTransform target = GetMotionTarget();
        if (target != null)
        {
            _motionCanvasGroup = target.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (_audioSource == null || clip == null)
        {
            return;
        }

        _audioSource.PlayOneShot(clip);
    }
}

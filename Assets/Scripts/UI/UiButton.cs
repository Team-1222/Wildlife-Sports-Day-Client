using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/Ui Button")]
public sealed class UiButton : Button
{
    public enum ScreenAction
    {
        None,
        Open,
        Close,
        Change,
    }

    [SerializeField, HideInInspector] private AudioSource _audioSource;
    [SerializeField, HideInInspector] private AudioClip _pointerEnterSound;
    [SerializeField, HideInInspector] private AudioClip _pointerDownSound;
    [SerializeField, HideInInspector] private AudioClip _clickSound;

    [SerializeField, HideInInspector] private bool _useInteractionAnimations = true;
    [SerializeField, HideInInspector] private UiAnimation _pointerEnterAnimation;
    [SerializeField, HideInInspector] private UiAnimation _pointerExitAnimation;
    [SerializeField, HideInInspector] private UiAnimation _pointerDownAnimation;
    [SerializeField, HideInInspector] private UiAnimation _pointerUpAnimation;
    [SerializeField, HideInInspector] private UiAnimation _clickAnimation;
    [SerializeField, HideInInspector] private UiAnimation _disabledAnimation;

    [SerializeField, HideInInspector] private ScreenAction _screenAction = ScreenAction.None;
    [SerializeField, HideInInspector] private UiScreen _currentScreen;
    [SerializeField, HideInInspector] private UiScreen _targetScreen;
    [SerializeField, HideInInspector] private UiScreen.ScreenTransitionMode _transitionMode = UiScreen.ScreenTransitionMode.CloseThenOpen;
    [SerializeField, HideInInspector] private bool _useTransitionAnimation = true;
    [SerializeField, HideInInspector] private bool _blockClickDuringTransition = true;
    [SerializeField, HideInInspector] private UnityEvent _transitionCompleted;

    private Tween _activeAnimation;
    private bool _isTransitioning;

    protected override void Awake()
    {
        base.Awake();

        if (_audioSource == null)
        {
            _audioSource = GetComponentInParent<AudioSource>();
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        RestoreAnimation(_pointerEnterAnimation);
        RestoreAnimation(_pointerExitAnimation);
        RestoreAnimation(_pointerDownAnimation);
        RestoreAnimation(_pointerUpAnimation);
        RestoreAnimation(_clickAnimation);
        RestoreAnimation(_disabledAnimation);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);

        if (!IsInteractable())
        {
            return;
        }

        PlaySound(_pointerEnterSound);
        PlayShowAnimation(_pointerEnterAnimation);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        if (!IsInteractable())
        {
            return;
        }

        PlayHideAnimation(_pointerExitAnimation);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);

        if (!IsInteractable())
        {
            return;
        }

        PlaySound(_pointerDownSound);
        PlayShowAnimation(_pointerDownAnimation);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        if (!IsInteractable())
        {
            return;
        }

        PlayHideAnimation(_pointerUpAnimation);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (_blockClickDuringTransition && _isTransitioning)
        {
            return;
        }

        base.OnPointerClick(eventData);

        if (!IsInteractable())
        {
            return;
        }

        PlaySound(_clickSound);
        PlayShowAnimation(_clickAnimation);
        ExecuteScreenAction();
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);

        if (state == SelectionState.Disabled)
        {
            PlayShowAnimation(_disabledAnimation);
        }
    }

    private void ExecuteScreenAction()
    {
        Tween transitionTween = _screenAction switch
        {
            ScreenAction.Open => _targetScreen != null ? _targetScreen.Open(_useTransitionAnimation) : null,
            ScreenAction.Close => _currentScreen != null ? _currentScreen.Close(_useTransitionAnimation) : null,
            ScreenAction.Change => _currentScreen != null ? _currentScreen.ChangeTo(_targetScreen, _transitionMode, _useTransitionAnimation) : null,
            _ => null,
        };

        if (transitionTween == null)
        {
            _transitionCompleted?.Invoke();
            return;
        }

        _isTransitioning = true;
        StartCoroutine(WaitForTransition(transitionTween));
    }

    private void PlayShowAnimation(UiAnimation animation)
    {
        if (!_useInteractionAnimations || animation == null)
        {
            return;
        }

        if (_activeAnimation != null && _activeAnimation.IsActive())
        {
            _activeAnimation.Kill();
        }

        _activeAnimation = animation.PlayShow();
    }

    private void PlayHideAnimation(UiAnimation animation)
    {
        if (!_useInteractionAnimations || animation == null)
        {
            return;
        }

        if (_activeAnimation != null && _activeAnimation.IsActive())
        {
            _activeAnimation.Kill();
        }

        _activeAnimation = animation.PlayHide();
    }

    private void RestoreAnimation(UiAnimation animation)
    {
        if (animation != null)
        {
            animation.RestoreInitialState();
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

    private IEnumerator WaitForTransition(Tween transitionTween)
    {
        yield return transitionTween.WaitForCompletion();

        _isTransitioning = false;
        _transitionCompleted?.Invoke();
    }
}

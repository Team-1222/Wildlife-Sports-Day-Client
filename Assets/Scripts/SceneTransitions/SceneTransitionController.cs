using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬 로드 전후에 전체 화면 셰이더 기반 타일 전환을 표시합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SceneTransitionController : MonoBehaviour
{
    private const string MaterialAddress = "UI/SceneTransitionIrisNoiseMaterial";
    private const string ProgressProperty = "_Progress";

    private static SceneTransitionController _instance;

    [SerializeField, Min(0f)] private float _fadeOutSeconds = 0.45f;
    [SerializeField, Min(0f)] private float _coveredHoldSeconds = 0.05f;
    [SerializeField, Min(0)] private int _sceneLoadedHoldFrames = 2;
    [SerializeField, Min(0f)] private float _sceneLoadedHoldSeconds = 0.03f;
    [SerializeField, Min(0f)] private float _fadeInSeconds = 0.5f;

    private Material _material;
    private CanvasGroup _canvasGroup;
    private Image _transitionImage;
    private AsyncOperationHandle<Material> _materialLoadHandle;
    private bool _isMaterialLoading;
    private bool _isTransitioning;

    public static bool IsTransitioning => _instance != null && _instance._isTransitioning;

    public static void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        EnsureInstance().StartLoad(sceneName, mode);
    }

    private static SceneTransitionController EnsureInstance()
    {
        if (_instance != null)
        {
            return _instance;
        }

        GameObject root = new("SceneTransitionController");
        _instance = root.AddComponent<SceneTransitionController>();
        DontDestroyOnLoad(root);
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
        SetProgress(0f);
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        if (_material != null)
        {
            Destroy(_material);
        }

        if (_materialLoadHandle.IsValid())
        {
            Addressables.Release(_materialLoadHandle);
        }
    }

    private void StartLoad(string sceneName, LoadSceneMode mode)
    {
        if (_isTransitioning)
        {
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName, mode));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode mode)
    {
        _isTransitioning = true;
        yield return EnsureMaterialLoaded();
        SetVisible(true);

        yield return AnimateProgress(0f, 1f, _fadeOutSeconds);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, mode);
        if (loadOperation == null)
        {
            Debug.LogError($"Failed to start async scene load: {sceneName}");
            SetVisible(false);
            _isTransitioning = false;
            yield break;
        }

        loadOperation.allowSceneActivation = false;
        while (loadOperation.progress < 0.9f)
        {
            yield return null;
        }

        if (_coveredHoldSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(_coveredHoldSeconds);
        }

        loadOperation.allowSceneActivation = true;
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        for (int i = 0; i < _sceneLoadedHoldFrames; i++)
        {
            yield return null;
        }

        if (_sceneLoadedHoldSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(_sceneLoadedHoldSeconds);
        }

        yield return AnimateProgress(1f, 0f, _fadeInSeconds);

        SetVisible(false);
        _isTransitioning = false;
    }

    private IEnumerator AnimateProgress(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetProgress(to);
            yield break;
        }

        float elapsedSeconds = 0f;
        while (elapsedSeconds < duration)
        {
            elapsedSeconds += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedSeconds / duration);
            float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
            SetProgress(Mathf.Lerp(from, to, easedTime));
            yield return null;
        }

        SetProgress(to);
    }

    private void BuildOverlay()
    {
        if (_canvasGroup != null)
        {
            return;
        }

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        gameObject.AddComponent<GraphicRaycaster>();

        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        GameObject imageObject = new("TransitionImage");
        imageObject.transform.SetParent(transform, false);

        RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        _transitionImage = imageObject.AddComponent<Image>();
        _transitionImage.raycastTarget = true;
        _transitionImage.color = Color.white;
    }

    /// <summary>
    /// Addressables의 전환 머티리얼을 로드하고 오버레이 이미지에 런타임 복제본을 적용합니다.
    /// </summary>
    private IEnumerator EnsureMaterialLoaded()
    {
        if (_material != null)
        {
            yield break;
        }

        if (!_isMaterialLoading)
        {
            _isMaterialLoading = true;
            _materialLoadHandle = Addressables.LoadAssetAsync<Material>(MaterialAddress);
        }

        yield return _materialLoadHandle;
        _isMaterialLoading = false;

        if (_materialLoadHandle.Status != AsyncOperationStatus.Succeeded || _materialLoadHandle.Result == null)
        {
            Debug.LogError($"Addressable scene transition material not found: {MaterialAddress}");
            yield break;
        }

        _material = new Material(_materialLoadHandle.Result)
        {
            name = "SceneTransitionIrisNoiseRuntime"
        };

        if (_transitionImage != null)
        {
            _transitionImage.material = _material;
        }

        SetProgress(0f);
    }

    private void SetProgress(float progress)
    {
        if (_material == null)
        {
            return;
        }

        _material.SetFloat(ProgressProperty, Mathf.Clamp01(progress));
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.blocksRaycasts = visible;
    }
}

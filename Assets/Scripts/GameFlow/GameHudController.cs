using TMPro;
using UnityEngine;

/// <summary>
/// 전체 게임 런 동안 유지되는 점수/남은 시간 HUD를 화면 위에 표시합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameHudController : MonoBehaviour
{
    private const string HudPrefabResourcePath = "UI/GameHudCanvas";

    private static GameHudController _instance;

    [SerializeField] private TMP_Text _scoreValueText;
    [SerializeField] private TMP_Text _timerValueText;

    /// <summary>
    /// HUD 프리팹을 생성하거나 기존 인스턴스를 다시 표시합니다.
    /// </summary>
    public static void Show()
    {
        EnsureInstance();
        if (_instance == null)
        {
            return;
        }

        _instance.gameObject.SetActive(true);
        _instance.Refresh();
    }

    /// <summary>
    /// HUD 인스턴스를 파괴하지 않고 화면에서만 숨깁니다.
    /// </summary>
    public static void Hide()
    {
        if (_instance != null)
        {
            _instance.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 게임 런이 끝났을 때 유지 중인 HUD 인스턴스를 제거합니다.
    /// </summary>
    public static void DestroyHud()
    {
        if (_instance == null)
        {
            return;
        }

        Destroy(_instance.gameObject);
        _instance = null;
    }

    /// <summary>
    /// 중복 HUD를 제거하고 씬 전환 중에도 유지되는 HUD로 설정합니다.
    /// </summary>
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        NormalizeCanvasRoot();
        gameObject.SetActive(GameLoopSession.IsActive && !GameLoopSession.IsResultRequested);
        Refresh();
    }

    /// <summary>
    /// 파괴 시 정적 인스턴스를 정리합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// 전체 게임 런이 살아 있는 동안 점수와 전체 남은 시간을 갱신합니다.
    /// </summary>
    private void Update()
    {
        if (!GameLoopSession.IsActive || GameLoopSession.IsResultRequested)
        {
            gameObject.SetActive(false);
            return;
        }

        Refresh();
    }

    /// <summary>
    /// Resources 폴더의 HUD 프리팹을 로드해 인스턴스를 만듭니다.
    /// </summary>
    private static void EnsureInstance()
    {
        if (_instance != null)
        {
            return;
        }

        GameHudController prefab = Resources.Load<GameHudController>(HudPrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogError($"Game HUD prefab not found at Resources/{HudPrefabResourcePath}.");
            return;
        }

        Instantiate(prefab);
    }

    /// <summary>
    /// 프리팹 루트 RectTransform이 런타임에서 0 스케일이나 잘못된 앵커로 생성되지 않게 보정합니다.
    /// </summary>
    private void NormalizeCanvasRoot()
    {
        if (transform is RectTransform rectTransform)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    /// <summary>
    /// HUD 텍스트에 현재 점수와 전체 게임 남은 시간을 반영합니다.
    /// </summary>
    private void Refresh()
    {
        if (_scoreValueText != null)
        {
            _scoreValueText.text = $"현재 점수 {GameLoopSession.Score}점";
        }

        if (_timerValueText != null)
        {
            int remaining = Mathf.Max(0, Mathf.CeilToInt(GameLoopSession.RemainingSeconds));
            int minutes = remaining / 60;
            int seconds = remaining % 60;
            _timerValueText.text = $"남은 시간 {minutes:00}:{seconds:00}";
        }
    }
}

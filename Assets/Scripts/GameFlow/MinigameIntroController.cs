using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 미니게임 씬 진입 직후 잠시 표시되는 공통 인트로 패널을 제어합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MinigameIntroController : MonoBehaviour
{
    private const string IntroPrefabResourcePath = "UI/MinigameIntroCanvas";

    [SerializeField, Min(0f)] private float _durationSeconds = 1.5f;
    [SerializeField] private TMP_Text _mainText;
    [SerializeField] private TMP_Text _subText;

    private Action _closed;
    private float _elapsedSeconds;
    private bool _isInitialized;
    private bool _isClosed;

    /// <summary>
    /// 인트로 프리팹을 생성하고 닫힐 때 호출할 콜백을 등록합니다.
    /// </summary>
    public static void Show(Action closed)
    {
        MinigameIntroController prefab = Resources.Load<MinigameIntroController>(IntroPrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogError($"Minigame intro prefab not found at Resources/{IntroPrefabResourcePath}.");
            closed?.Invoke();
            return;
        }

        MinigameIntroController instance = Instantiate(prefab);
        instance.Initialize(closed);
    }

    /// <summary>
    /// 닫힘 콜백을 보관하고 패널 표시 시간을 초기화합니다.
    /// </summary>
    private void Initialize(Action closed)
    {
        _closed = closed;
        _elapsedSeconds = 0f;
        _isClosed = false;
        _isInitialized = true;
        RefreshTexts();
    }

    /// <summary>
    /// 설정된 시간이 지나면 패널을 닫습니다.
    /// </summary>
    private void Update()
    {
        if (!_isInitialized || _isClosed)
        {
            return;
        }

        _elapsedSeconds += Time.unscaledDeltaTime;
        if (_elapsedSeconds >= _durationSeconds)
        {
            Close();
        }
    }

    /// <summary>
    /// 닫힘 콜백을 한 번만 호출하고 인트로 패널을 제거합니다.
    /// </summary>
    private void Close()
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        Action closed = _closed;
        _closed = null;
        closed?.Invoke();
        Destroy(gameObject);
    }

    /// <summary>
    /// 현재 선택된 미니게임 정의 값을 인트로 텍스트에 반영합니다.
    /// </summary>
    private void RefreshTexts()
    {
        MinigameDefinition definition = GameLoopSession.CurrentMinigameDefinition;
        string mainText = GameLoopSession.CurrentMinigameName;
        string subText = GameLoopSession.CurrentMinigameDescription;

        if (definition != null)
        {
            mainText = definition.DisplayName;
            subText = string.IsNullOrWhiteSpace(definition.Objective)
                ? definition.Description
                : definition.Objective;
        }

        if (_mainText != null)
        {
            _mainText.text = mainText;
        }

        if (_subText != null)
        {
            _subText.text = subText;
        }
    }
}

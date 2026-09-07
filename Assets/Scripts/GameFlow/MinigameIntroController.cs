using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 미니게임 씬 진입 직후 잠시 표시되는 공통 인트로 패널을 제어합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MinigameIntroController : MonoBehaviour
{
    private const string IntroPrefabAddress = "UI/MinigameIntroCanvas";

    private static readonly List<Action> PendingClosedCallbacks = new();

    private static AsyncOperationHandle<GameObject> _introPrefabLoadHandle;
    private static bool _isIntroPrefabLoading;

    [SerializeField, Min(0f)] private float _durationSeconds = 1.5f;
    [SerializeField] private TMP_Text _mainText;
    [SerializeField] private TMP_Text _subText;

    private Action _closed;
    private float _elapsedSeconds;
    private bool _isInitialized;
    private bool _isClosed;

    /// <summary>
    /// Addressables에 등록된 인트로 프리팹을 생성하고 닫힐 때 호출할 콜백을 등록합니다.
    /// </summary>
    public static void Show(Action closed)
    {
        if (_introPrefabLoadHandle.IsValid() && _introPrefabLoadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            CreateInstance(_introPrefabLoadHandle.Result, closed);
            return;
        }

        PendingClosedCallbacks.Add(closed);
        if (_isIntroPrefabLoading)
        {
            return;
        }

        _isIntroPrefabLoading = true;
        _introPrefabLoadHandle = Addressables.LoadAssetAsync<GameObject>(IntroPrefabAddress);
        _introPrefabLoadHandle.Completed += HandleIntroPrefabLoaded;
    }

    /// <summary>
    /// 인트로 프리팹 로드가 끝나면 대기 중인 표시 요청을 처리합니다.
    /// </summary>
    private static void HandleIntroPrefabLoaded(AsyncOperationHandle<GameObject> handle)
    {
        _isIntroPrefabLoading = false;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Debug.LogError($"Addressable minigame intro prefab not found: {IntroPrefabAddress}");
            InvokePendingClosedCallbacks();
            return;
        }

        foreach (Action closed in PendingClosedCallbacks)
        {
            CreateInstance(handle.Result, closed);
        }

        PendingClosedCallbacks.Clear();
    }

    /// <summary>
    /// 로드된 프리팹으로 인트로 인스턴스를 생성합니다.
    /// </summary>
    private static void CreateInstance(GameObject prefab, Action closed)
    {
        if (prefab == null)
        {
            closed?.Invoke();
            return;
        }

        MinigameIntroController controller = prefab.GetComponent<MinigameIntroController>();
        if (controller == null)
        {
            Debug.LogError($"Addressable minigame intro prefab has no {nameof(MinigameIntroController)}: {IntroPrefabAddress}");
            closed?.Invoke();
            return;
        }

        MinigameIntroController instance = Instantiate(controller);
        instance.Initialize(closed);
    }

    /// <summary>
    /// 프리팹 로드에 실패했을 때 대기 중인 흐름을 멈추지 않게 합니다.
    /// </summary>
    private static void InvokePendingClosedCallbacks()
    {
        foreach (Action closed in PendingClosedCallbacks)
        {
            closed?.Invoke();
        }

        PendingClosedCallbacks.Clear();
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

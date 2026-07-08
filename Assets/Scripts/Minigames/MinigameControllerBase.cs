using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 모든 미니게임 컨트롤러가 공유하는 전체 런 시간 갱신과 씬 전환 흐름을 제공합니다.
/// </summary>
[DisallowMultipleComponent]
public abstract class MinigameControllerBase : MonoBehaviour
{
    private bool _isIntroFinished;
    private bool _isEnabled;

    /// <summary>
    /// 현재 세션에서 선택된 미니게임 정의 에셋입니다.
    /// </summary>
    protected MinigameDefinition Definition => GameLoopSession.CurrentMinigameDefinition;

    /// <summary>
    /// 미니게임 오브젝트가 활성화될 때 공통 인트로 패널을 먼저 표시합니다.
    /// </summary>
    protected virtual void OnEnable()
    {
        _isEnabled = true;
        _isIntroFinished = false;
        MinigameIntroController.Show(HandleIntroClosed);
    }

    /// <summary>
    /// 비활성화된 컨트롤러에서 늦게 도착한 인트로 콜백이 시작 로직을 실행하지 않게 합니다.
    /// </summary>
    protected virtual void OnDisable()
    {
        _isEnabled = false;
    }

    /// <summary>
    /// 전체 게임 제한시간을 갱신하고, 세션 상태가 유효할 때만 하위 미니게임 로직을 실행합니다.
    /// </summary>
    protected virtual void Update()
    {
        if (!_isIntroFinished)
        {
            return;
        }

        GameLoopSession.Tick(Time.deltaTime);
        if (GameLoopSession.IsResultRequested || !GameLoopSession.IsPlayingMinigame)
        {
            LoadNextScene();
            return;
        }

        TickMinigame();
    }

    /// <summary>
    /// 현재 미니게임을 별 등급으로 정산하고 다음 씬으로 넘어갑니다.
    /// </summary>
    protected void CompleteMinigame(int stars)
    {
        GameLoopSession.CompleteCurrentMinigame(stars);
        LoadNextScene();
    }

    /// <summary>
    /// 현재 미니게임을 실패 처리하고 다음 씬으로 넘어갑니다.
    /// </summary>
    protected void FailMinigame()
    {
        GameLoopSession.FailCurrentMinigame();
        LoadNextScene();
    }

    /// <summary>
    /// 미니게임 씬 진입 시 필요한 초기화를 하위 컨트롤러에서 구현합니다.
    /// </summary>
    protected abstract void OnMinigameStarted();

    /// <summary>
    /// 미니게임별 입력, 진행도, 표시 갱신을 하위 컨트롤러에서 구현합니다.
    /// </summary>
    protected abstract void TickMinigame();

    /// <summary>
    /// 인트로 패널이 닫힌 뒤 실제 미니게임 시작 처리를 실행합니다.
    /// </summary>
    private void HandleIntroClosed()
    {
        if (!_isEnabled)
        {
            return;
        }

        _isIntroFinished = true;
        OnMinigameStarted();
    }

    /// <summary>
    /// 세션이 고른 다음 미니게임 또는 결과 씬을 로드합니다.
    /// </summary>
    private static void LoadNextScene()
    {
        SceneManager.LoadScene(GameLoopSession.GetNextSceneNameOrResult(), LoadSceneMode.Single);
    }
}

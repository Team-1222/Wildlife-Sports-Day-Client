using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 모든 미니게임 컨트롤러가 공유하는 전체 런 시간 갱신과 씬 전환 흐름을 제공합니다.
/// </summary>
[DisallowMultipleComponent]
public abstract class MinigameControllerBase : MonoBehaviour
{
    /// <summary>
    /// 현재 세션에서 선택된 미니게임 정의 에셋입니다.
    /// </summary>
    protected MinigameDefinition Definition => GameLoopSession.CurrentMinigameDefinition;

    /// <summary>
    /// 미니게임 오브젝트가 활성화될 때 하위 컨트롤러의 시작 처리를 호출합니다.
    /// </summary>
    protected virtual void OnEnable()
    {
        OnMinigameStarted();
    }

    /// <summary>
    /// 전체 게임 제한시간을 갱신하고, 세션 상태가 유효할 때만 하위 미니게임 로직을 실행합니다.
    /// </summary>
    protected virtual void Update()
    {
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
    /// 세션이 고른 다음 미니게임 또는 결과 씬을 로드합니다.
    /// </summary>
    private static void LoadNextScene()
    {
        SceneManager.LoadScene(GameLoopSession.GetNextSceneNameOrResult(), LoadSceneMode.Single);
    }
}

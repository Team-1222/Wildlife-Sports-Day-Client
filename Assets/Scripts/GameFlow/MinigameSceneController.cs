using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 별도 전용 컨트롤러가 없는 미니게임 씬에서 기본 완료 흐름을 처리합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MinigameSceneController : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private Button _completeButton;

    /// <summary>
    /// 완료 버튼 이벤트를 연결하고 현재 미니게임 정보를 표시합니다.
    /// </summary>
    private void OnEnable()
    {
        if (_completeButton != null)
        {
            _completeButton.onClick.AddListener(CompleteMinigame);
        }

        RefreshTexts();
    }

    /// <summary>
    /// 씬이 비활성화될 때 완료 버튼 이벤트를 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        if (_completeButton != null)
        {
            _completeButton.onClick.RemoveListener(CompleteMinigame);
        }
    }

    /// <summary>
    /// 전체 게임 제한시간을 갱신하고, 현재 미니게임이 끝났는지 확인합니다.
    /// </summary>
    private void Update()
    {
        GameLoopSession.Tick(Time.deltaTime);
        if (GameLoopSession.IsResultRequested)
        {
            LoadNextScene();
            return;
        }

        if (!GameLoopSession.IsPlayingMinigame)
        {
            LoadNextScene();
            return;
        }

        RefreshTexts();
    }

    /// <summary>
    /// 선택된 미니게임 정보를 씬 UI에 반영합니다.
    /// </summary>
    private void RefreshTexts()
    {
        if (!GameLoopSession.IsPlayingMinigame)
        {
            return;
        }

        if (_titleText != null)
        {
            _titleText.text = GameLoopSession.CurrentMinigameName;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = GameLoopSession.CurrentMinigameDescription;
        }

        if (_progressText != null)
        {
            _progressText.text = "목표를 완료하면 다음 미니게임으로 넘어갑니다.";
        }
    }

    /// <summary>
    /// 현재 미니게임을 기본 3성 완료로 정산하고 다음 씬으로 넘어갑니다.
    /// </summary>
    private void CompleteMinigame()
    {
        GameLoopSession.CompleteCurrentMinigame();
        LoadNextScene();
    }

    /// <summary>
    /// 세션이 고른 다음 미니게임 또는 결과 씬을 로드합니다.
    /// </summary>
    private static void LoadNextScene()
    {
        SceneManager.LoadScene(GameLoopSession.GetNextSceneNameOrResult(), LoadSceneMode.Single);
    }
}

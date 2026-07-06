using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MinigameSceneController : MonoBehaviour
{
    [SerializeField] private string _selectionSceneName = "MinigameSelectScene";
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private Button _completeButton;

    private void OnEnable()
    {
        if (_completeButton != null)
        {
            _completeButton.onClick.AddListener(CompleteMinigame);
        }

        RefreshTexts();
    }

    private void OnDisable()
    {
        if (_completeButton != null)
        {
            _completeButton.onClick.RemoveListener(CompleteMinigame);
        }
    }

    private void Update()
    {
        GameLoopSession.Tick(Time.deltaTime);
        if (GameLoopSession.IsResultRequested)
        {
            SceneManager.LoadScene(_selectionSceneName, LoadSceneMode.Single);
            return;
        }

        if (!GameLoopSession.IsPlayingMinigame)
        {
            SceneManager.LoadScene(_selectionSceneName, LoadSceneMode.Single);
            return;
        }

        if (GameLoopSession.CurrentMinigameRemainingSeconds <= 0f)
        {
            CompleteMinigame();
            return;
        }

        RefreshTexts();
    }

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
            _progressText.text = $"남은 시간: {Mathf.CeilToInt(GameLoopSession.CurrentMinigameRemainingSeconds)}초";
        }
    }

    private void CompleteMinigame()
    {
        GameLoopSession.CompleteCurrentMinigame();
        SceneManager.LoadScene(_selectionSceneName, LoadSceneMode.Single);
    }
}

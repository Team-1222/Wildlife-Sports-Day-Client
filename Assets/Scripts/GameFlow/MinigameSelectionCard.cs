using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니게임 선택 룰렛에 표시되는 카드 한 장의 텍스트와 강조 상태를 갱신합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MinigameSelectionCard : MonoBehaviour
{
    [SerializeField] private Image _panelImage;
    [SerializeField] private Image _accentImage;
    [SerializeField] private Image _selectionFrameImage;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private CanvasGroup _canvasGroup;

    /// <summary>
    /// 카드에 미니게임 정의 정보를 표시합니다.
    /// </summary>
    public void SetDefinition(MinigameDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        if (_titleText != null)
        {
            _titleText.text = definition.DisplayName;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = string.IsNullOrWhiteSpace(definition.Objective)
                ? definition.Description
                : definition.Objective;
        }

        Color accentColor = GetAccentColor(definition.Difficulty);
        if (_panelImage != null)
        {
            _panelImage.color = new Color(accentColor.r * 0.22f, accentColor.g * 0.22f, accentColor.b * 0.28f, 0.96f);
        }

        if (_accentImage != null)
        {
            _accentImage.color = accentColor;
        }
    }

    /// <summary>
    /// 중앙 선택선에 가까운 카드가 도드라지도록 투명도와 테두리를 갱신합니다.
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        if (_selectionFrameImage != null)
        {
            _selectionFrameImage.enabled = highlighted;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = highlighted ? 1f : 0.64f;
        }

        transform.localScale = highlighted ? Vector3.one * 1.04f : Vector3.one;
    }

    /// <summary>
    /// 난이도별로 카드에 사용할 보조 색상을 반환합니다.
    /// </summary>
    private static Color GetAccentColor(MinigameDifficultyType difficulty)
    {
        return difficulty switch
        {
            MinigameDifficultyType.Easy => new Color(0.26f, 0.82f, 0.53f),
            MinigameDifficultyType.Hard => new Color(0.95f, 0.32f, 0.26f),
            _ => new Color(1f, 0.72f, 0.16f),
        };
    }
}

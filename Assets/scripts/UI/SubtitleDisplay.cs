using UnityEngine;
using TMPro;

public class SubtitleDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private bool useDefaultFormatting = true;
    [SerializeField] private Color defaultTextColor = Color.white;
    [SerializeField] private int defaultFontSize = 24;

    private void Awake()
    {
        if (subtitleText == null)
        {
            subtitleText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        HideSubtitle();
    }

    public void ShowSubtitle(string text, Color color, int fontSize, float alpha = 1f)
    {
        if (subtitleText == null)
            return;

        subtitleText.text = text;
        
        if (useDefaultFormatting)
        {
            subtitleText.color = defaultTextColor;
            subtitleText.fontSize = defaultFontSize;
        }
        else
        {
            subtitleText.color = color;
            subtitleText.fontSize = fontSize;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }

        gameObject.SetActive(true);
    }

    public void HideSubtitle()
    {
        gameObject.SetActive(false);
    }
}

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class FadeScreen : MonoBehaviour
{
    private Image fadeImage;
    private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private bool startVisible = false;
    [SerializeField] private Color defaultColor = Color.black;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (fadeImage == null)
        {
            fadeImage = GetComponent<Image>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (fadeImage == null)
        {
            fadeImage = gameObject.AddComponent<Image>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (fadeImage != null)
        {
            fadeImage.color = defaultColor;
            fadeImage.raycastTarget = false;
        }

        if (startVisible)
        {
            SetAlpha(1f);
        }
        else if (canvasGroup != null)
        {
            SetAlpha(0f);
        }
    }

    public void SetFade(Color color, float alpha)
    {
        Initialize();
        if (fadeImage != null)
        {
            fadeImage.color = color;
        }
        SetAlpha(alpha);
    }

    public void SetAlpha(float alpha)
    {
        Initialize();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Clamp01(alpha);
            canvasGroup.blocksRaycasts = alpha > 0.01f;
        }
    }

    public void SetColor(Color color)
    {
        Initialize();
        if (fadeImage != null)
        {
            fadeImage.color = color;
        }
    }

    public void FadeToBlack(float alpha)
    {
        SetFade(Color.black, alpha);
    }

    public void FadeToColor(Color color, float alpha)
    {
        SetFade(color, alpha);
    }
}

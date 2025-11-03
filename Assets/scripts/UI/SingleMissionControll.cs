using System.Collections;
using TMPro;
using UnityEngine;

public class SingleMissionControll : MonoBehaviour
{
    public TMP_Text MissionTitle;
    public TMP_Text MissionDescription;
    
    [Header("Animation Settings")]
    public float slideOutDuration = 1f;
    public float slideOutDistance = 2000f; // Distance to move right off screen
    
    private RectTransform rectTransform;
    private bool isCompleting = false;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    
    public void SetMission(string title, string description)
    {
        MissionTitle.text = title;
        MissionDescription.text = description;
    }
    
    public void CompleteMission()
    {
        if (!isCompleting)
        {
            StartCoroutine(SlideOutAndDestroy());
        }
    }
    
    private IEnumerator SlideOutAndDestroy()
    {
        isCompleting = true;
        
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 targetPosition = startPosition + new Vector2(slideOutDistance, 0);
        
        float elapsed = 0f;
        
        while (elapsed < slideOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideOutDuration;
            
            // Use smoothstep for a smoother animation
            float smoothT = t * t * (3f - 2f * t);
            
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, smoothT);
            
            yield return null;
        }
        
        // Ensure final position is set
        rectTransform.anchoredPosition = targetPosition;
        
        // Destroy the game object after animation completes
        Destroy(gameObject);
    }

}

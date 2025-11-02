using UnityEngine;
using TMPro;

public class ItemNameTag : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshPro nameText;
    [SerializeField] private Canvas canvas;
    
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float fontSize = 5f;
    [SerializeField] private bool alwaysFaceCamera = true;
    [SerializeField] private float maxVisibilityDistance = 20f;
    
    private Transform mainCamera;
    private item parentItem;
    
    public void Initialize(string itemName, item parent)
    {
        parentItem = parent;
        
        // Create canvas if it doesn't exist
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Set canvas size
            RectTransform rectTransform = canvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(5, 1);
        }
        
        // Create TextMeshPro if it doesn't exist
        if (nameText == null)
        {
            GameObject textObj = new GameObject("NameText");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one;
            
            nameText = textObj.AddComponent<TextMeshPro>();
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontSize = fontSize;
            nameText.color = textColor;
            
            // Set sorting order to render on top
            nameText.sortingOrder = 10;
        }
        
        // Set the name
        nameText.text = itemName;
        
        // Position the nametag above the item
        transform.localPosition = offset;
    }
    
    private void Start()
    {
        mainCamera = Camera.main?.transform;
    }
    
    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main?.transform;
            if (mainCamera == null) return;
        }
        
        // Always face the camera
        if (alwaysFaceCamera)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.position);
        }
        
        // Fade out based on distance
        if (maxVisibilityDistance > 0 && nameText != null)
        {
            float distance = Vector3.Distance(transform.position, mainCamera.position);
            float alpha = 1f - Mathf.Clamp01((distance - maxVisibilityDistance * 0.5f) / (maxVisibilityDistance * 0.5f));
            
            Color currentColor = nameText.color;
            currentColor.a = alpha;
            nameText.color = currentColor;
        }
    }
    
    public void SetVisibility(bool visible)
    {
        if (nameText != null)
        {
            nameText.gameObject.SetActive(visible);
        }
    }
    
    public void UpdateName(string newName)
    {
        if (nameText != null)
        {
            nameText.text = newName;
        }
    }
}

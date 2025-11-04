using TMPro;
using UnityEngine;

public class ItemNameTag : MonoBehaviour
{
    public TMP_Text nameTagText;
    
    public void InitializeItemTag(string text)
    {
        nameTagText.text = text;
        
        // Calculate position on top of collision box
        float ypos = CalculateTopOfCollider();
        transform.localPosition = new Vector3(0, ypos, 0);
        
        // Set global scale to one
        if (transform.parent != null)
        {
            Vector3 parentScale = transform.parent.lossyScale;
            transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
        }
        else
        {
            transform.localScale = Vector3.one;
        }
    }
    
    private float CalculateTopOfCollider()
    {
        if (transform.parent == null)
            return 1f; // Default fallback
        
        // Try to get collider from parent
        Collider parentCollider = transform.parent.GetComponent<Collider>();
        
        if (parentCollider != null)
        {
            // Get the bounds in local space
            Bounds bounds = parentCollider.bounds;
            Vector3 localMax = transform.parent.InverseTransformPoint(bounds.max);
            return localMax.y + 0.2f; // Add small offset above collider
        }
        
        // If no collider found, try children
        Collider[] childColliders = transform.parent.GetComponentsInChildren<Collider>();
        if (childColliders.Length > 0)
        {
            // Find the highest point among all colliders
            float maxY = float.MinValue;
            foreach (Collider col in childColliders)
            {
                Bounds bounds = col.bounds;
                Vector3 localMax = transform.parent.InverseTransformPoint(bounds.max);
                if (localMax.y > maxY)
                    maxY = localMax.y;
            }
            return maxY + 0.2f; // Add small offset above collider
        }
        
        // Fallback to default
        return 1f;
    }
}

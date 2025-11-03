using TMPro;
using UnityEngine;

public class ItemNameTag : MonoBehaviour
{
    public TMP_Text nameTagText;
    public void InitializeItemTag(string text,float ypos)
    {
        nameTagText.text = text;
        
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
}

using TMPro;
using UnityEngine;

public class ItemNameTag : MonoBehaviour
{
    public TMP_Text nameTagText;
    public void InitializeItemTag(string text,float ypos)
    {
            nameTagText.text = text;
        
        transform.localPosition = new Vector3(0, ypos, 0);
        transform.localScale = Vector3.one;
    }
}

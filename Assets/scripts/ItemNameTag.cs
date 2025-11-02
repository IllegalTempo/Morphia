using TMPro;
using UnityEngine;

public class ItemNameTag : MonoBehaviour
{
    public TMP_Text nameTagText;
    public void InitializeItemTag(item itemComponent,float ypos)
    {
        if (itemComponent != null && nameTagText != null)
        {
            nameTagText.text = itemComponent.ItemName;
        }
        transform.localPosition = new Vector3(0, ypos, 0);
    }
}

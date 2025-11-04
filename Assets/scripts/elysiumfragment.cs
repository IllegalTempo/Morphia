using UnityEngine;

public class elsiumfragment : Selectable
{
    public string id;
    [TextArea(3, 10)] // Min 3 lines, max 10 lines
    public string Content;
    
    public override void OnClicked()
    {
        base.OnClicked();
        gamecore.instance.OnPickEF(this);
        Destroy(gameObject);
    }
}

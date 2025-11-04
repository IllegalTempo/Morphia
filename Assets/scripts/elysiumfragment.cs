using UnityEngine;

public class elsiumfragment : Selectable
{
    public string id;
    public string Content;
    public override void OnClicked()
    {
        base.OnClicked();
        gamecore.instance.OnPickEF(this);
        Destroy(gameObject);
    }
}

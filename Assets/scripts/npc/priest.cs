using UnityEngine;

public class priest : npc
{
    public override void OnClicked()
    {
        base.OnClicked();
        gamecore.instance.AddConversation("priest_1");
        // Additional priest-specific interaction logic can be added here
    }

}

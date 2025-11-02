using UnityEngine;

public class blacksmith : npc
{
    public override void OnClicked()
    {
        base.OnClicked();
        gamecore.instance.AddConversation("blacksmith_1");

        // Additional blacksmith-specific interaction logic can be added here
    }

}

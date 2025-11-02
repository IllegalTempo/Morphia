using UnityEngine;

public class blacksmith : npc
{
    public override void OnClicked()
    {
        base.OnClicked();
        EnterConversation(0);


        // Additional blacksmith-specific interaction logic can be added here
    }

}

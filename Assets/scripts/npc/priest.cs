using UnityEngine;

public class priest : npc
{
    public override void OnClicked()
    {
        base.OnClicked(); 
        EnterConversation(0);


        // Additional priest-specific interaction logic can be added here
    }

}

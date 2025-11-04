using UnityEngine;

public class priest : npc
{
    public override void OnClicked()
    {
        base.OnClicked(); 
        EnterConversation(PlayConvID);


        // Additional priest-specific interaction logic can be added here
    }

}

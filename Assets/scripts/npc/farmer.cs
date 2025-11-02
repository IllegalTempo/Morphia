using UnityEngine;

public class farmer : npc
{
    public override void OnClicked()
    {
        base.OnClicked();
        gamecore.instance.AddConversation("investigation_1");
        gamecore.instance.PlayNextDialogue();

        // Additional priest-specific interaction logic can be added here
    }
}

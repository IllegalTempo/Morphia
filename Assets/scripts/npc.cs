using System.Collections.Generic;
using UnityEngine;

public class npc :Selectable
{
    public string NpcName;
    public List<string> Conversations = new List<string>();
    public override void OnClicked()
    {
        base.OnClicked();
    }
    protected void EnterConversation(int index)
    {
        if (index < 0 || index >= Conversations.Count || Conversations[index] == null)
        {
            Debug.LogError($"NPC {NpcName} has no conversation at index {index} or Already Played");
            return;
        }
        gamecore.instance.AddConversation(Conversations[index]);
        gamecore.instance.PlayNextDialogue();
        Conversations[index] = null;

    }
}

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
    private void Start()
    {
        ItemNameTag tag = Instantiate(gamecore.instance.ItemNameTagPrefab, transform).GetComponent<ItemNameTag>();
        tag.InitializeItemTag(NpcName, 3f);

    }
    protected void EnterConversation(int index)
    {
        if (index < 0 || index >= Conversations.Count || Conversations[index] == null)
        {
            Debug.LogWarning($"NPC {NpcName} has no conversation at index {index} or Already Played");
            return;
        }
        gamecore.instance.StartConversation(Conversations[index],this,index,true);

    }
}

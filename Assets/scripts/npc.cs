using System.Collections.Generic;
using UnityEngine;

public class npc :Selectable
{
    public string NpcName;
    public List<string> Conversations = new List<string>();
    public override void OnClicked()
    {
        base.OnClicked();
        Debug.Log("NPC clicked: " + gameObject.name);
    }
}

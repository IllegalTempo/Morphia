using System.Collections.Generic;
using UnityEngine;

public class npc :Selectable
{
    public string NpcName;
    public List<conversation> Conversations = new List<conversation>();
    public override void OnClicked()
    {
        base.OnClicked();
    }
}

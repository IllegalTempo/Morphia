using UnityEngine;

public class criteria
{
    public static bool tutorial_Check3npcTalked()
    {
        npc npc1 = gamecore.instance.CurrentStage.GetNPC["priest"];
        var npc2 = gamecore.instance.CurrentStage.GetNPC["blacksmith"];
        var npc3 = gamecore.instance.CurrentStage.GetNPC["farmer"];
        return npc1.Conversations[0] == null && npc2.Conversations[0] == null && npc3.Conversations[0] == null;

    }
    
}

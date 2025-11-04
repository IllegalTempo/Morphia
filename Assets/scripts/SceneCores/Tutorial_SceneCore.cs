using UnityEngine;

public class Tutorial_SceneCore : SceneData
{
    protected override void StartMethod()
    {
        if(!save.instance.Missions["tutorial_1"].Completed)
        {
            criteria.instance.Conversation_onFinish += tutorial_ambush_rev;
            MissionData missionData = save.instance.Missions["tutorial_1"];
            gamecore.instance.AddMission(missionData);
        }
            
        
    }
    public void tutorial_ambush_rev()
    {
        npc npc1 = gamecore.instance.CurrentStage.GetNPC["priest"];
        var npc2 = gamecore.instance.CurrentStage.GetNPC["blacksmith"];
        var npc3 = gamecore.instance.CurrentStage.GetNPC["farmer"];
        gamecore.instance.FinishMission("tutorial_1");
        bool allTalked = npc1.Conversations[0] == null && npc2.Conversations[0] == null && npc3.Conversations[0] == null;
        if (allTalked)
        {

            gamecore.instance.StartConversation("ambush_revolutionaries",true);
        }
    }
}

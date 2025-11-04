using UnityEngine;

public class Tutorial_SceneCore : SceneData
{
    protected override void StartMethod()
    {
        if (!NetworkSystem.instance.IsServer) return;
        if (!save.instance.Missions["tutorial_1"].Completed)
        {
            criteria.instance.Conversation_onFinish += tutorial_ambush_rev;
            MissionData missionData = save.instance.Missions["tutorial_1"];
            gamecore.instance.AddMission(missionData);
        }
        if (!save.instance.Missions["intro"].Completed)
        {
            MissionData missionData = save.instance.Missions["intro"];
            gamecore.instance.AddMission(missionData);
            gamecore.instance.StartConversation("intro", true);
            criteria.instance.Conversation_onFinish += FinishIntro;

        }



    }

    public void FinishIntro()
    {
        gamecore.instance.FinishMission("intro");
        criteria.instance.Conversation_onFinish -= FinishIntro;

    }
    public void tutorial_ambush_rev()
    {
        npc npc1 = gamecore.instance.CurrentStage.GetNPC["priest"];
        var npc2 = gamecore.instance.CurrentStage.GetNPC["blacksmith"];
        var npc3 = gamecore.instance.CurrentStage.GetNPC["farmer"];
        bool allTalked = npc1.Conversations[0] == null && npc2.Conversations[0] == null && npc3.Conversations[0] == null;
        if (allTalked && !save.instance.Missions["tutorial_1"].Completed)
        {
            gamecore.instance.FinishMission("tutorial_1");
            criteria.instance.Conversation_onFinish -= tutorial_ambush_rev;
            GameObject.Find("enemygroup").SetActive(true);
            gamecore.instance.StartConversation("ambush_revolutionaries",true);
        }
    }
    public void tutorial_ambush_rev_end()
    {
        gamecore.instance.FinishMission("tutorial_2");
    }
}

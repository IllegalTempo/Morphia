using UnityEditor.Build.Content;
using UnityEngine;

public class Tutorial_SceneCore : SceneData
{
    [SerializeField]
    private GameObject EnemiesGroup;
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
            criteria.instance.EF_onFinish += FinishIntro;

        }
        EnemiesGroup.SetActive(false);



    }

    public void FinishIntro(string id)
    {
        if(id == "tutorial_1")
        {

            gamecore.instance.FinishMission("intro");
            criteria.instance.EF_onFinish -= FinishIntro;
            gamecore.instance.StartConversation("intro", true);
        }


    }
    public void tutorial_ambush_rev(string conversationid)
    {
        npc npc1 = gamecore.instance.CurrentStage.GetNPC["priest"];
        var npc2 = gamecore.instance.CurrentStage.GetNPC["blacksmith"];
        var npc3 = gamecore.instance.CurrentStage.GetNPC["farmer"];
        bool allTalked = npc1.Conversations[0] == null && npc2.Conversations[0] == null && npc3.Conversations[0] == null;
        if (allTalked && !save.instance.Missions["tutorial_1"].Completed)
        {
            gamecore.instance.FinishMission("tutorial_1");
            criteria.instance.Conversation_onFinish -= tutorial_ambush_rev;
            EnemiesGroup.SetActive(true);
            gamecore.instance.StartConversation("revolutionary_unit", true);
            criteria.instance.Conversation_onFinish += tutorial_retreive_stolen_items;

        }
    }
    public void tutorial_retreive_stolen_items(string conversationid)
    {
        EnemiesGroup.SetActive(false);
        MissionData missionData = save.instance.Missions["tutorial_2"];
        gamecore.instance.AddMission(missionData);
        criteria.instance.EF_onFinish += FinishRetreive1;
        criteria.instance.Conversation_onFinish -= tutorial_retreive_stolen_items;
    }
    public void FinishRetreive1(string id)
    {
        if(id == "tutorial_2")
        {
            gamecore.instance.FinishMission("tutorial_2");

            gamecore.instance.StartConversation("dialogue_6", true);
            MissionData missionData = save.instance.Missions["tutorial_3"];
            gamecore.instance.AddMission(missionData);
            criteria.instance.EF_onFinish -= FinishRetreive1;
            criteria.instance.EF_onFinish += tutorial_retreive_core_fragment;

        }
    }
    public void tutorial_retreive_core_fragment(string id)
    {
        if(id == "tutorial_3")
        {
            MissionData missionData = save.instance.Missions["tutorial_4"];
            gamecore.instance.AddMission(missionData);
            gamecore.instance.FinishMission("tutorial_3");
            criteria.instance.Conversation_onFinish += returnedPriest;
            criteria.instance.EF_onFinish -= tutorial_retreive_core_fragment;
            gamecore.instance.CurrentStage.GetNPC["priest"].PlayConvID = 1;
        }
        

    }
    public void returnedPriest(string convid)
    {
        //FUCKING COMPLETED TUTORIAL
        if(convid == "returning_materials") 
        {
            gamecore.instance.FinishMission("tutorial_4");

            criteria.instance.Conversation_onFinish -= returnedPriest;


        }

    }


}

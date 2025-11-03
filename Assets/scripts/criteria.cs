using UnityEngine;

public class criteria
{
    // Delegate definitions for various listener types
    public delegate void CriteriaEventHandler();
    public delegate void CriteriaEventHandler<T>(T arg);
    public delegate void CriteriaEventHandler<T1, T2>(T1 arg1, T2 arg2);
    
    // Example event listeners
    public static event CriteriaEventHandler Conversation_onFinish;

    // Method to invoke Conversation_onFinish event
    public static void TriggerConversationFinish()
    {
        Conversation_onFinish?.Invoke();
    }

    public static bool tutorial_Check3npcTalked()
    {
        npc npc1 = gamecore.instance.CurrentStage.GetNPC["priest"];
        var npc2 = gamecore.instance.CurrentStage.GetNPC["blacksmith"];
        var npc3 = gamecore.instance.CurrentStage.GetNPC["farmer"];
        bool allTalked = npc1.Conversations[0] == null && npc2.Conversations[0] == null && npc3.Conversations[0] == null;
        
        
        return allTalked;
    }
    
    
}

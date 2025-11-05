using System.Collections.Generic;
using UnityEngine;

public class criteria : MonoBehaviour
{
    // Delegate definitions for various listener types
    public delegate void CriteriaEventHandler();
    public delegate void CriteriaEventHandler<T>(T arg);
    public delegate void CriteriaEventHandler<T1, T2>(T1 arg1, T2 arg2);

    // Example event listeners
    public event CriteriaEventHandler<string> Conversation_onFinish;
    public event CriteriaEventHandler<string> EF_onFinish;

    // Track completed conversations and EF fragments
    public HashSet<string> CompletedConversations = new HashSet<string>();
    public HashSet<string> CompletedEFs = new HashSet<string>();

    public static criteria instance;

    private void Awake()
    {
        instance = this;
    }

    // Method to invoke Conversation_onFinish event
    public void TriggerConversationFinish(string conversationid)
    {
        if (!string.IsNullOrEmpty(conversationid) && !CompletedConversations.Contains(conversationid))
        {
            CompletedConversations.Add(conversationid);
            Debug.Log($"Conversation completed and tracked: {conversationid}");
        }
        Conversation_onFinish?.Invoke(conversationid);
    }

    public void TriggerEFFinish(string id)
    {
        if (!string.IsNullOrEmpty(id) && !CompletedEFs.Contains(id))
        {
            CompletedEFs.Add(id);
            Debug.Log($"Elysium Fragment completed and tracked: {id}");
        }
        EF_onFinish?.Invoke(id);
    }

    // Method to get save data for serialization
    public CriteriaSaveData GetSaveData()
    {
        return new CriteriaSaveData
        {
            completedConversations = new List<string>(CompletedConversations),
            completedEFs = new List<string>(CompletedEFs)
        };
    }

    // Method to load data from save
    public void LoadSaveData(CriteriaSaveData data)
    {
        if (data == null) return;

        CompletedConversations = new HashSet<string>(data.completedConversations ?? new List<string>());
        CompletedEFs = new HashSet<string>(data.completedEFs ?? new List<string>());

        Debug.Log($"Loaded criteria data: {CompletedConversations.Count} conversations, {CompletedEFs.Count} EFs");
    }

    // Method to check if conversation was completed
    public bool IsConversationCompleted(string conversationId)
    {
        return CompletedConversations.Contains(conversationId);
    }

    // Method to check if EF was completed
    public bool IsEFCompleted(string efId)
    {
        return CompletedEFs.Contains(efId);
    }
}

// Serializable class for saving criteria data
[System.Serializable]
public class CriteriaSaveData
{
    public List<string> completedConversations = new List<string>();
    public List<string> completedEFs = new List<string>();
}

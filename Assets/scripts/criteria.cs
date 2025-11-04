using UnityEngine;

public class criteria : MonoBehaviour
{
    // Delegate definitions for various listener types
    public delegate void CriteriaEventHandler();
    public delegate void CriteriaEventHandler<T>(T arg);
    public delegate void CriteriaEventHandler<T1, T2>(T1 arg1, T2 arg2);

    // Example event listeners
    public event CriteriaEventHandler Conversation_onFinish;
    public event CriteriaEventHandler<string> EF_onFinish;
    public static criteria instance;
    private void Awake()
    {
        instance = this;


    }
    // Method to invoke Conversation_onFinish event
    public void TriggerConversationFinish()
    {
        Conversation_onFinish?.Invoke();
    }
    public void TriggerEFFinish(string id)
    {
        EF_onFinish?.Invoke(id);
    }




}

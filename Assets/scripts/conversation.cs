using System;
using UnityEngine;

[Serializable]
public class conversation 
{
    public string conversationKey;
    public dialogue[] Dialogues;
}

[Serializable]
public class dialogue
{
    public string CharacterName; //Reference the name of GameObject
    public string DialogueText;
}

[Serializable]
public class ConversationCollection
{
    public conversation[] conversations;
}

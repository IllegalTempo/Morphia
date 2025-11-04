using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Color = UnityEngine.Color;
[System.Serializable]
public class MissionData
{
    public string MissionID;
    public string MissionName;
    public string MissionDescription;
    public bool Completed;
    public MissionData(string id, string name, string description)
    {
        MissionID = id;
        MissionName = name;
        MissionDescription = description;
        Completed = false;
    }


}
public class Conv_Ref
{
    public int conversationIndex; //in npc
    public npc npc;
    public Conv_Ref(npc npc, int index)
    {
        this.npc = npc;
        conversationIndex = index;
    }
}
public class gamecore : MonoBehaviour
{
    //instance reference
    public static gamecore instance;
    public Color ShadeColor;
    [Header("UI/MissionSystem")]
    public GameObject MissionGroup;
    public GameObject SingleMissionInstance;
    public Transform MissionParent;
    [Header("UI/LoadingScreen")]
    public GameObject LoadingScreen;
    public Slider LoadingProgress;
    public TMP_Text LoadingText;
    [Header("UI/Dialogue")]
    public GameObject DialogueUI;
    public TMP_Text DialogueCharacterName;
    public TMP_Text DialogueText;
    [Header("UI/ELYSIUM FRAGMENT")]
    public GameObject EF_UI;
    public TMP_Text EF_CONTENT;
    [Header("Dialogue Camera Settings")]
    public float dialogueCameraDistance = 3f; // Distance from character
    public float dialogueCameraHeight = 1.5f; // Height offset from character position
    public float dialogueCameraTransitionSpeed = 2f; // Speed of camera movement
    public Vector2 dialogueCharacterOffset = new Vector2(-2f, 0.5f); // Screen offset for character during dialogue

    public InteractionSelector I_interactionSelector;

    public GameObject ItemNameTagPrefab;
    public NetworkPlayerObject LocalPlayer;
    public bool InLobby = true;
    public bool InDialogue = false;
    public float groundCheckDistance = 0.1f; // Distance to check for ground
    public SceneData CurrentStage;
    public LayerMask Interactable;
    public LayerMask groundLayer;
    public string SelectedSaveName = "";
    public Dictionary<string, conversation> GetConversation = new Dictionary<string, conversation>();
    public List<dialogue> CurrentPlayingConversation;
    private Coroutine currentDialogueCameraCoroutine;
    public GameObject ObjectiveDisplay;


    public bool IsLocal(int id)
    {
        return LocalPlayer != null && LocalPlayer.NetworkID == id;
    }
    public void SetObjective(Vector3 dest)
    {
        ObjectiveDisplay.SetActive(true);
        ObjectiveDisplay.transform.position = dest;
    }

    private void Update()
    {
        // Handle dialogue progression with Enter key using new Input System
        if (InDialogue && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (NetworkSystem.instance.IsServer)
            {
                PacketSend.Server_nextdialogue();
            }
            else
            {
                PacketSend.Client_Send_nextdialogue();
            }
            PlayNextDialogue();

        }
        if(readingfragment != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            FinishEF();

        }



    }
    Conv_Ref CurrentConversation;
    string Currentconversationkey = "";
    public void StartConversation(string conversationKey, npc npc, int index, bool send)
    {

        CurrentConversation = new Conv_Ref(npc, index);

        if (send)
        {
            if (NetworkSystem.instance.IsServer)
            {
                PacketSend.Server_Send_enterconversation(conversationKey, npc.NpcName, index);

            }
            else
            {
                PacketSend.Client_Send_EnterConversation(conversationKey, npc.NpcName, index);
            }
        }

        CurrentPlayingConversation.AddRange(GetConversation[conversationKey].Dialogues);
        gamecore.instance.PlayNextDialogue();
        InDialogue = true;
        Currentconversationkey = conversationKey;

    }
    public void StartConversation(string conversationKey, bool send)
    {
        if (send)
        {
            if (NetworkSystem.instance.IsServer)
            {
                PacketSend.Server_Send_enterconversation(conversationKey, "", -1);
            }
            else
            {
                PacketSend.Client_Send_EnterConversation(conversationKey, "", -1);
            }
        }
        CurrentPlayingConversation.AddRange(GetConversation[conversationKey].Dialogues);
        gamecore.instance.PlayNextDialogue();
        InDialogue = true;
        Currentconversationkey = conversationKey;

    }
    public elsiumfragment readingfragment;
    public void OnPickEF(elsiumfragment fragment)
    {
        EF_UI.SetActive(true);
        EF_CONTENT.text = fragment.Content;
        readingfragment = fragment;

    }
    public void FinishEF()
    {
        Debug.Log("Finish EF");
        EF_UI.SetActive(false);

        criteria.instance.TriggerEFFinish(readingfragment.id);
        Destroy(readingfragment.gameObject);
        readingfragment = null;

    }
    public void StartGame(string savename)
    {
        MainScreenUI.instance.animator.Play("mainmenu_prestart");
        MainScreenUI.instance.StatusDisplay.text = "Starting in 5 seconds...";

        //Wait 5 seconds 
        StartCoroutine(WaitAndStartGame(savename));
    }
    public void PlayNextDialogue()
    {
        if (CurrentPlayingConversation.Count == 0)
        {
            EndDialogue();
            return;
        }

        dialogue dialogue = CurrentPlayingConversation[0];
        string[] ignorelist = { "Gameplay", "narrator" };
        // Find the character GameObject by name
        GameObject character = GameObject.Find(dialogue.CharacterName.ToLower());

        if (character == null)
        {
            Debug.LogWarning($"Character '{dialogue.CharacterName}' not found in scene!");
            //set character to local player if not found
            character = LocalPlayer.gameObject;
        }

        // Set dialogue state

        // Show dialogue UI
        DialogueUI.SetActive(true);

        // Set dialogue text
        DialogueText.text = dialogue.DialogueText;
        if (dialogue.CharacterName.Contains("/"))
        {
            DialogueCharacterName.text = dialogue.CharacterName.Split("/")[1];

        } else
        {
            DialogueCharacterName.text = dialogue.CharacterName;
        }

        // Position camera in front of character
        if (LocalPlayer != null && LocalPlayer.playerMovement != null && LocalPlayer.playerMovement.playerCamera != null && !ignorelist.Contains(dialogue.CharacterName))
        {
            // Stop any existing camera transition
            if (currentDialogueCameraCoroutine != null)
            {
                StopCoroutine(currentDialogueCameraCoroutine);
            }

            // Start smooth camera transition
            currentDialogueCameraCoroutine = StartCoroutine(TransitionCameraToDialoguePosition(character.transform));

            // Set character screen offset for dialogue view
            LocalPlayer.playerMovement.SetCharacterScreenOffset(dialogueCharacterOffset);
        }

        CurrentPlayingConversation.RemoveAt(0);

    }

    /// <summary>
    /// Smoothly transitions the camera to face the speaking character from the front
    /// </summary>
    private IEnumerator TransitionCameraToDialoguePosition(Transform characterTransform)
    {
        PlayerMovement playerMovement = LocalPlayer.playerMovement;
        Camera camera = playerMovement.playerCamera;

        // Calculate target position: in front of the character, facing towards them
        Vector3 characterForward = -characterTransform.forward;
        Vector3 targetPosition = characterTransform.position
                                - characterForward * dialogueCameraDistance // In front of character
                                + Vector3.up * dialogueCameraHeight; // At appropriate height

        // Calculate target rotation: looking at the character
        Vector3 lookDirection = (characterTransform.position + Vector3.up * dialogueCameraHeight) - targetPosition;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        // Get start position and rotation
        Vector3 startPosition = camera.transform.position;
        Quaternion startRotation = camera.transform.rotation;

        float elapsed = 0f;
        float duration = 1f / dialogueCameraTransitionSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            // Smoothly interpolate position and rotation
            camera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            camera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        // Ensure final position is exact
        camera.transform.position = targetPosition;
        camera.transform.rotation = targetRotation;

        currentDialogueCameraCoroutine = null;
    }

    /// <summary>
    /// Ends the dialogue and restores player control
    /// </summary>
    public void EndDialogue()
    {
        InDialogue = false;
        DialogueUI.SetActive(false);

        // Stop camera transition if still running
        if (currentDialogueCameraCoroutine != null)
        {
            StopCoroutine(currentDialogueCameraCoroutine);
            currentDialogueCameraCoroutine = null;
        }

        // Reset character screen offset
        if (LocalPlayer != null && LocalPlayer.playerMovement != null)
        {
            LocalPlayer.playerMovement.SetCharacterScreenOffset(Vector2.zero);
        }

        // Trigger criteria event for conversation finish
        if (CurrentConversation != null)
        {
            // Mark conversation as completed for the NPC
            npc npc = CurrentConversation.npc;
            int index = CurrentConversation.conversationIndex;
            if (npc != null && index >= 0 && index < npc.Conversations.Count)
            {
                npc.Conversations[index] = null;
            }
            CurrentConversation = null;
            Debug.Log("MK_GC_268");
        }
        criteria.instance.TriggerConversationFinish(Currentconversationkey);
        Currentconversationkey = "";
        Debug.Log("Conversation ended");
    }

    private void LoadConversation(string scenename)
    {
        try
        {
            // Path to the conversation JSON file (adjust as needed)
            string conversationPath = Application.streamingAssetsPath + $"/{scenename}_conversations.json";

            if (!System.IO.File.Exists(conversationPath))
            {
                Debug.LogWarning("Conversation file not found at: " + conversationPath);
                return;
            }

            // Read JSON file
            string json = System.IO.File.ReadAllText(conversationPath);

            // Deserialize JSON to conversation collection
            ConversationCollection collection = JsonUtility.FromJson<ConversationCollection>(json);

            // Clear existing conversations
            GetConversation.Clear();

            // Populate dictionary with conversations
            foreach (conversation conv in collection.conversations)
            {
                GetConversation[conv.conversationKey] = conv;
            }

            Debug.Log($"Loaded {GetConversation.Count} conversations successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load conversations: " + e.Message);
        }
    }

    public void Server_OnSecondPlayerJoined(NetworkPlayer p)
    {
        MainScreenUI.instance.StatusDisplay.text = p.SteamName + " Has Joined!";
        save.instance.LoadFromFile(SelectedSaveName);

        StartGame(SelectedSaveName);
        PacketSend.Server_Send_StartGame(save.instance.CurrentStage);
    }

    private IEnumerator WaitAndStartGame(string savename)
    {
        yield return new WaitForSeconds(5f);
        print("Wait and Startgame: " + savename);
        if (!NetworkSystem.instance.IsServer)
        {
            StartCoroutine(Client_UseSave(savename));
        }
        else
        {
            StartCoroutine(UseSave(savename));
            NetworkListener.Server_OnPlayerJoinSuccessful -= Server_OnSecondPlayerJoined;
        }
    }

    private void Awake()
    {
        //singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        //Set shader global variable
        Shader.SetGlobalColor("_ShadeColor", ShadeColor);
        SceneManager.sceneLoaded += OnSceneLoad;
        NetworkListener.Server_OnPlayerJoinSuccessful += Server_OnSecondPlayerJoined;

        // Setup dialogue input action
    }

    private void Start()
    {
        LoadingScreen.SetActive(false);
        DialogueUI.SetActive(false);
        EF_UI.SetActive(false);
        ObjectiveDisplay.SetActive(false);

    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoad;

    }

    /// <summary>
    /// Sets up the Input Action for dialogue progression
    /// </summary>



    private void SetUpScene(SceneData sd)
    {

        foreach (item item in sd.items)
        {
            ItemIdentifier identifier = new ItemIdentifier(sd.stage, item.ItemID);
            if (save.instance.FindSavedItem.ContainsKey(identifier))
            {
                SaveInfo_Item savedItem = save.instance.FindSavedItem[identifier];
                item.transform.position = savedItem.position;
                item.transform.rotation = savedItem.rotation;
                item.netObj.Owner = savedItem.Parented_To_Player;
            }
        }

        //if sdnpcs null
        if (sd.npcs == null)
        {
            Debug.LogWarning("SceneData NPCs is null!");
        }
        else
        {
            foreach (npc npc in sd.npcs)
            {
                if (save.instance.FindNPC.ContainsKey(npc.NpcName))
                {
                    NpcSaveData savedNpc = save.instance.FindNPC[npc.NpcName];
                    savedNpc.ApplyToNpc(npc);
                }
            }
        }

    }

    private Dictionary<string, SingleMissionControll> InUIMission = new Dictionary<string, SingleMissionControll>();
    public void AddMission(string MissionID, string MissionTitle, string MissionDescription)
    {
        if (NetworkSystem.instance.IsServer)
        {
            PacketSend.Server_Send_Distribute_Mission(MissionID, MissionTitle, MissionDescription, true);
        }
        SingleMissionControll ui = Instantiate(SingleMissionInstance, MissionParent).GetComponent<SingleMissionControll>();
        ui.SetMission(MissionTitle, MissionDescription);
        InUIMission.Add(MissionID, ui);

    }
    public void AddMission(MissionData md)
    {
        AddMission(md.MissionID, md.MissionName, md.MissionDescription);
    }
    public void FinishMission(string MissionID)
    {
        if (NetworkSystem.instance.IsServer)
        {
            PacketSend.Server_Send_Distribute_Mission(MissionID, "", "", false);
            save.instance.Missions[MissionID].Completed = true;


        }

        InUIMission[MissionID].CompleteMission();

    }

    public void OnSceneLoad(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        LoadConversation(scene.name);

        LoadingScreen.SetActive(false);

        SceneData sd = GameObject.Find("SceneCore").GetComponent<SceneData>();

        if (NetworkSystem.instance.IsServer && scene.name != "mainscreen")
        {
            LocalPlayer.playerMovement.transform.position = sd.Spawnpoint[0].position;
            foreach (KeyValuePair<ulong, NetworkPlayer> player in NetworkSystem.instance.server.players)
            {
                int NetworkID = player.Value.NetworkID;
                PacketSend.Server_Send_DistributeInitialPos(player.Value, sd.Spawnpoint[NetworkID % sd.Spawnpoint.Length].position, sd.Spawnpoint[NetworkID % sd.Spawnpoint.Length].rotation);
            }
        }
        if (sd.IsLobby)
        {
            MissionGroup.SetActive(false);

        }
        else
        {
            MissionGroup.SetActive(true);
            LocalPlayer.playerMovement.InGameSetup();

            if (NetworkSystem.instance.IsServer)
            {
                ToSave();
                SetUpScene(sd);
            }
        }



    }

    public IEnumerator Client_UseSave(string scenename)
    {
        yield return StartCoroutine(LoadScene(scenename));
    }

    public IEnumerator UseSave(string savename) //Use it when all players is in lobby!
    {
        yield return StartCoroutine(LoadScene(save.instance.CurrentStage));
    }

    public void StartLoading(string loadingtext)
    {
        LoadingScreen.SetActive(true);
        LoadingText.text = loadingtext;
        LoadingProgress.value = 0f;
    }

    public IEnumerator LoadScene(string scenename)
    {
        StartLoading("Loading Scene: " + scenename);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scenename);

        // Optional: prevent scene from activating immediately
        // asyncLoad.allowSceneActivation = false;

        // The game continues running during this loop
        while (!asyncLoad.isDone)
        {
            LoadingProgress.value = asyncLoad.progress;
            // You can access asyncLoad.progress here (0.0 to 1.0)
            // float progress = asyncLoad.progress;

            yield return null; // Wait for next frame
        }
    }




    public void ToSave()
    {
        if (CurrentStage == null)
        {
            Debug.LogWarning("Cannot save: CurrentStage is null");
            return;
        }

        // Update current stage name
        save.instance.CurrentStage = CurrentStage.stage;

        // Save player positions and rotations

        if (CurrentStage.items == null)
        {
            Debug.LogWarning("Cannot save items: CurrentStage.items is null");
        }
        else
        {
            foreach (item item in CurrentStage.items)
            {
                ItemIdentifier identifier = new ItemIdentifier(CurrentStage.stage, item.ItemID);
                SaveInfo_Item savedItem = new SaveInfo_Item(
                    item.transform.position,
                    item.transform.rotation,
                    item.netObj.Owner
                );

                // Add to both the list (for serialization) and dictionary (for quick lookup)
                ItemDataEntry entry = new ItemDataEntry
                {
                    identifier = identifier,
                    itemInfo = savedItem
                };
                save.instance.FindSavedItem[identifier] = savedItem;
            }
        }
        // Save all items in the current stage

        if (CurrentStage.npcs == null)
        {
            Debug.LogWarning("Cannot save NPCs: CurrentStage.npcs is null");
        }
        else
        {
            foreach (npc npc in CurrentStage.npcs)
            {
                save.instance.FindNPC[npc.NpcName] = new NpcSaveData(npc);
            }
        }

        save.instance.ParseDict();
        save.instance.SaveToFile(save.instance.GetSavePath(save.instance.CurrentSaveName));

        Debug.Log($"Game state parsed to save object. Stage: {CurrentStage.stage}, Players: {NetworkSystem.instance.PlayerList.Count}, Items: {save.instance.ItemData.Count}, NPCs: {save.instance.npcs.Count}");
    }
}

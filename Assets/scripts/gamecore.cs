using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Color = UnityEngine.Color;

public class gamecore : MonoBehaviour
{
    //instance reference
    public static gamecore instance;
    public Color ShadeColor;
    [Header("UI/MissionSystem")]
    public GameObject SingleMissionInstance;
    public Transform MissionParent;
    [Header("UI/LoadingScreen")]
    public GameObject LoadingScreen;
    public Slider LoadingProgress;
    public TMP_Text LoadingText;
    [Header("UI/Dialogue")]
    public GameObject DialogueUI;
    public TMP_Text DialogueText;


    public InteractionSelector I_interactionSelector;


    public NetworkPlayerObject LocalPlayer;
    public bool InLobby = true;
    public bool InDialogue = false;
    public float groundCheckDistance = 0.1f; // Distance to check for ground
    public SceneData CurrentStage;
    public LayerMask Interactable;
    public LayerMask groundLayer; // Set this in Inspector to only check ground objects
    private bool SaveLoaded = false;
    public string SelectedSaveName = "";
    public Dictionary<string, conversation> GetConversation = new Dictionary<string, conversation>();
    public List<dialogue> CurrentPlayingConversation;

    public bool IsLocal(int id)
    {
        return LocalPlayer != null && LocalPlayer.NetworkID == id;
    }
    private void Update()
    {

    }
    public void StartGame(string savename)
    {
        MainScreenUI.instance.animator.Play("mainmenu_prestart");
        MainScreenUI.instance.StatusDisplay.text = "Starting in 5 seconds...";
        Debug.Log("StartGame " + savename);

        //Wait 5 seconds 
        StartCoroutine(WaitAndStartGame(savename));
    }
    public void PlayNextDialogue()
    {
        dialogue dialogue = CurrentPlayingConversation[0];
        // Find the character GameObject by name
        GameObject character = GameObject.Find(dialogue.CharacterName);

        if (character == null)
        {
            Debug.LogError($"Character '{dialogue.CharacterName}' not found in scene!");
            return;
        }

        // Set dialogue state
        InDialogue = true;

        // Show dialogue UI
        DialogueUI.SetActive(true);

        // Set dialogue text
        DialogueText.text = dialogue.DialogueText;

        // Make camera follow the character
        if (LocalPlayer != null && LocalPlayer.playerMovement != null && LocalPlayer.playerMovement.playerCamera != null)
        {
            // Calculate direction from player to character
            Vector3 directionToCharacter = (character.transform.position - LocalPlayer.transform.position).normalized;

            // Calculate desired camera rotation to look at character
            directionToCharacter.y = 0; // Keep on horizontal plane

            if (directionToCharacter != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToCharacter);

                // Apply rotation to camera (you may want to smooth this with a coroutine)
                LocalPlayer.playerMovement.playerCamera.transform.rotation = targetRotation;
            }
        }
        CurrentPlayingConversation.RemoveAt(0);
    }
    private void LoadConversation()
    {
        try
        {
            // Path to the conversation JSON file (adjust as needed)
            string conversationPath = Application.streamingAssetsPath + "/conversations.json";

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
        Debug.Log("Second Player Joined: " + p.SteamName + SelectedSaveName);
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


    }
    private void Start()
    {
        LoadingScreen.SetActive(false);
        DialogueUI.SetActive(false);
        LoadConversation();
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoad;
    }
    public void StartDialogue(string DialogueKey)
    {

    }
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
                    npc savednpc = save.instance.FindNPC[npc.NpcName];
                    npc.Conversations = savednpc.Conversations;

                }
            }
        }


        SaveLoaded = true;
    }
    public void OnSceneLoad(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {


        Debug.Log("Scene Loaded: " + scene.name);
        LoadingScreen.SetActive(false);

        SceneData sd = GameObject.Find("SceneCore").GetComponent<SceneData>();

        if (NetworkSystem.instance.IsServer)
        {
            LocalPlayer.playerMovement.transform.position = sd.Spawnpoint[0].position;
            foreach (KeyValuePair<ulong, NetworkPlayer> player in NetworkSystem.instance.server.players)
            {
                int NetworkID = player.Value.NetworkID;
                PacketSend.Server_Send_DistributeInitialPos(player.Value, sd.Spawnpoint[NetworkID % sd.Spawnpoint.Length].position, sd.Spawnpoint[NetworkID % sd.Spawnpoint.Length].rotation);
            }
        }
        if (sd.IsLobby) return;
        LocalPlayer.playerMovement.InGameSetup();

        ToSave();
        SetUpScene(sd);



    }
    public IEnumerator Client_UseSave(string scenename)
    {
        yield return StartCoroutine(LoadScene(scenename));

    }
    public IEnumerator UseSave(string savename) //Use it when all players is in lobby!
    {
        Debug.Log("Server using save " + savename + "Stage:" + save.instance.CurrentStage);

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
    public void AddMission(string MissionTitle, string MissionDescription)
    {
        Instantiate(SingleMissionInstance, MissionParent).GetComponent<SingleMissionControll>().SetMission(MissionTitle, MissionDescription);


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
                save.instance.FindNPC[npc.NpcName] = npc;
            }
        }

        save.instance.ParseDict();
        save.instance.SaveToFile(save.instance.GetSavePath(save.instance.CurrentSaveName));

        Debug.Log($"Game state parsed to save object. Stage: {CurrentStage.stage}, Players: {NetworkSystem.instance.PlayerList.Count}, Items: {save.instance.ItemData.Count}, NPCs: {save.instance.npcs.Count}");
    }

}

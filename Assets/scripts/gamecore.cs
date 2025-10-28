

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

    public NetworkPlayerObject LocalPlayer;
    public bool InLobby = true;
    public float groundCheckDistance = 0.1f; // Distance to check for ground
    public SceneData CurrentStage;
    public LayerMask Interactable;
    public LayerMask groundLayer; // Set this in Inspector to only check ground objects
    private bool SaveLoaded = false;
    public string SelectedSaveName = "";
    public bool IsLocal(int id)
    {
        return LocalPlayer != null && LocalPlayer.NetworkID == id;
    }
    public void StartGame(string savename)
    {
        MainScreenUI.instance.animator.Play("mainmenu_prestart");
        MainScreenUI.instance.StatusDisplay.text = "Starting in 5 seconds...";

        //Wait 5 seconds 
        StartCoroutine(WaitAndStartGame(savename));
    }
    public void Server_OnSecondPlayerJoined(NetworkPlayer p)
    {
        MainScreenUI.instance.StatusDisplay.text = p.SteamName + " Has Joined!";

        StartGame(save.instance.CurrentSaveName);
        PacketSend.Server_Send_StartGame(save.instance.CurrentStage);



    }
    private IEnumerator WaitAndStartGame(string savename)
    {
        yield return new WaitForSeconds(5f);
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
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoad;
    }
    public void OnSceneLoad(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {


        Debug.Log("Scene Loaded: " + scene.name);
        LoadingScreen.SetActive(false);

        SceneData sd = GameObject.Find("SceneCore").GetComponent<SceneData>();
        if (sd.IsLobby) return;
        LocalPlayer.playerMovement.InGameSetup();

        if (NetworkSystem.instance.IsServer)
        {
            foreach(KeyValuePair<ulong,NetworkPlayer> player in NetworkSystem.instance.server.players)
            {
                int NetworkID = player.Value.NetworkID;
                PacketSend.Server_Send_DistributeInitialPos(player.Value, sd.Spawnpoint[NetworkID % sd.Spawnpoint.Length].position, sd.Spawnpoint[NetworkID % sd.Spawnpoint.Length].rotation);
            }
        }
        if (SaveLoaded)
        {
            CurrentStage = sd;
            if (LocalPlayer != null)
            {
                LocalPlayer.transform.position = sd.Spawnpoint[LocalPlayer.NetworkID%sd.Spawnpoint.Length].position;
            } else
            {
                Debug.LogError("LocalPlayer is null on scene load!");
            }
        }
        foreach(item item in sd.items)
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
        

    }
    public IEnumerator Client_UseSave(string scenename)
    {
        yield return StartCoroutine(LoadScene(scenename));

    }
    public IEnumerator UseSave(string savename) //Use it when all players is in lobby!
    {
        save.instance.LoadFromFile(save.instance.GetSavePath(savename));
        yield return StartCoroutine(LoadScene(save.instance.CurrentStage));

        foreach (NetworkPlayerObject p in NetworkSystem.instance.PlayerList)
        {
            if (p.NetworkID > save.instance.playerSaveInfos.Length)
            {
                Debug.LogError($"Network ID {p.NetworkID} not in playerSaveInfo");
            }
            else
            {
                SaveInfo_Player player = save.instance.playerSaveInfos[p.NetworkID];
                p.transform.position = player.position;
                p.transform.rotation = player.rotation;

            }


        }
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
}

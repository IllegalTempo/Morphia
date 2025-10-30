using Steamworks;
using Steamworks.Data;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

//Unity New InputSystem
public class MainScreenUI : MonoBehaviour
{
    public static MainScreenUI instance;
    [Header("Object References")]
    public GameObject SelectedSaveIndicator;
    public Animator animator;
    [Header("Object Reference/AfterHost")]
    public TMP_Text StatusDisplay;
    public GameObject AfterHost_Canvas;
    public GameObject Instance_SaveButton;
    public Transform SaveButtonsView;
    public TMP_Text InviteCodeDisplay;
    public Vector2 SaveButtonPosOffset = new Vector2(0, 0.5f);
    [Header("Object Reference/AfterJoin")]
    public GameObject AfterJoin_Canvas;
    public TMP_InputField InviteCodeInput;





    private Vector2 SelectedSavePos;

    private void Update()
    {
        var indicatorRect = SelectedSaveIndicator.GetComponent<RectTransform>();
        indicatorRect.position = Vector2.Lerp(indicatorRect.position, SelectedSavePos, Time.deltaTime * 10f);

    }
    private void Start()
    {
        AfterHost_Canvas.SetActive(false);
        AfterJoin_Canvas.SetActive(false);

    }
    public void CopyInviteCode()
    {
        GUIUtility.systemCopyBuffer = NetworkSystem.instance.GetInviteCode().ToString();
        StatusDisplay.text = "Invite Code Copied to Clipboard!" ;
    }
    public void InviteFriend()
    {
        SteamFriends.OpenGameInviteOverlay(NetworkSystem.instance.CurrentLobby.Id);
    }
    public void OnClickJoinLobby()
    {
        string input = InviteCodeInput.text;
        if (ulong.TryParse(input, out ulong lobbyid))
        {
            NetworkSystem.instance.JoinLobby(lobbyid);
            StatusDisplay.text = "Joining Lobby...";
        }
        else
        {
            StatusDisplay.text = "Invalid Invite Code!";
        }
    }
    public void OnSecondPlayerJoining(ConnectionInfo info)
    {
        StatusDisplay.text = new Friend(info.Identity.SteamId).Name + " Is Joining!";
    }
    public void Initialize_AfterHost_UI()
    {
        animator.Play("mainmenu_AfterHost");
        NetworkSystem.instance.CreateGameLobby();
        AfterHost_Canvas.SetActive(true);
        //clear existing buttons
        foreach (Transform child in SaveButtonsView)
        {
            Destroy(child.gameObject);
        }
        foreach (string path in save.instance.GetFilesInSaveFolder())
        {
            Instance_SaveButton savebutton = Instantiate(Instance_SaveButton, SaveButtonsView).GetComponent<Instance_SaveButton>();
            savebutton.InitSaveButton(save.instance.GetSaveName(path));
        }
        if(save.instance.GetFilesInSaveFolder().Length > 0)
        {
            gamecore.instance.SelectedSaveName = save.instance.GetFilesInSaveFolder()[0];

        }

        Instance_SaveButton newbutton = Instantiate(Instance_SaveButton, SaveButtonsView).GetComponent<Instance_SaveButton>();
        newbutton.InitNewSaveButton();

    }
    public void Initialize_AfterJoin_UI()
    {
        animator.Play("mainmenu_AfterJoin");
        AfterJoin_Canvas.SetActive(true);
    }
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        NetworkListener.Server_OnPlayerJoining += OnSecondPlayerJoining;
    }

    public void OnSaveButtonClicked(string savename, Vector2 SaveButtonAnchoredPos)
    {
        SelectedSaveIndicator.SetActive(true);
        gamecore.instance.SelectedSaveName = save.instance.GetSavePath(savename);
        SelectedSavePos = SaveButtonAnchoredPos + SaveButtonPosOffset;
    }

    public void OnNewSaveButtonClicked(Vector2 SaveButtonAnchoredPos)
    {
        SelectedSaveIndicator.SetActive(true);
        string savename = "Save - " + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        save.instance.NewSave(savename);
        gamecore.instance.SelectedSaveName = savename;
        foreach (Transform child in SaveButtonsView)
        {
            Destroy(child.gameObject);
        }
        foreach (string path in save.instance.GetFilesInSaveFolder())
        {
            Instance_SaveButton savebutton = Instantiate(Instance_SaveButton, SaveButtonsView).GetComponent<Instance_SaveButton>();
            savebutton.InitSaveButton(save.instance.GetSaveName(path));
        }
        Instance_SaveButton newbutton = Instantiate(Instance_SaveButton, SaveButtonsView).GetComponent<Instance_SaveButton>();
        newbutton.InitNewSaveButton();
        SelectedSavePos = SaveButtonAnchoredPos +SaveButtonPosOffset;
    }
}

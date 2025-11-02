using System.Collections.Generic;
using System.IO.Pipes;
using UnityEngine;

public class SceneData : MonoBehaviour
{
    public string stage;
    public item[] items;
    public npc[] npcs;
    public Transform[] Spawnpoint;
    public string NextSceneName;
    public bool IsLobby = false;
    public void NextStage()
    {
        StartCoroutine(gamecore.instance.LoadScene(NextSceneName));
    }
    private void Awake()
    {
        if (stage == "mainscreen") return;
        gamecore.instance.CurrentStage = this;

    }
    private void Start()
    {
        if (stage == "mainscreen")
        {
            gamecore.instance.CurrentStage = this;
        }
    }

}

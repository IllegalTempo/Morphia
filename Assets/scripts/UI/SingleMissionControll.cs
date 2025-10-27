using TMPro;
using UnityEngine;

public class SingleMissionControll : MonoBehaviour
{
    public TMP_Text MissionTitle;
    public TMP_Text MissionDescription;
    public void SetMission(string title, string description)
    {
        MissionTitle.text = title;
        MissionDescription.text = description;
    }

}

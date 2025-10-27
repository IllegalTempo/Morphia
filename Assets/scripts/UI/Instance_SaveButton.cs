using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Instance_SaveButton : MonoBehaviour
{
    [SerializeField]
    private TMP_Text buttonText;
    [SerializeField]
    private Button button;
    [SerializeField]
    private RectTransform rectTransform;
    public void InitSaveButton(string savename)
    {
        buttonText.text = savename;
        button.onClick.AddListener(() =>
        {
            MainScreenUI.instance.OnSaveButtonClicked(savename, rectTransform.position);
        });

    }
    public void InitNewSaveButton()
    {
        buttonText.text = "New Save";
        button.onClick.AddListener(() =>
        {
            MainScreenUI.instance.OnNewSaveButtonClicked(rectTransform.position);
        });
    }

}

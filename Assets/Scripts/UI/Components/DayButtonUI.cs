using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DayButtonUI : MonoBehaviour
{
    public TMP_Text dayNumberText;
    public GameObject entryIcon; 
    public Button button;

    public void Setup(int day, bool hasEntry, bool isOtherMonth, System.Action onClick)
    {
        dayNumberText.text = day.ToString("D2");

        entryIcon.SetActive(hasEntry);
        if (isOtherMonth)
        {
            dayNumberText.color = Color.gray;
            button.interactable = false;
        }
        else
        {
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }
    }
}

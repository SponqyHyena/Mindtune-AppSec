using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DayEntryUI : MonoBehaviour
{
    public TMP_Text dayNumberText;
    public TMP_Text entryPreviewText;
    public Button selectButton;

    public void Setup(int day, string entryText, System.Action onClick)
    {
        dayNumberText.text = day.ToString("D2");
        entryPreviewText.text = string.IsNullOrEmpty(entryText) ? "" : entryText;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => onClick());
    }
}

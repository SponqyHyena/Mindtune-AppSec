using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewEntryUI : MonoBehaviour
{
    public Button didButton;
    public Button feltButton;
    public Button sensationsButton;
    public Button thoughtsButton;

    public Color normalColor;
    public Color selectedColor;

    public TMP_Text userNameText;
    public TMP_InputField mainInput;      
    public TMP_InputField subInput;       
    
    private string currentCategory;   
    private DiaryEntry currentEntry;
    private string currentDate;

    public void LoadEntry(string date)
    {
        currentDate = date;

        userNameText.text = UserManager.CurrentUser.Name;
        currentEntry = UserManager.CurrentUser.GetDiaryEntry(date);

        if (currentEntry == null)
        {
            currentEntry = new DiaryEntry();
            UserManager.CurrentUser.SetDiaryEntry(date, currentEntry);
        }

        mainInput.text = currentEntry.mainText;
        SetCategory("did");
    }

    public void SetCategory(string category)
    {
        SaveCurrentSubInput();
        currentCategory = category;

        ResetButtonColors();

        switch (category)
        {
            case "did": 
                subInput.text = currentEntry.did;
                SetButtonColor(didButton, selectedColor);
                break;
            case "felt": 
                subInput.text = currentEntry.felt;
                SetButtonColor(feltButton, selectedColor);
                break;
            case "sensations": 
                subInput.text = currentEntry.sensations;
                SetButtonColor(sensationsButton, selectedColor);
                break;
            case "thoughts": 
                subInput.text = currentEntry.thoughts;
                SetButtonColor(thoughtsButton, selectedColor);
                break;
        }
    }
    private void ResetButtonColors()
    {
        SetButtonColor(didButton, normalColor);
        SetButtonColor(feltButton, normalColor);
        SetButtonColor(sensationsButton, normalColor);
        SetButtonColor(thoughtsButton, normalColor);
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button != null)
            button.GetComponent<Image>().color = color;
    }

    private void SaveCurrentSubInput()
    {
        if (currentEntry == null) return;

        switch (currentCategory)
        {
            case "did": currentEntry.did = subInput.text; break;
            case "felt": currentEntry.felt = subInput.text; break;
            case "sensations": currentEntry.sensations = subInput.text; break;
            case "thoughts": currentEntry.thoughts = subInput.text; break;
        }
    }

    public void SaveEntry()
    {
        SaveCurrentSubInput();
        currentEntry.mainText = mainInput.text;

        if (!string.IsNullOrEmpty(currentEntry.mainText))
        {
            currentEntry.mainText = currentEntry.mainText.Length > 1000
                ? currentEntry.mainText.Substring(0, 1000)
                : currentEntry.mainText;

            currentEntry.mainText = InputValidator.SanitizeInput(currentEntry.mainText);
        }
        UserManager.CurrentUser.SetDiaryEntry(currentDate, currentEntry);

        FindFirstObjectByType<UserManager>().SaveUsersData();
        UIManager.Instance.ShowDiary();
    }
}

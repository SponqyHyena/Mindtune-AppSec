using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MoodEntryUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text dayOfWeekText;
    public TMP_Text dayNumberText;
    public Slider moodSlider;

    [Header("Emoji setup")]
    public List<Button> emojiButtons;
    public GameObject emojiPrefab;   

    public Button saveButton;
    public Button backButton;

    private string currentDateKey;
    private MoodEntry currentEntry;
    private HashSet<int> selectedIndices = new HashSet<int>();
    private Dictionary<int, GameObject> activePrefabs = new Dictionary<int, GameObject>();

    private void Awake()
    {
        saveButton.onClick.AddListener(SaveEntry);
        backButton.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowMoodDiary();
        });

        for (int i = 0; i < emojiButtons.Count; i++)
        {
            int index = i;
            emojiButtons[i].onClick.AddListener(() => ToggleEmoji(index));
        }
    }

    public void LoadEntry(int year, int month, int day)
    {
        DateTime date = new DateTime(year, month, day);
        currentDateKey = $"{year}-{month:D2}-{day:D2}";
        string[] daysRU = { "вс", "пн", "вт", "ср", "чт", "пт", "сб" };
        dayOfWeekText.text = daysRU[(int)date.DayOfWeek];
        dayNumberText.text = date.Day.ToString();

        currentEntry = UserManager.CurrentUser.GetMoodEntry(currentDateKey);
        if (currentEntry == null)
        {
            currentEntry = new MoodEntry();
            UserManager.CurrentUser.SetMoodEntry(currentDateKey, currentEntry);
        }

        moodSlider.value = currentEntry.moodValue;

        foreach (var kvp in activePrefabs)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }
        activePrefabs.Clear();
        selectedIndices.Clear();

        foreach (int index in currentEntry.selectedEmojis)
        {
            if (index >= 0 && index < emojiButtons.Count)
            {
                AddPrefab(index);
                selectedIndices.Add(index);
            }
        }
    }

    private void ToggleEmoji(int index)
    {
        if (selectedIndices.Contains(index))
        {
            selectedIndices.Remove(index);
            RemovePrefab(index);
        }
        else
        {
            if (selectedIndices.Count < 3)
            {
                selectedIndices.Add(index);
                AddPrefab(index);
            }
        }
    }

    private void AddPrefab(int index)
    {
        if (emojiPrefab == null) return;
        if (activePrefabs.ContainsKey(index)) return;

        GameObject prefabInstance = Instantiate(emojiPrefab, emojiButtons[index].transform);
        prefabInstance.transform.localPosition = Vector3.zero;
        activePrefabs[index] = prefabInstance;
    }

    private void RemovePrefab(int index)
    {
        if (activePrefabs.ContainsKey(index))
        {
            if (activePrefabs[index] != null)
                Destroy(activePrefabs[index]);
            activePrefabs.Remove(index);
        }
    }

    private void SaveEntry()
    {
        if (currentEntry == null) return;

        currentEntry.moodValue = (int)moodSlider.value;
        currentEntry.selectedEmojis = new List<int>(selectedIndices);

        UserManager.CurrentUser.SetMoodEntry(currentDateKey, currentEntry);
        FindFirstObjectByType<UserManager>().SaveUsersData();

        UIManager.Instance.ShowMoodDiary();
    }
}

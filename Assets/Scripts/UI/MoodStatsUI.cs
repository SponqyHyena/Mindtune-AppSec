using System;
using UnityEngine;
using TMPro;


public class MoodStatsUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text userNameText;
    public Transform daysContainer;
    public GameObject dayStatsPrefab;

    public TMP_InputField fromDateField;
    public TMP_InputField toDateField;

    [Header("Emoji Sprites")]
    public Sprite[] emojiSprites;
    public GameObject emojiPrefab;

    private DateTime fromDate;
    private DateTime toDate;

    private void OnEnable()
    {
        if (UserManager.CurrentUser == null) return;
        userNameText.text = UserManager.CurrentUser.Name;

        fromDateField.onEndEdit.AddListener((text) => OnFromDateChanged(fromDateField.text));
        toDateField.onEndEdit.AddListener((text) => OnToDateChanged(toDateField.text));

        DateTime now = DateTime.Now;
        fromDate = new DateTime(now.Year, now.Month, 1);
        toDate = now;

        fromDateField.text = fromDate.ToString("dd.MM.yyyy");
        toDateField.text = toDate.ToString("dd.MM.yyyy");

        RefreshUI();
    }

    public void OnFromDateChanged(string newDate)
    {
        DateTime parsed;
        if (DateTime.TryParse(newDate, out parsed))
        {
            fromDate = parsed;
            RefreshUI();
        }
    }

    public void OnToDateChanged(string newDate)
    {
        DateTime parsed;
        if (DateTime.TryParse(newDate, out parsed))
        {
            toDate = parsed;
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        foreach (Transform child in daysContainer)
            Destroy(child.gameObject);

        if (UserManager.CurrentUser == null) return;

        DateTime date = fromDate;
        while (date <= toDate)
        {
            string key = $"{date.Year}-{date.Month:D2}-{date.Day:D2}";
            MoodEntry entry = UserManager.CurrentUser.GetMoodEntry(key);

            GameObject obj = Instantiate(dayStatsPrefab, daysContainer);
            DayStatsUI ui = obj.GetComponent<DayStatsUI>();
            ui.Setup(date.Day, entry, emojiSprites);

            date = date.AddDays(1);
        }
    }
}

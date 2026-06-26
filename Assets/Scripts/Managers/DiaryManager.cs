using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiaryManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text userNameText;
    public TMP_Text yearText;
    public TMP_Text monthText;
    public TMP_Text currentDayOfWeekText;
    public TMP_Text currentDayText;

    public Transform daysContainer;
    public GameObject dayEntryPrefab; 

    public Button prevYearButton;
    public Button nextYearButton;
    public Button prevMonthButton;
    public Button nextMonthButton;
    public Button currentDayButton;

    private int currentYear;
    private int currentMonth;
    private int todayDay;

    private UserData currentUser;

    private void OnEnable()
    {
        if (UserManager.CurrentUser == null) return;
        currentUser = UserManager.CurrentUser;

        userNameText.text = currentUser.Name;

        DateTime now = DateTime.Now;
        currentYear = now.Year;
        currentMonth = now.Month;
        todayDay = now.Day;

        UpdateCalendar();
        UpdateCurrentDayInfo(now);

        currentDayButton.onClick.AddListener(() => FindFirstObjectByType<UIManager>().ShowNewEntry(currentYear, currentMonth, DateTime.Now.Day));
    }

    private void UpdateCalendar()
    {
        yearText.text = currentYear.ToString();
        monthText.text = new DateTime(currentYear, currentMonth, 1).ToString("MMMM");

        foreach (Transform child in daysContainer)
        {
            Destroy(child.gameObject);
        }

        int daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth);

        for (int day = 1; day <= daysInMonth; day++)
        {
            GameObject entryObj = Instantiate(dayEntryPrefab, daysContainer);
            DayEntryUI entryUI = entryObj.GetComponent<DayEntryUI>();

            string key = $"{currentYear}-{currentMonth:D2}-{day:D2}";
            string text = "";
            if (UserManager.CurrentUser != null)
            {
                DiaryEntry entry = UserManager.CurrentUser.GetDiaryEntry(key);
                if (entry != null)
                {
                    text = entry.mainText;
                }
            }
           
            int capturedDay = day;

            entryUI.Setup(capturedDay, text, () =>
            {
                FindFirstObjectByType<UIManager>().ShowNewEntry(currentYear, currentMonth, capturedDay);
            });
        }
    }

    private void UpdateCurrentDayInfo(DateTime date)
    {
        string[] daysRU = {"бя", "ом", "бр", "яп", "вр", "ор", "яа" };
        currentDayOfWeekText.text = daysRU[(int)date.DayOfWeek];

        currentDayText.text = date.Day.ToString();
    }

    public void OnPrevYear()
    {
        currentYear--;
        UpdateCalendar();
    }

    public void OnNextYear()
    {
        currentYear++;
        UpdateCalendar();
    }

    public void OnPrevMonth()
    {
        if (currentMonth > 1) currentMonth--;
        else { currentMonth = 12; currentYear--; }
        UpdateCalendar();
    }

    public void OnNextMonth()
    {
        if (currentMonth < 12) currentMonth++;
        else { currentMonth = 1; currentYear++; }
        UpdateCalendar();
    }

}

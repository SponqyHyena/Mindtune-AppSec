using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class MoodDiaryManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text monthText;
    public TMP_Text yearText;
    public Transform daysContainer;
    public GameObject dayButtonPrefab;

    private int currentYear;
    private int currentMonth;

    private void OnEnable()
    {
        DateTime now = DateTime.Now;
        currentYear = now.Year;
        currentMonth = now.Month;

        UpdateCalendar();
    }

    public void ChangeMonth(int delta)
    {
        currentMonth += delta;
        if (currentMonth < 1)
        {
            currentMonth = 12;
            currentYear--;
        }
        else if (currentMonth > 12)
        {
            currentMonth = 1;
            currentYear++;
        }

        UpdateCalendar();
    }

    public void ChangeYear(int delta)
    {
        currentYear += delta;
        UpdateCalendar();
    }

    private void UpdateCalendar()
    {
        foreach (Transform child in daysContainer)
            Destroy(child.gameObject);

        DateTime firstDay = new DateTime(currentYear, currentMonth, 1);
        int daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth);

        monthText.text = firstDay.ToString("MMMM", CultureInfo.GetCultureInfo("ru-RU"));
        yearText.text = currentYear.ToString();

        int startDayOfWeek = (int)firstDay.DayOfWeek;
        if (startDayOfWeek == 0) startDayOfWeek = 7;

        DateTime prevMonth = firstDay.AddMonths(-1);
        int prevMonthDays = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);

        for (int i = prevMonthDays - (startDayOfWeek - 2); i <= prevMonthDays; i++)
        {
            CreateDayButton(prevMonth.Year, prevMonth.Month, i, true);
        }

        for (int day = 1; day <= daysInMonth; day++)
        {
            CreateDayButton(currentYear, currentMonth, day, false);
        }

        DateTime lastDay = new DateTime(currentYear, currentMonth, daysInMonth);
        int endDayOfWeek = (int)lastDay.DayOfWeek;
        if (endDayOfWeek == 0) endDayOfWeek = 7;

        int extraDays = 7 - endDayOfWeek;
        for (int i = 1; i <= extraDays; i++)
        {
            DateTime nextMonth = firstDay.AddMonths(1);
            CreateDayButton(nextMonth.Year, nextMonth.Month, i, true);
        }
    }

    private void CreateDayButton(int year, int month, int day, bool isOtherMonth)
    {
        GameObject obj = Instantiate(dayButtonPrefab, daysContainer);
        DayButtonUI btn = obj.GetComponent<DayButtonUI>();

        string dateKey = $"{year}-{month:D2}-{day:D2}";

        bool hasEntry = false;
        if (UserManager.CurrentUser != null)
        {
            MoodEntry entry = UserManager.CurrentUser.GetMoodEntry(dateKey);
            if (entry != null && (entry.moodValue != 0 || entry.selectedEmojis.Count > 0))
            {
                hasEntry = true;
            }
        }

        int capturedDay = day;

        btn.Setup(capturedDay, hasEntry, isOtherMonth, () =>
        {
            if (!isOtherMonth)
            {
                FindFirstObjectByType<UIManager>().ShowMoodEntry(year, month, capturedDay);
            }
        });
    }
}

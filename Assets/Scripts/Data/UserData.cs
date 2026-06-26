using System;
using System.Collections.Generic;

[Serializable]
public class DiaryEntry
{
    public string mainText;
    public string did;
    public string felt;
    public string sensations;
    public string thoughts;
    public string photoPath;

    public DiaryEntry() { }
}

[Serializable]
public class MoodEntryWrapper
{
    public string Date;
    public MoodEntry Entry;

    public MoodEntryWrapper() { }

    public MoodEntryWrapper(string date, MoodEntry entry)
    {
        Date = date;
        Entry = entry;
    }
}

[Serializable]
public class DiaryEntryWrapper
{
    public string Date;
    public DiaryEntry Entry;

    public DiaryEntryWrapper() { }

    public DiaryEntryWrapper(string date, DiaryEntry entry)
    {
        Date = date;
        Entry = entry;
    }
}

[Serializable]
public class UserData
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Username;
    public string PasswordHash;
    public string Role;
    public string UserID;
    public string avatarImageData;

    public List<MoodEntryWrapper> moodEntries = new List<MoodEntryWrapper>();
    public List<DiaryEntryWrapper> diaryEntries = new List<DiaryEntryWrapper>();

    public UserData()
    {
        UserID = Guid.NewGuid().ToString();
        moodEntries = new List<MoodEntryWrapper>();
        diaryEntries = new List<DiaryEntryWrapper>();
    }

    public UserData(string name, string email, string username, string passwordHash, string role) : this()
    {
        Name = name;
        Email = email;
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
    }

    public void SetDiaryEntry(string date, DiaryEntry entry)
    {
        var existing = diaryEntries.Find(e => e.Date == date);
        if (existing != null)
        {
            existing.Entry = entry;
        }
        else
        {
            diaryEntries.Add(new DiaryEntryWrapper(date, entry));
        }
    }

    public DiaryEntry GetDiaryEntry(string date)
    {
        var existing = diaryEntries.Find(e => e.Date == date);
        return existing != null ? existing.Entry : null;
    }

    public void SetMoodEntry(string date, MoodEntry entry)
    {
        var existing = moodEntries.Find(e => e.Date == date);
        if (existing != null) existing.Entry = entry;
        else moodEntries.Add(new MoodEntryWrapper(date, entry));
    }

    public MoodEntry GetMoodEntry(string date)
    {
        var existing = moodEntries.Find(e => e.Date == date);
        return existing != null ? existing.Entry : null;
    }
}
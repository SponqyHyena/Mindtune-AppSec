using System.Collections.Generic;

[System.Serializable]
public class MoodEntry
{
    public int moodValue;
    public List<int> selectedEmojis = new List<int>();

    public MoodEntry()
    {
        selectedEmojis = new List<int>();
    }

    public MoodEntry(int value, List<int> emojis) : this()
    {
        moodValue = value;
        selectedEmojis = emojis ?? new List<int>();
    }
}
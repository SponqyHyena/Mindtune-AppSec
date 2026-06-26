using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DayStatsUI : MonoBehaviour
{
    public TMP_Text dayText;
    public Slider moodSlider;
    public Transform emojiContainer;
    public GameObject emojiPrefab;
    private Sprite[] emojiSprites;

    public void Setup(int day, MoodEntry entry, Sprite[] sprites)
    {
        dayText.text = day.ToString("D2");
        emojiSprites = sprites;

        if (entry != null)
        {
            moodSlider.value = entry.moodValue;

            foreach (Transform child in emojiContainer)
                Destroy(child.gameObject);

            foreach (int index in entry.selectedEmojis)
            {
                if (index >= 0 && index < emojiSprites.Length)
                {
                    GameObject obj = Instantiate(emojiPrefab, emojiContainer);
                    obj.GetComponent<Image>().sprite = emojiSprites[index];
                }
            }
        }
        else
        {
            moodSlider.value = 0;

            foreach (Transform child in emojiContainer)
                Destroy(child.gameObject);
        }
    }
}

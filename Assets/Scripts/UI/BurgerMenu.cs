using UnityEngine;
using UnityEngine.UI;

public class BurgerMenu : MonoBehaviour
{
    [Header("References")]
    public GameObject menuPanel;
    public GameObject photoUser;

    [Header("Menu Buttons")]
    public Button diarySelfButton;
    public Button moodDiaryButton;
    public Button moodStatsButton;
    public Button testsButton;
    public Button settingsButton;
    public Button logoutButton;

    private void OnEnable()
    {
        menuPanel.SetActive(false);
        photoUser.SetActive(true);
    }

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(ToggleMenu);

        diarySelfButton.onClick.AddListener(() =>
        {
            FindFirstObjectByType<UIManager>().ShowDiary(); 
        });

        moodDiaryButton.onClick.AddListener(() =>
        {
            FindFirstObjectByType<UIManager>().ShowMoodDiary();
        });

        moodStatsButton.onClick.AddListener(() =>
        {
            FindFirstObjectByType<UIManager>().ShowMoodAnalisys();
        });

        testsButton.onClick.AddListener(() =>
        {
            FindFirstObjectByType<UIManager>().ShowTests();
        });

        settingsButton.onClick.AddListener(() =>
        {
            FindFirstObjectByType<UIManager>().ShowSettings();
        });

        logoutButton.onClick.AddListener(() =>
        {
            FindFirstObjectByType<UIManager>().OnLogoutClick();
        });
    }

    private void ToggleMenu()
    {
        photoUser.SetActive(!photoUser.activeSelf);
        menuPanel.SetActive(!menuPanel.activeSelf);
    }

}

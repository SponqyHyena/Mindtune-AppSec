using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject mainMenuPanel;
    public GameObject newEntryPanel;
    public GameObject diaryPanel;
    public GameObject settingsPanel;
    public GameObject moodDiaryPanel;
    public GameObject moodEntryPanel;
    public GameObject moodAnalisysPanel;

    [Header("Register Fields")]
    public TMP_InputField regNameInput;
    public TMP_InputField regEmailInput;
    public TMP_InputField regUsernameInput;
    public TMP_InputField regPasswordInput;
    public TMP_InputField regConfirmPasswordInput; 
    public TMP_Dropdown regRoleDropdown;
    public TMP_Text regErrorText;

    [Header("Login Fields")]
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;
    public TMP_Text loginErrorText;

    public static UIManager Instance;

    public MoodEntryUI moodEntryUI;
    public UserData currentUser;
    private NewEntryUI newEntryUI;
    private UserManager userManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        userManager = FindFirstObjectByType<UserManager>();
        ShowLogin();
    }

    public void ShowLogin()
    {
        HideAll();
        loginPanel.SetActive(true);
        ClearLoginFields();
        ClearErrorText();
    }

    public void ShowRegister()
    {
        HideAll();
        registerPanel.SetActive(true);
        ClearLoginFields();
        ClearErrorText();
    }

    public void ShowMainMenu()
    {
        HideAll();
        mainMenuPanel.SetActive(true);
    }

    public void ShowDiary()
    {
        HideAll();
        diaryPanel.SetActive(true);
    }

    public void ShowTests()
    {
        HideAll();
    }

    public void ShowSettings()
    {
        HideAll();
        settingsPanel.SetActive(true);
    }

    public void ShowMoodAnalisys()
    {
        HideAll();
        moodAnalisysPanel.SetActive(true);
    }

    public void ShowMoodDiary()
    {
        HideAll();
        moodDiaryPanel.SetActive(true);
    }

    public void ShowNewEntry(int year, int month, int day)
    {
        HideAll();
        newEntryPanel.SetActive(true);

        newEntryUI = FindFirstObjectByType<NewEntryUI>();
        if (newEntryUI != null)
        {
            string key = $"{year:D4}-{month:D2}-{day:D2}";
            newEntryUI.LoadEntry(key);
        }
    }

    public void ShowMoodEntry(int year, int month, int day)
    {
        HideAll();
        moodEntryPanel.SetActive(true);

        if (moodEntryUI != null)
            moodEntryUI.LoadEntry(year, month, day);
    }

    private void HideAll()
    {
        settingsPanel.SetActive(false);
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        mainMenuPanel.SetActive(false);
        diaryPanel.SetActive(false);
        newEntryPanel.SetActive(false);
        moodDiaryPanel.SetActive(false);
        moodEntryPanel.SetActive(false);
        moodAnalisysPanel.SetActive(false);
    }

    public void OnRegisterClick()
    {
        ClearErrorText();
        ClearHighlightFields();

        string name = regNameInput.text;
        string email = regEmailInput.text;
        string username = regUsernameInput.text;
        string password = regPasswordInput.text;
        string role = regRoleDropdown.options[regRoleDropdown.value].text;

        if (string.IsNullOrWhiteSpace(regNameInput.text))
        {
            ShowError("Введите ваше имя");
            HighlightField(regNameInput, true);
            return;
        }

        if (string.IsNullOrWhiteSpace(regEmailInput.text))
        {
            ShowError("Введите email");
            HighlightField(regEmailInput, true);
            return;
        }

        if (string.IsNullOrWhiteSpace(regUsernameInput.text))
        {
            ShowError("Введите имя пользователя");
            HighlightField(regUsernameInput, true);
            return;
        }

        if (string.IsNullOrWhiteSpace(regPasswordInput.text))
        {
            ShowError("Введите пароль");
            HighlightField(regPasswordInput, true);
            return;
        }

        if (string.IsNullOrWhiteSpace(regConfirmPasswordInput.text))
        {
            ShowError("Подтвердите пароль");
            HighlightField(regConfirmPasswordInput, true);
            return;
        }

        if (regPasswordInput.text != regConfirmPasswordInput.text)
        {
            ShowError("Пароли не совпадают!");
            HighlightField(regPasswordInput, true);
            HighlightField(regConfirmPasswordInput, true);
            regConfirmPasswordInput.text = "";
            regConfirmPasswordInput.Select();
            return;
        }


        var result = userManager.RegisterWithError(name, email, username, password, role);

        switch (result)
        {
            case UserManager.RegistrationError.Success:
                ShowLogin();
                break;

            case UserManager.RegistrationError.InvalidName:
                ShowError("Имя должно содержать только буквы (2-15 символов)");
                HighlightField(regNameInput, true);
                break;

            case UserManager.RegistrationError.InvalidEmail:
                ShowError("Введите корректный email (например: user@mail.com)");
                HighlightField(regEmailInput, true);
                break;

            case UserManager.RegistrationError.InvalidUsername:
                ShowError("Имя пользователя должно содержать 3-15 символов (буквы, цифры)");
                HighlightField(regUsernameInput, true);
                break;

            case UserManager.RegistrationError.InvalidPassword:
                ShowError("Пароль должен содержать минимум 8 символов, буквы, цифры и спецсимволы");
                HighlightField(regPasswordInput, true);
                regPasswordInput.text = "";
                regConfirmPasswordInput.text = "";
                break;

            case UserManager.RegistrationError.UsernameTaken:
                ShowError("Это имя пользователя уже занято. Выберите другое.");
                HighlightField(regUsernameInput, true);
                break;

            default:
                ShowError("Ошибка регистрации. Попробуйте позже.");
                break;
        }
    }

    public void OnLoginClick()
    {
        ClearErrorText();

        string username = loginUsernameInput.text;
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(username))
        {

            ShowLoginError("Введите имя пользователя");
            HighlightField(loginUsernameInput, true);
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowLoginError("Введите пароль");
            HighlightField(loginPasswordInput, true);
            return;
        }


        username = InputValidator.SanitizeInput(username);

        bool success = userManager.Login(username, password);

        if (success)
        {
            ClearLoginError();
            AvatarManager.LoadAvatarForCurrentUser();
            ShowMainMenu();
        }
        else
        {
            ShowLoginError("Неверное имя пользователя или пароль");
            HighlightField(loginUsernameInput, true);
            HighlightField(loginPasswordInput, true);

            loginPasswordInput.text = "";
            loginPasswordInput.Select();
        }
    }

    public void OnLogoutClick()
    {
        UserManager.CurrentUser = null;
        ShowLogin();
    }

    private void ShowError(string message)
    {
        if (regErrorText != null)
        {
            regErrorText.text = message;
            regErrorText.gameObject.SetActive(true);
        }
        Debug.LogError($"Registration error: {message}");

        Invoke(nameof(ClearErrorText), 5f);
    }

    private void ShowLoginError(string message)
    {
        if (loginErrorText != null)
        {
            loginErrorText.text =  message;
            loginErrorText.gameObject.SetActive(true);
        }
        Debug.LogError($"Login error: {message}");

        Invoke(nameof(ClearLoginError), 5f);
    }

    private void ClearErrorText()
    {
        if (regErrorText != null)
        {
            regErrorText.text = "";
            regErrorText.gameObject.SetActive(false);
        }
    }

    private void ClearLoginError()
    {
        if (loginErrorText != null)
        {
            loginErrorText.text = "";
            loginErrorText.gameObject.SetActive(false);
        }
    }

    private void ClearLoginFields()
    {
        if (loginUsernameInput != null) loginUsernameInput.text = "";
        if (loginPasswordInput != null) loginPasswordInput.text = "";
        HighlightField(loginUsernameInput, false);
        HighlightField(loginPasswordInput, false);
    }

    private void ClearHighlightFields()
    {
        HighlightField(regNameInput, false);
        HighlightField(regEmailInput, false);
        HighlightField(regUsernameInput, false);
        HighlightField(regPasswordInput, false);
        HighlightField(regConfirmPasswordInput, false);
    }

    private void HighlightField(TMP_InputField field, bool highlight)
    {
        if (field == null) return;

        if (highlight)
        {
            var image = field.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = Color.red;
            }
        }
        else
        {
            var image = field.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = Color.white;
            }
        }
    }

}

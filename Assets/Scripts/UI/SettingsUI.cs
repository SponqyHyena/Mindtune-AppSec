using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField nameInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;

    [Header("Texts")]
    public TMP_Text userIdText;
    public TMP_Text errorText;

    [Header("UI Elements")]
    public Toggle themeToggle;
    public Button saveButton;
    public Button backButton;

    private void OnEnable()
    {
        var user = UserManager.CurrentUser;
        if (user != null)
        {
            nameInput.text = user.Name;
            passwordInput.text = "";
            confirmPasswordInput.text = "";
            userIdText.text = user.UserID;
        }

        errorText.text = "";
    }

    public void OnSave()
    {
        var user = UserManager.CurrentUser;
        if (user == null) return;

        if (!string.IsNullOrEmpty(passwordInput.text) || !string.IsNullOrEmpty(confirmPasswordInput.text))
        {
            if (passwordInput.text != confirmPasswordInput.text)
            {
                errorText.text = "Пароли не совпадают!";
                return;
            }

            if (!InputValidator.IsValidPassword(passwordInput.text))
            {
                errorText.text = "Пароль должен содержать минимум 8 символов, заглавную и строчную буквы, цифру и спецсимвол";
                return;
            }

            var userManager = FindFirstObjectByType<UserManager>();
            if (userManager != null)
                userManager.ChangePassword(user, passwordInput.text);
        }

        user.Name = nameInput.text;

        if (ThemeManager.Instance != null)
            ThemeManager.Instance.ToggleTheme(themeToggle.isOn);

        var userManager2 = FindFirstObjectByType<UserManager>();
        if (userManager2 != null)
            userManager2.SaveUsersData();

        errorText.text = "Изменения сохранены!";
    }

    public void OnBack()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowMainMenu();
    }
}
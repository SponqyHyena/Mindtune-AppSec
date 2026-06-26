using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ThemeElement
{
    public Graphic uiElement;

    public bool useColor = false;
    public Color lightColor = Color.white;
    public Color darkColor = Color.black;

    public bool useSprite = false;
    public Sprite lightSprite;
    public Sprite darkSprite;

}

[System.Serializable]
public class ThemePage
{
    [Header("Название страницы")]
    public string pageName;
    public List<ThemeElement> elements = new List<ThemeElement>();
}

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance;

    [Header("Список элементов для смены темы")]
    public List<ThemePage> pages = new List<ThemePage>();

    [Header("Текущая тема")]
    public bool isDarkTheme = false;

    public SettingsUI settingsUI;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey("Theme"))
        {
            bool dark = PlayerPrefs.GetInt("Theme") == 1;
            settingsUI.themeToggle.isOn = dark;
            ApplyTheme(dark);
        }
        else
        {
            ApplyTheme(isDarkTheme);
        }
    }

    public void ApplyTheme(bool dark)
    {
        isDarkTheme = dark;

        foreach (var page in pages)
        {
            foreach (var element in page.elements)
            {
                if (element.uiElement == null) continue;

                if (element.useColor)
                    element.uiElement.color = dark ? element.darkColor : element.lightColor;

                if (element.useSprite && element.uiElement is Image img)
                {
                    Sprite newSprite = dark ? element.darkSprite : element.lightSprite;
                    if (newSprite != null)
                        img.sprite = newSprite;
                }
            }
        }
    }

    public void ToggleTheme(bool dark)
    {
        ApplyTheme(dark);
        PlayerPrefs.SetInt("Theme", dark ? 1 : 0);
        PlayerPrefs.Save();
    }

}

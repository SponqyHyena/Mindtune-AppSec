using UnityEngine;
using TMPro;

public class NameUserUpdate : MonoBehaviour
{
    private TMP_Text userNameText;

    private void Awake()
    {
        userNameText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        userNameText.text = UserManager.CurrentUser.Name;
    }
}

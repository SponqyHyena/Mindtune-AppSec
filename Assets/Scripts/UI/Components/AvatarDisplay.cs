using UnityEngine;
using UnityEngine.UI;

public class AvatarDisplay : MonoBehaviour
{
    public Image avatarImage;
    public Sprite placeholder;

    private void OnEnable()
    {
        AvatarManager.OnAvatarChanged += UpdateAvatar;
        LoadCurrentAvatar();
    }

    private void OnDisable()
    {
        AvatarManager.OnAvatarChanged -= UpdateAvatar;
    }

    private void LoadCurrentAvatar()
    {
        if (AvatarManager.CurrentAvatar != null)
        {
            UpdateAvatar(AvatarManager.CurrentAvatar);
        }
        else if (placeholder != null)
        {
            avatarImage.sprite = placeholder;
        }
    }

    private void UpdateAvatar(Sprite sprite)
    {
        if (sprite != null)
        {
            avatarImage.sprite = sprite;
        }
        else if (placeholder != null)
        {
            avatarImage.sprite = placeholder;
        }
        avatarImage.SetMaterialDirty();
        avatarImage.SetVerticesDirty();
        Canvas.ForceUpdateCanvases();
    }

    public void Refresh()
    {
        LoadCurrentAvatar();
    }
}
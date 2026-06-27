using UnityEngine;

public static class GlobalInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Debug.Log("Initializing all managers...");

        UserManager.Initialize();
        AvatarManager.Initialize();
        ThemeManager.Initialize();
        UIManager.Initialize();
        StorageKeys.Initialize();

        DG.Tweening.DOTween.Init();

        Debug.Log("All managers initialized!");
    }
}
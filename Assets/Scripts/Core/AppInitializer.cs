using UnityEngine;

public class AppInitializer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        StorageKeys.Initialize();
        Debug.Log("StorageKeys initialized");
    }
}
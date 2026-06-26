using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class SecureJsonSerializer
{
    private static readonly JsonSerializerSettings SafeSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.None,  
        MissingMemberHandling = MissingMemberHandling.Ignore,
        PreserveReferencesHandling = PreserveReferencesHandling.None,
        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
        NullValueHandling = NullValueHandling.Ignore
    };
    
    public static string Serialize<T>(T obj)
    {
        if (obj == null) return null;
        
        try
        {
            return JsonConvert.SerializeObject(obj, SafeSettings);
        }
        catch (JsonException e)
        {
            Debug.LogError($"Serialization error: {e.Message}");
            return null;
        }
    }
    
    public static T Deserialize<T>(string json)
    {
        if (string.IsNullOrEmpty(json)) return default;
        
        try
        {
            JToken.Parse(json);
            return JsonConvert.DeserializeObject<T>(json, SafeSettings);
        }
        catch (JsonException e)
        {
            Debug.LogError($"Deserialization error: {e.Message}");
            return default;
        }
    }
}
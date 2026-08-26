using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public sealed class MythbreakerAssetPatch010 : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindFirstObjectByType<MythbreakerAssetPatch010>() == null)
            new GameObject("MYTHBREAKER ASSET PATCH 0.10").AddComponent<MythbreakerAssetPatch010>();
    }

    void Start()
    {
        Apply();
    }

    void Apply()
    {
        MythbreakerBootstrap boot = FindFirstObjectByType<MythbreakerBootstrap>();
        if (boot == null) return;

        SetTexture(boot, "assassin", Resources.Load<Texture2D>("MythbreakerSprites/assassin"));
        SetTexture(boot, "gorgon", Resources.Load<Texture2D>("MythbreakerSprites/gorgon"));
        SetTexture(boot, "minotaur", Resources.Load<Texture2D>("MythbreakerSprites/minotaur"));

        FieldInfo diagnostic = typeof(MythbreakerBootstrap).GetField("diagnostic", BindingFlags.Instance | BindingFlags.NonPublic);
        if (diagnostic != null)
            diagnostic.SetValue(boot, "MYTHBREAKER 0.10 • REAL ASSETS");
    }

    static void SetTexture(MythbreakerBootstrap boot, string fieldName, Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogError("MYTHBREAKER 0.10 missing real texture: " + fieldName);
            return;
        }

        FieldInfo field = typeof(MythbreakerBootstrap).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            Debug.LogError("MYTHBREAKER 0.10 missing bootstrap field: " + fieldName);
            return;
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        field.SetValue(boot, texture);
        Debug.Log("MYTHBREAKER 0.10 loaded real texture: " + fieldName + " (" + texture.width + "x" + texture.height + ")");
    }
}

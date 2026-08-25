using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Hard bootstrap stored directly in the first scene.
/// It guarantees that Mythbreaker starts even when a cloud builder skips editor preparation.
/// It also reconstructs the approved menu artwork directly from the embedded Base64 TextAsset.
/// </summary>
[DefaultExecutionOrder(-10000)]
public sealed class MythbreakerBootstrap : MonoBehaviour
{
    Texture2D runtimeMenu;

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.Portrait;

        EnsureGame();
        LoadEmbeddedMenu();
        InjectMenuTexture();
    }

    void Start()
    {
        EnsureGame();
        InjectMenuTexture();
    }

    void EnsureGame()
    {
        if (FindFirstObjectByType<MythbreakerGame>() != null) return;

        GameObject game = new GameObject("MYTHBREAKER GAME");
        game.AddComponent<MythbreakerGame>();
    }

    void LoadEmbeddedMenu()
    {
        TextAsset encoded = Resources.Load<TextAsset>("mythbreaker_menu_b64");
        if (encoded == null || string.IsNullOrWhiteSpace(encoded.text))
        {
            Debug.LogError("MYTHBREAKER: embedded menu artwork is missing.");
            return;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(encoded.text.Trim());
            runtimeMenu = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!runtimeMenu.LoadImage(bytes, false))
            {
                Destroy(runtimeMenu);
                runtimeMenu = null;
                Debug.LogError("MYTHBREAKER: embedded menu artwork could not be decoded.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("MYTHBREAKER: menu decode failed: " + e.Message);
        }
    }

    void InjectMenuTexture()
    {
        if (runtimeMenu == null) return;

        MythbreakerGame game = FindFirstObjectByType<MythbreakerGame>();
        if (game == null) return;

        FieldInfo menuField = typeof(MythbreakerGame).GetField("menuTexture", BindingFlags.Instance | BindingFlags.NonPublic);
        if (menuField == null)
        {
            Debug.LogError("MYTHBREAKER: menuTexture field not found.");
            return;
        }

        menuField.SetValue(game, runtimeMenu);
    }
}

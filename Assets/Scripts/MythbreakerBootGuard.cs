using System;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-20000)]
public sealed class MythbreakerBootGuard : MonoBehaviour
{
    Texture2D menu;
    bool frontEnd = true;
    string error;

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.Portrait;
        DontDestroyOnLoad(gameObject);
        LoadMenu();
        EnsureGame();
    }

    void LoadMenu()
    {
        try
        {
            TextAsset encoded = Resources.Load<TextAsset>("mythbreaker_menu_b64");
            if (encoded == null || string.IsNullOrWhiteSpace(encoded.text)) return;
            byte[] bytes = Convert.FromBase64String(encoded.text.Trim());
            menu = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!menu.LoadImage(bytes, false)) menu = null;
        }
        catch (Exception e)
        {
            error = "MENU: " + e.Message;
        }
    }

    MythbreakerGame EnsureGame()
    {
        MythbreakerGame game = FindFirstObjectByType<MythbreakerGame>();
        if (game != null) return game;
        try
        {
            return new GameObject("MYTHBREAKER GAME 0.3").AddComponent<MythbreakerGame>();
        }
        catch (Exception e)
        {
            error = "GAME: " + e.Message;
            return null;
        }
    }

    void OnGUI()
    {
        int w = Screen.width;
        int h = Screen.height;

        if (frontEnd)
        {
            if (menu != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0, 0, w, h), menu, ScaleMode.ScaleAndCrop);
            }
            else
            {
                GUI.color = new Color(0.015f, 0.05f, 0.18f, 1f);
                GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
                GUI.color = new Color(0.95f, 0.68f, 0.10f);
                GUI.Label(new Rect(0, h * 0.35f, w, 90), "MYTHBREAKER", Center(48));
                GUI.color = Color.white;
                GUI.Label(new Rect(0, h * 0.44f, w, 60), "lello's game", Center(26));
            }

            GUIStyle invisible = new GUIStyle(GUI.skin.button);
            invisible.normal.background = null;
            invisible.hover.background = null;
            invisible.active.background = null;
            invisible.normal.textColor = new Color(0, 0, 0, 0);

            if (GUI.Button(new Rect(w * 0.12f, h * 0.77f, w * 0.76f, h * 0.12f), "START", invisible))
                StartGame();

            if (GUI.Button(new Rect(w * 0.04f, h * 0.89f, w * 0.44f, h * 0.10f), "HEROES", invisible))
                OpenState("Heroes");

            if (GUI.Button(new Rect(w * 0.52f, h * 0.89f, w * 0.44f, h * 0.10f), "SETTINGS", invisible))
                OpenState("Settings");
        }

        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(new Rect(0, h - 28, w, 28), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, h - 28, w, 28), "MYTHBREAKER 0.3  •  MAIN", Center(16));

        if (!string.IsNullOrEmpty(error))
        {
            GUI.color = new Color(0.65f, 0.02f, 0.02f, 0.92f);
            GUI.DrawTexture(new Rect(10, 45, w - 20, 100), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(18, 52, w - 36, 86), error, Center(15));
        }
        GUI.color = Color.white;
    }

    GUIStyle Center(int size)
    {
        return new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = size,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
    }

    void StartGame()
    {
        try
        {
            MythbreakerGame game = EnsureGame();
            if (game == null) return;
            MethodInfo start = typeof(MythbreakerGame).GetMethod("StartRun", BindingFlags.Instance | BindingFlags.NonPublic);
            if (start == null) { error = "StartRun non trovato"; return; }
            start.Invoke(game, null);
            frontEnd = false;
        }
        catch (Exception e)
        {
            error = "START: " + (e.InnerException != null ? e.InnerException.Message : e.Message);
        }
    }

    void OpenState(string stateName)
    {
        try
        {
            MythbreakerGame game = EnsureGame();
            if (game == null) return;
            Type stateType = typeof(MythbreakerGame).GetNestedType("GameState", BindingFlags.NonPublic);
            FieldInfo stateField = typeof(MythbreakerGame).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);
            if (stateType == null || stateField == null) return;
            stateField.SetValue(game, Enum.Parse(stateType, stateName));
            frontEnd = false;
        }
        catch (Exception e)
        {
            error = stateName.ToUpperInvariant() + ": " + e.Message;
        }
    }
}

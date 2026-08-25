#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MythbreakerBuild
{
    const string MainScene = "Assets/Scenes/Main.unity";
    const string MenuBase64 = "Assets/Resources/mythbreaker_menu_b64.txt";
    const string MenuJpg = "Assets/Resources/mythbreaker_menu.jpg";
    const string IconPng = "Assets/Art/mythbreaker_icon.png";

    [MenuItem("Mythbreaker/Prepare Project")]
    public static void PrepareProject()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Resources");
        Directory.CreateDirectory("Assets/Art");
        Directory.CreateDirectory("Builds/Android");

        PrepareEmbeddedArt();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var entry = new GameObject("Mythbreaker Entry");
        entry.AddComponent<MythbreakerGame>();
        EditorSceneManager.SaveScene(scene, MainScene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };

        PlayerSettings.companyName = "Lello's Game";
        PlayerSettings.productName = "Mythbreaker";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.lellosgame.mythbreaker");
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPng);
        if (icon != null)
        {
            try { PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { icon }); }
            catch (Exception e) { Debug.LogWarning("Icon assignment skipped: " + e.Message); }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("MYTHBREAKER project prepared: Build 0.1");
    }

    static void PrepareEmbeddedArt()
    {
        if (!File.Exists(MenuBase64))
        {
            Debug.LogWarning("Embedded menu art not found yet. Using code fallback background.");
            return;
        }

        string raw = File.ReadAllText(MenuBase64).Trim();
        byte[] bytes = Convert.FromBase64String(raw);
        File.WriteAllBytes(MenuJpg, bytes);

        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (source.LoadImage(bytes))
        {
            int side = Mathf.Min(source.width, source.height);
            int cropY = Mathf.Clamp(40, 0, source.height - side);
            Color[] pixels = source.GetPixels(0, cropY, side, side);
            var icon = new Texture2D(side, side, TextureFormat.RGBA32, false);
            icon.SetPixels(pixels);
            icon.Apply();
            File.WriteAllBytes(IconPng, icon.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(icon);
        }
        UnityEngine.Object.DestroyImmediate(source);
        AssetDatabase.Refresh();
    }

    [MenuItem("Mythbreaker/Build Android APK 0.1")]
    public static void BuildAndroid()
    {
        PrepareProject();
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            throw new Exception("Unable to switch Unity to Android target. Install Android Build Support in Unity Hub.");

        EditorUserBuildSettings.buildAppBundle = false;
        Directory.CreateDirectory("Builds/Android");

        var options = new BuildPlayerOptions
        {
            scenes = new[] { MainScene },
            locationPathName = "Builds/Android/Mythbreaker-0.1.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Mythbreaker Android build failed: " + report.summary.result);

        Debug.Log("MYTHBREAKER APK READY: Builds/Android/Mythbreaker-0.1.apk");
    }
}
#endif

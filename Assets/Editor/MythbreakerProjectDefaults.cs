#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MythbreakerProjectDefaults
{
    const string MenuBase64 = "Assets/Resources/mythbreaker_menu_b64.txt";
    const string MenuJpg = "Assets/Resources/mythbreaker_menu.jpg";
    const string IconPng = "Assets/Art/mythbreaker_icon.png";
    const string MainScene = "Assets/Scenes/Main.unity";

    static MythbreakerProjectDefaults()
    {
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("Mythbreaker/Apply Project Defaults")]
    public static void Apply()
    {
        try
        {
            Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory("Assets/Art");

            EnsureArtwork();

            PlayerSettings.companyName = "Lello's Game";
            PlayerSettings.productName = "Mythbreaker";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.lellosgame.mythbreaker");
            PlayerSettings.Android.bundleVersionCode = 2;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            if (File.Exists(MainScene))
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPng);
            if (icon != null)
            {
                try { PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { icon }); }
                catch (Exception e) { Debug.LogWarning("MYTHBREAKER icon assignment skipped: " + e.Message); }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("MYTHBREAKER PROJECT DEFAULTS APPLIED: product, bundle id, icon and startup scene are ready.");
        }
        catch (Exception e)
        {
            Debug.LogError("MYTHBREAKER PROJECT DEFAULTS FAILED: " + e);
        }
    }

    static void EnsureArtwork()
    {
        if (!File.Exists(MenuBase64)) return;

        string encoded = File.ReadAllText(MenuBase64).Trim();
        if (string.IsNullOrEmpty(encoded)) return;

        byte[] bytes = Convert.FromBase64String(encoded);
        File.WriteAllBytes(MenuJpg, bytes);
        AssetDatabase.ImportAsset(MenuJpg, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        Texture2D source = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (!source.LoadImage(bytes, false))
        {
            UnityEngine.Object.DestroyImmediate(source);
            return;
        }

        int side = Mathf.Min(source.width, source.height);
        int x = Mathf.Max(0, (source.width - side) / 2);
        int y = Mathf.Max(0, (source.height - side) / 2);
        Color[] pixels = source.GetPixels(x, y, side, side);

        Texture2D icon = new Texture2D(side, side, TextureFormat.RGB24, false);
        icon.SetPixels(pixels);
        icon.Apply(false, false);
        File.WriteAllBytes(IconPng, icon.EncodeToPNG());

        UnityEngine.Object.DestroyImmediate(icon);
        UnityEngine.Object.DestroyImmediate(source);

        AssetDatabase.ImportAsset(IconPng, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class MythbreakerCloudBuildPrep : IPreprocessBuildWithReport
{
    const string MainScene = "Assets/Scenes/Main.unity";
    const string MenuBase64 = "Assets/Resources/mythbreaker_menu_b64.txt";
    const string MenuJpg = "Assets/Resources/mythbreaker_menu.jpg";
    const string IconPng = "Assets/Art/mythbreaker_icon.png";

    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("MYTHBREAKER 0.4 PREBUILD START");

        Directory.CreateDirectory("Assets/Resources");
        Directory.CreateDirectory("Assets/Art");
        EnsureArtworkExists();

        PlayerSettings.companyName = "Lello's Game";
        PlayerSettings.productName = "Mythbreaker";
        PlayerSettings.bundleVersion = "0.4.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.lellosgame.mythbreaker");
        PlayerSettings.Android.bundleVersionCode = 4;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        if (!File.Exists(MainScene))
            throw new BuildFailedException("Mythbreaker Main.unity is missing.");

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPng);
        if (icon != null)
        {
            try
            {
                int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
                if (sizes != null && sizes.Length > 0)
                {
                    Texture2D[] icons = new Texture2D[sizes.Length];
                    for (int i = 0; i < icons.Length; i++) icons[i] = icon;
                    PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);
                    Debug.Log("MYTHBREAKER: Android launcher icons assigned to " + icons.Length + " slots.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("MYTHBREAKER icon assignment warning: " + e.Message);
            }
        }
        else Debug.LogWarning("MYTHBREAKER icon texture could not be loaded.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("MYTHBREAKER 0.4 PREBUILD READY - scene preserved, product settings applied.");
    }

    static void EnsureArtworkExists()
    {
        if (!File.Exists(MenuBase64)) return;

        try
        {
            string encoded = File.ReadAllText(MenuBase64).Trim();
            if (string.IsNullOrEmpty(encoded)) return;
            byte[] bytes = Convert.FromBase64String(encoded);

            if (!File.Exists(MenuJpg))
            {
                File.WriteAllBytes(MenuJpg, bytes);
                AssetDatabase.ImportAsset(MenuJpg, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }

            if (!File.Exists(IconPng))
            {
                Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (source.LoadImage(bytes, false))
                {
                    int side = Mathf.Min(source.width, source.height);
                    int x = Mathf.Max(0, (source.width - side) / 2);
                    int y = Mathf.Max(0, (source.height - side) / 2);
                    Color[] pixels = source.GetPixels(x, y, side, side);
                    Texture2D square = new Texture2D(side, side, TextureFormat.RGBA32, false);
                    square.SetPixels(pixels);
                    square.Apply(false, false);
                    File.WriteAllBytes(IconPng, square.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(square);
                }
                UnityEngine.Object.DestroyImmediate(source);
                AssetDatabase.ImportAsset(IconPng, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("MYTHBREAKER artwork generation warning: " + e.Message);
        }
    }
}
#endif

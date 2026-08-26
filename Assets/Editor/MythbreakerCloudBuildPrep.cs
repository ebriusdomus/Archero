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
    const string IconBase64 = "Assets/Resources/mythbreaker_icon_b64.txt";
    const string IconJpg = "Assets/Art/mythbreaker_icon.jpg";

    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("MYTHBREAKER 0.5 PREBUILD START");

        Directory.CreateDirectory("Assets/Resources");
        Directory.CreateDirectory("Assets/Art");

        WriteImageFromBase64(MenuBase64, MenuJpg, "menu");
        WriteImageFromBase64(IconBase64, IconJpg, "icon");

        PlayerSettings.companyName = "Lello's Game";
        PlayerSettings.productName = "Mythbreaker";
        PlayerSettings.bundleVersion = "0.5.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.lellosgame.mythbreaker");
        PlayerSettings.Android.bundleVersionCode = 5;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        if (!File.Exists(MainScene))
            throw new BuildFailedException("Mythbreaker Main.unity is missing.");

        // Preserve the committed scene. Never generate or overwrite Main.unity during cloud builds.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };

        AssetDatabase.ImportAsset(MenuJpg, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(IconJpg, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconJpg);
        if (icon == null)
            throw new BuildFailedException("Mythbreaker launcher icon failed to import.");

        try
        {
            int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
            if (sizes == null || sizes.Length == 0)
                throw new Exception("Unity returned no Android launcher icon slots.");

            Texture2D[] icons = new Texture2D[sizes.Length];
            for (int i = 0; i < icons.Length; i++) icons[i] = icon;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);
            Debug.Log("MYTHBREAKER: assigned branded launcher icon to " + icons.Length + " Android slots.");
        }
        catch (Exception e)
        {
            throw new BuildFailedException("Mythbreaker Android icon assignment failed: " + e.Message);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("MYTHBREAKER 0.5 PREBUILD READY - menu, launcher icon, player settings and committed scene verified.");
    }

    static void WriteImageFromBase64(string sourcePath, string outputPath, string label)
    {
        if (!File.Exists(sourcePath))
            throw new BuildFailedException("Mythbreaker " + label + " Base64 source is missing: " + sourcePath);

        string encoded = File.ReadAllText(sourcePath).Trim();
        if (string.IsNullOrEmpty(encoded))
            throw new BuildFailedException("Mythbreaker " + label + " Base64 source is empty.");

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(encoded);
        }
        catch (Exception e)
        {
            throw new BuildFailedException("Mythbreaker " + label + " Base64 is invalid: " + e.Message);
        }

        if (bytes.Length < 1000)
            throw new BuildFailedException("Mythbreaker " + label + " image data is unexpectedly small.");

        File.WriteAllBytes(outputPath, bytes);
        Debug.Log("MYTHBREAKER: wrote " + label + " asset (" + bytes.Length + " bytes) to " + outputPath);
    }
}
#endif

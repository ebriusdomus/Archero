#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class MythbreakerCloudBuildPrep : IPreprocessBuildWithReport
{
    const string MainScene = "Assets/Scenes/Main.unity";
    const string IconBase64 = "Assets/Resources/mythbreaker_icon_b64.txt";
    const string GeneratedDir = "Assets/Generated";
    const string IconPath = GeneratedDir + "/mythbreaker_icon.png";

    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("MYTHBREAKER 0.7 PREBUILD START");

        if (!File.Exists(MainScene))
            throw new BuildFailedException("Main scene missing: " + MainScene);
        if (!File.Exists(IconBase64))
            throw new BuildFailedException("Icon source missing: " + IconBase64);

        Directory.CreateDirectory(GeneratedDir);
        WriteIconFile();
        ImportIcon();
        ApplyPlayerSettings();
        ApplyAndroidIcons();

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Debug.Log("MYTHBREAKER 0.7 PREBUILD READY");
    }

    static void WriteIconFile()
    {
        string encoded = File.ReadAllText(IconBase64)
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace(" ", "")
            .Trim();

        byte[] bytes;
        try { bytes = Convert.FromBase64String(encoded); }
        catch (Exception e) { throw new BuildFailedException("Icon Base64 invalid: " + e.Message); }

        if (bytes.Length < 1000)
            throw new BuildFailedException("Icon data is too small.");

        File.WriteAllBytes(IconPath, bytes);
    }

    static void ImportIcon()
    {
        AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (importer == null)
            throw new BuildFailedException("Icon TextureImporter unavailable.");

        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }

    static void ApplyPlayerSettings()
    {
        PlayerSettings.companyName = "Lello's Game";
        PlayerSettings.productName = "Mythbreaker";
        PlayerSettings.bundleVersion = "0.7.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.lellosgame.mythbreaker");
        PlayerSettings.Android.bundleVersionCode = 7;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
    }

    static void ApplyAndroidIcons()
    {
        Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (iconTexture == null)
            throw new BuildFailedException("Imported launcher icon could not be loaded.");

        NamedBuildTarget target = NamedBuildTarget.Android;
        PlatformIconKind[] kinds = PlayerSettings.GetSupportedIconKinds(target);
        if (kinds == null || kinds.Length == 0)
            throw new BuildFailedException("Unity returned no Android icon kinds.");

        int assigned = 0;
        foreach (PlatformIconKind kind in kinds)
        {
            PlatformIcon[] slots = PlayerSettings.GetPlatformIcons(target, kind);
            if (slots == null) continue;

            for (int i = 0; i < slots.Length; i++)
            {
                int layers = Mathf.Max(1, slots[i].maxLayerCount);
                Texture2D[] textures = new Texture2D[layers];
                for (int layer = 0; layer < layers; layer++) textures[layer] = iconTexture;
                slots[i].SetTextures(textures);
                assigned++;
            }

            PlayerSettings.SetPlatformIcons(target, kind, slots);
        }

        int[] legacySizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
        if (legacySizes != null && legacySizes.Length > 0)
        {
            Texture2D[] legacy = new Texture2D[legacySizes.Length];
            for (int i = 0; i < legacy.Length; i++) legacy[i] = iconTexture;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, legacy);
        }

        Debug.Log("MYTHBREAKER 0.7 assigned launcher artwork to " + assigned + " modern Android icon slots.");
    }
}
#endif
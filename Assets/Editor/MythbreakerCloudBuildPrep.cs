#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Unity Build Automation does not invoke our custom BuildAndroid method.
/// This hook runs automatically before EVERY Unity build (local or cloud)
/// and prepares the real Mythbreaker scene, menu artwork and Android icon.
/// </summary>
public sealed class MythbreakerCloudBuildPrep : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("MYTHBREAKER CLOUD PREBUILD: preparing scene, menu art and Android icon...");
        MythbreakerBuild.PrepareProject();
        Debug.Log("MYTHBREAKER CLOUD PREBUILD: ready.");
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class MythbreakerVersion011 : IPreprocessBuildWithReport
{
    public int callbackOrder => -800;

    public void OnPreprocessBuild(BuildReport report)
    {
        PlayerSettings.bundleVersion = "0.12.0";
        PlayerSettings.Android.bundleVersionCode = 12;
        Debug.Log("MYTHBREAKER 0.12 VISUAL REBUILD READY");
    }
}
#endif

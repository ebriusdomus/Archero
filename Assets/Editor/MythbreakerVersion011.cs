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
        PlayerSettings.bundleVersion = "0.11.0";
        PlayerSettings.Android.bundleVersionCode = 11;
        Debug.Log("MYTHBREAKER 0.11 VISUAL BUILD READY");
    }
}
#endif

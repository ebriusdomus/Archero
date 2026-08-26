#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class MythbreakerVersion010 : IPreprocessBuildWithReport
{
    public int callbackOrder => -900;

    public void OnPreprocessBuild(BuildReport report)
    {
        PlayerSettings.bundleVersion = "0.10.0";
        PlayerSettings.Android.bundleVersionCode = 10;
        Debug.Log("MYTHBREAKER 0.10 VERSION OVERRIDE READY");
    }
}
#endif

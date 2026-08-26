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
        PlayerSettings.bundleVersion = "0.13.0";
        PlayerSettings.Android.bundleVersionCode = 13;
        Debug.Log("MYTHBREAKER 0.13 VISUAL POLISH READY");
    }
}
#endif

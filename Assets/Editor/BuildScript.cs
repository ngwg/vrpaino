using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    public static void BuildIOS()
    {
        Debug.Log("Starting iOS build...");

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/ARPiano.unity" },
            locationPathName = "Build/iOS",
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {report.summary.totalSize} bytes");
        }
        else
        {
            Debug.LogError($"Build failed: {report.summary.totalErrors} errors");
            System.Environment.Exit(1);
        }
    }
}

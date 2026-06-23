using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using System.IO;

public class iOSPostProcess : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS)
            return;

        string projPath = PBXProject.GetPBXProjectPath(report.summary.outputPath);
        var proj = new PBXProject();
        proj.ReadFromString(File.ReadAllText(projPath));

        string mainTarget = proj.GetUnityMainTargetGuid();
        string frameworkTarget = proj.GetUnityFrameworkTargetGuid();

        proj.AddFrameworkToProject(mainTarget, "ARKit.framework", false);
        proj.AddFrameworkToProject(frameworkTarget, "ARKit.framework", false);

        File.WriteAllText(projPath, proj.WriteToString());
        UnityEngine.Debug.Log("[VRPiano] Added ARKit.framework to Xcode project");
    }
}

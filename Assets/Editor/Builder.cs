using UnityEditor;
using UnityEngine;
using System.Linq;

public static class Builder
{
    public static void BuildWebGL()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        BuildProject(BuildTarget.WebGL, "build/WebGL");
    }

    public static void BuildMac()
    {
        BuildProject(BuildTarget.StandaloneOSX, "build/Mac/Game.app");
    }

    public static void BuildFromCI()
    {
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        switch (buildTarget)
        {
            case BuildTarget.WebGL:
                BuildWebGL();
                break;
            case BuildTarget.StandaloneOSX:
                BuildMac();
                break;
            case BuildTarget.StandaloneWindows64:
                BuildProject(BuildTarget.StandaloneWindows64, "build/Windows/Game.exe");
                break;
            default:
                Debug.LogError($"Unsupported build target/CI setup: {buildTarget}");
                // Instead of failing, try to perform a direct build based on target
                // This handles cases where target might be StandaloneLinux64, etc.
                BuildProject(buildTarget, $"build/{buildTarget}");
                break;
        }
    }

    private static void BuildProject(BuildTarget buildTarget, string buildPath)
    {
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = buildPath,
            target = buildTarget,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize} bytes");
        }
        else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError("Build failed");
            EditorApplication.Exit(1);
        }
    }
}

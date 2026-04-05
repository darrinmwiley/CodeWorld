using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Runs rebuild-proto.ps1 before entering Play mode so that C# gRPC stubs
/// and the Java server are always in sync with game.proto.
///
/// Toggle via: Tools > Proto Rebuild > Enable Auto-Rebuild on Play
/// </summary>
[InitializeOnLoad]
public static class RebuildProtoOnPlay
{
    private const string PrefKey  = "RebuildProto_AutoRebuildEnabled";
    private const string MenuPath = "Tools/Proto Rebuild/Enable Auto-Rebuild on Play";

    static RebuildProtoOnPlay()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static bool IsEnabled
    {
        get => EditorPrefs.GetBool(PrefKey, false);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    [MenuItem(MenuPath)]
    private static void ToggleAutoRebuild()
    {
        IsEnabled = !IsEnabled;
        UnityEngine.Debug.Log($"[ProtoRebuild] Auto-rebuild on play: {(IsEnabled ? "ENABLED" : "DISABLED")}");
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleAutoRebuildValidate()
    {
        Menu.SetChecked(MenuPath, IsEnabled);
        return true;
    }

    [MenuItem("Tools/Proto Rebuild/Rebuild Now")]
    public static void RebuildNow()
    {
        RunRebuildScript();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Fire just before the Editor switches into Play mode
        if (state != PlayModeStateChange.ExitingEditMode) return;
        if (!IsEnabled) return;

        bool success = RunRebuildScript();
        if (!success)
        {
            // Stop entering Play mode if the build failed
            EditorApplication.isPlaying = false;
        }
    }

    private static bool RunRebuildScript()
    {
        // Resolve path to rebuild-proto.ps1 at project root
        string projectRoot  = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string scriptPath   = Path.Combine(projectRoot, "rebuild-proto.ps1");

        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogError($"[ProtoRebuild] Script not found: {scriptPath}");
            return false;
        }

        UnityEngine.Debug.Log("[ProtoRebuild] Running rebuild-proto.ps1...");

        var psi = new ProcessStartInfo
        {
            FileName               = "powershell.exe",
            Arguments              = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
            WorkingDirectory       = projectRoot,
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
        };

        using var process = Process.Start(psi);
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(stdout))
            UnityEngine.Debug.Log("[ProtoRebuild] " + stdout.Trim());

        if (!string.IsNullOrWhiteSpace(stderr))
            UnityEngine.Debug.LogWarning("[ProtoRebuild] stderr: " + stderr.Trim());

        if (process.ExitCode != 0)
        {
            UnityEngine.Debug.LogError($"[ProtoRebuild] rebuild-proto.ps1 failed with exit code {process.ExitCode}");
            return false;
        }

        UnityEngine.Debug.Log("[ProtoRebuild] Rebuild complete.");
        AssetDatabase.Refresh(); // Reload the newly generated .cs files in Unity
        return true;
    }
}

namespace ScrcpyLauncher.Core.Models;

public sealed class LauncherPaths
{
    public required string ToolsDirectory { get; init; }

    public required string ScrcpyExecutablePath { get; init; }

    public string? ScrcpyServerPath { get; init; }

    public string? AdbExecutablePath { get; init; }
}

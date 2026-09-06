namespace ScrcpyLauncher.Core.Models;

public sealed class LauncherState
{
    public List<LaunchProfile> Presets { get; set; } = new();

    public string? LastSelectedPresetName { get; set; }

    public static LauncherState CreateDefault()
    {
        return new LauncherState
        {
            Presets =
            [
                new LaunchProfile(),
            ],
            LastSelectedPresetName = "Default",
        };
    }
}

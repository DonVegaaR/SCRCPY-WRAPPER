namespace ScrcpyLauncher.Core.Models;

public sealed record class LaunchProfile
{
    public string Name { get; set; } = "Default";

    public string ToolsDirectory { get; set; } = string.Empty;

    public string? SelectedDeviceSerial { get; set; }

    public string MaxSize { get; set; } = string.Empty;

    public string MaxFps { get; set; } = string.Empty;

    public string VideoBitRate { get; set; } = string.Empty;

    public bool AudioEnabled { get; set; } = true;

    public bool VideoEnabled { get; set; } = true;

    public bool ControlEnabled { get; set; } = true;

    public bool Fullscreen { get; set; }

    public bool StayAwake { get; set; }

    public bool TurnScreenOff { get; set; }

    public bool AlwaysOnTop { get; set; }

    public string RecordPath { get; set; } = string.Empty;

    public string WindowTitle { get; set; } = string.Empty;

    public string AdditionalArguments { get; set; } = string.Empty;

    public bool PauseOnExitIfError { get; set; } = true;
}

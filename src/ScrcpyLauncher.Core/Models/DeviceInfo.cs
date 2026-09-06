namespace ScrcpyLauncher.Core.Models;

public sealed class DeviceInfo
{
    public string Serial { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;

    public string? Model { get; init; }

    public bool IsOnline => string.Equals(State, "device", StringComparison.OrdinalIgnoreCase);

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Model)
            ? $"{Serial} ({State})"
            : $"{Model} [{Serial}] ({State})";
}

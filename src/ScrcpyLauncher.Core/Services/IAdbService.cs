using ScrcpyLauncher.Core.Models;

namespace ScrcpyLauncher.Core.Services;

public interface IAdbService
{
    Task<IReadOnlyList<DeviceInfo>> ListDevicesAsync(string? adbExecutablePath, CancellationToken cancellationToken);
}

using ScrcpyLauncher.Core.Models;

namespace ScrcpyLauncher.Core.Services;

public interface ILaunchProfileStore
{
    string FilePath { get; }

    Task<LauncherState> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(LauncherState state, CancellationToken cancellationToken);
}

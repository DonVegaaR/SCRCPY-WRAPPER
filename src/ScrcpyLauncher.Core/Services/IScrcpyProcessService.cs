using ScrcpyLauncher.Core.Models;

namespace ScrcpyLauncher.Core.Services;

public interface IScrcpyProcessService : IAsyncDisposable
{
    bool IsRunning { get; }

    Task StartAsync(
        LauncherPaths paths,
        IReadOnlyList<string> arguments,
        Action<string> onOutput,
        Action<int?> onExit,
        CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

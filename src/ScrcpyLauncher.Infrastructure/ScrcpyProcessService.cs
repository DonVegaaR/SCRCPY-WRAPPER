using System.Diagnostics;
using ScrcpyLauncher.Core.Models;

namespace ScrcpyLauncher.Infrastructure;

public sealed class ScrcpyProcessService
{
    private readonly object gate = new();
    private Process? process;

    public bool IsRunning
    {
        get
        {
            lock (gate)
            {
                return process is { HasExited: false };
            }
        }
    }

    public Task StartAsync(
        LauncherPaths paths,
        IReadOnlyList<string> arguments,
        Action<string> onOutput,
        Action<int?> onExit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(onOutput);
        ArgumentNullException.ThrowIfNull(onExit);

        lock (gate)
        {
            if (process is { HasExited: false })
            {
                throw new InvalidOperationException("scrcpy is already running.");
            }

            var startInfo = new ProcessStartInfo(paths.ScrcpyExecutablePath)
            {
                WorkingDirectory = paths.ToolsDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            if (!string.IsNullOrWhiteSpace(paths.ScrcpyServerPath))
            {
                startInfo.Environment["SCRCPY_SERVER_PATH"] = paths.ScrcpyServerPath;
            }

            process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            if (!process.Start())
            {
                throw new InvalidOperationException("scrcpy could not be started.");
            }

            _ = PumpOutputAsync(process.StandardOutput, onOutput, cancellationToken);
            _ = PumpOutputAsync(process.StandardError, onOutput, cancellationToken);
            _ = MonitorExitAsync(process, onExit, cancellationToken);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Process? runningProcess;
        lock (gate)
        {
            runningProcess = process;
        }

        if (runningProcess is null || runningProcess.HasExited)
        {
            return;
        }

        try
        {
            if (!runningProcess.CloseMainWindow())
            {
                runningProcess.Kill(entireProcessTree: true);
                await runningProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            using var gracePeriod = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            gracePeriod.CancelAfter(TimeSpan.FromSeconds(2));

            try
            {
                await runningProcess.WaitForExitAsync(gracePeriod.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && !runningProcess.HasExited)
            {
                runningProcess.Kill(entireProcessTree: true);
                await runningProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (InvalidOperationException)
        {
            // The process may have exited between the initial checks and the stop request.
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopAsync(CancellationToken.None);
        }
        catch
        {
            // Ignore cleanup errors on shutdown.
        }
    }

    private async Task PumpOutputAsync(StreamReader reader, Action<string> onOutput, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                onOutput(line);
            }
        }
    }

    private async Task MonitorExitAsync(Process startedProcess, Action<int?> onExit, CancellationToken cancellationToken)
    {
        try
        {
            await startedProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            onExit(startedProcess.ExitCode);
        }
        catch (OperationCanceledException)
        {
            onExit(null);
        }
        finally
        {
            lock (gate)
            {
                if (ReferenceEquals(process, startedProcess))
                {
                    process.Dispose();
                    process = null;
                }
            }
        }
    }
}

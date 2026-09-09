using System.Diagnostics;
using ScrcpyLauncher.Core.Models;

namespace ScrcpyLauncher.Infrastructure;

public sealed class AdbService
{
    public async Task<IReadOnlyList<DeviceInfo>> ListDevicesAsync(string? adbExecutablePath, CancellationToken cancellationToken)
    {
        var executable = string.IsNullOrWhiteSpace(adbExecutablePath) ? "adb" : adbExecutablePath;
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("devices");
        startInfo.ArgumentList.Add("-l");

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(stderr)
                    ? "adb devices failed."
                    : stderr.Trim());
        }

        return ParseDevices(stdout);
    }

    private static IReadOnlyList<DeviceInfo> ParseDevices(string output)
    {
        var devices = new List<DeviceInfo>();
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("List of devices attached", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            string? model = null;
            foreach (var part in parts.Skip(2))
            {
                if (part.StartsWith("model:", StringComparison.OrdinalIgnoreCase))
                {
                    model = part["model:".Length..].Replace('_', ' ');
                    break;
                }
            }

            devices.Add(new DeviceInfo
            {
                Serial = parts[0],
                State = parts[1],
                Model = model,
            });
        }

        return devices;
    }
}

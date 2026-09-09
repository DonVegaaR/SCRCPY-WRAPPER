using System.Text.Json;
using ScrcpyLauncher.Core.Models;

namespace ScrcpyLauncher.Infrastructure;

public sealed class JsonLaunchProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public JsonLaunchProfileStore()
    {
        var baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScrcpyLauncher");

        FilePath = Path.Combine(baseDirectory, "presets.json");
    }

    public string FilePath { get; }

    public async Task<LauncherState> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(FilePath))
        {
            return LauncherState.CreateDefault();
        }

        await using var stream = File.OpenRead(FilePath);
        var state = await JsonSerializer.DeserializeAsync<LauncherState>(
            stream,
            SerializerOptions,
            cancellationToken);

        if (state is null || state.Presets.Count == 0)
        {
            return LauncherState.CreateDefault();
        }

        return state;
    }

    public async Task SaveAsync(LauncherState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException("Preset directory could not be resolved.");

        Directory.CreateDirectory(directory);

        await using var stream = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(stream, state, SerializerOptions, cancellationToken);
    }
}

using ScrcpyLauncher.Core.Models;
using ScrcpyLauncher.Core.Services;

namespace ScrcpyLauncher.Infrastructure;

public sealed class ScrcpyBinaryLocator : IScrcpyLocator
{
    private const int SearchDepth = 5;

    public LauncherPaths? FindTools(string? preferredDirectory)
    {
        foreach (var root in EnumerateRoots(preferredDirectory))
        {
            var resolved = TryResolve(root);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateRoots(string? preferredDirectory)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roots = new List<string>();

        AddCandidate(preferredDirectory, seen, roots);
        AddCandidate(AppContext.BaseDirectory, seen, roots);
        AddCandidate(Directory.GetCurrentDirectory(), seen, roots);

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; current is not null && i < 7; ++i)
        {
            AddCandidate(current.FullName, seen, roots);
            current = current.Parent;
        }

        return roots;
    }

    private static void AddCandidate(string? rawPath, HashSet<string> seen, List<string> roots)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return;
        }

        string candidate;
        try
        {
            candidate = File.Exists(rawPath)
                ? Path.GetDirectoryName(Path.GetFullPath(rawPath)) ?? string.Empty
                : Path.GetFullPath(rawPath);
        }
        catch
        {
            return;
        }

        if (!Directory.Exists(candidate))
        {
            return;
        }

        if (seen.Add(candidate))
        {
            roots.Add(candidate);
        }
    }

    private static LauncherPaths? TryResolve(string root)
    {
        LauncherPaths? bestMatch = null;
        var bestScore = int.MaxValue;

        foreach (var directory in EnumerateDirectories(root, SearchDepth))
        {
            var scrcpyExecutablePath = Path.Combine(directory, "scrcpy.exe");
            if (!File.Exists(scrcpyExecutablePath))
            {
                continue;
            }

            var scrcpyServerPath = File.Exists(Path.Combine(directory, "scrcpy-server"))
                ? Path.Combine(directory, "scrcpy-server")
                : null;

            var adbExecutablePath = File.Exists(Path.Combine(directory, "adb.exe"))
                ? Path.Combine(directory, "adb.exe")
                : FindOnPath("adb.exe");

            var score = Score(root, directory, scrcpyServerPath);
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestMatch = new LauncherPaths
            {
                ToolsDirectory = directory,
                ScrcpyExecutablePath = scrcpyExecutablePath,
                ScrcpyServerPath = scrcpyServerPath,
                AdbExecutablePath = adbExecutablePath,
            };
        }

        return bestMatch;
    }

    private static IEnumerable<string> EnumerateDirectories(string root, int maxDepth)
    {
        var queue = new Queue<(string Path, int Depth)>();
        queue.Enqueue((root, 0));

        while (queue.Count > 0)
        {
            var (currentPath, depth) = queue.Dequeue();
            yield return currentPath;

            if (depth >= maxDepth)
            {
                continue;
            }

            IEnumerable<string> subdirectories;
            try
            {
                subdirectories = Directory.EnumerateDirectories(currentPath);
            }
            catch
            {
                continue;
            }

            foreach (var subdirectory in subdirectories)
            {
                queue.Enqueue((subdirectory, depth + 1));
            }
        }
    }

    private static int Score(string root, string candidate, string? scrcpyServerPath)
    {
        var score = 0;
        if (scrcpyServerPath is null)
        {
            score += 20;
        }

        score += CountSegments(Path.GetRelativePath(root, candidate));
        return score;
    }

    private static int CountSegments(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath == ".")
        {
            return 0;
        }

        return relativePath.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static string? FindOnPath(string executableName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var entry in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(entry.Trim(), executableName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                // Ignore invalid PATH entries.
            }
        }

        return null;
    }
}

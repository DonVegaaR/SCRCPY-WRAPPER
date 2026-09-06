using ScrcpyLauncher.Core.Models;

namespace ScrcpyLauncher.Core.Services;

public interface IScrcpyLocator
{
    LauncherPaths? FindTools(string? preferredDirectory);
}

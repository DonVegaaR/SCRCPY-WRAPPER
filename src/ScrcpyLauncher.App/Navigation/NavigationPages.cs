namespace ScrcpyLauncher.App.Navigation;

public sealed class DevicePage
{
    public static DevicePage Instance { get; } = new();

    private DevicePage()
    {
    }
}

public sealed class LaunchPage
{
    public static LaunchPage Instance { get; } = new();

    private LaunchPage()
    {
    }
}

public sealed class RecordingPage
{
    public static RecordingPage Instance { get; } = new();

    private RecordingPage()
    {
    }
}

public sealed class ProfilePage
{
    public static ProfilePage Instance { get; } = new();

    private ProfilePage()
    {
    }
}

public sealed class ToolsPage
{
    public static ToolsPage Instance { get; } = new();

    private ToolsPage()
    {
    }
}

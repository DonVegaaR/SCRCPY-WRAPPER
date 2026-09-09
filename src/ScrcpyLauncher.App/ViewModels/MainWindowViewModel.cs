using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Threading;
using Microsoft.Win32;
using ScrcpyLauncher.App.Infrastructure;
using ScrcpyLauncher.App.Navigation;
using ScrcpyLauncher.Core;
using ScrcpyLauncher.Core.Models;
using ScrcpyLauncher.Infrastructure;

namespace ScrcpyLauncher.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject, IAsyncDisposable
{
    private readonly JsonLaunchProfileStore profileStore;
    private readonly ScrcpyBinaryLocator locator;
    private readonly AdbService adbService;
    private readonly ScrcpyProcessService processService;
    private readonly Dispatcher dispatcher;
    private readonly StringBuilder logBuilder = new();

    private LaunchProfile? selectedPreset;
    private DeviceInfo? selectedDevice;
    private object currentPage = new DevicePage();
    private string presetName = "Default";
    private string toolsDirectory = string.Empty;
    private string adbSerial = string.Empty;
    private string maxSize = string.Empty;
    private string maxFps = string.Empty;
    private string videoBitRate = string.Empty;
    private bool audioEnabled = true;
    private bool videoEnabled = true;
    private bool controlEnabled = true;
    private bool fullscreen;
    private bool stayAwake;
    private bool turnScreenOff;
    private bool alwaysOnTop;
    private string recordPath = string.Empty;
    private string windowTitle = string.Empty;
    private string additionalArguments = string.Empty;
    private bool pauseOnExitIfError = true;
    private string commandPreview = "scrcpy.exe --pause-on-exit=if-error";
    private string logText = string.Empty;
    private string statusMessage = "Ready.";
    private string toolsVersionLabel = "No scrcpy build detected";
    private string toolsDetailLabel = "Choose a folder that contains scrcpy.exe and scrcpy-server.";
    private bool hasDetectedTools;
    private bool isRunning;
    private bool isBusy;

    public MainWindowViewModel(
        JsonLaunchProfileStore profileStore,
        ScrcpyBinaryLocator locator,
        AdbService adbService,
        ScrcpyProcessService processService,
        Dispatcher dispatcher)
    {
        this.profileStore = profileStore;
        this.locator = locator;
        this.adbService = adbService;
        this.processService = processService;
        this.dispatcher = dispatcher;

        AutoDetectToolsCommand = new AsyncRelayCommand(() => AutoDetectToolsAsync(false));
        RefreshDevicesCommand = new AsyncRelayCommand(RefreshDevicesAsync, () => !IsRunning);
        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsRunning && !IsBusy);
        StopCommand = new AsyncRelayCommand(StopAsync, () => IsRunning);
        SavePresetCommand = new AsyncRelayCommand(SavePresetAsync);
        DeletePresetCommand = new AsyncRelayCommand(DeletePresetAsync, () => Presets.Count > 0);
        NewPresetCommand = new AsyncRelayCommand(NewPresetAsync);
        AdbPairCommand = new AsyncRelayCommand(PairAdbAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(AdbSerial));
        BrowseToolsCommand = new RelayCommand(BrowseTools);
        BrowseRecordCommand = new RelayCommand(BrowseRecord);
        NavigateCommand = new RelayCommand(Navigate);

        RefreshPreview();
        RefreshToolsSummary();
    }

    public ObservableCollection<LaunchProfile> Presets { get; } = new();

    public ObservableCollection<DeviceInfo> Devices { get; } = new();

    public object CurrentPage
    {
        get => currentPage;
        private set => SetProperty(ref currentPage, value);
    }

    public LaunchProfile? SelectedPreset
    {
        get => selectedPreset;
        set
        {
            if (ReferenceEquals(selectedPreset, value))
            {
                return;
            }

            if (selectedPreset is not null)
            {
                OverwriteProfile(selectedPreset, CaptureProfile());
            }

            selectedPreset = value;
            RaisePropertyChanged();

            if (selectedPreset is not null)
            {
                ApplyProfile(selectedPreset);
            }

            NotifyCommandStates();
        }
    }

    public DeviceInfo? SelectedDevice
    {
        get => selectedDevice;
        set => SetProperty(ref selectedDevice, value, OnSelectedDeviceChanged);
    }

    public string ConnectionStatus
    {
        get
        {
            if (SelectedDevice?.IsOnline == true)
            {
                return "Connected";
            }

            if (!string.IsNullOrWhiteSpace(SelectedDevice?.State))
            {
                return char.ToUpperInvariant(SelectedDevice.State[0]) + SelectedDevice.State[1..];
            }

            return "Waiting";
        }
    }

    public string StatusBadgeText
    {
        get
        {
            if (IsRunning)
            {
                return "Running";
            }

            if (IsBusy)
            {
                return "Working";
            }

            return HasDetectedTools ? "Ready" : "Setup";
        }
    }

    public string PresetName
    {
        get => presetName;
        set => SetProperty(ref presetName, value, RefreshPreview);
    }

    public string ToolsDirectory
    {
        get => toolsDirectory;
        set => SetProperty(ref toolsDirectory, value, OnToolsDirectoryChanged);
    }

    public string AdbSerial
    {
        get => adbSerial;
        set => SetProperty(ref adbSerial, value, NotifyCommandStates);
    }

    public string MaxSize
    {
        get => maxSize;
        set => SetProperty(ref maxSize, value, RefreshPreview);
    }

    public string MaxFps
    {
        get => maxFps;
        set => SetProperty(ref maxFps, value, RefreshPreview);
    }

    public string VideoBitRate
    {
        get => videoBitRate;
        set => SetProperty(ref videoBitRate, value, RefreshPreview);
    }

    public bool AudioEnabled
    {
        get => audioEnabled;
        set => SetProperty(ref audioEnabled, value, RefreshPreview);
    }

    public bool VideoEnabled
    {
        get => videoEnabled;
        set => SetProperty(ref videoEnabled, value, RefreshPreview);
    }

    public bool ControlEnabled
    {
        get => controlEnabled;
        set => SetProperty(ref controlEnabled, value, RefreshPreview);
    }

    public bool Fullscreen
    {
        get => fullscreen;
        set => SetProperty(ref fullscreen, value, RefreshPreview);
    }

    public bool StayAwake
    {
        get => stayAwake;
        set => SetProperty(ref stayAwake, value, RefreshPreview);
    }

    public bool TurnScreenOff
    {
        get => turnScreenOff;
        set => SetProperty(ref turnScreenOff, value, RefreshPreview);
    }

    public bool AlwaysOnTop
    {
        get => alwaysOnTop;
        set => SetProperty(ref alwaysOnTop, value, RefreshPreview);
    }

    public string RecordPath
    {
        get => recordPath;
        set => SetProperty(ref recordPath, value, RefreshPreview);
    }

    public string WindowTitle
    {
        get => windowTitle;
        set => SetProperty(ref windowTitle, value, RefreshPreview);
    }

    public string AdditionalArguments
    {
        get => additionalArguments;
        set => SetProperty(ref additionalArguments, value, RefreshPreview);
    }

    public bool PauseOnExitIfError
    {
        get => pauseOnExitIfError;
        set => SetProperty(ref pauseOnExitIfError, value, RefreshPreview);
    }

    public string CommandPreview
    {
        get => commandPreview;
        private set => SetProperty(ref commandPreview, value);
    }

    public string LogText
    {
        get => logText;
        private set => SetProperty(ref logText, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public string ToolsVersionLabel
    {
        get => toolsVersionLabel;
        private set => SetProperty(ref toolsVersionLabel, value);
    }

    public string ToolsDetailLabel
    {
        get => toolsDetailLabel;
        private set => SetProperty(ref toolsDetailLabel, value);
    }

    public bool HasDetectedTools
    {
        get => hasDetectedTools;
        private set
        {
            if (SetProperty(ref hasDetectedTools, value))
            {
                RaisePropertyChanged(nameof(StatusBadgeText));
            }
        }
    }

    public bool IsRunning
    {
        get => isRunning;
        private set
        {
            if (SetProperty(ref isRunning, value))
            {
                RaisePropertyChanged(nameof(StatusBadgeText));
                NotifyCommandStates();
            }
        }
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                RaisePropertyChanged(nameof(StatusBadgeText));
                NotifyCommandStates();
            }
        }
    }

    public AsyncRelayCommand AutoDetectToolsCommand { get; }

    public AsyncRelayCommand RefreshDevicesCommand { get; }

    public AsyncRelayCommand StartCommand { get; }

    public AsyncRelayCommand StopCommand { get; }

    public AsyncRelayCommand SavePresetCommand { get; }

    public AsyncRelayCommand DeletePresetCommand { get; }

    public AsyncRelayCommand NewPresetCommand { get; }

    public AsyncRelayCommand AdbPairCommand { get; }

    public RelayCommand BrowseToolsCommand { get; }

    public RelayCommand BrowseRecordCommand { get; }

    public RelayCommand NavigateCommand { get; }

    public async Task InitializeAsync()
    {
        StartupLogger.Write("MainWindowViewModel.InitializeAsync started.");
        var state = await profileStore.LoadAsync(CancellationToken.None);
        StartupLogger.Write($"Loaded presets: {state.Presets.Count}");
        foreach (var preset in state.Presets)
        {
            Presets.Add(preset);
        }

        if (Presets.Count == 0)
        {
            Presets.Add(new LaunchProfile());
        }

        SelectedPreset = Presets.FirstOrDefault(p => string.Equals(p.Name, state.LastSelectedPresetName, StringComparison.Ordinal))
            ?? Presets[0];

        if (string.IsNullOrWhiteSpace(ToolsDirectory))
        {
            await AutoDetectToolsAsync(true);
        }
        else
        {
            RefreshToolsSummary();
        }

        await RefreshDevicesAsync();
        AppendLog($"Preset store: {profileStore.FilePath}");
        StartupLogger.Write("MainWindowViewModel.InitializeAsync finished.");
    }

    public async ValueTask DisposeAsync()
    {
        await processService.DisposeAsync();
    }

    private async Task AutoDetectToolsAsync(bool silent)
    {
        StartupLogger.Write($"AutoDetectToolsAsync silent={silent}");
        var paths = locator.FindTools(ToolsDirectory);
        if (paths is null)
        {
            RefreshToolsSummary();
            if (!silent)
            {
                StatusMessage = "Could not find a scrcpy tools folder yet.";
                AppendLog("Auto-detect could not locate scrcpy.exe.");
            }

            return;
        }

        ToolsDirectory = paths.ToolsDirectory;
        StatusMessage = $"Using tools from {paths.ToolsDirectory}";
        AppendLog($"Detected tools in {paths.ToolsDirectory}");
        StartupLogger.Write($"Detected tools directory: {paths.ToolsDirectory}");
        await RefreshDevicesAsync();
    }

    private async Task RefreshDevicesAsync()
    {
        StartupLogger.Write("RefreshDevicesAsync started.");
        try
        {
            IsBusy = true;

            var adbPath = locator.FindTools(ToolsDirectory)?.AdbExecutablePath;
            StartupLogger.Write($"Using adb path: {adbPath ?? "<PATH>"}");
            var devices = await adbService.ListDevicesAsync(adbPath, CancellationToken.None);

            Devices.Clear();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }

            SelectedDevice = Devices.FirstOrDefault(d =>
                    string.Equals(d.Serial, selectedPreset?.SelectedDeviceSerial, StringComparison.Ordinal))
                ?? Devices.FirstOrDefault(d => d.IsOnline)
                ?? Devices.FirstOrDefault();

            StatusMessage = Devices.Count == 0
                ? "No devices found."
                : $"Found {Devices.Count} device(s).";
            StartupLogger.Write($"RefreshDevicesAsync finished. Devices={Devices.Count}");
        }
        catch (Exception ex)
        {
            StatusMessage = "Device refresh failed.";
            AppendLog(ex.Message);
            StartupLogger.WriteException("RefreshDevicesAsync failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StartAsync()
    {
        try
        {
            StartupLogger.Write("StartAsync entered.");
            IsBusy = true;

            var paths = locator.FindTools(ToolsDirectory);
            if (paths is null)
            {
                throw new InvalidOperationException("Select a folder that contains scrcpy.exe before launching.");
            }

            var profile = CaptureProfile();
            var arguments = ScrcpyArgumentBuilder.Build(profile);

            if (selectedPreset is not null)
            {
                OverwriteProfile(selectedPreset, CaptureProfile());
                await PersistStateAsync();
            }

            AppendLog($"Launching from {paths.ToolsDirectory}");
            AppendLog(ScrcpyArgumentBuilder.BuildPreview(profile));
            StartupLogger.Write($"Starting scrcpy. SelectedDevice={SelectedDevice?.Serial ?? "<none>"} Args={arguments.Count}");

            await processService.StartAsync(
                paths,
                arguments,
                line => AppendLog(line),
                exitCode => HandleExitFromWorker(exitCode),
                CancellationToken.None);

            StartupLogger.Write("processService.StartAsync returned.");
            IsRunning = true;
            StatusMessage = "scrcpy is running.";
            StartupLogger.Write("Launcher marked as running.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Launch failed.";
            AppendLog(ex.Message);
            StartupLogger.WriteException("StartAsync failed", ex);
        }
        finally
        {
            IsBusy = false;
            StartupLogger.Write("StartAsync finished.");
        }
    }

    private async Task StopAsync()
    {
        try
        {
            StartupLogger.Write("StopAsync entered.");
            StatusMessage = "Stopping scrcpy...";
            await processService.StopAsync(CancellationToken.None);
            StatusMessage = "Stop requested.";
            StartupLogger.Write("StopAsync completed.");
        }
        catch (Exception ex)
        {
            AppendLog(ex.Message);
            StartupLogger.WriteException("StopAsync failed", ex);
        }
    }

    private async Task SavePresetAsync()
    {
        var profile = CaptureProfile();

        if (selectedPreset is null)
        {
            selectedPreset = profile;
            Presets.Add(selectedPreset);
            RaisePropertyChanged(nameof(SelectedPreset));
        }
        else
        {
            OverwriteProfile(selectedPreset, profile);
        }

        await PersistStateAsync();
        StatusMessage = $"Saved preset '{profile.Name}'.";
    }

    private async Task DeletePresetAsync()
    {
        if (selectedPreset is null)
        {
            return;
        }

        var removedName = selectedPreset.Name;
        var removedIndex = Presets.IndexOf(selectedPreset);
        Presets.Remove(selectedPreset);

        if (Presets.Count == 0)
        {
            Presets.Add(new LaunchProfile());
        }

        SelectedPreset = Presets[Math.Clamp(removedIndex, 0, Presets.Count - 1)];
        await PersistStateAsync();
        StatusMessage = $"Deleted preset '{removedName}'.";
    }

    private async Task NewPresetAsync()
    {
        var baseName = string.IsNullOrWhiteSpace(PresetName) ? "Preset" : PresetName.Trim();
        var uniqueName = MakeUniquePresetName(baseName);
        var preset = CaptureProfile();
        preset.Name = uniqueName;
        Presets.Add(preset);
        SelectedPreset = preset;
        await PersistStateAsync();
        StatusMessage = $"Created preset '{uniqueName}'.";
    }

    private Task PairAdbAsync()
    {
        var serial = AdbSerial.Trim();
        if (string.IsNullOrWhiteSpace(serial))
        {
            StatusMessage = "Enter a serial or IP before pairing.";
            return Task.CompletedTask;
        }

        StatusMessage = "ADB pairing is not wired up yet.";
        AppendLog($"ADB pairing requested for '{serial}', but the pairing flow is not implemented yet.");
        return Task.CompletedTask;
    }

    private void BrowseTools()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select scrcpy.exe",
            Filter = "scrcpy executable|scrcpy.exe|Executable files|*.exe",
            CheckFileExists = true,
            FileName = "scrcpy.exe",
        };

        if (dialog.ShowDialog() == true)
        {
            ToolsDirectory = Path.GetDirectoryName(dialog.FileName) ?? ToolsDirectory;
        }
    }

    private void BrowseRecord()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Choose recording output",
            Filter = "MP4 file|*.mp4|MKV file|*.mkv|All files|*.*",
            AddExtension = true,
            DefaultExt = "mp4",
        };

        if (dialog.ShowDialog() == true)
        {
            RecordPath = dialog.FileName;
        }
    }

    private void Navigate(object? parameter)
    {
        CurrentPage = (parameter as string) switch
        {
            "Launch" => new LaunchPage(),
            "Recording" => new RecordingPage(),
            "Profile" => new ProfilePage(),
            "Tools" => new ToolsPage(),
            _ => new DevicePage(),
        };
    }

    private void OnSelectedDeviceChanged()
    {
        if (selectedDevice is not null)
        {
            AdbSerial = selectedDevice.Serial;
        }

        RefreshPreview();
        RaisePropertyChanged(nameof(ConnectionStatus));
    }

    private void OnToolsDirectoryChanged()
    {
        RefreshPreview();
        RefreshToolsSummary();
    }

    private void ApplyProfile(LaunchProfile profile)
    {
        PresetName = profile.Name;
        ToolsDirectory = profile.ToolsDirectory;
        MaxSize = profile.MaxSize;
        MaxFps = profile.MaxFps;
        VideoBitRate = profile.VideoBitRate;
        AudioEnabled = profile.AudioEnabled;
        VideoEnabled = profile.VideoEnabled;
        ControlEnabled = profile.ControlEnabled;
        Fullscreen = profile.Fullscreen;
        StayAwake = profile.StayAwake;
        TurnScreenOff = profile.TurnScreenOff;
        AlwaysOnTop = profile.AlwaysOnTop;
        RecordPath = profile.RecordPath;
        WindowTitle = profile.WindowTitle;
        AdditionalArguments = profile.AdditionalArguments;
        PauseOnExitIfError = profile.PauseOnExitIfError;
        SelectedDevice = Devices.FirstOrDefault(d => string.Equals(d.Serial, profile.SelectedDeviceSerial, StringComparison.Ordinal));
        if (SelectedDevice is null && !string.IsNullOrWhiteSpace(profile.SelectedDeviceSerial))
        {
            AdbSerial = profile.SelectedDeviceSerial;
        }

        RefreshPreview();
    }

    private LaunchProfile CaptureProfile()
    {
        return new LaunchProfile
        {
            Name = string.IsNullOrWhiteSpace(PresetName) ? "Default" : PresetName.Trim(),
            ToolsDirectory = ToolsDirectory.Trim(),
            SelectedDeviceSerial = SelectedDevice?.Serial,
            MaxSize = MaxSize.Trim(),
            MaxFps = MaxFps.Trim(),
            VideoBitRate = VideoBitRate.Trim(),
            AudioEnabled = AudioEnabled,
            VideoEnabled = VideoEnabled,
            ControlEnabled = ControlEnabled,
            Fullscreen = Fullscreen,
            StayAwake = StayAwake,
            TurnScreenOff = TurnScreenOff,
            AlwaysOnTop = AlwaysOnTop,
            RecordPath = RecordPath.Trim(),
            WindowTitle = WindowTitle.Trim(),
            AdditionalArguments = AdditionalArguments.Trim(),
            PauseOnExitIfError = PauseOnExitIfError,
        };
    }

    private static void OverwriteProfile(LaunchProfile target, LaunchProfile source)
    {
        target.Name = source.Name;
        target.ToolsDirectory = source.ToolsDirectory;
        target.SelectedDeviceSerial = source.SelectedDeviceSerial;
        target.MaxSize = source.MaxSize;
        target.MaxFps = source.MaxFps;
        target.VideoBitRate = source.VideoBitRate;
        target.AudioEnabled = source.AudioEnabled;
        target.VideoEnabled = source.VideoEnabled;
        target.ControlEnabled = source.ControlEnabled;
        target.Fullscreen = source.Fullscreen;
        target.StayAwake = source.StayAwake;
        target.TurnScreenOff = source.TurnScreenOff;
        target.AlwaysOnTop = source.AlwaysOnTop;
        target.RecordPath = source.RecordPath;
        target.WindowTitle = source.WindowTitle;
        target.AdditionalArguments = source.AdditionalArguments;
        target.PauseOnExitIfError = source.PauseOnExitIfError;
    }

    private async Task PersistStateAsync()
    {
        var state = new LauncherState
        {
            Presets = Presets.ToList(),
            LastSelectedPresetName = selectedPreset?.Name ?? PresetName,
        };

        await profileStore.SaveAsync(state, CancellationToken.None);
        NotifyCommandStates();
    }

    private string MakeUniquePresetName(string baseName)
    {
        if (!Presets.Any(p => string.Equals(p.Name, baseName, StringComparison.OrdinalIgnoreCase)))
        {
            return baseName;
        }

        var index = 2;
        while (Presets.Any(p => string.Equals(p.Name, $"{baseName} {index}", StringComparison.OrdinalIgnoreCase)))
        {
            ++index;
        }

        return $"{baseName} {index}";
    }

    private void RefreshPreview()
    {
        try
        {
            CommandPreview = ScrcpyArgumentBuilder.BuildPreview(CaptureProfile());
        }
        catch (Exception ex)
        {
            CommandPreview = ex.Message;
        }
    }

    private void RefreshToolsSummary()
    {
        var paths = locator.FindTools(ToolsDirectory);
        if (paths is null)
        {
            HasDetectedTools = false;
            ToolsVersionLabel = "No scrcpy build detected";
            ToolsDetailLabel = "Choose a folder that contains scrcpy.exe and scrcpy-server.";
            return;
        }

        HasDetectedTools = true;
        ToolsVersionLabel = DescribeScrcpy(paths.ScrcpyExecutablePath);
        ToolsDetailLabel = DescribeAdb(paths.AdbExecutablePath);
    }

    private static string DescribeScrcpy(string executablePath)
    {
        var version = TryReadVersion(executablePath);
        return string.IsNullOrWhiteSpace(version)
            ? "scrcpy detected"
            : $"scrcpy {version} detected";
    }

    private static string DescribeAdb(string? adbPath)
    {
        if (string.IsNullOrWhiteSpace(adbPath))
        {
            return "adb.exe was not found in the tools folder or on PATH.";
        }

        var version = TryReadVersion(adbPath);
        if (!string.IsNullOrWhiteSpace(version))
        {
            return $"adb {version}";
        }

        return $"adb detected in {Path.GetDirectoryName(adbPath)}";
    }

    private static string? TryReadVersion(string executablePath)
    {
        try
        {
            var info = FileVersionInfo.GetVersionInfo(executablePath);
            var version = info.ProductVersion;
            if (string.IsNullOrWhiteSpace(version))
            {
                version = info.FileVersion;
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                return null;
            }

            var normalized = version.Trim();
            var plusIndex = normalized.IndexOf('+');
            if (plusIndex > 0)
            {
                normalized = normalized[..plusIndex];
            }

            return normalized;
        }
        catch
        {
            return null;
        }
    }

    private void AppendLog(string line)
    {
        if (logBuilder.Length > 64_000)
        {
            logBuilder.Clear();
            logBuilder.AppendLine("[log buffer truncated]");
        }

        logBuilder.Append('[')
                  .Append(DateTime.Now.ToString("HH:mm:ss"))
                  .Append("] ")
                  .AppendLine(line);

        LogText = logBuilder.ToString();
    }

    private void HandleExitFromWorker(int? exitCode)
    {
        StartupLogger.Write($"HandleExitFromWorker invoked. ExitCode={(exitCode.HasValue ? exitCode.Value.ToString() : "null")}");
        _ = dispatcher.BeginInvoke(() =>
        {
            IsRunning = false;
            var exitText = exitCode.HasValue ? exitCode.Value.ToString() : "unknown";
            StatusMessage = $"scrcpy exited with code {exitText}.";
            AppendLog($"scrcpy exited with code {exitText}.");
        });
    }

    private void NotifyCommandStates()
    {
        AutoDetectToolsCommand.NotifyCanExecuteChanged();
        RefreshDevicesCommand.NotifyCanExecuteChanged();
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        SavePresetCommand.NotifyCanExecuteChanged();
        DeletePresetCommand.NotifyCanExecuteChanged();
        NewPresetCommand.NotifyCanExecuteChanged();
        AdbPairCommand.NotifyCanExecuteChanged();
        BrowseToolsCommand.NotifyCanExecuteChanged();
        BrowseRecordCommand.NotifyCanExecuteChanged();
        NavigateCommand.NotifyCanExecuteChanged();
    }
}

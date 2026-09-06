using System.Text;
using ScrcpyLauncher.Core.Models;

namespace ScrcpyLauncher.Core;

public static class ScrcpyArgumentBuilder
{
    public static IReadOnlyList<string> Build(LaunchProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!profile.AudioEnabled && !profile.VideoEnabled)
        {
            throw new InvalidOperationException("At least video or audio must remain enabled.");
        }

        var arguments = new List<string>();

        if (!string.IsNullOrWhiteSpace(profile.SelectedDeviceSerial))
        {
            arguments.Add("--serial");
            arguments.Add(profile.SelectedDeviceSerial.Trim());
        }

        if (!profile.AudioEnabled)
        {
            arguments.Add("--no-audio");
        }

        if (!profile.VideoEnabled)
        {
            arguments.Add("--no-video");
        }

        if (!profile.ControlEnabled)
        {
            arguments.Add("--no-control");
        }

        AppendOption(arguments, "--max-size", profile.MaxSize);
        AppendOption(arguments, "--max-fps", profile.MaxFps);
        AppendOption(arguments, "--video-bit-rate", profile.VideoBitRate);
        AppendOption(arguments, "--window-title", profile.WindowTitle);
        AppendOption(arguments, "--record", profile.RecordPath);

        if (profile.Fullscreen)
        {
            arguments.Add("--fullscreen");
        }

        if (profile.StayAwake)
        {
            arguments.Add("--stay-awake");
        }

        if (profile.TurnScreenOff)
        {
            arguments.Add("--turn-screen-off");
        }

        if (profile.AlwaysOnTop)
        {
            arguments.Add("--always-on-top");
        }

        if (profile.PauseOnExitIfError)
        {
            arguments.Add("--pause-on-exit=if-error");
        }

        foreach (var token in SplitArguments(profile.AdditionalArguments))
        {
            arguments.Add(token);
        }

        return arguments;
    }

    public static string BuildPreview(LaunchProfile profile)
    {
        var arguments = Build(profile);
        var builder = new StringBuilder("scrcpy.exe");
        foreach (var argument in arguments)
        {
            builder.Append(' ');
            builder.Append(Quote(argument));
        }

        return builder.ToString();
    }

    private static void AppendOption(List<string> arguments, string option, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        arguments.Add(option);
        arguments.Add(value.Trim());
    }

    private static string Quote(string value)
    {
        if (value.Length == 0)
        {
            return "\"\"";
        }

        if (!value.Any(char.IsWhiteSpace) && !value.Contains('"'))
        {
            return value;
        }

        return "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal)
                           .Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private static IReadOnlyList<string> SplitArguments(string rawArguments)
    {
        if (string.IsNullOrWhiteSpace(rawArguments))
        {
            return Array.Empty<string>();
        }

        var result = new List<string>();
        var token = new StringBuilder();
        var inQuotes = false;
        var quoteChar = '\0';

        foreach (var ch in rawArguments)
        {
            if (inQuotes)
            {
                if (ch == quoteChar)
                {
                    inQuotes = false;
                }
                else
                {
                    token.Append(ch);
                }

                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                inQuotes = true;
                quoteChar = ch;
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                FlushToken(result, token);
                continue;
            }

            token.Append(ch);
        }

        if (inQuotes)
        {
            throw new InvalidOperationException("Additional arguments contain an unmatched quote.");
        }

        FlushToken(result, token);
        return result;
    }

    private static void FlushToken(List<string> tokens, StringBuilder token)
    {
        if (token.Length == 0)
        {
            return;
        }

        tokens.Add(token.ToString());
        token.Clear();
    }
}

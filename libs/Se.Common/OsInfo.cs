using System;

namespace Se.Common;

public static class OsInfo
{
    /// <summary>
    /// Detects the current OS platform.
    /// </summary>
    public static OsPlatform GetCurrent()
    {
        if (OperatingSystem.IsWindows())
        {
            return OsPlatform.Windows;
        }

        if (OperatingSystem.IsMacOS())
        {
            return OsPlatform.MacOS;
        }

        if (OperatingSystem.IsLinux())
        {
            return OsPlatform.Linux;
        }

        return OsPlatform.Unknown;
    }

    /// <summary>
    /// Returns a short string id for the current OS.
    /// </summary>
    public static string GetId()
    {
        return GetCurrent() switch
        {
            OsPlatform.Windows => "win",
            OsPlatform.MacOS => "mac",
            OsPlatform.Linux => "linux",
            OsPlatform.Unknown => "unknown",
            _ => "unknown",
        };
    }
}

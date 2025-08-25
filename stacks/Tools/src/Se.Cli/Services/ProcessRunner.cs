using System.Diagnostics;

namespace Se.Cli.Services;

internal static class ProcessRunner
{
    internal static (int ExitCode, string Stdout, string Stderr) Run(string fileName, string arguments, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
        };

        using var p = Process.Start(psi);
        if (p == null)
        {
            return (-1, string.Empty, "failed to start process");
        }

        var outTask = p.StandardOutput.ReadToEndAsync();
        var errTask = p.StandardError.ReadToEndAsync();
        p.WaitForExit();
        return (p.ExitCode, outTask.Result, errTask.Result);
    }
}

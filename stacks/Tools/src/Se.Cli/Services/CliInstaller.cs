using System.Runtime.InteropServices;

namespace Se.Cli.Services;

internal static class CliInstaller
{
    internal static bool IsInstalled(string osId, string exeName)
    {
        var cmd = osId == "win" ? "where" : "which";
        var (exitCode, _, _) = ProcessRunner.Run(cmd, exeName);
        return exitCode == 0;
    }

    internal static void Install(string osId)
    {
        const string ExeName = "se-cli";
        var rid = GetRidForOs(osId);
        if (string.IsNullOrEmpty(rid))
        {
            Console.WriteLine("Unsupported OS for automatic install.");
            return;
        }

        if (IsInstalled(osId, ExeName))
        {
            Console.WriteLine("CLI already installed. Use 'se update' to refresh.");
            return;
        }

        var projectPath = Path.GetFullPath("stacks/Tools/src/Se.Cli/Se.Cli.csproj");
        var tmp = Path.Combine(Path.GetTempPath(), "secli-publish-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);

        Console.WriteLine($"Publishing self-contained executable for RID {rid}...");
        var publishArgs = $"publish \"{projectPath}\" -c Release -r {rid} -p:PublishSingleFile=true -p:SelfContained=true -p:PublishTrimmed=true -o \"{tmp}\"";
        var (exitCode, _, stderr) = ProcessRunner.Run("dotnet", publishArgs);
        if (exitCode != 0)
        {
            Console.WriteLine("dotnet publish failed:\n" + stderr);
            Console.WriteLine("You can run the following command yourself:");
            Console.WriteLine(publishArgs);
            return;
        }

        string? publishedExe = null;
        foreach (var f in Directory.GetFiles(tmp))
        {
            var name = Path.GetFileName(f);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    publishedExe = f;
                    break;
                }
            }
            else
            {
                if (!name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    publishedExe = f;
                    break;
                }
            }
        }

        if (publishedExe == null)
        {
            Console.WriteLine("Failed to find published executable in " + tmp);
            return;
        }

        var targetDir = "/usr/local/bin";
        if (osId == "mac" && Directory.Exists("/opt/homebrew/bin"))
        {
            targetDir = "/opt/homebrew/bin";
        }

        var targetPath = Path.Combine(targetDir, ExeName);
        try
        {
            File.Copy(publishedExe, targetPath, overwrite: true);
            File.SetAttributes(targetPath, FileAttributes.Normal);
            _ = ProcessRunner.Run("chmod", $"+x \"{targetPath}\"");
            Console.WriteLine($"Installed to {targetPath}");

            var aliasPath = Path.Combine(targetDir, "ss");
            _ = ProcessRunner.Run("ln", $"-sf \"{targetPath}\" \"{aliasPath}\"");
            Console.WriteLine($"Alias created: {aliasPath} -> {targetPath}");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Permission denied when trying to copy to system bin. Re-run install with sudo or move the binary manually:");
            Console.WriteLine($"sudo mv \"{publishedExe}\" {targetPath}");
            Console.WriteLine($"sudo chmod +x {targetPath}");
            Console.WriteLine($"sudo ln -sf {targetPath} {Path.Combine(targetDir, "ss")}");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Install failed: " + ex.Message);
            return;
        }

        Console.WriteLine("Install finished.");
    }

    internal static void Update(string osId)
    {
        const string ExeName = "se";
        if (!IsInstalled(osId, ExeName))
        {
            Console.WriteLine("CLI not installed, performing install...");
            Install(osId);
            return;
        }

        Install(osId);
    }

    internal static void Remove(string osId)
    {
        var targetDir = "/usr/local/bin";
        if (osId == "mac" && Directory.Exists("/opt/homebrew/bin"))
        {
            targetDir = "/opt/homebrew/bin";
        }

        var targetPath = Path.Combine(targetDir, "se-cli");
        var aliasPath = Path.Combine(targetDir, "ss");
        try
        {
            if (File.Exists(aliasPath) || Directory.Exists(aliasPath))
            {
                File.Delete(aliasPath);
                Console.WriteLine($"Removed alias {aliasPath}");
            }

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
                Console.WriteLine($"Removed {targetPath}");
            }

            Console.WriteLine("Uninstall finished.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Permission denied when removing files. Re-run removal with sudo, e.g:");
            Console.WriteLine($"sudo rm {aliasPath} {targetPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Remove failed: " + ex.Message);
        }
    }

    private static string GetRidForOs(string osId)
    {
        if (osId == "mac")
        {
            var arch = RuntimeInformation.OSArchitecture;
            return arch == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        }

        if (osId == "linux")
        {
            var arch = RuntimeInformation.OSArchitecture;
            return arch == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
        }

        if (osId == "win")
        {
            return "win-x64";
        }

        return string.Empty;
    }
}

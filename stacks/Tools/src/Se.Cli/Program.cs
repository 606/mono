using Se.Cli.Services;
using Se.Common;

Console.WriteLine("Starting Se.Cli..");
var osId = OsInfo.GetId();
Console.WriteLine(osId);

if (args.Length <= 1)
{
    Console.WriteLine("For more help use -h or --help");
    return;
}

var cmd = args[1].ToLowerInvariant();
switch (cmd)
{
    case "install":
        CliInstaller.Install(osId);
        break;
    case "update":
        CliInstaller.Update(osId);
        break;
    case "remove":
    case "uninstall":
        CliInstaller.Remove(osId);
        break;
    case "-h":
    case "--help":
        Console.WriteLine("Usage: se install|update|remove");
        break;
    default:
        Console.WriteLine("Unknown command: " + cmd);
        break;
}

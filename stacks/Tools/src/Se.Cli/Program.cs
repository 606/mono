using Se.Common;

var os = OsInfo.GetCurrent();
Console.WriteLine($"Se.Cli: running on {os} ({OsInfo.GetId()})");

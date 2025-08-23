using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace Se.Aws.Shared.Helpers.Helpers;

/// <summary>
/// Helper for safe execution of async actions with error logging.
/// </summary>
public static class SafeExecutor
{
    public static async Task Run(Func<Task> action, ILambdaContext context, string operation)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"[ERR] {operation}: {ex.Message}");
        }
    }
}

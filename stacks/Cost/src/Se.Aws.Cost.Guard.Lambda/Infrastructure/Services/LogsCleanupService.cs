using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.Lambda.Core;
using Se.Aws.Cost.Guard.Lambda.Application.Abstractions;
using Se.Aws.Cost.Guard.Lambda.Domain.Options;

namespace Se.Aws.Cost.Guard.Lambda.Infrastructure.Services;

internal sealed class LogsCleanupService : ICleanupService
{
    private readonly CostGuardOptions opt;

    public LogsCleanupService(CostGuardOptions opt) => this.opt = opt;

    public async Task ExecuteAsync(ILambdaContext ctx, string regionSystemName)
    {
        _ = new AmazonCloudWatchLogsClient(RegionEndpoint.GetBySystemName(regionSystemName));

        // ... (повний код з aws, як у попередньому фрагменті) ...
    }

    private string Pfx(string s) => opt.DryRun ? $"[DRY] {s}" : s;
}

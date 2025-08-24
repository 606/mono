using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Se.Aws.Cost.Guard.Lambda.Application.Abstractions;
using Se.Aws.Cost.Guard.Lambda.Domain.Options;
using Se.Aws.Cost.Guard.Lambda.Infrastructure.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Se.Aws.Cost.Guard.Lambda;

public sealed class Function
{
    private readonly CostGuardOptions opt;
    private readonly IReadOnlyList<ICleanupService> services;

    public Function()
    {
        opt = CostGuardOptions.FromEnv();

        services =
        [
            new VpcCleanupService(opt),
            new ElbCleanupService(opt),
            new Ec2CleanupService(opt),
            new LogsCleanupService(opt)
        ];
    }

    // Entry point
#pragma warning disable IDE0060 // Remove unused parameter exclusion
    public async Task Handler(object? input, ILambdaContext context)
#pragma warning restore IDE0060
    {
        foreach (var region in opt.Regions)
        {
            context.Logger.LogLine(Separator($"Region {region} | Mode={opt.Mode} | DryRun={opt.DryRun}"));
            foreach (var svc in services)
            {
                await svc.ExecuteAsync(context, region);
            }
        }

        context.Logger.LogLine(Separator("Completed"));
    }

    private string Separator(string title)
        => opt.DryRun ? $"[DRY] === {title} ===" : $"=== {title} ===";
}

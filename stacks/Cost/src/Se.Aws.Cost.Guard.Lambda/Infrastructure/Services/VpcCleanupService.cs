using System.Threading.Tasks;
using Amazon;
using Amazon.EC2;
using Amazon.Lambda.Core;
using Se.Aws.Cost.Guard.Lambda.Application.Abstractions;
using Se.Aws.Cost.Guard.Lambda.Domain.Options;

namespace Se.Aws.Cost.Guard.Lambda.Infrastructure.Services;

internal sealed class VpcCleanupService : ICleanupService
{
    private readonly CostGuardOptions opt;

    public VpcCleanupService(CostGuardOptions opt) => this.opt = opt;

    public async Task ExecuteAsync(ILambdaContext ctx, string regionSystemName)
    {
        _ = new AmazonEC2Client(RegionEndpoint.GetBySystemName(regionSystemName));

        // ... (повний код з aws, як у попередньому фрагменті) ...
    }

    private string Pfx(string s) => opt.DryRun ? $"[DRY] {s}" : s;
}

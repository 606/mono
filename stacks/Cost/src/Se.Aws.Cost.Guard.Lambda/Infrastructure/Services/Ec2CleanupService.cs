using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Lambda.Core;
using Se.Aws.Cost.Guard.Lambda.Application.Abstractions;
using Se.Aws.Cost.Guard.Lambda.Domain.Options;
using Se.Aws.Shared.Helpers.Helpers;

namespace Se.Aws.Cost.Guard.Lambda.Infrastructure.Services;

internal sealed class Ec2CleanupService(CostGuardOptions opt) : ICleanupService
{
    public async Task ExecuteAsync(ILambdaContext ctx, string regionSystemName)
    {
        var ec2 = new AmazonEC2Client(RegionEndpoint.GetBySystemName(regionSystemName));

        await HandleEc2InstancesAsync(ec2, ctx);
        await DeleteUnattachedVolumesAsync(ec2, ctx);
        await DeleteOldSnapshotsAsync(ec2, ctx);
    }

    private async Task HandleEc2InstancesAsync(IAmazonEC2 ec2, ILambdaContext ctx)
    {
        var instances = await GetAllInstancesAsync(ec2);
        var active = instances.Where(i => i.State != null && i.State.Name != null &&
            i.State.Name != InstanceStateName.Terminated &&
            i.State.Name != InstanceStateName.ShuttingDown).ToList();

        if (opt.IsHard)
        {
            await StopAndTerminateInstancesAsync(ec2, ctx, active);
        }
        else
        {
            await StopSoftInstancesAsync(ec2, ctx, active);
        }
    }

    private async Task StopAndTerminateInstancesAsync(IAmazonEC2 ec2, ILambdaContext ctx, List<Instance> active)
    {
        var ids = active.Select(i => i.InstanceId).ToList();
        if (ids.Count > 0)
        {
            ctx.Logger.LogLine(Pfx($"Stop {ids.Count} EC2 (HARD)"));
            if (!opt.DryRun)
            {
                await SafeExecutor.Run(async () => await ec2.StopInstancesAsync(new StopInstancesRequest { InstanceIds = ids, Force = true }), ctx, "StopInstances");
            }

            ctx.Logger.LogLine(Pfx($"Terminate {ids.Count} EC2 (HARD)"));
            if (!opt.DryRun)
            {
                await SafeExecutor.Run(async () => await ec2.TerminateInstancesAsync(new TerminateInstancesRequest { InstanceIds = ids }), ctx, "TerminateInstances");
            }
        }
    }

    private async Task StopSoftInstancesAsync(IAmazonEC2 ec2, ILambdaContext ctx, List<Instance> active)
    {
        var ids = active.Where(i => !ShouldKeep(i)).Select(i => i.InstanceId).ToList();
        if (ids.Count > 0)
        {
            ctx.Logger.LogLine(Pfx($"Stop {ids.Count} EC2 (SOFT)"));
            if (!opt.DryRun)
            {
                await SafeExecutor.Run(async () => await ec2.StopInstancesAsync(new StopInstancesRequest { InstanceIds = ids, Force = true }), ctx, "StopInstances");
            }
        }
    }

    private async Task DeleteUnattachedVolumesAsync(IAmazonEC2 ec2, ILambdaContext ctx)
    {
#pragma warning disable IDE0028 // Collection initialization can be simplified
        var vols = await ec2.DescribeVolumesAsync(new DescribeVolumesRequest
        {
            Filters = new() { new("status", new() { "available" }) }
        });
#pragma warning restore IDE0028
        foreach (var v in vols.Volumes)
        {
            ctx.Logger.LogLine(Pfx($"Delete unattached EBS {v.VolumeId} ({v.Size} GiB)"));
            if (!opt.DryRun)
            {
                await SafeExecutor.Run(async () => await ec2.DeleteVolumeAsync(new DeleteVolumeRequest { VolumeId = v.VolumeId }), ctx, "DeleteVolume");
            }
        }
    }

    private async Task DeleteOldSnapshotsAsync(IAmazonEC2 ec2, ILambdaContext ctx)
    {
#pragma warning disable IDE0028 // Collection initialization can be simplified
        var snaps = await ec2.DescribeSnapshotsAsync(new DescribeSnapshotsRequest { OwnerIds = new() { "self" } });
#pragma warning restore IDE0028
        var cutoff = DateTime.UtcNow.AddDays(-opt.SnapshotRetentionDays);
        foreach (var s in snaps.Snapshots.Where(s => s.StartTime.ToUniversalTime() <= cutoff))
        {
            ctx.Logger.LogLine(Pfx($"Delete snapshot {s.SnapshotId} ({s.StartTime:u})"));
            if (!opt.DryRun)
            {
                await SafeExecutor.Run(async () => await ec2.DeleteSnapshotAsync(new DeleteSnapshotRequest { SnapshotId = s.SnapshotId }), ctx, "DeleteSnapshot");
            }
        }
    }

    // Get all EC2 instances in the region
    private static async Task<List<Instance>> GetAllInstancesAsync(IAmazonEC2 ec2)
    {
        var all = new List<Instance>();
        var req = new DescribeInstancesRequest();
        do
        {
            var resp = await ec2.DescribeInstancesAsync(req);
            all.AddRange(resp.Reservations.SelectMany(r => r.Instances));
            req.NextToken = resp.NextToken;
        }
        while (!string.IsNullOrEmpty(req.NextToken));
        return all;
    }

    // Returns true if the instance should be kept (not stopped/terminated)
    private bool ShouldKeep(Instance i)
    {
        var t = i.InstanceType?.Value ?? i.InstanceType;
        var keepType = !string.IsNullOrEmpty(t) && opt.KeepInstanceTypes.Contains(t);
        var skipTag = opt.SkipTagKey is not null && opt.SkipTagValue is not null &&
                      i.Tags?.Any(kv => string.Equals(kv.Key, opt.SkipTagKey, StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(kv.Value, opt.SkipTagValue, StringComparison.OrdinalIgnoreCase)) == true;
        return keepType || skipTag;
    }

    private string Pfx(string s) => opt.DryRun ? $"[DRY] {s}" : s;
}

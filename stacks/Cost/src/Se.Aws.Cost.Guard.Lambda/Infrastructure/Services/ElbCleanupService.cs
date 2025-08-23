using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;
using Amazon.Lambda.Core;
using Se.Aws.Cost.Guard.Lambda.Application.Abstractions;
using Se.Aws.Cost.Guard.Lambda.Domain.Options;
using Se.Aws.Shared.Helpers.Helpers;

namespace Se.Aws.Cost.Guard.Lambda.Infrastructure.Services;

internal sealed class ElbCleanupService : ICleanupService
{
    private readonly CostGuardOptions opt;
    public ElbCleanupService(CostGuardOptions opt)
    {
        this.opt = opt;
    }

    public async Task ExecuteAsync(ILambdaContext ctx, string regionSystemName)
    {
        var elb = new AmazonElasticLoadBalancingClient(RegionEndpoint.GetBySystemName(regionSystemName));
        var elbv2 = new AmazonElasticLoadBalancingV2Client(RegionEndpoint.GetBySystemName(regionSystemName));

        // Classic ELB
        var clbResp = await elb.DescribeLoadBalancersAsync(new Amazon.ElasticLoadBalancing.Model.DescribeLoadBalancersRequest());
        foreach (var lbName in clbResp.LoadBalancerDescriptions.Select(lb => lb.LoadBalancerName))
        {
            ctx.Logger.LogLine(Pfx($"Delete CLB {lbName}", opt.DryRun));
            if (!opt.DryRun)
            {
                await SafeExecutor.Run(() => elb.DeleteLoadBalancerAsync(new Amazon.ElasticLoadBalancing.Model.DeleteLoadBalancerRequest { LoadBalancerName = lbName }), ctx, "DeleteCLB");
            }
        }

        // ALB/NLB
        var v2Resp = await elbv2.DescribeLoadBalancersAsync(new DescribeLoadBalancersRequest());
        foreach (var lb in v2Resp.LoadBalancers)
        {
            ctx.Logger.LogLine(Pfx($"Delete ELBv2 {lb.LoadBalancerName} ({lb.Type})", opt.DryRun));
            if (!opt.DryRun)
            {
                await SafeExecutor.Run(() => elbv2.DeleteLoadBalancerAsync(new DeleteLoadBalancerRequest { LoadBalancerArn = lb.LoadBalancerArn }), ctx, "DeleteELBv2");
            }
        }

        // Orphan Target Groups
        var tgs = await elbv2.DescribeTargetGroupsAsync(new DescribeTargetGroupsRequest());
        foreach (var tgArn in tgs.TargetGroups.Where(t => t.LoadBalancerArns == null || t.LoadBalancerArns.Count == 0).Select(tg => tg.TargetGroupArn))
        {
            ctx.Logger.LogLine(Pfx($"Delete orphan TargetGroup {tgArn}", opt.DryRun));
            if (!opt.DryRun)
            {
                await SafeExecutor.Run(() => elbv2.DeleteTargetGroupAsync(new DeleteTargetGroupRequest { TargetGroupArn = tgArn }), ctx, "DeleteTG");
            }
        }
    }

    private static string Pfx(string s, bool dryRun) => dryRun ? $"[DRY] {s}" : s;
}

using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace Se.Cost.Stack.Infrastructure;

public class CostStack : Amazon.CDK.Stack
{
    public CostStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        // Lambda function asset path (adjust to point to the lambda build output)
        var lambdaAsset = "../../src/Se.Aws.Cost.Guard.Lambda/bin/Release/net8.0";

        // IAM Role for Lambda
        var lambdaRole = new Role(this, "CostGuardLambdaRole", new RoleProps
        {
            AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
            ManagedPolicies =
            [
                ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
                ManagedPolicy.FromAwsManagedPolicyName("AmazonEC2FullAccess"),
                ManagedPolicy.FromAwsManagedPolicyName("ElasticLoadBalancingFullAccess"),
                ManagedPolicy.FromAwsManagedPolicyName("AmazonVPCFullAccess"),
                ManagedPolicy.FromAwsManagedPolicyName("CloudWatchFullAccess"),
                ManagedPolicy.FromAwsManagedPolicyName("AmazonS3ReadOnlyAccess")
            ]
        });

        // Lambda function
        _ = new Function(this, "CostGuardLambda", new FunctionProps
        {
            Runtime = Runtime.DOTNET_8,
            Handler = "Se.Aws.Cost.Guard.Lambda::Se.Aws.Cost.Guard.Lambda.Function::Handler",
            Code = Code.FromAsset(lambdaAsset),
            Role = lambdaRole,
            Timeout = Duration.Seconds(900),
            MemorySize = 512,
            Environment = new Dictionary<string, string>
            {
                { "DRY_RUN", "false" },
                { "MODE", "HARD" },
                { "REGIONS", "us-east-1" },
                { "LOG_RETENTION_DAYS", "3" },
                { "SNAPSHOT_RETENTION_DAYS", "7" }
            }
        });
    }
}

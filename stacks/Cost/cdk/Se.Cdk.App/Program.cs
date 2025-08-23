using Amazon.CDK;
using Se.Cost.Stack.Infrastructure;

namespace Se.Cdk.App;

internal sealed class Program
{
    public static void Main(string[] args)
    {
        _ = args; // Suppress unused parameter warning
        var app = new Amazon.CDK.App();

        // Deploy Cost Guard Stack
        _ = new CostStack(app, "CostStack", new StackProps
        {
            Env = new Environment
            {
                Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
            }
        });

        app.Synth();
    }
}

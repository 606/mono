using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace Se.Aws.Cost.Guard.Lambda.Application.Abstractions;

public interface ICleanupService
{
    Task ExecuteAsync(ILambdaContext ctx, string regionSystemName);
}

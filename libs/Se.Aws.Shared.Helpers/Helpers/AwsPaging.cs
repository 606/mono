using System.Collections.Generic;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

namespace Se.Aws.Shared.Helpers.Helpers;

/// <summary>
/// Helper for paginated AWS API calls.
/// </summary>
public static class AwsPaging
{
    public static async IAsyncEnumerable<LogGroup> EnumerateLogGroupsAsync(IAmazonCloudWatchLogs client)
    {
        string? next = null;
        do
        {
            var response = await client.DescribeLogGroupsAsync(new DescribeLogGroupsRequest { NextToken = next });
            foreach (var logGroups in response.LogGroups)
            {
                yield return logGroups;
            }

            next = response.NextToken;
        }
        while (!string.IsNullOrEmpty(next));
    }
}

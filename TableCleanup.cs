using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using LinqToTwitter;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;

namespace RedditTwitterSyndicator
{
    public static class TableCleanup
    {
        [FunctionName("TableCleanup")]
        public static async Task Run([TimerTrigger("0 0 1 * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }

        static async Task<List<PostQueueEntity>> ReadPostsFromTable()
        {
            CloudTable table = AzureTableHelpers.GetTable();
            var query = new TableQuery<PostQueueEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterConditionForBool(nameof(PostQueueEntity.Tweeted), QueryComparisons.Equal, true),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForDate(nameof(PostQueueEntity.Timestamp), QueryComparisons.LessThan, DateTime.Now.AddDays(-7))
                )
            );

            TableContinuationToken continuationToken = null;
            List<PostQueueEntity> queryResults = new List<PostQueueEntity>();
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResult.ContinuationToken;
                queryResults.AddRange(queryResult.Results);
            } while (continuationToken != null);

            return queryResults;
        }
    }
}

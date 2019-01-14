using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using LinqToTwitter;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace RedditTwitterSyndicator
{
    public static class TwitterPush
    {
        [FunctionName("TwitterPush")]
        public static async Task Run([TimerTrigger("0 40 23 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            List<PostQueueEntity> posts = await ReadPostsFromTable();
        }

        static async Task<List<PostQueueEntity>> ReadPostsFromTable()
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("PostQueue");

            var query = new TableQuery<PostQueueEntity>().Where(TableQuery.GenerateFilterCondition("Tweeted", QueryComparisons.Equal, "false"));

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

        static async Task<bool> UpdatePostsInTable(List<PostQueueEntity> posts)
        {
        }
    }
}

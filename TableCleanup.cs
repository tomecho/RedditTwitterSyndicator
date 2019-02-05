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
        public static async void Run([TimerTrigger("0 0 1 * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var tableWrapper = new PostTableWrapper();
            await ReadPostsFromTable(tableWrapper);
        }

        static async Task ReadPostsFromTable(PostTableWrapper wrapper)
        {
            var query = new TableQuery<PostQueueEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterConditionForBool(nameof(PostQueueEntity.Tweeted), QueryComparisons.Equal, true),
                    TableOperators.Or,
                    TableQuery.GenerateFilterConditionForDate(nameof(PostQueueEntity.Timestamp), QueryComparisons.LessThan, DateTime.Now.AddDays(-7))
                )
            );
            var posts = await wrapper.QueryPosts(query);

            TableBatchOperation batchOperation = new TableBatchOperation();
            posts.ForEach(post => batchOperation.Delete(post));

            await wrapper.table.ExecuteBatchAsync(batchOperation);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace RedditTwitterSyndicator
{
    public class PostTableWrapper
    {
        CloudTable _table;
        public PostTableWrapper()
        {
            _table = GetTable();
        }

        public CloudTable GetTable()
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();
            return tableClient.GetTableReference("PostQueue");
        }

        public async Task<List<PostQueueEntity>> QueryPosts(TableQuery<PostQueueEntity> query)
        {
            TableContinuationToken continuationToken = null;
            List<PostQueueEntity> queryResults = new List<PostQueueEntity>();
            do
            {
                var queryResult = await _table.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResult.ContinuationToken;
                queryResults.AddRange(queryResult.Results);
            } while (continuationToken != null);

            return queryResults;
        }
    }
}
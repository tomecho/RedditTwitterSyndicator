using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;

namespace RedditTwitterSyndicator
{
    public class BigAzureTable<T> where T: TableEntity
    {
        private string _tableName;
        public CloudTable table { get; }
        public BigAzureTable(string tableName)
        {
            _tableName = tableName;
            table = GetTable();
        }

        CloudTable GetTable()
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();
            return tableClient.GetTableReference(_tableName);
        }

        public async Task<List<T>> QueryPosts(TableQuery<PostQueueEntity> query)
        {
            TableContinuationToken continuationToken = null;
            var queryResults = new List<T>();
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResult.ContinuationToken;
                queryResults.AddRange(queryResult.Results as List<T>);
            } while (continuationToken != null);

            return queryResults;
        }

        public async Task ExecuteBatchOperation(TableBatchOperation bigBatch)
        {
            int taken = 0;
            while (taken < bigBatch.Count)
            {
                var partBatch = new TableBatchOperation();
                foreach(TableOperation operation in bigBatch.Skip(taken).Take(100))
                {
                    partBatch.Add(operation); // adds to batch not an add operation
                };
                taken += 100;

                if (partBatch.Count > 0)
                {
                    await table.ExecuteBatchAsync(partBatch);
                }
            }
        }
    }
}
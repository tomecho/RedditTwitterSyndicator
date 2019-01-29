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
    public static class TwitterPush
    {
        [FunctionName("TwitterPush")]
        public static async Task Run([TimerTrigger("0 40 23 * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            List<PostQueueEntity> posts = await ReadPostsFromTable();
            await TweetPosts(posts);
            await UpdatePostsInTable(posts);
        }

        static SingleUserAuthorizer GetAuthorizer()
        {
            var auth = new SingleUserAuthorizer()
            {
                CredentialStore = new SingleUserInMemoryCredentialStore()
                {
                    ConsumerKey = Environment.GetEnvironmentVariable("TWITTER_CONSUMER_KEY"),
                    ConsumerSecret = Environment.GetEnvironmentVariable("TWITTER_CONSUMER_SECRET"),
                    AccessToken = Environment.GetEnvironmentVariable("TWITTER_ACCESS_TOKEN"),
                    AccessTokenSecret = Environment.GetEnvironmentVariable("TWITTER_ACCESS_TOKEN_SECRET"),
                }
            };
            return auth;
        }

        static async Task TweetPosts(List<PostQueueEntity> posts)
        {
            var twitterCtx = new TwitterContext(GetAuthorizer());
            
            var tweetTasks = posts.Select(post => 
                new
                { 
                    Status = twitterCtx.TweetAsync(post.Url),
                    Post = post 
                }
            );
            await Task.WhenAll(tweetTasks.Select(tweetTask => tweetTask.Status));
            posts.ForEach(post => post.Tweeted = true);
        }

        static CloudTable GetTable()
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();
            return tableClient.GetTableReference("PostQueue");
        }

        static async Task<List<PostQueueEntity>> ReadPostsFromTable()
        {
            CloudTable table = GetTable();
            var query = new TableQuery<PostQueueEntity>().Where(TableQuery.GenerateFilterConditionForBool("Tweeted", QueryComparisons.Equal, false));

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

        static Task UpdatePostsInTable(List<PostQueueEntity> posts)
        {
            CloudTable table = GetTable();
            TableBatchOperation updateOperation = new TableBatchOperation();
            posts.ForEach(post => updateOperation.Merge(post));

            return table.ExecuteBatchAsync(updateOperation);
        }
    }
}

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
            SquashDuplicateTweets(posts);

            await TweetPosts(posts.Where(post => !post.Tweeted).ToList());
            posts.ForEach(post => post.Tweeted = true); // mark all posts as tweeted
            await UpdatePostsInTable(posts);
        }

        static void SquashDuplicateTweets(List<PostQueueEntity> posts)
        {
            posts
                .GroupBy(post => post.Url)
                .Where(postGrouping => postGrouping.Count() > 1) // duplicate groupings
                .Select(postGrouping => postGrouping.Skip(1)) // leave the first one alone
                .ToList()
                .ForEach(postGrouping => 
                    postGrouping
                        .ToList()
                        .ForEach(post => post.Tweeted = true) // mark as already tweeted
                );
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

        static Task TweetPosts(List<PostQueueEntity> posts)
        {
            var twitterCtx = new TwitterContext(GetAuthorizer());
            
            IEnumerable<Task> tweetTasks = posts.Select(post =>
                Task.Run(async () => {
                    try 
                    {
                        await twitterCtx.TweetAsync(post.Url);
                    }
                    catch
                    {
                        await Task.CompletedTask;
                    }
                })
            );
            return Task.WhenAll(tweetTasks);
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

        const float MAX_OPERATIONS_PER_BATCH = 100F;
        static Task UpdatePostsInTable(List<PostQueueEntity> posts)
        {
            CloudTable table = GetTable();

            int numGroups = (int) System.Math.Ceiling(posts.Count() / (float) MAX_OPERATIONS_PER_BATCH);
            var updateBatchRequests = posts
                .Select((post, index) => new { Post = post, Id = index + 1 })
                .GroupBy(indexedPost => indexedPost.Id % numGroups)
                .Select(updatableGroup => {
                    TableBatchOperation updateOperation = new TableBatchOperation();
                    updatableGroup
                        .Select(indexedPost => indexedPost.Post)
                        .ToList()
                        .ForEach(post => updateOperation.Merge(post));
                    return table.ExecuteBatchAsync(updateOperation);
                });

            return Task.WhenAll(updateBatchRequests);
        }
    }
}

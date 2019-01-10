using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using RestSharp;
using Newtonsoft.Json;

namespace RedditTwitterSyndicator
{
    public static class RedditPull
    {
        [FunctionName("RedditPull")]
        // todo disable RunOnStartup the value will be unexpected when deployed
        public static void Run([TimerTrigger("0 0 23 * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            string accessToken = GetAccessToken(log);
        }

        private static string GetAccessToken(ILogger log)
        {
            string redditAppId = Environment.GetEnvironmentVariable("REDDIT_APP_ID");
            string redditAppSecret = Environment.GetEnvironmentVariable("REDDIT_APP_SECRET");
            string redditUserName = Environment.GetEnvironmentVariable("REDDIT_USER_NAME");
            string redditPassword = Environment.GetEnvironmentVariable("REDDIT_PASSWORD");

            RestClient client = new RestClient();
            RestRequest request = new RestRequest("https://www.reddit.com/api/v1/access_token", Method.POST);
            request.AddHeader("User-Agent", Environment.GetEnvironmentVariable("USER_AGENT"));
            request.AddHeader("x-li-format", "json");

            request.AddParameter("grant_type", "password");
            request.AddParameter("username", redditUserName);
            request.AddParameter("password", redditPassword);

            client.Authenticator = new RestSharp.Authenticators.HttpBasicAuthenticator(redditAppId, redditAppSecret);

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                log.LogCritical($"Failed to get reddit access token {response.Content}");
            }

            dynamic responseContent = JsonConvert.DeserializeObject(response.Content.ToString());
            return responseContent["access_token"];
        }
    }
}

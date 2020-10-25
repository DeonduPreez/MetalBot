using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MutualClasses.Reddit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedditPollingService
{
    public class Worker : BackgroundService
    {
        private readonly ReadOnlyDictionary<string, string> subreddits = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()
        {
            {"EscapefromTarkov", "https://discord.com/api/webhooks/769327933667147817/Mx4651BIywL0q9S-N5aFwnHvJBTLgcyTeuOivMVblxlbkq2N8-8PntRyvKUuvzssZU0r"}
        });

        private string PreviousPostId { get; set; }

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                foreach (var (subreddit, webhook) in subreddits)
                {
                    var newPost = await GetNewRedditPost(subreddit);
                    if (newPost != null)
                    {
                        await PostToWebHook(webhook, newPost);
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task<RedditTextPost> GetNewRedditPost(string subreddit)
        {
            try
            {
                var client = new HttpClient();
                var result = await client.GetStringAsync($"https://reddit.com/r/{subreddit}/random.json?limit=1");
                var arr = JArray.Parse(result);
                var str = arr[0]["data"]["children"][0]["data"].ToString();
                return JsonConvert.DeserializeObject<RedditTextPost>(str);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex);
#endif
                _logger.LogError(ex.ToString());
                return null;
            }
        }

        private async Task PostToWebHook(string webhook, RedditTextPost newPost)
        {
            try
            {
                var client = new HttpClient();
                var content = GetPostFormattedJSON(newPost);

                var response = await client.PostAsync(webhook, content);
                Console.WriteLine(response);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex);
#endif
                _logger.LogError(ex.ToString());
            }
        }

        private StringContent GetPostFormattedJSON(RedditTextPost newPost)
        {
            // TODO : Check if post has image
            // TODO : Check if post has video
            
            // Title: New post on subreddit
            // URL: Title of post with link
            // Description: Content of post
            // Post Author: /u/Id
            // Content Warning: NSFW Or None

            var contentString = $"{{\"embeds\":[{{\"title\":\"{newPost.title}\",\"url\":\"{newPost.url}\",\"description\":\"{newPost.selftext}\",\"footer\":{{\"icon_url\":\"https://www.redditstatic.com/desktop2x/img/favicon/favicon-32x32.png\",\"text\":\"/u/{newPost.author}\",\"url\":\"https://reddit.com/r/EscapefromTarkov\"}}}}]}}";
            return new StringContent(contentString);
            
            throw new NotImplementedException();
        }
    }
}
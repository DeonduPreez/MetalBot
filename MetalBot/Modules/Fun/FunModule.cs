using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;

namespace MetalBot.Modules.Fun
{
    public class FunModule : ModuleBase
    {
        [Command("meme")]
        [Alias("reddit")]
        public async Task Meme(string subreddit = null)
        {
            try
            {
                // TODO : Check if reddit post is for over_18s then check if nsfw channel
                var client = new HttpClient();

                JObject meme;

                try
                {
                    var result = await client.GetStringAsync($"https://reddit.com/r/{subreddit ?? "memes"}/random.json?limit=1");
                    if (!result.StartsWith("["))
                    {
                        await Context.Channel.SendMessageAsync($"r/{subreddit} does not exist. Please check the name before trying again.");
                        return;
                    }

                    var arr = JArray.Parse(result);
                    meme = JObject.Parse(arr[0]["data"]["children"][0]["data"].ToString());
                }
                catch (HttpRequestException hex)
                {
                    if (hex.Message.Contains("404"))
                    {
                        await Context.Channel.SendMessageAsync($"r/{subreddit} does not exist. Please check the name before trying again.");
                        return;
                    }

                    if (hex.Message.Contains("403"))
                    {
                        await Context.Channel.SendMessageAsync($"r/{subreddit} is private. Please check the name before trying again.");
                        return;
                    }

                    await Context.Channel.SendMessageAsync("An unknown error occurred, please try again later.");
                    // TODO : Get logging service
                    Console.WriteLine(hex);
                    return;
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync("An unknown error occurred, please try again later.");
                    // TODO : Get logging service
                    Console.WriteLine(ex);
                    return;
                }

                // if (bool.TryParse(meme["over_18"].ToString(), out _) && !((IChannel) Context.Channel).)
                // {
                // }

                var builder = new EmbedBuilder()
                    .WithImageUrl(meme["url"].ToString())
                    .WithColor(Color.Green)
                    .WithTitle(meme["title"].ToString())
                    .WithUrl($"https://reddit.com{meme["permalink"]}")
                    .WithFooter($"🗨️ {meme["num_comments"]} ⬆ {meme["ups"]}");

                var embed = builder.Build();
                await Context.Channel.SendMessageAsync(null, false, embed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await ReplyAsync("An unexpected error occurred");
            }
        }
    }
}
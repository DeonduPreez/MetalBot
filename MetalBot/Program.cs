using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MetalBot.Exceptions.Startup;

namespace MetalBot
{
    public class Program
    {
        private DiscordSocketClient _client;

        public static void Main(string[] args)
            => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args)
        {
            // TODO : Move this out to a config file if any more config options are necessary
            var token = Environment.GetEnvironmentVariable("token");
            if (string.IsNullOrWhiteSpace(token))
            {
                if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
                {
                    throw new TokenNotFoundException("Environment variable 'token' does not exist or is empty");
                }

                token = args[0];
            }

            _client = new DiscordSocketClient();

            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
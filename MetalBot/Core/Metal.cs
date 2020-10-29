using System;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using MetalBot.Exceptions.Startup;
using MetalBot.Helpers;
using MetalBot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MetalBot.Core
{
    public class Metal
    {
        public static Task StartAsync(string[] args)
        {
            Console.Title = "MetalBot";
            Console.CursorVisible = false;
            return new Metal().LoginAsync(args);
        }

        private IServiceProvider _provider;
        private DiscordSocketClient _client;
        private CommandService _commands;

        private static IServiceProvider BuildServiceProvider()
        {
            return new ServiceCollection()
                .AddSingleton(GetDiscordSocketClient())
                .AddSingleton(GetDiscordCommandsService())
                .AddSingleton(GetLoggingService())
                .AddSingleton<InteractiveService>()
                .AddSingleton<HandleMessageQueue>()
                .BuildServiceProvider();
        }

        private async Task LoginAsync(string[] args)
        {
            // if (!Config.Initialize()) return;aa

            _provider = BuildServiceProvider();

            _client = _provider.GetRequiredService<DiscordSocketClient>();
            _commands = _provider.GetRequiredService<CommandService>();
            var logger = _provider.GetRequiredService<LoggingService>();

            _client.Log += logger.LogSystemMessage;
            _commands.Log += logger.LogSystemMessage;
            _client.Ready += ClientReady;

            var commandHandler = new CommandHandler(_provider);
            await commandHandler.InstallCommandsAsync();


            await _client.StartAsync();

            var token = Environment.GetEnvironmentVariable("token");
            if (string.IsNullOrWhiteSpace(token))
            {
                if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
                {
                    throw new TokenNotFoundException("Environment variable 'token' does not exist or is empty");
                }

                token = args[0];
            }

            await _client.LoginAsync(TokenType.Bot, token);

            // Initialize(_provider);

            try
            {
                await Task.Delay(-1);
            }
            catch (TaskCanceledException) //this exception always occurs when CancellationTokenSource#Cancel() is called; so we put the shutdown logic inside the catch block
            {
                logger.Critical("Bot shutdown requested; shutting down and cleaning up.");
                await ShutdownAsync(_provider);
            }
        }

        public static Task ShutdownAsync(IServiceProvider provider)
        {
            foreach (var disposable in provider.GetServices<IDisposable>())
            {
                disposable?.Dispose();
            }

            Environment.Exit(0);
            return Task.CompletedTask;
        }

        private Task ClientReady()
        {
            Console.WriteLine("Bot is connected!");
            return Task.CompletedTask;
        }

        private static DiscordSocketClient GetDiscordSocketClient()
        {
            return new DiscordSocketClient(new DiscordSocketConfig
            {
                // How much logging do you want to see?
                LogLevel = LogSeverity.Info,
                ExclusiveBulkDelete = true

                // If you or another service needs to do anything with messages
                // (eg. checking Reactions, checking the content of edited/deleted messages),
                // you must set the MessageCacheSize. You may adjust the number as needed.
                //MessageCacheSize = 50,

                // If your platform doesn't have native WebSockets,
                // add Discord.Net.Providers.WS4Net from NuGet,
                // add the `using` at the top, and uncomment this line:
                //WebSocketProvider = WS4NetProvider.Instance
            });
        }

        private static CommandService GetDiscordCommandsService()
        {
            return new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false,
                SeparatorChar = ';'
            });
        }

        private static LoggingService GetLoggingService()
        {
            return new LoggingService();
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MetalBot.Services;

namespace MetalBot.Helpers
{
    public class HandleMessageQueue
    {
        private const char Prefix = '!';

        private readonly BackgroundWorker _backgroundWorker = new BackgroundWorker();
        private readonly ConcurrentDictionary<ulong, SocketUserMessage> _concurrentDictionary = new ConcurrentDictionary<ulong, SocketUserMessage>();

        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly LoggingService _loggingService;

        public HandleMessageQueue(DiscordSocketClient client, CommandService commands, IServiceProvider services, LoggingService loggingService)
        {
            _client = client;
            _commands = commands;
            _services = services;
            _loggingService = loggingService;
            _backgroundWorker.DoWork += HandleMessage;
            _backgroundWorker.RunWorkerAsync();
        }

        private async void HandleMessage(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (_concurrentDictionary.IsEmpty)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(5000));
                    continue;
                }

                var first = _concurrentDictionary.First().Key;
                _concurrentDictionary.TryRemove(first, out var msg);

                if (msg == null)
                {
                    continue;
                }
                
                Console.WriteLine($"Doing msg with Id: {msg.Id}");

                // Create a number to track where the prefix ends and the command begins
                var pos = 0;
                // Replace the '!' with whatever character
                // you want to prefix your commands with.
                // Uncomment the second half if you also want
                // commands to be invoked by mentioning the bot instead.
                if (!msg.HasCharPrefix(Prefix, ref pos) && !msg.HasMentionPrefix(_client.CurrentUser, ref pos))
                {
                    continue;
                }

                // Create a Command Context.
                var context = new SocketCommandContext(_client, msg);

                // Execute the command. (result does not indicate a return value, 
                // rather an object stating if the command executed successfully).
                var result = await _commands.ExecuteAsync(context, pos, _services);

                // Uncomment the following lines if you want the bot
                // to send a message if it failed.
                // This does not catch errors from commands with 'RunMode.Async',
                // subscribe a handler for '_commands.CommandExecuted' to see those.
                if (!result.IsSuccess)
                {
                    if (result.Error != CommandError.UnknownCommand)
                    {
                        await msg.Channel.SendMessageAsync("Good job fucking up your command");
                    }

                    _loggingService.Error($"Error executing command: {result.ErrorReason}");
                }
            }
        }

        public void EnqueueMessage(SocketUserMessage msg)
        {
            _concurrentDictionary.AddOrUpdate(msg.Id, msg, (arg1, message) => message);
        }

        public void DequeueMessage(ulong id)
        {
            if (_concurrentDictionary.ContainsKey(id))
            {
                Console.WriteLine($"Removing msg with Id: {id}");
                _concurrentDictionary.TryRemove(id, out _);
            }
        }
    }
}
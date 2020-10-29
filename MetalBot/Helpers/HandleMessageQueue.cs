﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Discord.Commands;
using Discord.WebSocket;
using MetalBot.Services;

namespace MetalBot.Helpers
{
    public class HandleMessageQueue : IDisposable
    {
        private const char Prefix = '!';
        private const int WorkerCount = 5;

        private readonly List<BackgroundWorker> _backgroundWorkers = new List<BackgroundWorker>();
        private readonly ConcurrentQueue<SocketUserMessage> _concurrentDictionary = new ConcurrentQueue<SocketUserMessage>();
        private readonly object _mutex = new object();
        private bool _running;

        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly LoggingService _loggingService;

        public HandleMessageQueue(DiscordSocketClient client, CommandService commands, IServiceProvider services, LoggingService loggingService)
        {
            _running = true;
            _client = client;
            _commands = commands;
            _services = services;
            _loggingService = loggingService;
            for (var i = 0; i < WorkerCount; i++)
            {
                var backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += HandleMessage;
                backgroundWorker.RunWorkerAsync();
                _backgroundWorkers.Add(backgroundWorker);
            }
        }

        private async void HandleMessage(object sender, DoWorkEventArgs e)
        {
            while (_running)
            {
                if (_concurrentDictionary.IsEmpty)
                {
                    lock (_mutex)
                    {
                        Monitor.Wait(_mutex, 100);
                    }

                    continue;
                }

                if (!_concurrentDictionary.TryDequeue(out var msg) || msg == null)
                {
                    continue;
                }

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
            _concurrentDictionary.Enqueue(msg);
            lock (_mutex)
            {
                Monitor.Pulse(_mutex);
            }
        }

        public void Dispose()
        {
            _running = false;
            foreach (var backgroundWorker in _backgroundWorkers)
            {
                backgroundWorker.Dispose();
            }

            _backgroundWorkers.Clear();
            _client?.Dispose();
            ((IDisposable) _commands)?.Dispose();
            _loggingService?.Dispose();
        }
    }
}
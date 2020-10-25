﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MetalBot.Modules.General
{
    public class GeneralModule : ModuleBase
    {
        private readonly CommandService _commandService;

        public GeneralModule(CommandService commandService)
        {
            _commandService = commandService;
        }

        [Command("info")]
        [RequireContext(ContextType.Guild)]
        public async Task Info(SocketGuildUser user = null)
        {
            try
            {
                await Context.Message.DeleteAsync();

                user ??= (SocketGuildUser) Context.User;

                var builder = new EmbedBuilder()
                    .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                    .WithDescription($"Here's some info about {user.Mention}")
                    .WithColor(Color.Green)
                    .AddField("User ID", user.Id, true)
                    .AddField("Created at", user.CreatedAt.ToString("dd/MM/yyyy"), true)
                    .AddField("Joined at", user.JoinedAt.Value.ToString("dd/MM/yyyy"), true)
                    .AddField("Roles", string.Join(" ", user.Roles.Where(r => !r.IsEveryone).Select(r => r.Mention)), true)
                    .WithCurrentTimestamp();

                var embed = builder.Build();
                await Context.Channel.SendMessageAsync(null, false, embed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await ReplyAsync("An unexpected error occurred");
            }
        }

        [Command("help")]
        [Summary("Sends all available commands")]
        public async Task Help()
        {
            try
            {
                var commands = _commandService.Commands.ToList();
                var embedBuilder = new EmbedBuilder();

                foreach (var command in commands)
                {
                    var pc = await command.CheckPreconditionsAsync(Context);

                    if (!pc.IsSuccess)
                    {
                        continue;
                    }

                    // Get the command Summary attribute information
                    var embedFieldText = command.Summary ?? "No description available" + Environment.NewLine;

                    embedBuilder.AddField(command.Name, embedFieldText);
                }

                await ReplyAsync("Here's a list of commands and their description: ", false, embedBuilder.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await ReplyAsync("An unexpected error occurred");
            }
        }

        [Command("say")]
        [Summary("Echoes a message.")]
        [RequireContext(ContextType.Guild)]
        public async Task SayAsync([Remainder] [Summary("The text to echo")]
            string echo) => await ReplyAsync(echo);
    }
}
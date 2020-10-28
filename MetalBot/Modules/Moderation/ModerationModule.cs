using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using MetalBot.Services;
using MetalBotDAL;
using MetalBotDAL.Entities.Discord;
using Microsoft.EntityFrameworkCore;

namespace MetalBot.Modules.Moderation
{
    public class ModerationModule : InteractiveBase
    {
        private readonly CommandService _commandService;
        private readonly LoggingService _loggingService;

        public ModerationModule(CommandService commandService, LoggingService loggingService)
        {
            _commandService = commandService;
            _loggingService = loggingService;
        }


        [Command("deletemessages")]
        [RequireContext(ContextType.DM)]
        public async Task DeleteMessages(int amount)
        {
            try
            {
                var enumerable = (await Context.Channel.GetMessagesAsync().FlattenAsync()).Where(m => m.Author.Id == Context.Client.CurrentUser.Id).Take(amount);
                var messages = enumerable as IMessage[] ?? enumerable.ToArray();

                foreach (var message in messages)
                {
                    await ((SocketDMChannel) Context.Channel).DeleteMessageAsync(message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await ReplyAsync("An unexpected error occurred");
            }
        }


        [Command("purge")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        public async Task Purge(int amount, bool includePinned = false)
        {
            try
            {
                var enumerable = await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync();
                if (!includePinned)
                {
                    enumerable = enumerable.Where(m => !m.IsPinned);
                }
                var messages = enumerable as IMessage[] ?? enumerable.ToArray();
                await ((SocketTextChannel) Context.Channel).DeleteMessagesAsync(messages);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await ReplyAsync("An unexpected error occurred");
            }
        }

        [Command("ban")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireContext(ContextType.Guild)]
        public async Task Ban(SocketGuildUser user, string reason = null)
        {
            try
            {
                await user.BanAsync(0, reason);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await ReplyAsync("An unexpected error occurred");
            }
        }

        [Command("showbans")]
        [Alias("banlist", "bans")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireContext(ContextType.Guild)]
        public async Task ShowBans()
        {
            try
            {
                var bans = await Context.Guild.GetBansAsync();

                if (bans.Count == 0)
                {
                    await ReplyAsync("No users have been banned yet");
                    return;
                }

                var embedBuilder = new EmbedBuilder();

                foreach (var ban in bans)
                {
                    // Get the command Summary attribute information
                    embedBuilder.AddField(ban.User.Username, ban.Reason ?? "No reason was supplied");
                }

                await ReplyAsync("Here's a list of all banned users and the reasons they have been banned: ", false, embedBuilder.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await ReplyAsync("An unexpected error occurred");
            }
        }

        [Command("mute")]
        [Alias("shutthefuckup", "shutup")]
        [RequireUserPermission(ChannelPermission.MuteMembers)]
        [RequireContext(ContextType.Guild)]
        public async Task Mute(SocketGuildUser user = null)
        {
            await ReplyAsync("This command has not been implemented");
            // TODO : Set up roles on startup foreach guild
            // TODO : Set up muted role on discord guild in database
            // TODO : Edit setup to accommodate for muted role
            user ??= (SocketGuildUser) Context.User;
            
            // user.
        }

        [Command("setup", RunMode = RunMode.Async)]
        [Summary("Sets up the database with data from this server, e.g. roles channel, etc")]
        [RequireOwner]
        [RequireContext(ContextType.Guild)]
        public async Task SetupGuild()
        {
            var messages = new List<IUserMessage>()
            {
                Context.Message
            };

            var adminChannel = await GetMentionedChannel("Admin", messages);
            if (adminChannel == null)
            {
                return;
            }

            var rolesChannel = await GetMentionedChannel("Roles", messages);
            if (rolesChannel == null)
            {
                return;
            }

            var rulesChannel = await GetMentionedChannel("Rules", messages);
            if (rulesChannel == null)
            {
                return;
            }

            try
            {
                await using var dbContext = new MetalBotContext();
                var discordGuild = await dbContext.DiscordGuilds.AsQueryable().FirstAsync();
                discordGuild.AdminChannel = new DiscordChannel(adminChannel);
                discordGuild.RolesChannel = new DiscordChannel(rolesChannel);
                discordGuild.RulesChannel = new DiscordChannel(rulesChannel);
                dbContext.DiscordGuilds.Update(discordGuild);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error setting up server");
                Console.WriteLine(e);
                _loggingService.Error($"Error setting up guild with Id: {Context.Guild.Id}");
                return;
            }
            finally
            {
                await ((SocketTextChannel) Context.Channel).DeleteMessagesAsync(messages);
            }

            await ReplyAsync($"Server set up successfully");
        }

        private async Task<SocketTextChannel> GetMentionedChannel(string channelName, List<IUserMessage> socketMessages)
        {
            var message = $"Please tag the {channelName} channel";
            var response = await GetNextMessageAsync(message, socketMessages);
            var channel = response?.MentionedChannels.FirstOrDefault() as SocketTextChannel;
            var failCount = 1;
            var channelIsNull = channel == null;
            while (channelIsNull)
            {
                if (failCount == 5)
                {
                    await ReplyAsync("You dumb cunt, you fucked up 5 times. Restart the !setup process");
                    return null;
                }

                failCount++;
                response = await GetNextMessageAsync(message, socketMessages);
                channel = response.MentionedChannels.FirstOrDefault() as SocketTextChannel;
                channelIsNull = channel == null;
            }

            return channel;
        }

        private async Task<SocketMessage> GetNextMessageAsync(string message, List<IUserMessage> socketMessages)
        {
            socketMessages.Add(await ReplyAsync(message));
            var response = await NextMessageAsync();

            if (response == null)
            {
                await ReplyAsync("You did not reply before the timeout, please run !setup again");
            }
            else
            {
                socketMessages.Add((IUserMessage) response);
            }

            return response;
        }
    }
}
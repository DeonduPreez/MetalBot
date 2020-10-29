using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using MetalBot.Helpers.Interactive;
using MetalBot.Services;
using MetalBotDAL;
using MetalBotDAL.Entities.Discord;
using Microsoft.EntityFrameworkCore;

namespace MetalBot.Modules.Moderation
{
    public class ModerationModule : InteractiveBase
    {
        private readonly LoggingService _loggingService;

        public ModerationModule(LoggingService loggingService)
        {
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
        [RequireUserPermission(GuildPermission.Administrator)]
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
                adminChannel = await HandleNoChannelExists("Admin", messages);
                if (adminChannel != null)
                {
                    var everyone = Context.Guild.EveryoneRole;
                    await adminChannel.AddPermissionOverwriteAsync(everyone, OverwritePermissions.DenyAll(adminChannel));
                }
            }

            var rolesChannel = await GetMentionedChannel("Roles", messages) ?? await HandleNoChannelExists("Roles", messages);
            var rulesChannel = await GetMentionedChannel("Rules", messages) ?? await HandleNoChannelExists("Rules", messages);

            try
            {
                await using var dbContext = new MetalBotContext();
                var discordGuild = await dbContext.DiscordGuilds.AsQueryable().FirstAsync();
                discordGuild.AdminChannel = adminChannel == null ? discordGuild.AdminChannel : new DiscordChannel(adminChannel);
                discordGuild.RolesChannel = rolesChannel == null ? discordGuild.RolesChannel : new DiscordChannel(rolesChannel);
                discordGuild.RulesChannel = rulesChannel == null ? discordGuild.RulesChannel : new DiscordChannel(rulesChannel);
                if (discordGuild.Owner == null || discordGuild.Owner.IdExternal != (long) Context.Guild.OwnerId)
                {
                    discordGuild.Owner = dbContext.DiscordUsers.First(du => du.IdExternal == (long) Context.Guild.OwnerId);
                }

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

            await ReplyAndDeleteAsync($"Server set up successfully");
        }

        private async Task<IGuildChannel> GetMentionedChannel(string channelName, List<IUserMessage> socketMessages)
        {
            var message = $"Please tag the {channelName} channel, replying \"null\" implies no channel exists";
            var failCount = 0;
            SocketTextChannel channel = null;
            var channelIsNull = true;
            while (channelIsNull)
            {
                if (failCount == 5)
                {
                    await ReplyAsync("Good job, you fucked up 5 times. Restart the !setup process");
                    return null;
                }

                failCount++;
                var response = await GetNextMessageAsync(message, socketMessages);
                if (response.Content.ToLower() == "null")
                {
                    return null;
                }

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

        private async Task<IGuildChannel> HandleNoChannelExists(string channelName, List<IUserMessage> socketMessages)
        {
            socketMessages.Add(await ReplyAsync($"Would you like to create the {channelName} channel?"));
            var response = await NextMessageAsync();
            socketMessages.Add((IUserMessage) response);
            if (ResponseHelper.IsYesResponse(response.Content))
            {
                return await CreateTextChannel(channelName, socketMessages);
            }

            socketMessages.Add(await ReplyAsync($"No {channelName} channel will be set up"));
            return null;
        }

        private async Task<RestTextChannel> CreateTextChannel(string channelName, List<IUserMessage> socketMessages)
        {
            var channel = await Context.Guild.CreateTextChannelAsync(channelName);
            socketMessages.Add(await ReplyAsync($"{channel.Mention} created"));
            return channel;
        }
    }
}
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MetalBot.Helpers;
using MetalBotDAL;
using MetalBotDAL.Entities.Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MetalBot.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly LoggingService _loggingService;
        private readonly IServiceProvider _services;
        private readonly HandleMessageQueue _handleMessageQueue;

        public static SocketRole _tarkoofRole;

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _loggingService = services.GetRequiredService<LoggingService>();
            _handleMessageQueue = services.GetRequiredService<HandleMessageQueue>();
            _services = services;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.Ready += ClientOnReady;
            _client.MessageReceived += HandleMessageReceivedAsync;
            _client.UserJoined += UserJoined;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            try
            {
                await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                    services: _services);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task ClientOnReady()
        {
            try
            {
                await using var dbContext = new MetalBotContext();
                // Uncomment with db changes
                // await dbContext.Database.EnsureDeletedAsync();
                await dbContext.Database.EnsureCreatedAsync();
                var firstSetup = !dbContext.DiscordGuilds.Any();
                await CreateOrUpdateGuilds(dbContext);
                await dbContext.SaveChangesAsync();

                if (firstSetup)
                {
                    foreach (var discordGuild in await dbContext.DiscordGuilds.AsAsyncEnumerable().ToListAsync())
                    {
                        var clientGuild = _client.Guilds.First(cg => (long) cg.Id == discordGuild.IdExternal);
                        await clientGuild.SystemChannel.SendMessageAsync(clientGuild.Owner.Mention + " please run !setup to set up the database");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            // _tarkoofRole = _metalsDictatorship.Roles.First(c => c.Name.ToLower() == "tarkoof");
        }

        public async Task UserJoined(SocketGuildUser user)
        {
            if (user.IsBot || user.IsWebhook)
            {
                return;
            }

            var rulesChannel = await ChannelHelper.GetRulesChannel(user.Guild);
            var rolesChannel = await ChannelHelper.GetRolesChannel(user.Guild);

            if (rulesChannel == null || rolesChannel == null)
            {
                await user.Guild.SystemChannel.SendMessageAsync();
            }

            await user.Guild.SystemChannel.SendMessageAsync($"{user.Mention} Sup bitch, go to {rulesChannel.Mention} then go to {rolesChannel.Mention} to get set up");
        }

        private Task HandleMessageReceivedAsync(SocketMessage arg)
        {
            // Bail out if it's a System Message.
            if (!(arg is SocketUserMessage msg))
            {
                return Task.CompletedTask;
            }

            // We don't want the bot to respond to itself or other bots.
            if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot)
            {
                return Task.CompletedTask;
            }

            _handleMessageQueue.EnqueueMessage(msg);

            return Task.CompletedTask;
        }

        private async Task HandleMessageUpdatedAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            // If the message was not in the cache, downloading it will result in getting a copy of `after`.
            var message = await before.GetOrDownloadAsync();
            _loggingService.Info($"Message updated: {message} -> {after}");
        }

        // private async Task HandleCommandAsync(SocketMessage messageParam)
        // {
        //     // Don't process the command if it was a system message
        //     var message = messageParam as SocketUserMessage;
        //     if (message == null) return;
        //
        //     // Create a number to track where the prefix ends and the command begins
        //     int argPos = 0;
        //
        //     // Determine if the message is a command based on the prefix and make sure no bots trigger commands
        //     if (!(message.HasCharPrefix('!', ref argPos) ||
        //           message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
        //         message.Author.IsBot)
        //         return;
        //
        //     // Create a WebSocket-based command context based on the message
        //     var context = new SocketCommandContext(_client, message);
        //
        //     // Execute the command with the command context we just
        //     // created, along with the service provider for precondition checks.
        //     await _commands.ExecuteAsync(
        //         context: context,
        //         argPos: argPos,
        //         services: null);
        // }

        private async Task CreateOrUpdateGuilds(MetalBotContext dbContext)
        {
            var socketGuilds = _client.Guilds.ToList();
            var dbGuilds = await AsyncEnumerable.ToListAsync(dbContext.DiscordGuilds);

            for (var i = dbGuilds.Count - 1; i >= 0; i--)
            {
                var dbGuild = dbGuilds[i];
                var socketGuild = socketGuilds.FirstOrDefault(dg => (long) dg.Id == dbGuild.IdExternal);

                if (socketGuild == null)
                {
                    dbContext.DiscordGuilds.Remove(dbGuild);
                }
                else
                {
                    await CreateOrUpdateGuildUsers(dbContext, socketGuild);
                    await CreateOrUpdateGuildChannels(dbContext, socketGuild);
                    var guildUsers = await dbContext.DiscordUsers.AsQueryable().Where(du => socketGuild.Users.Select(sgu => (long) sgu.Id).Contains(du.IdExternal)).ToListAsync();
                    var guildChannels = await dbContext.DiscordChannels.AsQueryable().Where(du => socketGuild.Channels.Select(sgc => (long) sgc.Id).Contains(du.IdExternal)).ToListAsync();

                    dbGuild.Name = socketGuild.Name;
                    dbGuild.Description = socketGuild.Description;
                    dbGuild.DefaultChannel = socketGuild.DefaultChannel == null ? null : guildChannels.First(dc => dc.IdExternal == (long) socketGuild.DefaultChannel.Id);
                    dbGuild.EmbedChannel = socketGuild.EmbedChannel == null ? null : guildChannels.First(dc => dc.IdExternal == (long) socketGuild.EmbedChannel.Id);
                    dbGuild.SystemChannel = socketGuild.SystemChannel == null ? null : guildChannels.First(dc => dc.IdExternal == (long) socketGuild.SystemChannel.Id);
                    if (socketGuild.OwnerId > 0)
                    {
                        dbGuild.Owner = dbContext.DiscordUsers.First(du => du.IdExternal == (long) socketGuild.OwnerId);
                    }

                    dbGuild.DiscordUsers = guildUsers;

                    dbContext.DiscordGuilds.Update(dbGuild);

                    socketGuilds.Remove(socketGuild);
                }
            }

            foreach (var socketGuild in socketGuilds)
            {
                await CreateOrUpdateGuildUsers(dbContext, socketGuild);
                await CreateOrUpdateGuildChannels(dbContext, socketGuild);

                var discordUsers = await dbContext.DiscordUsers.AsQueryable().Where(du => socketGuild.Users.Select(sgu => (long) sgu.Id).Contains(du.IdExternal)).ToListAsync();
                var discordChannels = await dbContext.DiscordChannels.AsQueryable().Where(du => socketGuild.Channels.Select(sgc => (long) sgc.Id).Contains(du.IdExternal)).ToListAsync();

                await dbContext.DiscordGuilds.AddAsync(new DiscordGuild(socketGuild, discordUsers, discordChannels));
            }
        }

        private async Task CreateOrUpdateGuildChannels(MetalBotContext dbContext, SocketGuild socketGuild)
        {
            var socketChannels = socketGuild.Channels.ToList();
            var dbChannels = await dbContext.DiscordChannels.AsQueryable()
                .Where(dc => socketChannels.Select(sc => (long) sc.Id).Contains(dc.IdExternal)).ToListAsync();

            for (var i = dbChannels.Count - 1; i >= 0; i--)
            {
                var dbChannel = dbChannels[i];
                var socketChannel = socketChannels.FirstOrDefault(dg => (long) dg.Id == dbChannel.IdExternal);

                if (socketChannel == null)
                {
                    dbContext.DiscordChannels.Remove(dbChannel);
                }
                else
                {
                    var dbUsers = await dbContext.DiscordUsers.AsQueryable().Where(du => socketChannel.Users.Select(scu => (long) scu.Id).Contains(du.IdExternal)).ToListAsync();
                    dbChannel.Name = socketChannel.Name;
                    dbChannel.Position = socketChannel.Position;
                    dbChannel.CreatedAt = socketChannel.CreatedAt;
                    dbChannel.DiscordUsers = dbUsers;

                    dbContext.DiscordChannels.Update(dbChannel);
                    socketChannels.Remove(socketChannel);
                }
            }

            foreach (var socketChannel in socketChannels)
            {
                await dbContext.DiscordChannels.AddAsync(new DiscordChannel(socketChannel));
            }

            await dbContext.SaveChangesAsync();
        }

        private async Task CreateOrUpdateGuildUsers(MetalBotContext dbContext, SocketGuild socketGuild)
        {
            var socketUsers = socketGuild.Users.ToList();
            var dbUsers = await dbContext.DiscordUsers.AsQueryable()
                .Where(du => socketUsers.Select(su => (long) su.Id).Contains(du.IdExternal)).ToListAsync();

            for (var i = dbUsers.Count - 1; i >= 0; i--)
            {
                var dbUser = dbUsers[i];
                var socketUser = socketUsers.FirstOrDefault(dg => (long) dg.Id == dbUser.IdExternal);

                if (socketUser == null)
                {
                    // Remove link between user and guild
                    dbUser.RemoveGuild(socketGuild.Id);
                }
                else
                {
                    // dbUser.DiscordGuilds.Remove(dbUser.DiscordGuilds.FirstOrDefault(dg => dg.IdExternal == socketGuild.Id));
                    dbUser.Username = socketUser.Username;
                    dbUser.Mention = socketUser.Mention;
                    dbUser.IsBot = socketUser.IsBot;
                    dbUser.IsWebhook = socketUser.IsWebhook;

                    dbContext.DiscordUsers.Update(dbUser);
                    socketUsers.Remove(socketUser);
                }
            }

            foreach (var socketUser in socketUsers)
            {
                await dbContext.DiscordUsers.AddAsync(new DiscordUser(socketUser));
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
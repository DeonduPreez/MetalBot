using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using MetalBotDAL;

namespace MetalBot.Helpers
{
    public static class ChannelHelper
    {
        public static SocketTextChannel _adminChannel;
        public static SocketTextChannel _rolesChannel;
        public static SocketTextChannel _rulesChannel;

        public static async Task<SocketTextChannel> GetAdminChannel(SocketGuild socketGuild)
        {
            var adminChannel = _adminChannel;
            if (adminChannel == null)
            {
                await using var dbContext = new MetalBotContext();
                var dbGuild = dbContext.DiscordGuilds.First(dg => dg.IdExternal == (long) socketGuild.Id);
                if (dbGuild.RulesChannel != null)
                {
                    adminChannel = (SocketTextChannel) socketGuild.Channels.FirstOrDefault(sgc => (long) sgc.Id == dbGuild.AdminChannel.IdExternal);
                    _adminChannel = adminChannel;
                }
            }

            return adminChannel;
        }

        public static async Task<SocketTextChannel> GetRulesChannel(SocketGuild socketGuild)
        {
            var rulesChannel = _rulesChannel;
            if (rulesChannel == null)
            {
                await using var dbContext = new MetalBotContext();
                var dbGuild = dbContext.DiscordGuilds.First(dg => dg.IdExternal == (long) socketGuild.Id);
                if (dbGuild.RulesChannel != null)
                {
                    rulesChannel = (SocketTextChannel) socketGuild.Channels.FirstOrDefault(sgc => (long) sgc.Id == dbGuild.RulesChannel.IdExternal);
                    _rulesChannel = rulesChannel;
                }
            }

            return rulesChannel;
        }

        public static async Task<SocketTextChannel> GetRolesChannel(SocketGuild socketGuild)
        {
            var rolesChannel = _rolesChannel;
            if (rolesChannel == null)
            {
                await using var dbContext = new MetalBotContext();
                var dbGuild = dbContext.DiscordGuilds.First(dg => dg.IdExternal == (long) socketGuild.Id);
                if (dbGuild.RulesChannel != null)
                {
                    rolesChannel = (SocketTextChannel) socketGuild.Channels.FirstOrDefault(sgc => (long) sgc.Id == dbGuild.RolesChannel.IdExternal);
                    _rolesChannel = rolesChannel;
                }
            }

            return rolesChannel;
        }
    }
}
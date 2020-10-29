using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;

namespace MetalBotDAL.Entities.Discord
{
    public class DiscordGuild
    {
        public DiscordGuild()
        {
        }

        public DiscordGuild(SocketGuild socketGuild, List<DiscordUser> discordUsers, List<DiscordChannel> discordChannels)
        {
            IdExternal = (long) socketGuild.Id;
            Name = socketGuild.Name;
            Description = socketGuild.Description;
            DefaultChannel = socketGuild.DefaultChannel == null ? null : discordChannels.FirstOrDefault(dc => dc.IdExternal == (long) socketGuild.DefaultChannel.Id);
            EmbedChannel = socketGuild.EmbedChannel == null ? null : discordChannels.FirstOrDefault(dc => dc.IdExternal == (long) socketGuild.EmbedChannel.Id);
            SystemChannel = socketGuild.SystemChannel == null ? null : discordChannels.FirstOrDefault(dc => dc.IdExternal == (long) socketGuild.SystemChannel.Id);
            if (socketGuild.Owner != null)
            {
                // Fuck knows why socketGuild.Owner is null
                Owner = discordUsers.First(du => du.IdExternal == (long) socketGuild.Owner.Id);                
            }
            DiscordUsers = discordUsers;
            DiscordChannels = discordChannels;
        }

        public int IdDiscordGuild { get; set; }
        public long IdExternal { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public virtual DiscordChannel DefaultChannel { get; set; }
        public virtual DiscordChannel EmbedChannel { get; set; }
        public virtual DiscordChannel SystemChannel { get; set; }
        public virtual DiscordChannel AdminChannel { get; set; }
        public virtual DiscordChannel RolesChannel { get; set; }
        public virtual DiscordChannel RulesChannel { get; set; }
        public virtual ICollection<DiscordChannel> DiscordChannels { get; set; }
        public virtual ICollection<DiscordUser> DiscordUsers { get; set; }
        public virtual DiscordUser Owner { get; set; }
    }
}
using System.Collections.Generic;
using System.Linq;
using Discord;

namespace MetalBotDAL.Entities.Discord
{
    public class DiscordUser
    {
        public DiscordUser()
        {
        }

        public DiscordUser(IUser socketGuildUser)
        {
            IdExternal = (long) socketGuildUser.Id;
            Username = socketGuildUser.Username;
            Mention = socketGuildUser.Mention;
            IsBot = socketGuildUser.IsBot;
            IsWebhook = socketGuildUser.IsWebhook;
        }

        public int IdDiscordUser { get; set; }

        public long IdExternal { get; set; }
        public string Username { get; set; }
        public string Mention { get; set; }
        public bool IsBot { get; set; }
        public bool IsWebhook { get; set; }

        public virtual ICollection<DiscordGuild> DiscordGuilds { get; set; }

        public void RemoveGuild(ulong socketGuildId)
        {
            DiscordGuilds.Remove(DiscordGuilds.FirstOrDefault(dg => dg.IdExternal == (long) socketGuildId));
        }
    }
}
using System;
using System.Collections.Generic;
using Discord;

namespace MetalBotDAL.Entities.Discord
{
    public class DiscordChannel
    {
        public DiscordChannel()
        {
        }

        public DiscordChannel(IGuildChannel socketChannel)
        {
            IdExternal = (long) socketChannel.Id;
            Name = socketChannel.Name;
            Position = socketChannel.Position;
            CreatedAt = socketChannel.CreatedAt;
        }


        public int IdDiscordChannel { get; set; }
        public long IdExternal { get; set; }

        public string Name { get; set; }
        public int Position { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public virtual DiscordGuild DiscordGuild { get; set; }
        public virtual ICollection<DiscordUser> DiscordUsers { get; set; }
    }
}
using System;
using MetalBotDAL.Entities.Discord;
using Microsoft.EntityFrameworkCore;

namespace MetalBotDAL
{
    public class MetalBotContext : DbContext
    {
        public DbSet<DiscordGuild> DiscordGuilds { get; set; }
        public DbSet<DiscordChannel> DiscordChannels { get; set; }
        public DbSet<DiscordUser> DiscordUsers { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = Environment.GetEnvironmentVariable("connectionString");
            optionsBuilder.UseMySQL(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DiscordGuild>(entity =>
            {
                entity.HasKey(e => e.IdDiscordGuild);
                entity.Property(e => e.IdDiscordGuild).ValueGeneratedOnAdd();
                entity.Property(e => e.IdExternal).IsRequired();
                
                entity.HasOne(e => e.Owner);
                entity.HasOne(e => e.DefaultChannel);
                entity.HasOne(e => e.EmbedChannel);
                entity.HasOne(e => e.SystemChannel);
                entity.HasOne(e => e.AdminChannel);
                entity.HasOne(e => e.RolesChannel);
                entity.HasOne(e => e.RulesChannel);
                entity.HasMany(e => e.DiscordChannels);
                entity.HasMany(e => e.DiscordUsers);
            });

            modelBuilder.Entity<DiscordChannel>(entity =>
            {
                entity.HasKey(e => e.IdDiscordChannel);
                entity.Property(e => e.IdDiscordChannel).ValueGeneratedOnAdd();
                
                entity.HasOne(e => e.DiscordGuild);
            });

            modelBuilder.Entity<DiscordUser>(entity =>
            {
                entity.HasKey(e => e.IdDiscordUser);
                entity.Property(e => e.IdDiscordUser).ValueGeneratedOnAdd();
                entity.Property(e => e.IdExternal).IsRequired();
                entity.Property(e => e.Username).IsRequired();
                entity.Property(e => e.Mention).IsRequired();
                
                entity.HasMany(e => e.DiscordGuilds);
            });
        }
    }
}
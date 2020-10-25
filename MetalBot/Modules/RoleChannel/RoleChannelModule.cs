using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MetalBot.Helpers;
using MetalBot.Services;

namespace MetalBot.Modules.RoleChannel
{
    public class RoleChannelModule : ModuleBase
    {
        [Command("tarkoof")]
        [Alias("eft", "escapefromtarkov", "tarkov")]
        [Summary("Toggles the tarkoof role on you, which allows you to view the tarkoof channel that has a webhook that updates with every post from r/EscapefromTarkov")]
        [RequireContext(ContextType.Guild)]
        public async Task TarkoofAsync()
        {
            if ((long) Context.Channel.Id != ((await ChannelHelper.GetRolesChannel((SocketGuild) Context.Guild))?.Id ?? -1m))
            {
                return;
            }

            await ToggleRole(CommandHandler._tarkoofRole);

            // TODO : Create webhook with polling to check if subreddit has been updated
            // TODO : Add polling service into MetalBot or run separate Idk
        }

        private async Task ToggleRole(IRole role)
        {
            var contextUser = (SocketGuildUser) Context.User;
            var hasRole = contextUser.Roles.Any(r => r.Id == role.Id);
            if (hasRole)
            {
                await contextUser.RemoveRoleAsync(role);
            }
            else
            {
                await contextUser.AddRoleAsync(role);
            }

            await Context.Message.DeleteAsync();
        }
    }

    // Create a module with the 'sample' prefix
    [Group("sample")]
    public class SampleModule : ModuleBase
    {
        // ~sample square 20 -> 400
        [Command("square")]
        [Summary("Squares a number.")]
        public async Task SquareAsync(
            [Summary("The number to square.")]
            int num)
        {
            // We can also access the channel from the Command Context.
            await Context.Channel.SendMessageAsync($"{num}^2 = {Math.Pow(num, 2)}");
        }

        // ~sample userinfo --> foxbot#0282
        // ~sample userinfo @Khionu --> Khionu#8708
        // ~sample userinfo Khionu#8708 --> Khionu#8708
        // ~sample userinfo Khionu --> Khionu#8708
        // ~sample userinfo 96642168176807936 --> Khionu#8708
        // ~sample whois 96642168176807936 --> Khionu#8708
        [Command("userinfo")]
        [Summary
            ("Returns info about the current user, or the user parameter, if one passed.")]
        [Alias("user", "whois")]
        public async Task UserInfoAsync(
            [Summary("The (optional) user to get info from")]
            SocketUser user = null)
        {
            var userInfo = user ?? (SocketUser) Context.Client.CurrentUser;
            await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");
        }
    }
}
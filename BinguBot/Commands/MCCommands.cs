using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinguBot.Commands
{
    public class MCCommands : BaseCommandModule
    {
        [Command("minecraft")]
        [Description("Provides a dialog box")]
        public async Task Join(CommandContext ctx)
        {
            var joinEmbed = new DiscordEmbedBuilder
            {
                Title = "Would you like to get notifications when the French Fry Club Minecraft Server goes online?",
                Color = DiscordColor.Yellow
            };

            var joinMessage = await ctx.Channel.SendMessageAsync(embed: joinEmbed).ConfigureAwait(false);

            var thumbsUpEmoji = DiscordEmoji.FromName(Bot.Client, ":+1:");
            var thumbsDownEmoji = DiscordEmoji.FromName(Bot.Client, ":-1:");

            await joinMessage.CreateReactionAsync(thumbsUpEmoji).ConfigureAwait(false);
            await joinMessage.CreateReactionAsync(thumbsDownEmoji).ConfigureAwait(false);

            var interactivity = Bot.Client.GetInteractivity();

            var reactionResult = await interactivity.WaitForReactionAsync(
                x => x.Message == joinMessage &&
                x.User == ctx.User &&
                (x.Emoji == thumbsUpEmoji || x.Emoji == thumbsDownEmoji)).ConfigureAwait(false);

            if (reactionResult.Result.Emoji == thumbsUpEmoji)
            {
                var role = ctx.Guild.GetRole(785598430306893875);
                await ctx.Member.GrantRoleAsync(role).ConfigureAwait(false);
            }
            else if (reactionResult.Result.Emoji == thumbsDownEmoji)
            {
                var role = ctx.Guild.GetRole(785598430306893875);
                await ctx.Member.RevokeRoleAsync(role).ConfigureAwait(false);
            }

            await joinMessage.DeleteAsync().ConfigureAwait(false);
        }
    }
}

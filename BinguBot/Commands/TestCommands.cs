using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinguBot.Commands
{
    [ModuleLifespan(ModuleLifespan.Transient)]
    class TestCommands : BaseCommandModule
    {

        [Command("ping")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("pong!");
        }

        [Command("purge")]
        [Description("Deletes the specified amount of messages.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        public async Task PurgeChat(CommandContext ctx, uint amount)
        {
            if (amount > 20) { return; }

            var messages = await ctx.Channel.GetMessagesAsync((int)amount + 1);

            await ctx.Channel.DeleteMessagesAsync(messages);
            const int delay = 2000;
            var m = await ctx.RespondAsync($"Purge completed. _This message will be deleted in {delay / 1000} seconds._");
            await Task.Delay(delay);
            await m.DeleteAsync();
        }

        //[Command("w")]
        public async Task Funny(CommandContext ctx)
        {
            DiscordEmbed embed = new DiscordEmbedBuilder()
            {
                Title = "Zero Two",
                Description = "Darling in the FranXX\nClaims: #1\nLikes: #1\n**1248** :kakera:",
                ImageUrl = "https://images-ext-2.discordapp.net/external/4M9xWHunJO_30xM66hND4K7WN3c_eRskq47qJWVP1Ks/https/imgur.com/fqbFdYD.png",
                Color = DiscordColor.Goldenrod
            };

            await ctx.Channel.SendMessageAsync("Wished By <@320685237979840515>, <@396090081313554432>", embed);
        }
    }
}

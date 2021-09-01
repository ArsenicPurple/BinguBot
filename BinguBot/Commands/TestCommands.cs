using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
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
        [RequireBotPermissions(DSharpPlus.Permissions.Administrator)]
        public async Task PurgeChat(CommandContext ctx, uint amount)
        {
            var messages = await ctx.Channel.GetMessagesAsync((int)amount + 1);

            await ctx.Channel.DeleteMessagesAsync(messages);
            const int delay = 2000;
            var m = await ctx.RespondAsync($"Purge completed. _This message will be deleted in {delay / 1000} seconds._");
            await Task.Delay(delay);
            await m.DeleteAsync();
        }

        [Command("Vote")]
        public async Task Vote(CommandContext ctx, int options)
        {
            if (options > 10)
            {
                var errorMessage = await ctx.Channel.SendMessageAsync("Parameter is to large. \"options\" int parameter must be 10 or less");

                await Task.Delay(5000);

                await ctx.Channel.DeleteMessageAsync(errorMessage);
                return;
            }

            var voteEmbed = new DiscordEmbedBuilder
            {
                Title = "Vote"
            };

            var voteMessage = await ctx.Channel.SendMessageAsync(embed: voteEmbed).ConfigureAwait(false);

            DiscordEmoji[] reactions = new DiscordEmoji[options];

            for (int i = 0; i < reactions.Length; i++)
            {
                string emoji = Converter.IntEmoji(i);
                reactions[i] = DiscordEmoji.FromName(Bot.Client, emoji);

                await voteMessage.CreateReactionAsync(reactions[i]).ConfigureAwait(false);
            }

            var interactivity = Bot.Client.GetInteractivity();

            var delay = TimeSpan.FromSeconds(5);
            var behaviour = PollBehaviour.DeleteEmojis;

            var results = await interactivity.DoPollAsync(voteMessage, reactions, behaviour, delay);

            var resultsEmbed = new DiscordEmbedBuilder
            {
                Title = "Results:"
            };

            foreach (var result in results)
            {
                string resultLine = result.Emoji + " : " + result.Total + "\n";
                resultsEmbed.Description += resultLine;
            }

            await ctx.Channel.DeleteMessageAsync(voteMessage);
            await ctx.Channel.SendMessageAsync(embed: resultsEmbed);
        }
    }
}

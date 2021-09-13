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
    }
}

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using DSharpPlus.EventArgs;

namespace BinguBot.Commands
{
    /** TODO
     * Refactor for consistency and efficiency
     * 
     * 
     */
    [ModuleLifespan(ModuleLifespan.Transient)]
    class MusicCommands : BaseCommandModule
    {
        List<LavalinkTrack> queue = new List<LavalinkTrack>();

        LavalinkGuildConnection connectedChannel;

        [Command("join")]
        public async Task Join(CommandContext ctx) {
            var channel = GetUserChannel(ctx).Result;
            if (channel == null) { return; }

            await ConnectToChannel(ctx, channel); 
        }
        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            DiscordChannel channel;
            if ((channel = ctx.Member.VoiceState.Channel) == null)
            {
                await ctx.RespondAsync("You are not currently in a Voice Channel");
                return;
            }

            var lava = Bot.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return;
            }

            var node = lava.ConnectedNodes.Values.First();

            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }

            var conn = node.GetGuildConnection(channel.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            if (conn.CurrentState.CurrentTrack != null)
            {
                await conn.StopAsync();
            }
            
            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Left {channel.Name}!");
        }

        [Command("queue")]
        public async Task Queue(CommandContext ctx, [RemainingText] string search)
        {
            LavalinkNodeConnection node;
            if ((node = GetConnection(ctx).Item1) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel or Bingu had an oopsie");
                return;
            }

            var loadResult = await node.Rest.GetTracksAsync(search);
            
            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed for {search}.");
                return;
            }

            LavalinkTrack track = loadResult.Tracks.First();
            queue.Add(track);

            await ctx.RespondAsync($"Queued {track.Title}!");
        }
        [Command("queue")]
        [Aliases("q")]
        public async Task Queue(CommandContext ctx)
        {
            string content = string.Empty;
            content += "Playing: " + connectedChannel.CurrentState.CurrentTrack.Title + "\n";
            for (int i = 0; i < queue.Count - 1; i++)
            {
                content += queue[i].Title + "\n";
            }
            await ctx.Channel.SendMessageAsync(content);
        }

        [Command("skip")]
        public async Task Skip(CommandContext ctx)
        {
            LavalinkGuildConnection conn;
            if ((conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel or Bingu had an oopsie");
                return;
            }

            if (queue.Count > 0)
            {
                await conn.PlayAsync(queue.First());
                queue.RemoveAt(0);
            }
        }

        [Command("clear")]
        public async Task Clear(CommandContext ctx)
        {
            LavalinkGuildConnection conn;
            if ((conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel or Bingu had an oopsie");
                return;
            }

            await conn.StopAsync();
            queue.Clear();
        }

        [Command("play")]
        [Aliases("p")]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            var channel = GetUserChannel(ctx).Result;
            if (channel == null) { return; }

            await ConnectToChannel(ctx, channel);

            LavalinkNodeConnection node;
            LavalinkGuildConnection conn;
            if ((node = GetConnection(ctx).Item1) == null || (conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel or Bingu had an oopsie");
                return;
            }

            var loadResult = await node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed for {search}.");
                return;
            }

            var track = loadResult.Tracks.First();

            if (conn.CurrentState.CurrentTrack != null)
            {
                queue.Add(track);
                await ctx.RespondAsync($"Queued {track.Title}!");
                return;
            }

            await conn.PlayAsync(track);

            await ctx.RespondAsync($"Now playing {track.Title}!");
        }

        [Command("play")]
        public async Task Play(CommandContext ctx, Uri url)
        {
            var channel = GetUserChannel(ctx).Result;
            if (channel == null) { return; }

            await ConnectToChannel(ctx, channel);

            LavalinkNodeConnection node;
            LavalinkGuildConnection conn;
            if ((node = GetConnection(ctx).Item1) == null || (conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel or Bingu had an oopsie");
                return;
            }

            var loadResult = await node.Rest.GetTracksAsync(url);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed.");
                return;
            }

            var track = loadResult.Tracks.First();

            if (conn.CurrentState.CurrentTrack != null)
            {
                queue.Add(track);
                await ctx.RespondAsync($"Queued {track.Title}!");
                return;
            }

            await conn.PlayAsync(track);

            await ctx.RespondAsync($"Now playing {track.Title}!");
        }

        [Command("pause")]
        public async Task Pause(CommandContext ctx)
        {
            LavalinkGuildConnection conn;
            if ((conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel or Bingu had an oopsie");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.PauseAsync();
        }

        //Mark:-- Utility Methods
        public (LavalinkNodeConnection, LavalinkGuildConnection) GetConnection(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null) { return (null, null); }
            var lava = Bot.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            return (node, conn);
        }

        public async Task<DiscordChannel> GetUserChannel(CommandContext ctx)
        {
            DiscordChannel channel;
            if ((channel = ctx.Member.VoiceState.Channel) == null)
            {
                Debug.WriteLine("Is Null");
                await ctx.RespondAsync("You are not currently in a Voice Channel");
                return null;
            }

            return channel;
        }

        public async Task ConnectToChannel(CommandContext ctx, DiscordChannel channel)
        {
            var lava = Bot.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return;
            }

            var node = lava.ConnectedNodes.Values.First();
            connectedChannel = await node.ConnectAsync(channel);
            connectedChannel.PlaybackFinished += PlaybackFinished;
        }

        private async Task PlaybackFinished(LavalinkGuildConnection sender, DSharpPlus.Lavalink.EventArgs.TrackFinishEventArgs e)
        {
            if (queue.Count == 0) { return; }
            await sender.PlayAsync(queue.First());
            queue.RemoveAt(0);
        }

        public static Task VoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
        {
            if (e.Before.Channel.Id != e.After.Channel.Id)
            {

            }
        }
    }
}

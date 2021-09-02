using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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
    class MusicCommands : BaseCommandModule
    {
        public MusicCommands()
        {
            /// Adds the "VoiceStateUpdated" Method to same listener.
            Bot.Client.VoiceStateUpdated += VoiceStateUpdated;
        }

        /// <summary>
        /// Queue of all tracks. Excludes the currently playing track.
        /// </summary>
        List<LavalinkTrack> queue = new List<LavalinkTrack>();

        /// <summary>
        /// Current Guild channel connection
        /// </summary>
        LavalinkGuildConnection connectedChannel;

        /// <summary>
        /// Joke Command that makes the bot join a voice channel and then deafen everyone
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("boom")]
        [RequirePermissions(Permissions.Administrator)]
        [Hidden()]
        public async Task Boom(CommandContext ctx)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            var channel = GetUserChannel(ctx).Result;
            if (channel == null) { return; }

            await ConnectToChannel(ctx, channel);

            LavalinkNodeConnection node;
            LavalinkGuildConnection conn;
            if ((node = GetConnection(ctx).Item1) == null || (conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel");
                return;
            }

            var loadResult = await node.Rest.GetTracksAsync(new Uri("https://www.youtube.com/watch?v=sFeC2yeOibg"));

            var track = loadResult.Tracks.First();

            await conn.PlayAsync(track);

            await connectedChannel.SeekAsync(TimeSpan.FromSeconds(10d));

            await Task.Delay(6000);

            foreach (DiscordMember member in channel.Users)
            {
                await Task.Delay(100);
                await member.SetDeafAsync(true, "Boom was very loud");
            }

            await Task.Delay(3000);

            foreach (DiscordMember member in channel.Users)
            {
                await Task.Delay(100);
                await member.SetDeafAsync(false);
            }
        }

        /// <summary>
        /// Forces the bot to join the channel in which the user that invoked the command is in.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("join")]
        public async Task Join(CommandContext ctx) {
            var channel = GetUserChannel(ctx).Result;
            if (channel == null) { return; }

            await ConnectToChannel(ctx, channel);
        }

        /// <summary>
        /// Forces the bot to leave the channel in which the use that invoked the command is in.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            LavalinkGuildConnection conn;
            if ((conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel");
                return;
            }

            if (conn.Channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }

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
            await ctx.RespondAsync($"Left {conn.Channel.Name}!");
        }

        /// <summary>
        /// Lists the currently playing track and all tracks in the queue.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("queue"), Aliases("q")]
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

        /// <summary>
        /// Skips the currently playing track and plays the next track in the queue. Stops player is the is nothing in the queue.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
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
            } else
            {
                await conn.StopAsync();
            }
        }

        /// <summary>
        /// Stops the currently playing track and clear the queue.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Searches for the value given and plays it. Queues the track if a track is already playing.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        [Command("play")]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            var channel = GetUserChannel(ctx).Result;
            if (channel == null) { return; }

            await ConnectToChannel(ctx, channel);

            LavalinkNodeConnection node;
            LavalinkGuildConnection conn;
            if ((node = GetConnection(ctx).Item1) == null || (conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel");
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
        [Command("p"), Hidden()]
        public async Task P(CommandContext ctx, [RemainingText] string search) { await Play(ctx, search); }

        /// <summary>
        /// Searches for the url given and plays it. Queues the track if a track is already playing.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="url"></param>
        /// <returns></returns>
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
                await ctx.RespondAsync("You are not in a voice channel");
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
        [Command("p"), Hidden()]
        public async Task P(CommandContext ctx, Uri url) { await Play(ctx, url); }

        /// <summary>
        /// Pauses the currently playing track.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("pause")]
        public async Task Pause(CommandContext ctx)
        {
            LavalinkGuildConnection conn;
            if ((conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.PauseAsync();
        }

        /// <summary>
        /// Resumes the currently paused track
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("resume")]
        public async Task Resume(CommandContext ctx)
        {
            LavalinkGuildConnection conn;
            if ((conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.ResumeAsync();
        }

        [Command("seek")]
        public async Task Seek(CommandContext ctx, int seconds)
        {
            LavalinkGuildConnection conn;
            if ((conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.SeekAsync(TimeSpan.FromSeconds(seconds));
        }

        //Mark:-- Utility Methods
        /// <summary>
        /// Gets the connection of the member the invoked the command in context.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
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
                if (ctx != null) {
                    await ctx.RespondAsync("The Lavalink connection is not established");
                }
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

        private async Task VoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
        {
            var newChannel = e.After.Channel;
            if (e.Before.Channel == null) { return; }
            if (e.Before.Channel.Id != newChannel.Id)
            {
                await ConnectToChannel(null, newChannel);
            }
        }
    }
}

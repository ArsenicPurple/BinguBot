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
using System.Collections;

namespace BinguBot.Commands
{
    /** TODO
     * 
     * 
     * 
     */
    class MusicCommands : BaseCommandModule
    {
        public MusicCommands()
        {
        }

        /// <summary>
        /// Queue of all tracks. Excludes the currently playing track.
        /// </summary>
        Queue<LavalinkTrack> queue = new Queue<LavalinkTrack>();

        bool IsLooping;

        [Command("rtt")]
        [Hidden()]
        public async Task RapTapTap(CommandContext ctx)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            var channel = GetUserChannel(ctx).Result;
            if (channel == null) { return; }

            await ConnectToChannel(ctx, channel);

            LavalinkNodeConnection node;
            LavalinkGuildConnection conn;
            if ((node = GetConnection(ctx).Item1) == null || (conn = GetConnection(ctx).Item2) == null)
            {
                return;
            }

            var loadResult = await node.Rest.GetTracksAsync(new Uri("https://youtu.be/3F88-fIMk54"));
            var track = loadResult.Tracks.First();
            await conn.PlayAsync(track);
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

            if (IsPlaying(conn))
            {
                await conn.StopAsync();
                await Task.Delay(100);
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
            LavalinkGuildConnection conn;
            if ((conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel or Bingu had an oopsie");
                return;
            }

            if (QueueIsEmpty())
            {
                await ctx.RespondAsync("There is nothing in the queue");
                return;
            }

            var qArray = queue.ToArray();

            string content = string.Empty;
            content += "```";
            content += $"Playing: {conn.CurrentState.CurrentTrack.Title}\n\n";
            content += "Next Up:\n";
            for (int i = 1; i < qArray.Length + 1; i++)
            {
                content += $"{i}: {qArray[i - 1].Title}\n";
            }
            content += "```";
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

            await conn.StopAsync();
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

            await conn.PauseAsync();
            queue.Clear();
            await conn.StopAsync();
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

            if (IsPlaying(conn))
            {
                queue.Enqueue(track);
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

            if (IsPlaying(conn))
            {
                queue.Enqueue(track);
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

        /// <summary>
        /// Skips to the timestamp in seconds given in the currently playing song.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Removes the track at the index given minus 1.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [Command("remove")]
        public async Task Remove(CommandContext ctx, int index)
        {
            var qList = queue.ToList();
            var title = qList[index - 1].Title;
            try
            {
                qList.RemoveAt(index - 1);
            }
            catch(ArgumentOutOfRangeException)
            {
                await ctx.RespondAsync($"There is no track at postion {index}");
            }
            
            Queue<LavalinkTrack> tmp = new Queue<LavalinkTrack>();
            foreach (LavalinkTrack track in qList)
            {
                tmp.Enqueue(track);
            }
            await ctx.RespondAsync($"Removed {title}");
            queue = tmp;
        }

        /// <summary>
        /// Toggles looping on the currently playing song.
        /// </summary>
        [Command("loop")]
        public async Task Loop(CommandContext ctx)
        {
            IsLooping = !IsLooping;
            if (IsLooping)
            {
                await ctx.RespondAsync("Bingu will now loop the currently playing track");
            }
            else
            {
                await ctx.RespondAsync("Bingu will no longer loop the currently playing track");
            }
        }

        [Command("funkify")]
        public async Task Funk(CommandContext ctx)
        {
            throw new NotImplementedException();
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

        /// <summary>
        /// Gets the user channel. Returns null if the user is not in a channel.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Connects to the given channel and add the playback finished event handler to the connection.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public async Task<LavalinkGuildConnection> ConnectToChannel(CommandContext ctx, DiscordChannel channel)
        {
            
            var lava = Bot.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                if (ctx != null) {
                    await ctx.RespondAsync("The Lavalink connection is not established");
                }
                return null;
            }

            var node = lava.ConnectedNodes.Values.First();
            var conn = await node.ConnectAsync(channel);
            conn.PlaybackFinished += PlaybackFinished;
            return conn;
        }

        /// <summary>
        /// Runs on playback finished. Checks if the track should loop and then plays the next song accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task PlaybackFinished(LavalinkGuildConnection sender, DSharpPlus.Lavalink.EventArgs.TrackFinishEventArgs e)
        {
            if (IsLooping) { await sender.PlayAsync(queue.Peek()); }
            if (QueueIsEmpty()) { return; }

            Debug.WriteLine("Playback Finished");
            await sender.PlayAsync(queue.Dequeue());
            e.Handled = true;
        }

        /// <summary>
        /// Checks whether there is a song currently playing.
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        private bool IsPlaying(LavalinkGuildConnection conn)
        {
            return conn.CurrentState.CurrentTrack != null;
        }

        /// <summary>
        /// Checks if the song queue is empty.
        /// </summary>
        /// <returns></returns>
        private bool QueueIsEmpty()
        {
            return queue.Count == 0;
        }
    }
}

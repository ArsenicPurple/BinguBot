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
     * Refactor to support multiple servers
     * 
     * 
     */
    class MusicCommands : BaseCommandModule
    {
        Dictionary<ulong, Queue<LavalinkTrack>> QueueDict = new Dictionary<ulong, Queue<LavalinkTrack>>();

        Dictionary<ulong, bool> LoopingDict = new Dictionary<ulong, bool>();

        Dictionary<ulong, DateTime> IdleDict = new Dictionary<ulong, DateTime>();

        public MusicCommands()
        {
            Bot.Client.GuildDownloadCompleted += GuildDownloadCompleted;
            Bot.Client.Heartbeated += Heartbeated;
            Debug.WriteLine("Music Commands Initialized");
        }

        [Command("rtt")]
        [Hidden()]
        public async Task RapTapTap(CommandContext ctx)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            LavalinkNodeConnection node;
            LavalinkGuildConnection conn;
            if ((node = GetConnection(ctx).Item1) == null || (conn = GetConnection(ctx).Item2) == null)
            {
                return;
            }

            var loadResult = await node.Rest.GetTracksAsync(new Uri("https://youtu.be/3F88-fIMk54"));
            var track = loadResult.Tracks.First();

            var timestamp = conn.CurrentState.CurrentTrack.Position;
            await InterruptPlayback(ctx, conn);
            await conn.PlayAsync(track);
            await conn.SeekAsync(timestamp);
        }

        /// <summary>
        /// Forces the bot to join the channel in which the user that invoked the command is in.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("join")]
        public async Task Join(CommandContext ctx) 
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            var key = ctx.Guild.Id;
            var channel = GetUserChannel(ctx).Result;
            if (channel == null) { return; }

            await ConnectToChannel(ctx, channel);
            IdleDict[key] = DateTime.Now;
        }

        /// <summary>
        /// Forces the bot to leave the channel in which the use that invoked the command is in.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            var key = ctx.Guild.Id;
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
                QueueDict[key].Clear();
                await conn.StopAsync();
                await Task.Delay(100);
            }

            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Left {conn.Channel.Name}!");
        }

        /// <summary>
        /// Lists the currently playing track and all tracks in the QueueDict[key].
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("queue"), Aliases("q")]
        public async Task Queue(CommandContext ctx)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            var key = ctx.Guild.Id;
            LavalinkGuildConnection conn;
            if ((conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel or Bingu had an oopsie");
                return;
            }

            if (QueueIsEmpty(key))
            {
                await ctx.RespondAsync("There is nothing in the queue");
                return;
            }

            var qArray = QueueDict[key].ToArray();

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
        /// Skips the currently playing track and plays the next track in the QueueDict[key]. Stops player is the is nothing in the QueueDict[key].
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("skip")]
        public async Task Skip(CommandContext ctx)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            LavalinkGuildConnection conn;
            if ((conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel or Bingu had an oopsie");
                return;
            }

            await ctx.RespondAsync($"Skipping {conn.CurrentState.CurrentTrack.Title}");
            await conn.StopAsync();
        }

        /// <summary>
        /// Stops the currently playing track and clear the QueueDict[key].
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("clear")]
        public async Task Clear(CommandContext ctx)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            var key = ctx.Guild.Id;
            LavalinkGuildConnection conn;
            if ((conn = GetConnection(ctx).Item2) == null)
            {
                await ctx.RespondAsync("You are not in a voice channel or Bingu had an oopsie");
                return;
            }

            await conn.PauseAsync();
            QueueDict[key].Clear();
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
            if (Uri.TryCreate(search, UriKind.Absolute, out var result))
            {
                await Play(ctx, result);
                return;
            }

            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            var key = ctx.Guild.Id;
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
                QueueDict[key].Enqueue(track);
                await ctx.RespondAsync($"Queued `{track.Title}`!");
                return;
            }

            await conn.PlayAsync(track);
            await ctx.RespondAsync($"Now playing `{track.Title}`!");
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
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            var key = ctx.Guild.Id;
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
                QueueDict[key].Enqueue(track);
                await ctx.RespondAsync($"Queued: `{track.Title}`!");
                return;
            }

            await conn.PlayAsync(track);
            await ctx.RespondAsync($"Now playing: `{track.Title}`!");
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
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

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
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

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
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

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

            var minutes = seconds % 60;
            seconds -= minutes * 60;
            await ctx.RespondAsync($"Moved to {minutes}:{seconds}");
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
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            var key = ctx.Guild.Id;
            var qList = QueueDict[key].ToList();
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
            QueueDict[key] = tmp;
        }

        /// <summary>
        /// Toggles looping on the currently playing song.
        /// </summary>
        [Command("loop")]
        public async Task Loop(CommandContext ctx)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            var key = ctx.Guild.Id;
            LoopingDict[key] = !LoopingDict[key];
            if (LoopingDict[key])
            {
                await ctx.RespondAsync("Bingu will now loop the currently playing track");
            }
            else
            {
                await ctx.RespondAsync("Bingu will no longer loop the currently playing track");
            }
        }

        [Command("suggest")]
        public async Task Suggest(CommandContext ctx)
        {
            await ctx.RespondAsync("Apologies, this command is not yet implemented");
        }

        /*
        [Command("funkify")]
        [Hidden()]
        public async Task Funk(CommandContext ctx, int id)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

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

            if (id < 0 || id > 14)
            {
                return;
            }

            var band = new LavalinkBandAdjustment(id, 0.25f);
            await conn.AdjustEqualizerAsync(band);
        }
        */

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

        public async Task InterruptPlayback(CommandContext ctx, LavalinkGuildConnection conn)
        {
            var key = ctx.Guild.Id;
            var tmp = QueueDict[key];

            QueueDict[key].Clear();
            await conn.StopAsync();
            QueueDict[key] = tmp;
        }

        /// <summary>
        /// Runs on playback finished. Checks if the track should loop and then plays the next song accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task PlaybackFinished(LavalinkGuildConnection sender, DSharpPlus.Lavalink.EventArgs.TrackFinishEventArgs e)
        {
            var key = sender.Guild.Id;
            if (LoopingDict[key]) { await sender.PlayAsync(e.Track); }
            if (QueueIsEmpty(key))
            {
                IdleDict[key] = DateTime.Now;
                return;
            }
            await sender.PlayAsync(QueueDict[key].Dequeue());
            e.Handled = true;
        }

        /// <summary>
        /// Runs on Guild List Download. Fills the Queue dictionary with guild ids as keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Task GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
        {
            foreach(var (key, _) in e.Guilds)
            {
                QueueDict.Add(key, new Queue<LavalinkTrack>());
                LoopingDict.Add(key, false);
            }
            e.Handled = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Runs occasionally. Checks if the bot has been idle in a channel for too long and disconnents it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Task Heartbeated(DiscordClient sender, HeartbeatEventArgs e)
        {
            foreach (var (key, guild) in sender.Guilds)
            {
                var lava = Bot.Client.GetLavalink();
                var node = lava.ConnectedNodes.Values.First();
                var conn = node.GetGuildConnection(guild);
                if (conn == null) { continue; }
                if (IsPlaying(conn)) { continue; }
                if (IdleDict[key].AddMinutes(3) > DateTime.Now) { continue; }
                conn.DisconnectAsync();
            }
            e.Handled = true;
            return Task.CompletedTask;
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
        /// Checks if the song QueueDict[key] is empty.
        /// </summary>
        /// <returns></returns>
        private bool QueueIsEmpty(ulong key)
        {
            return QueueDict[key].Count == 0;
        }
    }
}

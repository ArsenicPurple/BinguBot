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

using BinguBot.DataTypes;
using System.Net;
using System.IO;
using System.Text.Json;

namespace BinguBot.Commands
{
    /** TODO
     * Refactor to support multiple servers
     * 
     * 
     */
    class MusicCommands : BaseCommandModule
    {
        public static Dictionary<ulong, GuildData> Data = new Dictionary<ulong, GuildData>();
        
        public MusicCommands()
        {
            Bot.Client.GuildDownloadCompleted += GuildDownloadCompleted;
            Bot.Client.Heartbeated += Heartbeated;
            Bot.LogInfo("Music Commands Initialized");
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
            Data[key].TimeIdle = DateTime.Now;
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
                Data[key].GuildQueue.Clear();
                await conn.StopAsync();
                await Task.Delay(100);
            }

            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Left {conn.Channel.Name}!");
        }

        /// <summary>
        /// Lists the currently playing track and all tracks in the Queue.
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

            var qArray = Data[key].GuildQueue.ToArray();

            string content = string.Empty;
            content += "```";
            content += $"Playing: {conn.CurrentState.CurrentTrack.Title}\n\n";
            content += "Next Up:\n";
            for (int i = 1; i < qArray.Length + 1; i++)
            {
                content += $"{i}: {qArray[i - 1].Track.Title} : \n — Queued By {qArray[i - 1].queuedBy.DisplayName}\n";
            }
            content += "```";
            await ctx.Channel.SendMessageAsync(content);
        }

        /// <summary>
        /// Skips the currently playing track and plays the next track in the Queue. Stops player is the is nothing in the Queue.
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
        /// Stops the currently playing track and clear the Queue.
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
            Data[key].GuildQueue.Clear();
            await conn.StopAsync();

            await ctx.RespondAsync("Cleared the queue");
        }

        /// <summary>
        /// Searches for the value given and plays it. Queues the track to play after all other queued tracks if a track is already playing.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        [Command("play")]
        [Aliases("p")]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
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

            var loadResult = GetTrack(node, search).Result;

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed for {search}.");
                return;
            }

            var track = loadResult.Tracks.First();

            if (IsPlaying(conn))
            {
                Data[key].GuildQueue.Enqueue(new QueuedTrack(track, ctx));
                await ctx.RespondAsync($"Queued `{track.Title}`!");
                return;
            }

            await conn.PlayAsync(track);
            await ctx.RespondAsync($"Now playing `{track.Title}`!");
        }

        /// <summary>
        /// Searches for the value given and play it. Queues the track to play next if a track is already playing
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        [Command("playtop")]
        [Aliases("pt")]
        public async Task PlayTop(CommandContext ctx, [RemainingText] string search)
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

            var loadResult = GetTrack(node, search).Result;

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed for {search}.");
                return;
            }

            var track = loadResult.Tracks.First();

            if (IsPlaying(conn))
            {
                var qList = Data[key].GuildQueue.ToList();
                Queue<QueuedTrack> tmp = new Queue<QueuedTrack>();
                tmp.Enqueue(new QueuedTrack(track, ctx));
                foreach (QueuedTrack qtrack in qList)
                {
                    tmp.Enqueue(qtrack);
                }
                Data[key].GuildQueue = tmp;
                await ctx.RespondAsync($"Queued `{track.Title}` at the top!");
                return;
            }

            await conn.PlayAsync(track);
            await ctx.RespondAsync($"Now playing `{track.Title}`!");
        }

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
            if (seconds == 0)
            {
                await ctx.RespondAsync("Please use the `?restart` command to restart the track");
                return;
            }

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

            var timestamp = TimeSpan.FromSeconds(seconds);
            await ctx.RespondAsync($"Moved to {timestamp}");
            await conn.SeekAsync(TimeSpan.FromSeconds(seconds));
        }

        /// <summary>
        /// Restarts the currently playing track to 0 seconds
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("restart")]
        public async Task Restart(CommandContext ctx)
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

            await ctx.RespondAsync($"Restarting the track");
            await conn.SeekAsync(TimeSpan.FromSeconds(0));
        }

        /// <summary>
        /// Fast Forwards the currently playing track by the given time in seconds. 10 seconds if no value is given
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        [Command("fastforward")]
        [Aliases("ff", ">")]
        public async Task FastForward(CommandContext ctx, [RemainingText] int seconds)
        {
            seconds = seconds == 0 ? 10 : seconds;

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

            var timestamp = TimeSpan.FromSeconds(seconds);
            await ctx.RespondAsync($"Fast Forwarded by {timestamp}");
            await conn.SeekAsync(conn.CurrentState.PlaybackPosition.Add(timestamp));
        }

        /// <summary>
        /// Rewinds the currently playing track by the given time in seconds. 10 seconds if no value is given.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        [Command("rewind")]
        [Aliases("<")]
        public async Task Rewind(CommandContext ctx, [RemainingText] int seconds)
        {
            seconds = seconds == 0 ? 10 : seconds;

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

            var timestamp = TimeSpan.FromSeconds(seconds);
            await ctx.RespondAsync($"Fast Forwarded by {timestamp}");
            await conn.SeekAsync(conn.CurrentState.PlaybackPosition.Subtract(timestamp));
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
            var qList = Data[key].GuildQueue.ToList();
            var title = qList[index - 1].Track.Title;
            try
            {
                qList.RemoveAt(index - 1);
            }
            catch(ArgumentOutOfRangeException)
            {
                await ctx.RespondAsync($"There is no track at postion {index}");
            }
            
            Queue<QueuedTrack> tmp = new Queue<QueuedTrack>();
            foreach (QueuedTrack track in qList)
            {
                tmp.Enqueue(track);
            }
            await ctx.RespondAsync($"Removed {title}");
            Data[key].GuildQueue = tmp;
        }

        /// <summary>
        /// Toggles looping on the currently playing song.
        /// </summary>
        [Command("loop")]
        public async Task Loop(CommandContext ctx)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            var key = ctx.Guild.Id;
            Data[key].IsLooping = !Data[key].IsLooping;
            if (Data[key].IsLooping)
            {
                await ctx.RespondAsync("Bingu will now loop the currently playing track");
            }
            else
            {
                await ctx.RespondAsync("Bingu will no longer loop the currently playing track");
            }
        }

        /// <summary>
        /// Shuffles the all the songs currently in the queue.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("shuffle")]
        public async Task Shuffle(CommandContext ctx)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message);

            var key = ctx.Guild.Id;
            var qList = Data[key].GuildQueue.ToList();

            Queue<QueuedTrack> tmp = new Queue<QueuedTrack>();
            int total = qList.Count;
            Random r = new Random();
            for (int i = 0; i < total; i++)
            {
                int index = r.Next(qList.Count);
                var track = qList[index];
                qList.RemoveAt(index);
                tmp.Enqueue(track);
            }
            Data[key].GuildQueue = tmp;

            await ctx.RespondAsync("Shuffled the queue!");
        }

        [Command("info")]
        [Aliases("i", "song")]
        public async Task Song(CommandContext ctx)
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

            var CurrentTrack = conn.CurrentState.CurrentTrack;
            string turi = GetThumbnailUri(CurrentTrack.Uri);

            using (WebClient wc = new WebClient())
            {
                wc.DownloadFileAsync(new Uri(turi), "thumbnail.jpg");
            }

            DiscordEmbed embed = new DiscordEmbedBuilder()
            {
                ImageUrl = "thumbnail.jpg",
            };
            await ctx.Channel.SendMessageAsync(embed);
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

        public async Task<LavalinkLoadResult> GetTrack(LavalinkNodeConnection node, string search)
        {
            if (Uri.TryCreate(search, UriKind.Absolute, out var result))
            {
                if (search.Contains("spotify"))
                {
                    return await node.Rest.GetTracksAsync(await GetSpotifyAsync(result));
                }
                return await node.Rest.GetTracksAsync(result);
            }
            
            return await node.Rest.GetTracksAsync(search);
        }

        public async Task InterruptPlayback(CommandContext ctx, LavalinkGuildConnection conn)
        {
            var key = ctx.Guild.Id;
            var tmp = Data[key].GuildQueue;

            Data[key].GuildQueue.Clear();
            await conn.StopAsync();
            Data[key].GuildQueue = tmp;
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
            if (Data[key].IsLooping) { await sender.PlayAsync(e.Track); }
            if (QueueIsEmpty(key))
            {
                Data[key].TimeIdle = DateTime.Now;
                return;
            }
            await sender.PlayAsync(Data[key].GuildQueue.Dequeue().Track);
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
                Data.Add(key, new GuildData(new Queue<QueuedTrack>(), false));
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
                if (Data[key].TimeIdle.AddMinutes(3) > DateTime.Now) { continue; }
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
        /// Checks if the song Queue is empty.
        /// </summary>
        /// <returns></returns>
        private bool QueueIsEmpty(ulong key)
        {
            return Data[key].GuildQueue.Count == 0;
        }

        /// <summary>
        /// Gets the youtube thumbnail uri from a youtube link
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private string GetThumbnailUri(Uri uri)
        {
            return $"http://img.youtube.com/vi/{uri.Query.Substring(3)}/0.jpg";
        }

        public async Task<string> GetSpotifyAsync(Uri uri)
        {
            string id = uri.Segments[2];
            Uri.TryCreate($"https://api.spotify.com/v1/tracks/{id}", UriKind.Absolute, out Uri result);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(result);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Content-Type", "application/json");
            request.Headers.Add("Authorization", $"Bearer {Bot.spotifyJson.Token}");
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            dynamic json;
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            {
                json = await JsonSerializer.DeserializeAsync<dynamic>(stream);
            }

            return $"{json.name} {json.artists[0].name}";
            
        }
    }
}

using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace BinguBot.DataTypes
{
    class GuildData
    {
        public Queue<QueuedTrack> GuildQueue;
        public bool IsLooping;
        public DateTime TimeIdle;
        public DiscordChannel CommandChannel;

        public Dictionary<ulong, string> SuggestionList = new Dictionary<ulong, string>();

        public GuildData(Queue<QueuedTrack> GuildQueue, bool IsLooping)
        {
            this.GuildQueue = GuildQueue;
            this.IsLooping = IsLooping;
        }
    }
}

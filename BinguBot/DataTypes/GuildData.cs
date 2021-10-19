using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace BinguBot.DataTypes
{
    class GuildData
    {
        public List<QueuedTrack> GuildQueue;
        public List<LavalinkTrack> GuildHistory;
        public List<List<string>> SavedQueues;
        public bool IsLooping;
        public DateTime TimeIdle;
        public DiscordChannel CommandChannel;

        public GuildData(List<QueuedTrack> GuildQueue)
        {
            this.GuildQueue = GuildQueue;
            this.GuildHistory = new List<LavalinkTrack>(); 
        }
    }
}

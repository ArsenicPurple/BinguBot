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
        public List<QueuedTrack> GuildQueue;
        public bool IsLooping;
        public DateTime TimeIdle;
        public DiscordChannel CommandChannel;

        public GuildData(List<QueuedTrack> GuildQueue, bool IsLooping)
        {
            this.GuildQueue = GuildQueue;
            this.IsLooping = IsLooping;
        }
    }
}

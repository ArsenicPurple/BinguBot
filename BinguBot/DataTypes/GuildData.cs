using System;
using System.Collections.Generic;
using System.Text;

namespace BinguBot.DataTypes
{
    class GuildData
    {
        public Queue<QueuedTrack> GuildQueue;
        public bool IsLooping;
        public DateTime TimeIdle;

        public GuildData(Queue<QueuedTrack> GuildQueue, bool IsLooping)
        {
            this.GuildQueue = GuildQueue;
            this.IsLooping = IsLooping;
        }
    }
}

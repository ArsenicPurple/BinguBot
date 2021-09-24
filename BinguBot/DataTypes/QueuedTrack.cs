using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System;

namespace BinguBot.DataTypes
{
    public class QueuedTrack
    {
        public LavalinkTrack Track;
        public DiscordMember queuedBy;

        public QueuedTrack(LavalinkTrack Track, CommandContext ctx)
        {
            this.Track = Track;
            this.queuedBy = ctx.Member;
        }
    }
}

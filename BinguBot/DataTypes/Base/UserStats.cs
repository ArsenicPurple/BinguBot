using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BinguBot.DataTypes.Base
{
    class UserStats
    {
        public DiscordUser User;
        public int NumPets;
        public int PetStreak;
        public DateTime LastTimePet;
        public int Friendship;

        //public ArrayList<Item> Inventory;

        //public BinguSettings Settings;
        //public ArrayList<Apparel> Closet;
    }
}

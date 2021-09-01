using System;
using System.Collections.Generic;
using System.Text;

namespace BinguBot
{
    static class Converter
    {
        public static int Platform(string platformName)
        {
            switch (platformName.ToLower())
            {
                case "xbox":
                    return 1;
                case "psn":
                    return 2;
                case "steam":
                    return 3;
                default:
                    return 0;
            }
        }

        public static string IntEmoji(int num)
        {
            switch (num)
            {
                case 0:
                    return ":zero:";
                case 1:
                    return ":one:";
                case 2:
                    return ":two:";
                case 3:
                    return ":three:";
                case 4:
                    return ":four:";
                case 5:
                    return ":five:";
                case 6:
                    return ":six:";
                case 7:
                    return ":seven:";
                case 8:
                    return ":eight:";
                case 9:
                    return ":nine:";
                default:
                    return string.Empty;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace BinguBot.DataTypes.Base
{
    class Color
    {
        public byte red;
        public byte blue;
        public byte green;
        public byte alpha;

        public override string ToString()
        {
            string outString =
                "Red: " + red + "\n" +
                "Green: " + green + "\n" +
                "Blue: " + blue + "\n" +
                "Alpha: " + alpha + "\n";

            return outString;
        }
    }
}

using BinguBot.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace BinguBot
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.WriteLine("Hello");
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            Bot bot = new Bot();
            bot.RunAsync().GetAwaiter().GetResult();
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            Debug.WriteLine("Program Stopped");
        }
    }
}

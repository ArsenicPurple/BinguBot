﻿using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace BinguBot
{
    class Program
    {
        static void Main(string[] args)
        {
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

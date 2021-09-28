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

        static async void OnProcessExit(object sender, EventArgs e)
        {
            /*
            using FileStream createStream = File.Create(@"C:\Users\samcr\source\repos\BinguBot\BinguBot\Json\suggestions.json");
            Dictionary<string, Dictionary<string, string>> SerializableSuggestionList = new Dictionary<string, Dictionary<string, string>>();
            foreach (var (key, data) in MusicCommands.Data)
            {
                Dictionary<string, string> SubList = new Dictionary<string, string>();
                foreach (var (id, suggestion) in data.SuggestionList)
                {
                    SubList.Add(id.ToString(), suggestion);
                }
                SerializableSuggestionList.Add(key.ToString(), SubList);
            }

            await JsonSerializer.SerializeAsync(createStream, SerializableSuggestionList);
            await createStream.DisposeAsync();
            */

            Debug.WriteLine("Program Stopped");
        }
    }
}

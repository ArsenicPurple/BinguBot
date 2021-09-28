using BinguBot.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Net;
using DSharpPlus.Lavalink;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;
using BinguBot.DataTypes;

namespace BinguBot
{
    class Bot
    {
        public static DiscordClient Client { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }

        public static Dictionary<string, Dictionary<string, string>> _Data;

        public async Task RunAsync()
        {
            ConfigJson configJson;
            
            using (StreamReader r = new StreamReader(@"config.json"))
            {
                string json = r.ReadToEnd();
                Debug.WriteLine("JSON Read:" + json);
                Console.WriteLine("JSON Read:" + json);
                configJson = JsonSerializer.Deserialize<ConfigJson>(json);
            }

            /*
            using (StreamReader r = new StreamReader(@"C:\Users\samcr\source\repos\BinguBot\BinguBot\Json\suggestions.json"))
            {
                string json = r.ReadToEnd();
                _Data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
            }
            */

            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug
            };

            Client = new DiscordClient(config);

            Client.Ready += OnClientReady;

            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(1)
            });

            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] {configJson.Prefix},
                EnableDms = false,
                EnableMentionPrefix = true,
            };

            Commands = Client.UseCommandsNext(commandsConfig);

            Commands.RegisterCommands<TestCommands>();
            Commands.RegisterCommands<MusicCommands>();

            //Mark:-- Lavalink Section
            var endpoint = new ConnectionEndpoint
            {
                Hostname = "127.0.0.1", // From your server configuration.
                Port = 2333 // From your server configuration
            };

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = "youshallnotpass", // From your server configuration.
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };

            var lavalink = Client.UseLavalink();
            //Mark:-- Lavalink Section

            await Client.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);

            await Task.Delay(-1);
        }

        private Task OnClientReady(DiscordClient c, ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }
    }
}

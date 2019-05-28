﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using YNBBot;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;
using YNBBot.Reactions;
using YNBBot.NestedCommands;

// dotnet publish -c Release -r win10-x64
// dotnet publish -c Release -r linux-x64

public static class Var
{
    internal readonly static Version VERSION = new Version(0, 2);
    /// <summary>
    /// When put to false will stop the program
    /// </summary>
    internal static bool running = true;
    /// <summary>
    /// The client wrapper used to communicate with discords servers
    /// </summary>
    internal static DiscordSocketClient client;
    /// <summary>
    /// Embed color used for the bot
    /// </summary>
    internal static readonly Color BOTCOLOR = new Color(71, 71, 255);
    /// <summary>
    /// Embed color used for bot error messages
    /// </summary>
    internal static readonly Color ERRORCOLOR = new Color(255, 0, 0);
    /// <summary>
    /// Path containing the restart location
    /// </summary>
    internal static string RestartPath = string.Empty;
    /// <summary>
    /// Main culture that is set to all threads
    /// </summary>
    internal static CultureInfo Culture = new CultureInfo("en-us");
}
namespace YNBBot
{
    public class BotCore
    {
        static void Main(string[] args) => new BotCore().MainAsync().GetAwaiter().GetResult();

        /// <summary>
        /// Main Programs method running asynchronously
        /// </summary>
        /// <returns></returns>
        public async Task MainAsync()
        {
            Console.Title = "Ciridium Wing Bot v" + Var.VERSION.ToString();
            Thread.CurrentThread.CurrentCulture = Var.Culture;

            bool filesExist = false;
            bool foundToken = false;
            if (ResourcesModel.CheckSettingsFilesExistence())
            {
                filesExist = true;
                if (await SettingsModel.LoadSettingsAndCheckToken(Var.client))
                {
                    foundToken = true;
                }
            }

            if (foundToken)
            {
                Var.client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Info
                });

                InitTextCommands();
                InitReactionsCommands();

                Var.client.UserJoined += HandleUserJoined;
                Var.client.UserLeft += HandleUserLeft;
                Var.client.Log += Logger;
                SettingsModel.DebugMessage += Logger;
                Var.client.ReactionAdded += ReactionAddedHandler;
                Var.client.ChannelUpdated += ChannelUpdatedHandler;

                await Var.client.LoginAsync(TokenType.Bot, SettingsModel.token);
                await Var.client.StartAsync();

                await TimingThread.UpdateTimeActivity();

                while (Var.running)
                {
                    await Task.Delay(100);
                }

                if (string.IsNullOrEmpty(Var.RestartPath))
                {
                    await SettingsModel.SendDebugMessage("Shutting down ...", DebugCategories.misc);
                }
                else
                {
                    await SettingsModel.SendDebugMessage("Restarting ...", DebugCategories.misc);
                }

                Var.client.Dispose();
            }
            else
            {
                if (!filesExist)
                {
                    await Logger(new LogMessage(LogSeverity.Critical, "SETTINGS", string.Format("Could not find config files! Standard directory is \"{0}\".\nReply with 'y' if you want to generate basic files now!", ResourcesModel.SettingsDirectory)));
                    if (Console.ReadLine().ToCharArray()[0] == 'y')
                    {
                        await ResourcesModel.InitiateBasicFiles();
                    }
                }
                else
                {
                    await Logger(new LogMessage(LogSeverity.Critical, "SETTINGS", string.Format("Could not find a valid token in Settings file ({0}). Press any key to exit!", ResourcesModel.SettingsFilePath)));
                    Console.ReadLine();
                }
            }

            if (!string.IsNullOrEmpty(Var.RestartPath))
            {
                System.Diagnostics.Process.Start(Var.RestartPath);
            }
        }

        #region EventHandling

        private async Task ChannelUpdatedHandler(SocketChannel arg1, SocketChannel arg2)
        {
            SocketTextChannel old_version = arg1 as SocketTextChannel;
            SocketTextChannel new_version = arg2 as SocketTextChannel;

            if (old_version != null && new_version != null)
            {
                if (!old_version.Topic.Equals(new_version.Topic))
                {
                    await HandleTopicUpdated(new_version);
                }
            }
        }

        private async Task HandleTopicUpdated(SocketTextChannel channel)
        {
            if (GuildChannelHelper.TryGetChannel(GuildChannelHelper.DebugChannelId, out SocketTextChannel debugChannel))
            {
                EmbedBuilder debugembed = new EmbedBuilder
                {
                    Color = Var.BOTCOLOR,
                    Title = string.Format("Channel #{0}: Topic updated", channel.Name),
                    Description = string.Format("{0}```\n{1}```", channel.Mention, channel.Topic)
                };
                await debugChannel.SendEmbedAsync(debugembed);
            }
        }

        private async Task ReactionAddedHandler(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            SocketTextChannel guildChannel = channel as SocketTextChannel;
            await ReactionService.HandleReactionAdded(guildChannel, reaction);
        }

        private async Task HandleUserJoined(SocketGuildUser user)
        {
            await SettingsModel.SendDebugMessage(string.Format("{0} joined", user.Mention), DebugCategories.joinleave);
            await SettingsModel.WelcomeNewUser(user);
        }

        private async Task HandleUserLeft(SocketGuildUser user)
        {
            await SettingsModel.SendDebugMessage(string.Format("{0} left", user.Mention), DebugCategories.joinleave);
        }

        #endregion

        /// <summary>
        /// Logs messages to the console
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static Task Logger(LogMessage message)
        {
            var cc = Console.ForegroundColor;
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Error:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message}");
            if (message.Exception != null)
            {
                Console.WriteLine(string.Format("{0}\n{1}", message.Exception.Message, message.Exception.StackTrace));
            }

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Initiates and registers commands
        /// </summary>
        private void InitTextCommands()
        {
            Var.client.MessageReceived += CommandHandler.HandleMessage;
        }

        private static void InitReactionsCommands()
        {
            UtilityReactionCommand.Init();
        }
    }

    public enum AccessLevel
    {
        Basic,
        Admin,
        BotAdmin
    }
}
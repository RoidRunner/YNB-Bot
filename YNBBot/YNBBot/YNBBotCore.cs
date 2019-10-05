using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;
using YNBBot.Reactions;
using YNBBot.NestedCommands;
using YNBBot.Interactive;
using YNBBot.EventLogging;
using YNBBot.Moderation;
using BotCoreNET;

// dotnet publish -c Release -r win10-x64
// dotnet publish -c Release -r linux-x64

public static class Var
{
    internal readonly static Version VERSION = new Version(1, 2);
    /// <summary>
    /// When put to false will stop the program
    /// </summary>
    internal static bool running = true;
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
    public class YNBBotCore
    {
        static void Main(string[] args)
        {
            BotCore.Run();
        }

        /// <summary>
        /// Main Programs method running asynchronously
        /// </summary>
        /// <returns></returns>
        public async Task MainAsync()
        {
            Console.Title = "YNB Bot v" + Var.VERSION.ToString();
            Thread.CurrentThread.CurrentCulture = Var.Culture;

            bool filesExist = false;
            bool foundToken = false;
            if (ResourcesModel.CheckSettingsFilesExistence())
            {
                filesExist = true;
                if (await SettingsModel.LoadSettingsAndCheckToken())
                {
                    foundToken = true;
                }
            }

            if (foundToken)
            {
                await GuildModerationLog.LoadModerationLogs();

                InitReactionsCommands();

                BotCore.Client.MessageReceived += CommandHandler.HandleMessage;
                BotCore.Client.MessageReceived += PingSpamDefenceService.HandleMessage;
                BotCore.Client.UserJoined += EventLogger.WelcomeUser;
                BotCore.Client.UserLeft += EventLogger.HandleUserLeft;
                BotCore.Client.Log += Logger;
                SettingsModel.DebugMessage += Logger;
                BotCore.Client.ReactionAdded += ReactionAddedHandler;
                BotCore.Client.ReactionAdded += InteractiveMessageService.ReactionAddedHandler;
                //BotCore.Client.ChannelUpdated += ChannelUpdatedHandler;

                await BotCore.Client.LoginAsync(TokenType.Bot, SettingsModel.token);
                await BotCore.Client.StartAsync();

                await MinecraftGuildSystem.MinecraftGuildModel.Load();
                await TimingThread.UpdateTimeActivity();

                await Task.Delay(500);


                while (Var.running)
                {
                    await Task.Delay(100);
                }

                if (string.IsNullOrEmpty(Var.RestartPath))
                {
                    await SettingsModel.SendDebugMessage(DebugCategories.misc, "Shutting down ...");
                }
                else
                {
                    await SettingsModel.SendDebugMessage(DebugCategories.misc, "Restarting ...");
                }

                BotCore.Client.Dispose();
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
                if (old_version.Topic != new_version.Topic)
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
                    Color = BotCore.EmbedColor,
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

        private static void InitReactionsCommands()
        {
            UtilityReactionCommand.Init();
        }
    }

    public enum AccessLevel
    {
        Basic,
        Minecraft,
        Admin,
        BotAdmin
    }
}
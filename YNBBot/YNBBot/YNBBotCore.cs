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
using BotCoreNET.CommandHandling;
using BotCoreNET.BotVars;

// dotnet publish -c Release -r win10-x64
// dotnet publish -c Release -r linux-x64

public static class Var
{
    internal readonly static Version VERSION = new Version(1, 3);
    /// <summary>
    /// When put to false will stop the program
    /// </summary>
    internal static bool running = true;
    /// <summary>
    /// Path containing the restart location
    /// </summary>
    internal static string RestartPath = string.Empty;

    internal const string MinecraftBranchRoleBotVarId = "minecraftBranchRole";
}

namespace YNBBot
{
    public class YNBBotCore
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Main Programs method running asynchronously
        /// </summary>
        /// <returns></returns>
        public static async Task MainAsync()
        {
            Console.Title = "YNB Bot v" + Var.VERSION.ToString();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            SetupCommands();
            BotCore.OnBotVarDefaultSetup += SettingsModel.SetupSettingsUpdateListener;
            BotVarManager.OnGuildBotVarCollectionLoaded += EventLogger.SubscribeToBotVarCollection;

            EventLogger.SubscribeToDiscordEvents(BotCore.Client);
            EventLogger.SubscribeToModerationEvents();

            await GuildModerationLog.LoadModerationLogs();

            InitReactionsCommands();

            BotCore.Client.MessageReceived += PingSpamDefenceService.HandleMessage;
            SettingsModel.DebugMessage += Logger;
            BotCore.Client.ReactionAdded += ReactionAddedHandler;
            BotCore.Client.ReactionAdded += InteractiveMessageService.ReactionAddedHandler;
            //BotCore.Client.ChannelUpdated += ChannelUpdatedHandler;


            await MinecraftGuildSystem.MinecraftGuildModel.Load();

            BotCore.Run(commandParser: new YNBCommandParser(), aboutEmbed:getAboutEmbed());
        }

        private static EmbedBuilder getAboutEmbed()
        {
            return new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = "YNBBot v" + Var.VERSION,
                    IconUrl = "https://cdn.discordapp.com/avatars/589012413467328512/2550efa773fdc42b57d12e230d0552b8.png",
                },
                Color = BotCore.EmbedColor,
                Description = $"**Programming**\n" +
                $"\u23F5 BrainProtest#1394 (<@117260771200598019>)" +
                $"\n" +
                $"\n" +
                $"**Third Party Dependencies**\n" +
                $"\u23F5 [Discord.NET](https://github.com/discord-net/Discord.Net) Discord API Wrapper\n"
            };
        }

        private static Task SubscribeEventLoggerSettingsBotVar(SocketGuild guild)
        {
            BotVarCollection collection = BotVarManager.GetGuildBotVarCollection(guild.Id);
            collection.SubscribeToBotVarUpdateEvent(EventLogger.OnBotVarUpdatedGuild, "logChannels");
            return Task.CompletedTask;
        }
        private static void SetupCommands()
        {
            new UserInfoCommand("userinfo");
            new AvatarCommand("avatar");
            new ServerinfoCommand("serverinfo");
            CommandCollection GuildFamily = new CommandCollection("Guild", "Collection of commands used for founding and managing minecraft guilds");
            new CreateGuildCommand("guild-found", GuildFamily);
            new ModifyGuildCommand("guild-modify", GuildFamily);
            new GuildInfoCommand("guild-info", GuildFamily);
            new InviteMemberCommand("guild-invite", GuildFamily);
            new KickGuildMemberCommand("guild-kick", GuildFamily);
            new LeaveGuildCommand("guild-leave", GuildFamily);
            new PassCaptainRightsCommand("guild-passcaptain", GuildFamily);
            new PromoteMateCommand("guild-promote", GuildFamily);
            new DemoteMateCommand("guild-demote", GuildFamily);
            new SyncGuildsCommand("guild-sync", GuildFamily);
            CommandCollection ManagingFamily = new CommandCollection("Management", "Collection of commands used for managing discord entity properties");
            new SetUserNicknameCommand("setnick", ManagingFamily);
            new PurgeMessagesCommand("purge", ManagingFamily);
            new KickUserCommand("kick", ManagingFamily);
            new GetModLogsCommand("modlogs", ManagingFamily);
            new AddModLogNoteCommand("addnote", ManagingFamily);
            new WarnUserCommand("warn", ManagingFamily);
            new BanUserCommand("ban", ManagingFamily);
            new UnBanUserCommand("unban", ManagingFamily);
            new MuteUserCommand("mute", ManagingFamily);
            new UnMuteUserCommand("unmute", ManagingFamily);
        }

        private static async Task ReactionAddedHandler(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            SocketTextChannel guildChannel = channel as SocketTextChannel;
            await ReactionService.HandleReactionAdded(guildChannel, reaction);
        }

        /// <summary>
        /// Logs messages to the console
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static Task Logger(LogMessage message)
        {
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
}
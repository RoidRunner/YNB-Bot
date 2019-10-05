using BotCoreNET;
using BotCoreNET.CommandHandling;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.NestedCommands
{
    static class CommandHandler
    {
        public static char Prefix = '/';

        private static List<Command> directCommands = new List<Command>();
        private static List<CommandCollection> commandCollections = new List<CommandCollection>();
        public static IReadOnlyList<Command> DirectCommands => directCommands.AsReadOnly();
        public static IReadOnlyList<CommandCollection> CommandCollections => commandCollections.AsReadOnly();

        public static async Task HandleCommand(CommandContext context)
        {
            List<Command> matchedCommands = new List<Command>();
            CommandCollection matchedCollection = null;

            FindCommands(context.ContentSansIdentifier, ref matchedCommands, ref matchedCollection);

            if (matchedCommands.Count > 0)
            {
                if (await matchedCommands[0].TryHandleCommand(context) == Command.CommandMatchResult.IdentifiersMatch)
                {
                    await context.Channel.SendEmbedAsync($"The command that matched requires more arguments: `{matchedCommands[0].Syntax}`", true);
                }
            }
            else
            {
                if (matchedCollection != null)
                {
                    if (context.Message.Content.EndsWith("help", StringComparison.OrdinalIgnoreCase))
                    {
                        await CommandHelper.SendCommandCollectionHelp(context, matchedCollection);
                    }
                    else
                    {
                        await context.Channel.SendEmbedAsync($"Use `{Prefix}help {matchedCollection.Identifier}` for a list of all commands in the command family `{matchedCollection.Identifier}`", true);
                    }
                }
                else
                {
                    await context.Message.AddReactionAsync(UnicodeEmoteService.Question);
                }
            }
        }

        public static void FindCommands(string comparestr, ref List<Command> matchedCommands, ref CommandCollection matchedCollection)
        {
            foreach (Command command in directCommands)
            {
                if (comparestr.StartsWith(command.Identifier))
                {
                    matchedCommands.Add(command);
                }
            }
            foreach (CommandCollection collection in commandCollections)
            {
                if (comparestr.StartsWith(collection.Identifier))
                {
                    matchedCollection = collection;
                }
                if (collection.TryFindCommand(comparestr, ref matchedCommands))
                {
                    matchedCollection = collection;
                    break;
                }
            }
        }

        /// <summary>
        /// Handles a received message. If it is identified as a command (starts with prefix), generates the correct context and parses and executes the correct command
        /// </summary>
        /// <param name="message">The message received</param>
        /// <returns></returns>
        public static async Task HandleMessage(SocketMessage message)
        {
            if (message.Content.StartsWith(Prefix) && message.Author.Id != BotCore.Client.CurrentUser.Id)
            {
                // Now we know the message is most likely a command

                SocketUserMessage userMessage = message as SocketUserMessage;

                if (userMessage == null)
                {
                    // The message is a system message, and as such can not be a command.
                    return;
                }

                SocketTextChannel guildChannel = message.Channel as SocketTextChannel;

                if (guildChannel != null)
                {
                    GuildCommandContext guildContext = new GuildCommandContext(userMessage, guildChannel.Guild);

                    if (guildContext.IsDefined)
                    {
                        // The message was sent in a guild context

                        await HandleCommand(guildContext);
                        return;
                    }
                }

                CommandContext context = new CommandContext(userMessage);

                if (context.IsDefined)
                {
                    // The message was sent in PM context

                    await HandleCommand(context);
                }
            }
        }

        static CommandHandler()
        {

            directCommands.Add(new HelpCommand("help"));
            directCommands.Add(new UserInfoCommand("userinfo"));
            directCommands.Add(new AvatarCommand("avatar"));
            directCommands.Add(new ServerinfoCommand("serverinfo"));
            directCommands.Add(new AboutCommand("about"));
            CommandCollection ConfigFamily = new CommandCollection("config", "Collection of commands used for configuring the bot");
            commandCollections.Add(ConfigFamily);
            ConfigFamily.TryAddCommand(new DetectConfigCommand("config detect"));
            ConfigFamily.TryAddCommand(new EditChannelInfoCommand("config channel"));
            ConfigFamily.TryAddCommand(new SetOutputChannelCommand("config output"));
            ConfigFamily.TryAddCommand(new PrefixCommand("config prefix"));
            ConfigFamily.TryAddCommand(new SetRoleCommand("config role"));
            ConfigFamily.TryAddCommand(new SetTemplateCommand("config template"));
            ConfigFamily.TryAddCommand(new ToggleLoggingCommand("config logging"));
            ConfigFamily.TryAddCommand(new AutoRoleCommand("config autorole"));
            ConfigFamily.TryAddCommand(new RestartCommand("config restart"));
            ConfigFamily.TryAddCommand(new StopCommand("config stop"));
            CommandCollection GuildFamily = new CommandCollection("guild", "Collection of commands used for founding and managing minecraft guilds");
            commandCollections.Add(GuildFamily);
            GuildFamily.TryAddCommand(new CreateGuildCommand("guild found"));
            GuildFamily.TryAddCommand(new ModifyGuildCommand("guild modify"));
            GuildFamily.TryAddCommand(new GuildInfoCommand("guild info"));
            GuildFamily.TryAddCommand(new InviteMemberCommand("guild invite"));
            GuildFamily.TryAddCommand(new KickGuildMemberCommand("guild kick"));
            GuildFamily.TryAddCommand(new LeaveGuildCommand("guild leave"));
            GuildFamily.TryAddCommand(new PassCaptainRightsCommand("guild passcaptain"));
            GuildFamily.TryAddCommand(new PromoteMateCommand("guild promote"));
            GuildFamily.TryAddCommand(new DemoteMateCommand("guild demote"));
            GuildFamily.TryAddCommand(new SyncGuildsCommand("guild sync"));
            CommandCollection ManagingFamily = new CommandCollection("manage", "Collection of commands used for managing discord entity properties");
            commandCollections.Add(ManagingFamily);
            ManagingFamily.TryAddCommand(new SetUserNicknameCommand("setnick"));
            ManagingFamily.TryAddCommand(new PurgeMessagesCommand("purge"));
            ManagingFamily.TryAddCommand(new KickUserCommand("kick"));
            ManagingFamily.TryAddCommand(new GetModLogsCommand("modlogs"));
            ManagingFamily.TryAddCommand(new AddModLogNoteCommand("addnote"));
            ManagingFamily.TryAddCommand(new WarnUserCommand("warn"));
            ManagingFamily.TryAddCommand(new BanUserCommand("ban"));
        }
    }
}

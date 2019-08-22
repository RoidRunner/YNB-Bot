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

        public static CommandFamily BaseFamily { get; private set; } = new CommandFamily();

        public static async Task HandleCommand(CommandContext context)
        {
            List<Command> matchedCommands = new List<Command>();
            CommandFamily matchedFamily = null;
            IndexArray<string> args = context.Args;
            if (BaseFamily.TryFindFamilyOrCommand(ref args, ref matchedCommands, ref matchedFamily))
            {
                // something was found
                if (matchedCommands.Count == 0)
                {
                    await context.Channel.SendEmbedAsync($"Use `{Prefix}help {matchedFamily.FullIdentifier}` for a list of all commands in the command family `{matchedFamily.FullIdentifier}`", true);
                }
                else
                {
                    if (await matchedCommands[0].TryHandleCommand(context) == Command.CommandMatchResult.IdentifiersMatch)
                    {
                        await context.Channel.SendEmbedAsync($"The command that matched requires more arguments: `{matchedCommands[0].Syntax}`", true);
                    }
                }
            }
            else
            {
                if (matchedCommands.Count == 0 && matchedFamily == null)
                {
                    // nothing at all was found
                    await context.Message.AddReactionAsync(UnicodeEmoteService.Question);
                }
                else
                {
                    if (context.Message.Content.EndsWith("help", StringComparison.OrdinalIgnoreCase))
                    {
                        if (matchedCommands.Count > 0)
                        {
                            await CommandHelper.SendCommandHelp(context, matchedCommands[0]);
                        }
                        else
                        {
                            await CommandHelper.SendCommandCollectionHelp(context, matchedFamily);
                        }
                    }
                    else
                    {
                        await context.Message.AddReactionAsync(UnicodeEmoteService.Question);
                    }
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
            if (message.Content.StartsWith(Prefix) && message.Author.Id != Var.client.CurrentUser.Id)
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
            BaseFamily.TryAddCommand(new HelpCommand("help"));
            BaseFamily.TryAddCommand(new UserInfoCommand("userinfo"));
            BaseFamily.TryAddCommand(new AvatarCommand("avatar"));
            BaseFamily.TryAddCommand(new AboutCommand("about"));
            CommandFamily ConfigFamily = new CommandFamily("config", BaseFamily, "Collection of commands used for configuring the bot");
            ConfigFamily.TryAddCommand(new DetectConfigCommand("detect"));
            ConfigFamily.TryAddCommand(new EditChannelInfoCommand("channel"));
            ConfigFamily.TryAddCommand(new SetOutputChannelCommand("output"));
            ConfigFamily.TryAddCommand(new PrefixCommand("prefix"));
            ConfigFamily.TryAddCommand(new SetRoleCommand("role"));
            ConfigFamily.TryAddCommand(new SetTemplateCommand("template"));
            ConfigFamily.TryAddCommand(new ToggleLoggingCommand("logging"));
            ConfigFamily.TryAddCommand(new AutoRoleCommand("autorole"));
            ConfigFamily.TryAddCommand(new RestartCommand("restart"));
            ConfigFamily.TryAddCommand(new StopCommand("stop"));
            CommandFamily EmbedFamily = new CommandFamily("embed", BaseFamily, "Collection of commands used for analyzing, designing and sending fully custom embeds");
            EmbedFamily.TryAddCommand(new SendEmbedCommand("send"));
            EmbedFamily.TryAddCommand(new PreviewEmbedCommand("preview"));
            EmbedFamily.TryAddCommand(new GetEmbedCommand("get"));
            EmbedFamily.TryAddCommand(new ReplaceEmbedCommand("replace"));
            CommandFamily GuildFamily = new CommandFamily("guild", BaseFamily, "Collection of commands used for founding and managing minecraft guilds");
            GuildFamily.TryAddCommand(new CreateGuildCommand("found"));
            GuildFamily.TryAddCommand(new ModifyGuildCommand("modify"));
            GuildFamily.TryAddCommand(new GuildInfoCommand("info"));
            GuildFamily.TryAddCommand(new InviteMemberCommand("invite"));
            GuildFamily.TryAddCommand(new KickGuildMemberCommand("kick"));
            GuildFamily.TryAddCommand(new LeaveGuildCommand("leave"));
            GuildFamily.TryAddCommand(new PassCaptainRightsCommand("passcaptain"));
            GuildFamily.TryAddCommand(new PromoteMateCommand("promote"));
            GuildFamily.TryAddCommand(new DemoteMateCommand("demote"));
            GuildFamily.TryAddCommand(new SyncGuildsCommand("sync"));
            CommandFamily ManagingFamily = new CommandFamily("manage", BaseFamily, "Collection of commands used for managing discord entity properties");
            ManagingFamily.TryAddCommand(new SetUserNicknameCommand("setnick"));
            ManagingFamily.TryAddCommand(new PurgeMessagesCommand("purge"));
        }
    }
}

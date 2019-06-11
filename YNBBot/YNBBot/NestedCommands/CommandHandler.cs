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
                // nothing at all was found
                await context.Message.AddReactionAsync(UnicodeEmoteService.Question);
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
            BaseFamily.TryAddCommand(new HelpCommand());
            BaseFamily.TryAddCommand(new UserInfoCommand());
            BaseFamily.TryAddCommand(new AvatarCommand());
            BaseFamily.TryAddCommand(new StopCommand());
            BaseFamily.TryAddCommand(new RestartCommand());
            CommandFamily SettingsFamily = new CommandFamily("config", BaseFamily);
            CommandFamily SettingsChannelFamily = new CommandFamily("channel", SettingsFamily);
            SettingsChannelFamily.TryAddCommand(new SetOutputChannelCommand());
            SettingsFamily.TryAddCommand(new EditChannelInfoCommand());
            SettingsFamily.TryAddCommand(new DetectConfigCommand());
            SettingsFamily.TryAddCommand(new PrefixCommand());
            SettingsFamily.TryAddCommand(new SetRoleCommand());
            SettingsFamily.TryAddCommand(new SetTemplateCommand());
            SettingsFamily.TryAddCommand(new ToggleLoggingCommand());
            CommandFamily EmbedFamily = new CommandFamily("embed", BaseFamily);
            EmbedFamily.TryAddCommand(new SendEmbedCommand());
            EmbedFamily.TryAddCommand(new PreviewEmbedCommand());
            EmbedFamily.TryAddCommand(new GetEmbedCommand());
            EmbedFamily.TryAddCommand(new ReplaceEmbedCommand());
            CommandFamily GuildFamily = new CommandFamily("guild", BaseFamily);
            GuildFamily.TryAddCommand(new CreateGuildCommand());
        }
    }
}

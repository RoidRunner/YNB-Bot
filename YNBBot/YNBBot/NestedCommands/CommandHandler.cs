//using BotCoreNET;
//using BotCoreNET.CommandHandling;
//using Discord.WebSocket;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;

//namespace YNBBot.NestedCommands
//{
//    static class CommandHandler
//    {
//        public static char Prefix = '/';

//        private static List<Command> directCommands = new List<Command>();
//        private static List<CommandCollection> commandCollections = new List<CommandCollection>();
//        public static IReadOnlyList<Command> DirectCommands => directCommands.AsReadOnly();
//        public static IReadOnlyList<CommandCollection> CommandCollections => commandCollections.AsReadOnly();

//        public static async Task HandleCommand(CommandContext context)
//        {
//            List<Command> matchedCommands = new List<Command>();
//            CommandCollection matchedCollection = null;

//            FindCommands(context.ContentSansIdentifier, ref matchedCommands, ref matchedCollection);

//            if (matchedCommands.Count > 0)
//            {
//                if (await matchedCommands[0].TryHandleCommand(context) == Command.CommandMatchResult.IdentifiersMatch)
//                {
//                    await context.Channel.SendEmbedAsync($"The command that matched requires more arguments: `{matchedCommands[0].Syntax}`", true);
//                }
//            }
//            else
//            {
//                if (matchedCollection != null)
//                {
//                    if (context.Message.Content.EndsWith("help", StringComparison.OrdinalIgnoreCase))
//                    {
//                        await CommandHelper.SendCommandCollectionHelp(context, matchedCollection);
//                    }
//                    else
//                    {
//                        await context.Channel.SendEmbedAsync($"Use `{Prefix}help {matchedCollection.Identifier}` for a list of all commands in the command family `{matchedCollection.Identifier}`", true);
//                    }
//                }
//                else
//                {
//                    await context.Message.AddReactionAsync(UnicodeEmoteService.Question);
//                }
//            }
//        }

//        public static void FindCommands(string comparestr, ref List<Command> matchedCommands, ref CommandCollection matchedCollection)
//        {
//            foreach (Command command in directCommands)
//            {
//                if (comparestr.StartsWith(command.Identifier))
//                {
//                    matchedCommands.Add(command);
//                }
//            }
//            foreach (CommandCollection collection in commandCollections)
//            {
//                if (comparestr.StartsWith(collection.Identifier))
//                {
//                    matchedCollection = collection;
//                }
//                if (collection.TryFindCommand(comparestr, ref matchedCommands))
//                {
//                    matchedCollection = collection;
//                    break;
//                }
//            }
//        }

//        /// <summary>
//        /// Handles a received message. If it is identified as a command (starts with prefix), generates the correct context and parses and executes the correct command
//        /// </summary>
//        /// <param name="message">The message received</param>
//        /// <returns></returns>
//        public static async Task HandleMessage(SocketMessage message)
//        {
//            if (message.Content.StartsWith(Prefix) && message.Author.Id != BotCore.Client.CurrentUser.Id)
//            {
//                // Now we know the message is most likely a command

//                SocketUserMessage userMessage = message as SocketUserMessage;

//                if (userMessage == null)
//                {
//                    // The message is a system message, and as such can not be a command.
//                    return;
//                }

//                SocketTextChannel guildChannel = message.Channel as SocketTextChannel;

//                if (guildChannel != null)
//                {
//                    GuildCommandContext guildContext = new GuildCommandContext(userMessage, guildChannel.Guild);

//                    if (guildContext.IsDefined)
//                    {
//                        // The message was sent in a guild context

//                        await HandleCommand(guildContext);
//                        return;
//                    }
//                }

//                CommandContext context = new CommandContext(userMessage);

//                if (context.IsDefined)
//                {
//                    // The message was sent in PM context

//                    await HandleCommand(context);
//                }
//            }
//        }

//        static CommandHandler()
//        {

//        }
//    }
//}

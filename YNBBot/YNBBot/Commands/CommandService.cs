//#define PMSTACKTRACE

//using Discord;
//using Discord.Commands;
//using Discord.Rest;
//using Discord.WebSocket;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;

//namespace YNBBot
//{
//    [Obsolete]
//    static class CommandService
//    {
//        #region Variables

//        /// <summary>
//        /// The command prefix that marks messages as commands
//        /// </summary>
//        internal static char Prefix = '/';

//        /// <summary>
//        /// The dictionary storing commands by their first argument key
//        /// </summary>
//        public static List<CommandOld> commands { get; private set; }

//        #endregion
//        #region Init

//        /// <summary>
//        /// Constructor
//        /// </summary>
//        static CommandService()
//        {
//            commands = new List<CommandOld>();
//        }

//        #endregion
//        #region Command Adding

//        /// <summary>
//        /// Add a new command by key
//        /// </summary>
//        /// <param name="key">The key to identify the command</param>
//        /// <param name="command">The command object defining the commands behaviour</param>
//        public static void AddCommand(CommandOld cmd)
//        {
//            commands.Add(cmd);
//        }

//        #endregion
//        #region Command Handling

//        /// <summary>
//        /// Command handling
//        /// </summary>
//        /// <param name="context">The context the command runs in</param>
//        /// <returns></returns>
//        public static async Task HandleCommand(SocketUserMessage msg)
//        {
//            if (IsCommand(msg.Content))
//            {
//                CommandContextOld context = new CommandContextOld(Var.client, msg);
//                if (TryGetCommand(context, out CommandOld cmd))
//                {
//                    bool channelTypesMatch = cmd.RequiredChannelType == context.ChannelType;
//                    if (!cmd.UserHasPermission(context.UserAccessLevel))
//                    {
//                        // User lacks permission to execute command

//                        await context.Channel.SendEmbedAsync(
//                            string.Format("Insufficient Permissions. `/{0}` requires {1} access, you have {2} access",
//                            cmd.Key.KeyList, cmd.AccessLevel.ToString(), context.UserAccessLevel.ToString()), true);
//                    }
//                    else if (cmd.IsShitposting && !context.ChannelAllowsShitposting && context.UserAccessLevel < AccessLevel.Admin)
//                    {
//                        // Command requires shitposting, but is not in shitposting enabled channel, nor is the user Director or above to bypass this check

//                        RestUserMessage message = await context.Channel.SendEmbedAsync(context.User.Mention, "This channel is a **no-fun-zone**!", true);
//                        TimingThread.AddScheduleDelegate(() =>
//                        {
//                            context.Message.DeleteAsync();
//                            message.DeleteAsync();
//                            return Task.CompletedTask;
//                        }, 5000);
//                    }
//                    else
//                    {
//                        // The command passed all checks and is now executed

//                        if (context.ArgCnt >= cmd.Key.MinArgCnt)
//                        {
//                            try
//                            {
//                                if (cmd.UseTyping)
//                                {
//                                    using (msg.Channel.EnterTypingState())
//                                    {
//                                        await HandleCommand_Part2(cmd, context);
//                                    }
//                                }
//                                else
//                                {
//                                    await HandleCommand_Part2(cmd, context);
//                                }
//                            }
//                            catch (Exception e)
//                            {
//                                SendCommandExecutionExceptionMessage(e, context, cmd);
//                            }
//                        }
//                        else
//                        {
//                            await context.Channel.SendEmbedAsync(string.Format("The command `/{0}` expects {1} arguments, that is {2} more than you supplied! Try `/help {0}` for more info",
//                                cmd.Key.KeyList, cmd.Key.MinArgCnt, cmd.Key.MinArgCnt - context.ArgCnt
//                                ), true);
//                        }
//                    }
//                }
//                else
//                {
//                    await context.Message.AddReactionAsync(new Emote(Emotes.question));
//                    await SettingsModel.SendDebugMessage(string.Format("A potential command `{0}` could not be identified", msg.Content), DebugCategories.misc);
//                }
//            }
//        }

//        private static async Task HandleCommand_Part2(CommandOld cmd, CommandContextOld context)
//        {
//            if (cmd.async)
//            {
//                await cmd.HandleCommand(context);
//            }
//            else
//            {
//                cmd.HandleSynchronousCommand(context);
//            }
//        }

//        /// <summary>
//        /// Tries to match a context to a command
//        /// </summary>
//        /// <param name="context">The command context the command would execute in</param>
//        /// <param name="result">The command match</param>
//        /// <returns>Wether the command matching attempt was successful</returns>
//        public static bool TryGetCommand(CommandContextOld context, out CommandOld result)
//        {
//            result = new CommandOld();
//            int argCntMatched = -2;
//            foreach (CommandOld command in commands)
//            {
//                if (command.Key.Matches(context.Args) && command.Key.FixedArgCnt > argCntMatched)
//                {
//                    result = command;
//                    argCntMatched = command.Key.FixedArgCnt;
//                }
//            }
//            return argCntMatched != -2;
//        }

//        /// <summary>
//        /// Tries to match a key input to a command
//        /// </summary>
//        /// <param name="keys">The command keys identifying the command</param>
//        /// <param name="result">The command match</param>
//        /// <returns>Wether the command matching attempt was successful</returns>
//        public static bool TryGetCommand(string[] keys, out CommandOld result)
//        {
//            result = new CommandOld();
//            int argCntMatched = -2;
//            foreach (CommandOld command in commands)
//            {
//                if (command.Key.Matches(keys) && command.Key.FixedArgCnt >= argCntMatched)
//                {
//                    result = command;
//                    argCntMatched = command.Key.FixedArgCnt;
//                }
//            }
//            return argCntMatched != -2;
//        }

//        /// <summary>
//        /// Tries to match a context to all matching commands
//        /// </summary>
//        /// <param name="keys">The command keys identifying the command</param>
//        /// <param name="results">The command matches</param>
//        /// <returns>Wether the command matching attempt was successful (as in yielding >= 1 results)</returns>
//        public static bool TryGetCommands(string[] keys, out List<CommandOld> results)
//        {
//            results = new List<CommandOld>();
//            int argCntMatched = -2;
//            foreach (CommandOld command in commands)
//            {
//                if (command.Key.Matches(keys) && command.Key.FixedArgCnt >= argCntMatched)
//                {
//                    results.Add(command);
//                    argCntMatched = command.Key.FixedArgCnt;
//                }
//            }
//            return argCntMatched != -2;
//        }

//        private static bool IsCommand(string content)
//        {
//            return content.StartsWith(Prefix);
//        }

//        /// <summary>
//        /// Sends an exception message to the debugmessage channel pinging the botdevs about it
//        /// </summary>
//        /// <param name="e">Exception</param>
//        /// <param name="context">The context the command failing executed in</param>
//        /// <param name="cmd">The command matched to the context</param>
//        public async static void SendCommandExecutionExceptionMessage(Exception e, CommandContextOld context, CommandOld cmd)
//        {
//            await context.Channel.SendEmbedAsync("Something went horribly wrong trying to execute your command! I have contacted my creators to help fix this issue!", true);
//            ISocketMessageChannel channel = Var.client.GetChannel(SettingsModel.DebugMessageChannelId) as ISocketMessageChannel;
//            if (channel != null)
//            {
//                EmbedBuilder embed = new EmbedBuilder
//                {
//                    Color = Var.ERRORCOLOR,
//                    Title = "**__Exception__**"
//                };
//                embed.AddField("Command", cmd.Key.KeyList);
//                embed.AddField("Location", context.Guild.GetTextChannel(context.Channel.Id).Mention);
//                embed.AddField("Message", Macros.MultiLineCodeBlock(e.Message));
//                string stacktrace;
//                if (e.StackTrace.Length <= 500)
//                {
//                    stacktrace = e.StackTrace;
//                }
//                else
//                {
//                    stacktrace = e.StackTrace.Substring(0, 500);
//                }
//                embed.AddField("StackTrace", Macros.MultiLineCodeBlock(stacktrace));
//                string message = string.Empty;
//                SocketRole botDevRole = context.Guild.GetRole(SettingsModel.BotDevRole);
//                if (botDevRole != null)
//                {
//                    message = botDevRole.Mention;
//                }
//                await channel.SendMessageAsync(message, embed: embed.Build());
//            }
//            await BotCore.Logger(new LogMessage(LogSeverity.Error, "CMDSERVICE", string.Format("An Exception occured while trying to execute command `/{0}`.Message: '{1}'\nStackTrace {2}", cmd.Key.KeyList, e.Message, e.StackTrace)));
//        }

//        #endregion
//    }
//}

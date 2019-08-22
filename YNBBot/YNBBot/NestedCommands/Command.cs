using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YNBBot.MultiThreading;

namespace YNBBot.NestedCommands
{
    abstract class Command
    {
        #region Fields, Properties

        public CommandFamily ParentFamily { get; private set; }
        public int FirstArgumentIndex { get; private set; }
        /// <summary>
        /// If true, the command can only be executed in a guild context
        /// </summary>
        public bool RequireGuild { get; private set; } = false;
        public bool IsShitposting { get; protected set; } = false;
        /// <summary>
        /// The minimum AccessLevel required to execute this command
        /// </summary>
        public AccessLevel RequiredAccessLevel { get; private set; } = AccessLevel.Basic;

        public bool RunAsync { get; private set; }

        /// <summary>
        /// String id key used for message->command parsing
        /// </summary>
        public string Identifier { get; private set; }

        /// <summary>
        /// The maximum amount of non-identifier arguments the command can handle
        /// </summary>
        public int MaxArgCnt { get; private set; }
        /// <summary>
        /// The minimum amount of non-identifier arguments the command can handle
        /// </summary>
        public int MinArgCnt { get; private set; }

        private const int MESSAGE_DELETION_DELAY = 5000;
        /// <summary>
        /// The arguments used to define min/max argument count and argument helps
        /// </summary>
        public readonly Argument[] Arguments;

        public readonly Precondition[] Preconditions;

        private readonly OverriddenMethod CommandHandlerMethod;
        private readonly OverriddenMethod ArgumentParserMethod;

        public enum OverriddenMethod
        {
            None,
            BasicAsync,
            GuildAsync,
            BasicSynchronous,
            GuildSynchronous,
        }

        #endregion
        #region Constructors

        public Command(string identifier, OverriddenMethod argumentParser, OverriddenMethod commandHandler, bool offThread = false, Argument[] arguments = null, Precondition[] preconditions = null, string summary = default, string remarks = default, string helplink = default)
        {
            Identifier = identifier;
            RequireGuild = CommandHandlerMethod == OverriddenMethod.GuildAsync || CommandHandlerMethod == OverriddenMethod.GuildSynchronous || ArgumentParserMethod == OverriddenMethod.GuildAsync || ArgumentParserMethod == OverriddenMethod.GuildSynchronous;
            ArgumentParserMethod = argumentParser;
            CommandHandlerMethod = commandHandler;
            RunAsync = offThread;
            if (arguments == null)
            {
                Arguments = new Argument[0];
            }
            else
            {
                Arguments = arguments;
                MaxArgCnt = arguments.Length;
                MinArgCnt = arguments.Length;
                if (arguments.Length > 0)
                {
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        if (arguments[i].Multiple)
                        {
                            MaxArgCnt = 1000;
                            break;
                        }
                    }
                    for (int i = arguments.Length - 1; i >= 0; i--)
                    {
                        if (arguments[i].Optional)
                        {
                            MinArgCnt--;
                        }
                    }
                }
            }
            RequiredAccessLevel = AccessLevel.Basic;
            if (preconditions == null)
            {
                Preconditions = new Precondition[0];
            }
            else
            {
                Preconditions = preconditions;
                foreach (Precondition precondition in Preconditions)
                {
                    if (precondition.RequireGuild && !RequireGuild)
                    {
                        RequireGuild = true;
                    }
                    AccessLevelAuthPrecondition accessLevelAuthPrecondition = precondition as AccessLevelAuthPrecondition;
                    if (accessLevelAuthPrecondition != null)
                    {
                        RequiredAccessLevel = accessLevelAuthPrecondition.RequiredAccessLevel;
                    }
                }
            }
            Description = summary;
            Remarks = remarks;
            Link = helplink;
        }

        #endregion
        #region CommandHandlers

        public enum CommandMatchResult
        {
            NoMatch,
            IdentifiersMatch,
            CompleteMatch
        }

        /// <summary>
        /// Checks wether a given argument Indexarray could represent the command
        /// </summary>
        /// <param name="args">IndexArray with Index at the command Identifier</param>
        /// <param name="allowLessThanMin">For help command gathering. Allowing less than the minimum amount of arguments</param>
        /// <returns>True, if both Identifier and argument count match</returns>
        public CommandMatchResult CheckCommandMatch(IndexArray<string> args, bool allowLessThanMin = false)
        {
            if (args.First == Identifier)
            {
                if ((args.Count - 1 >= MinArgCnt || allowLessThanMin) && args.Count - 1 <= MaxArgCnt)
                {
                    return CommandMatchResult.CompleteMatch;
                }
                else
                {
                    return CommandMatchResult.IdentifiersMatch;
                }
            }
            else
            {
                return CommandMatchResult.NoMatch;
            }
        }

        /// <summary>
        /// Handles the command, if the given context is a match
        /// </summary>
        /// <param name="context">Commandcontext the command should execute in</param>
        /// <returns>True, if the context could be matched to the command</returns>
        public async Task<CommandMatchResult> TryHandleCommand(CommandContext context)
        {
            CommandMatchResult commandMatch = CheckCommandMatch(context.Args);

            if (commandMatch == CommandMatchResult.CompleteMatch)
            {
                context.Args.Index++;

                GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext);

                if (RequireGuild && guildContext == null)
                {
                    await context.Channel.SendEmbedAsync("This command can only be executed in guilds (commonly referred to as \"server\")!", true);
                }
                if ((guildContext != null) && !guildContext.ChannelConfig.AllowCommands && context.UserAccessLevel < AccessLevel.Admin)
                {
                    var message = await context.Channel.SendEmbedAsync("This channel is a no-command-zone!", true);
                    Macros.ScheduleMessagesForDeletion(MESSAGE_DELETION_DELAY, message, context.Message);
                }
                else if ((guildContext != null) && IsShitposting && !guildContext.ChannelConfig.AllowShitposting && context.UserAccessLevel < AccessLevel.Admin)
                {
                    var message = await context.Channel.SendEmbedAsync("Cannot use this command in this channel, as it is a no fun zone!", true);
                    Macros.ScheduleMessagesForDeletion(MESSAGE_DELETION_DELAY, message, context.Message);
                }
                else
                {
                    PreconditionCheck(context, guildContext, out List<string> failedAuthCheckMessages);
                    if (failedAuthCheckMessages.Count == 1)
                    {
                        EmbedBuilder embed = new EmbedBuilder()
                        {
                            Color = Var.ERRORCOLOR,
                            Title = "A Precondition has not been met!",
                            Description = failedAuthCheckMessages[0]
                        };
                        await context.Channel.SendEmbedAsync(embed);
                    }
                    else if (failedAuthCheckMessages.Count > 1)
                    {
                        EmbedBuilder embed = new EmbedBuilder()
                        {
                            Color = Var.ERRORCOLOR,
                            Title = $"{failedAuthCheckMessages.Count} Preconditions have not been met!",
                            Description = failedAuthCheckMessages.Join("\n")
                        };
                        await context.Channel.SendEmbedAsync(embed);
                    }
                    else
                    {

                        try
                        {
                            if (RequiredAccessLevel >= AccessLevel.Admin)
                            {
                                await SettingsModel.SendAdminCommandUsedMessage(context, this);
                            }

                            bool mayBeHelpRequest = false;
                            if (context.Args.Count >= 1)
                            {
                                mayBeHelpRequest = context.Args.First.Equals("help", StringComparison.OrdinalIgnoreCase) && context.Args.Count == 1;
                            }
                            ArgumentParseResult parseResult = await TryParseArguments(context, guildContext);
                            if (!parseResult.Success)
                            {
                                if (!mayBeHelpRequest)
                                {
                                    await context.Channel.SendEmbedAsync("Argument Parsing Error", parseResult.Message, true);
                                }
                                await CommandHelper.SendCommandHelp(context, this);
                            }
                            else
                            {
                                await HandleCommand(context, guildContext);
                            }
                        }
                        catch (Exception e)
                        {
                            string location;
                            if (context.IsGuildContext)
                            {
                                location = Macros.GetMessageURL(guildContext.Message, guildContext.Guild.Id);
                            }
                            else
                            {
                                location = $"PM with {context.User.Mention} ({context.User.Username}#{context.User.Discriminator})";
                            }
                            await GuildChannelHelper.SendExceptionNotification(e, $"Error Executing Command `{CommandHandler.Prefix}{FullIdentifier}`, here: {location}");
                            await context.Channel.SendEmbedAsync("The command you attempted to execute unexpectedly threw an exception. Bot Dev is notified, stand by!", true);
                        }
                    }
                }
            }
            return commandMatch;
        }

        public bool PreconditionCheck(CommandContext context, GuildCommandContext guildContext, out List<string> failedAuthCheckMessages)
        {
            failedAuthCheckMessages = new List<string>();
            if (!context.IsGuildContext && RequireGuild)
            {
                failedAuthCheckMessages.Add("Command can only be executed in a Guild Channel!");
                return false;
            }
            foreach (Precondition authCheck in Preconditions)
            {
                if (authCheck.RequireGuild)
                {
                    if (!authCheck.IsAuthorizedGuild(guildContext, out string message))
                    {
                        failedAuthCheckMessages.Add(message);
                    }
                }
                else
                {
                    if (!authCheck.IsAuthorized(context, out string message))
                    {
                        failedAuthCheckMessages.Add(message);
                    }
                }
            }

            return failedAuthCheckMessages.Count == 0;
        }

        private async Task HandleCommand(CommandContext context, GuildCommandContext guildContext)
        {
            if (RunAsync)
            {
                switch (CommandHandlerMethod)
                {
                    case OverriddenMethod.BasicAsync:
                        WorkerThreadService.QueueTask(new WorkerTask(() => { return HandleCommandAsync(context); }));
                        break;
                    case OverriddenMethod.GuildAsync:
                        WorkerThreadService.QueueTask(new WorkerTask(() => { return HandleCommandGuildAsync(guildContext); }));
                        break;
                    case OverriddenMethod.BasicSynchronous:
                        WorkerThreadService.QueueTask(new WorkerTask(() => { HandleCommandSynchronous(context); return Task.CompletedTask; }));
                        break;
                    case OverriddenMethod.GuildSynchronous:
                        WorkerThreadService.QueueTask(new WorkerTask(() => { HandleCommandGuildSynchronous(guildContext); return Task.CompletedTask; }));
                        break;
                    default:
                        throw new Exception("The overriden Method could not be parsed!");
                }
            }
            else
            {
                switch (CommandHandlerMethod)
                {
                    case OverriddenMethod.BasicAsync:
                        await HandleCommandAsync(context);
                        break;
                    case OverriddenMethod.GuildAsync:
                        await HandleCommandGuildAsync(guildContext);
                        break;
                    case OverriddenMethod.BasicSynchronous:
                        HandleCommandSynchronous(context);
                        break;
                    case OverriddenMethod.GuildSynchronous:
                        HandleCommandGuildSynchronous(guildContext);
                        break;
                    default:
                        throw new Exception("The overriden Method could not be parsed!");
                }
            }
        }

        protected virtual Task HandleCommandAsync(CommandContext context)
        {
            throw new UnpopulatedMethodException(OverriddenMethod.BasicAsync);
        }

        protected virtual Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            throw new UnpopulatedMethodException(OverriddenMethod.GuildAsync);
        }
        protected virtual void HandleCommandSynchronous(CommandContext context)
        {
            throw new UnpopulatedMethodException(OverriddenMethod.BasicSynchronous);
        }

        protected virtual void HandleCommandGuildSynchronous(GuildCommandContext context)
        {
            throw new UnpopulatedMethodException(OverriddenMethod.GuildSynchronous);
        }

        #endregion
        #region ArgumentParsers

        private async Task<ArgumentParseResult> TryParseArguments(CommandContext context, GuildCommandContext guildContext)
        {
            switch (ArgumentParserMethod)
            {
                case OverriddenMethod.None:
                    return ArgumentParseResult.DefaultNoArguments;
                case OverriddenMethod.BasicAsync:
                    return await TryParseArgumentsAsync(context);
                case OverriddenMethod.GuildAsync:
                    return await TryParseArgumentsGuildAsync(guildContext);
                case OverriddenMethod.BasicSynchronous:
                    return TryParseArgumentsSynchronous(context);
                case OverriddenMethod.GuildSynchronous:
                    return TryParseArgumentsGuildSynchronous(guildContext);
                default:
                    return new ArgumentParseResult($"Internal Error: `{Macros.GetCodeLocation()}`");
            }
        }

#pragma warning disable 1998
        protected async virtual Task<ArgumentParseResult> TryParseArgumentsAsync(CommandContext context)
        {
            throw new UnpopulatedMethodException(OverriddenMethod.BasicAsync);
        }

        protected async virtual Task<ArgumentParseResult> TryParseArgumentsGuildAsync(GuildCommandContext context)
        {
            throw new UnpopulatedMethodException(OverriddenMethod.GuildAsync);
        }
#pragma warning restore 1998

        protected virtual ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            throw new UnpopulatedMethodException(OverriddenMethod.BasicSynchronous);
        }

        protected virtual ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            throw new UnpopulatedMethodException(OverriddenMethod.GuildSynchronous);
        }

        #endregion
        #region Help

        /// <summary>
        /// All identifiers of the parent command families and the commands identifier together
        /// </summary>
        public string FullIdentifier;
        /// <summary>
        /// The command handlers prefix and the full identifier
        /// </summary>
        public string PrefixIdentifier
        {
            get
            {
                return CommandHandler.Prefix + FullIdentifier;
            }
        }
        /// <summary>
        /// The prefix identifier and all arguments
        /// </summary>
        public string Syntax
        {
            get
            {
                if (MaxArgCnt > 0)
                {
                    return CommandHandler.Prefix + FullIdentifier + " " + Arguments.Join(" ");
                }
                else
                {
                    return CommandHandler.Prefix + FullIdentifier;
                }
            }
        }

        /// <summary>
        /// The commands help description
        /// </summary>
        public readonly string Description;
        /// <summary>
        /// Optional remarks for a command
        /// </summary>
        public readonly string Remarks;
        /// <summary>
        /// Optional Link to online documentation of the command
        /// </summary>
        public readonly string Link;

        public bool HasDescription { get { return !string.IsNullOrEmpty(Description); } }
        public bool HasRemarks { get { return !string.IsNullOrEmpty(Remarks); } }
        public bool HasLink { get { return !string.IsNullOrEmpty(Link); } }

        #endregion
        #region Initialization

        public void RegisterParent(CommandFamily parent)
        {
            ParentFamily = parent;
            FirstArgumentIndex = parent.IndexDepth + 1;
            if (string.IsNullOrEmpty(parent.FullIdentifier))
            {
                FullIdentifier = Identifier;
            }
            else
            {
                FullIdentifier = parent.FullIdentifier + " " + Identifier;
            }
        }

        [Obsolete]
        public void InitializeHelp(string description, Argument[] arguments, string remarks = null, string helpLink = null)
        {
        }

        #endregion

    }
    public class UnpopulatedMethodException : Exception
    {
        public override string Message { get; }

        internal UnpopulatedMethodException(Command.OverriddenMethod environment) : base()
        {
            Message = $"The Commandhandler for {environment} has not been overridden!";
        }

        internal UnpopulatedMethodException(string message) : base()
        {
            Message = message;
        }
    }

    /// <summary>
    /// Provides parsing results for argument parsers. Only the static readonly objects represent a successful parse, all other represent a parse error!
    /// </summary>
    public class ArgumentParseResult
    {
        /// <summary>
        /// Wether parsing was successful or not
        /// </summary>
        public bool Success { get; private set; } = false;
        /// <summary>
        /// The error message, if parsing was unsuccessful
        /// </summary>
        public string Message { get; private set; }

        public static readonly ArgumentParseResult DefaultNoArguments = new ArgumentParseResult("No arguments given");
        public static readonly ArgumentParseResult SuccessfullParse = new ArgumentParseResult("Successful parse!");

        static ArgumentParseResult()
        {
            DefaultNoArguments.Success = true;
            SuccessfullParse.Success = true;
        }

        /// <summary>
        /// Creates an ArgumentParseResult with a simple error message
        /// </summary>
        /// <param name="errormessage">The error message text</param>
        public ArgumentParseResult(string errormessage)
        {
            Message = errormessage;
        }

        /// <summary>
        /// Creates an ArgumentParseResult based on a CommandArgument
        /// </summary>
        /// <param name="argument">Command Argument which could not be parsed</param>
        public ArgumentParseResult(Argument argument)
        {
            Message = $"*`{argument}`*: Failed to parse!";
        }

        /// <summary>
        /// Creates a detailed ArgumentParseResult based on a CommandArgument
        /// </summary>
        /// <param name="argument">Command Argument which could not be parsed</param>
        /// <param name="errormessage">Error message text that explains why parsing failed</param>
        public ArgumentParseResult(Argument argument, string errormessage)
        {
            Message = $"*`{argument}`*: {errormessage}";
        }
    }


    public enum CommandEnvironment
    {
        Base,
        Guild
    }
}

using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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
        public AccessLevel RequireAccessLevel { get; protected set; } = AccessLevel.Basic;

        /// <summary>
        /// String id key used for message->command parsing
        /// </summary>
        public string Identifier { get; private set; }

        /// <summary>
        /// The maximum amount of non-identifier arguments the command can handle
        /// </summary>
        public int MaxArgCnt { get; protected set; }
        /// <summary>
        /// The minimum amount of non-identifier arguments the command can handle
        /// </summary>
        public int MinArgCnt { get; protected set; }

        private const int MESSAGE_DELETION_DELAY = 5000;
        private CommandArgument[] args;
        /// <summary>
        /// The arguments used to define min/max argument count and argument helps
        /// </summary>
        public CommandArgument[] Arguments
        {
            get => args;
            set
            {
                args = value;
                MaxArgCnt = value.Length;
                MinArgCnt = value.Length;
                if (value.Length > 0)
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        if (value[i].Multiple)
                        {
                            MaxArgCnt = 1000;
                            break;
                        }
                    }
                    for (int i = value.Length - 1; i >= 0; i--)
                    {
                        if (value[i].Optional)
                        {
                            MinArgCnt--;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        public abstract OverriddenMethod CommandHandlerMethod { get; }
        public abstract OverriddenMethod ArgumentParserMethod { get; }

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

        public Command(string identifier, AccessLevel requireAccessLevel = AccessLevel.Basic)
        {
            Identifier = identifier;
            RequireAccessLevel = requireAccessLevel;
            RequireGuild = CommandHandlerMethod == OverriddenMethod.GuildAsync || CommandHandlerMethod == OverriddenMethod.GuildSynchronous || ArgumentParserMethod == OverriddenMethod.GuildAsync || ArgumentParserMethod == OverriddenMethod.GuildSynchronous;
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

                if ((guildContext != null) && !guildContext.ChannelConfig.AllowCommands && context.UserAccessLevel < AccessLevel.Admin)
                {
                    var message = await context.Channel.SendEmbedAsync("This channel is a no-command-zone!", true);
                    Macros.ScheduleMessagesForDeletion(MESSAGE_DELETION_DELAY, message, context.Message);
                }
                else if (context.UserAccessLevel < RequireAccessLevel)
                {
                    await context.Channel.SendEmbedAsync($"You don't have permission to use this command! It requires `{RequireAccessLevel}` access, but you have only `{context.UserAccessLevel}` access!", true);
                }
                else if ((guildContext != null) && IsShitposting && !guildContext.ChannelConfig.AllowShitposting && context.UserAccessLevel < AccessLevel.Admin)
                {
                    var message = await context.Channel.SendEmbedAsync("Cannot use this command in this channel, as it is a no fun zone!", true);
                    Macros.ScheduleMessagesForDeletion(MESSAGE_DELETION_DELAY, message, context.Message);
                }
                else
                {
                    try
                    {
                        if (RequireAccessLevel >= AccessLevel.Admin)
                        {
                            await SettingsModel.SendAdminCommandUsedMessage(context, this);
                        }

                        ArgumentParseResult parseResult = await TryParseArguments(context);
                        if (!parseResult.Success)
                        {
                            await context.Channel.SendEmbedAsync("Argument Parsing Error", parseResult.Message, true);
                        }
                        else
                        {
                            await HandleCommand(context);
                        }
                    }
                    catch (Exception e)
                    {
                        string location;
                        if (guildContext != null)
                        {
                            location = Macros.GetMessageURL(guildContext.Message, guildContext.Guild.Id);
                        }
                        else
                        {
                            location = $"PM with {context.User.Mention} ({context.User.Username}#{context.User.Discriminator})";
                        }
                        await GuildChannelHelper.SendExceptionNotification(e, $"Error Executing Command `{CommandHandler.Prefix}{FullIdentifier}`, here: {location}");
                        await context.Channel.SendEmbedAsync("The command you attempted to execute unexpectedly threw an exception. Bot Dev is notified, stand by!");
                    }
                }
            }
            return commandMatch;
        }

        private async Task HandleCommand(CommandContext context)
        {
            GuildCommandContext guildContext = context as GuildCommandContext;

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

#pragma warning disable 1998
        protected async virtual Task HandleCommandAsync(CommandContext context)
        {
            throw new UnpopulatedCommandMethodException(CommandEnvironment.Base);
        }

        protected async virtual Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            throw new UnpopulatedCommandMethodException(CommandEnvironment.Guild);
        }
#pragma warning restore 1998

        protected virtual void HandleCommandSynchronous(CommandContext context)
        {
            throw new UnpopulatedCommandMethodException(CommandEnvironment.Base);
        }

        protected virtual void HandleCommandGuildSynchronous(GuildCommandContext context)
        {
            throw new UnpopulatedCommandMethodException(CommandEnvironment.Base);
        }

        #endregion
        #region ArgumentParsers

        private async Task<ArgumentParseResult> TryParseArguments(CommandContext context)
        {
            if (RequireGuild == false || RequireGuild == context.IsGuildContext)
            {
                GuildCommandContext guildContext = context as GuildCommandContext;

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
            else
            {
                return new ArgumentParseResult($"Command can not be executed in DM Channels, it needs to be executed in Guild Channels!");
            }
        }

#pragma warning disable 1998
        protected async virtual Task<ArgumentParseResult> TryParseArgumentsAsync(CommandContext context)
        {
            throw new UnpopulatedCommandMethodException(CommandEnvironment.Base);
        }

        protected async virtual Task<ArgumentParseResult> TryParseArgumentsGuildAsync(GuildCommandContext context)
        {
            throw new UnpopulatedCommandMethodException(CommandEnvironment.Base);
        }
#pragma warning restore 1998

        protected virtual ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            throw new UnpopulatedCommandMethodException(CommandEnvironment.Base);
        }

        protected virtual ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            throw new UnpopulatedCommandMethodException(CommandEnvironment.Base);
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
                    return CommandHandler.Prefix + FullIdentifier + " " + string.Join(" ", args);
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
        public string Description = string.Empty;
        /// <summary>
        /// Optional remarks for a command
        /// </summary>
        public string Remarks = string.Empty;
        /// <summary>
        /// Optional Link to online documentation of the command
        /// </summary>
        public string Link = string.Empty;

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

        public void InitializeHelp(string description, CommandArgument[] arguments, string remarks = null, string helpLink = null)
        {
            Description = description;
            Remarks = remarks;
            Arguments = arguments;
            Link = helpLink;
        }

        #endregion
    }

    /// <summary>
    /// Contains parsing and help information for a command argument
    /// </summary>
    public struct CommandArgument
    {
        /// <summary>
        /// String identifier that represents the argument in syntax and help
        /// </summary>
        public string Identifier;
        /// <summary>
        /// Help text that provides information on usage of the argument
        /// </summary>
        public string Help;
        /// <summary>
        /// Wether the argument is optional or not
        /// </summary>
        public bool Optional;
        /// <summary>
        /// Wether multiple arguments are allowed or not
        /// </summary>
        public bool Multiple;

        /// <summary>
        /// Creates a new CommandArgument object
        /// </summary>
        /// <param name="identifier">String representation of the argument in syntax and help</param>
        /// <param name="help">Help text that provides information on usage of the argument</param>
        /// <param name="optional">Wether the argument is optional or not</param>
        /// <param name="multiple">Wether multiple arguments are allowed or not</param>
        public CommandArgument(string identifier, string help, bool optional = false, bool multiple = false)
        {
            Identifier = identifier;
            Help = help;
            Optional = optional;
            Multiple = multiple;
        }

        public override string ToString()
        {
            string result = Identifier;
            if (Multiple)
            {
                result = $"[{result}]";
            }

            if (Optional)
            {
                result = $"({result})";
            }
            else
            {
                result = $"<{result}>";
            }
            return result;
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
        public ArgumentParseResult(CommandArgument argument)
        {
            Message = $"*`{argument}`*: Failed to parse!";
        }

        /// <summary>
        /// Creates a detailed ArgumentParseResult based on a CommandArgument
        /// </summary>
        /// <param name="argument">Command Argument which could not be parsed</param>
        /// <param name="errormessage">Error message text that explains why parsing failed</param>
        public ArgumentParseResult(CommandArgument argument, string errormessage)
        {
            Message = $"*`{argument}`*: {errormessage}";
        }
    }

    public class UnpopulatedCommandMethodException : Exception
    {
        private CommandEnvironment environment;

        public override string Message => $"The Commandhandler for {environment} has not been overridden!";

        public UnpopulatedCommandMethodException(CommandEnvironment environment) : base()
        {
            this.environment = environment;
        }
    }

    public enum CommandEnvironment
    {
        Base,
        Guild
    }
}

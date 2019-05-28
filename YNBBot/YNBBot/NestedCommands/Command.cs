using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.NestedCommands
{
    abstract class Command
    {
        #region Definition

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
        public abstract string Identifier { get; }

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
                    if (value[value.Length - 1].Multiple)
                    {
                        MaxArgCnt = 1000;
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
            BasicAsync,
            GuildAsync,
            BasicSynchronous,
            GuildSynchronous,
        }

        #endregion
        #region Constructors

        public Command()
        {
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

                GuildCommandContext guildContext = context as GuildCommandContext;

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
                        await context.Channel.SendEmbedAsync(Macros.EmbedFromException(e));
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

        public void InitiateFullIdentifier(string parentFullIdentifier)
        {
            if (string.IsNullOrEmpty(parentFullIdentifier))
            {
                FullIdentifier = Identifier;
            }
            else
            {
                FullIdentifier = parentFullIdentifier + " " + Identifier;
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

    public struct CommandArgument
    {
        public string Identifier;
        public string Help;
        public bool Optional;
        public bool Multiple;

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

    public class ArgumentParseResult
    {
        public bool Success { get; private set; } = false;
        public string Message { get; private set; }

        public static readonly ArgumentParseResult DefaultNoArguments = new ArgumentParseResult("No arguments given");
        public static readonly ArgumentParseResult SuccessfullParse = new ArgumentParseResult("Successful parse!");

        static ArgumentParseResult()
        {
            DefaultNoArguments.Success = true;
            SuccessfullParse.Success = true;
        }

        public ArgumentParseResult(string errormessage)
        {
            Message = errormessage;
        }

        public ArgumentParseResult(CommandArgument argument)
        {
            Message = $"*`{argument}`*: Failed to parse!";
        }

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

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.NestedCommands
{
    class CommandFamily
    {
        #region Fields, Properties

        /// <summary>
        /// The parent family this family is attached to
        /// </summary>
        public CommandFamily ParentFamily { get; private set; }
        /// <summary>
        /// The index depth that had been reached once parsing determined this family as a hit
        /// </summary>
        public int IndexDepth { get; private set; }

        private Dictionary<string, Command> commands = new Dictionary<string, Command>();

        /// <summary>
        /// List of all commands contained in this family
        /// </summary>
        public ICollection<Command> Commands => commands.Values;

        private Dictionary<string, CommandFamily> nestedFamilies = new Dictionary<string, CommandFamily>();

        /// <summary>
        /// List of all families nested in this family
        /// </summary>
        public ICollection<CommandFamily> NestedFamilies => nestedFamilies.Values;

        /// <summary>
        /// Identifier used for parsing commands
        /// </summary>
        public string Identifier { get; private set; }

        public string Description { get; private set; } = "";

        /// <summary>
        /// Full identifier (includes parent families)
        /// </summary>
        public string FullIdentifier { get; private set; }

        #endregion
        #region Constructors

        /// <summary>
        /// Creates a new base command family
        /// </summary>
        public CommandFamily()
        {
            Identifier = string.Empty;
            IndexDepth = 0;
        }

        /// <summary>
        /// Creates a new command family with identifier and parent
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="parent"></param>
        public CommandFamily(string identifier, CommandFamily parent, string description = "")
        {
            Identifier = identifier;
            parent.TryAddCommandFamily(this);
            Description = description;
        }

        #endregion
        #region Command and Family Organisation

        /// <summary>
        /// Counts all commands in this and all nested families that fit the filters
        /// </summary>
        /// <param name="isGuildContext">Filter for the context being a guild or not</param>
        /// <param name="accessLevel">Filter for minimum accesslevel</param>
        /// <returns>Number of commands matching the filter</returns>
        public int CommandCount(bool isGuildContext, AccessLevel accessLevel)
        {
            int result = 0;
            foreach (CommandFamily family in NestedFamilies)
            {
                result += family.CommandCount(isGuildContext, accessLevel);
            }

            foreach (Command command in Commands)
            {
                if (!(!isGuildContext && command.RequireGuild) && accessLevel >= command.RequiredAccessLevel)
                {
                    result++;
                }
            }

            return result;
        }

        /// <summary>
        /// Counts all commands in this and all nested families that fit the filters
        /// </summary>
        /// <returns>Number of commands matching the filter</returns>
        public int CommandCount(CommandContext context, GuildCommandContext guildContext)
        {
            

            int result = 0;
            foreach (CommandFamily family in NestedFamilies)
            {
                result += family.CommandCount(context, guildContext);
            }

            foreach (Command command in Commands)
            {
                if (command.PreconditionCheck(context, guildContext, out _))
                {
                    result++;
                }
            }

            return result;
        }

        /// <summary>
        /// Attempts to add a command to this family
        /// </summary>
        /// <param name="command">Command to add</param>
        /// <returns>Wether the command could be added or not</returns>
        public bool TryAddCommand(Command command)
        {
            if (!commands.ContainsKey(command.Identifier) && command.ParentFamily == null)
            {
                commands.Add(command.Identifier, command);
                command.RegisterParent(this);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to add a command family to this family
        /// </summary>
        /// <param name="family">Family to add</param>
        /// <returns>Wether the family could be added or not</returns>
        public bool TryAddCommandFamily(CommandFamily family)
        {
            if (!nestedFamilies.ContainsKey(family.Identifier) && family.ParentFamily == null && family.NestedFamilies.Count == 0 && family.Commands.Count == 0)
            {
                nestedFamilies.Add(family.Identifier, family);

                family.RegisterParent(this);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Registers the parent of this family
        /// </summary>
        /// <param name="parent">Parent command family</param>
        private void RegisterParent(CommandFamily parent)
        {
            ParentFamily = parent;
            IndexDepth = parent.IndexDepth + 1;
            if (string.IsNullOrEmpty(parent.FullIdentifier))
            {
                FullIdentifier = Identifier;
            }
            else
            {
                FullIdentifier = parent.FullIdentifier + " " + Identifier;
            }
        }

        #endregion
        #region Parsing

        /// <summary>
        /// Traverses nested families to match to an argument indexarray
        /// </summary>
        /// <param name="args">Arguments to match the family to</param>
        /// <param name="matchedCommands">List of command results</param>
        /// <param name="matchedFamily">Matched command family</param>
        /// <returns>True, if atleast one command or one command family could be matched</returns>
        public bool TryFindFamilyOrCommand(ref IndexArray<string> args, ref List<Command> matchedCommands, ref CommandFamily matchedFamily)
        {
            string argument = args.First;

            bool matchingFamilyButNotEnoughArgs = false;

            if (nestedFamilies.TryGetValue(argument, out CommandFamily potentialMatch))
            {
                matchedFamily = potentialMatch;
                if (args.Index + 1 < args.TotalCount)
                {
                    // The current argument matches the family, and there are more arguments left to continue parsing
                    args.Index++;
                    bool success = potentialMatch.TryFindFamilyOrCommand(ref args, ref matchedCommands, ref potentialMatch);
                    if (success)
                    {
                        return true;
                    }
                    else
                    {
                        args.Index--;
                    }
                }
                else
                {
                    // we found a matching family, but can not continue parsing because no arguments left to identify commands in that family
                    matchingFamilyButNotEnoughArgs = true;
                }
            }

            int remainingArgs = args.TotalCount - args.Index - 1;

            if (commands.TryGetValue(argument, out Command command))
            {
                switch (command.CheckCommandMatch(args))
                {
                    case Command.CommandMatchResult.IdentifiersMatch:
                        matchedCommands.Add(command);
                        break;
                    case Command.CommandMatchResult.CompleteMatch:
                        matchedCommands.Add(command);
                        return true;
                }
            }

            return matchingFamilyButNotEnoughArgs;
        }

        #endregion
    }
}

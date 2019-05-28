using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.NestedCommands
{
    class CommandFamily
    {
        private List<Command> commands = new List<Command>();

        public IReadOnlyList<Command> Commands => commands.AsReadOnly();

        private List<CommandFamily> nestedFamilies = new List<CommandFamily>();

        public IReadOnlyList<CommandFamily> NestedFamilies => nestedFamilies.AsReadOnly();

        public bool TryAddCommand(Command value)
        {
            if (!commands.Contains(value))
            {
                commands.Add(value);
                value.InitiateFullIdentifier(FullIdentifier);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryAddCommandFamily(CommandFamily value)
        {
            if (!nestedFamilies.Contains(value))
            {
                nestedFamilies.Add(value);

                value.SetFullIdentifier(FullIdentifier);

                return true;
            }
            else
            {
                return false;
            }
        }

        public string Identifier { get; private set; }

        public string FullIdentifier
        {
            get;
            private set;
        }

        private void SetFullIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                FullIdentifier = Identifier;
            }
            else
            {
                FullIdentifier = value + " " + Identifier;
            }
        }

        public CommandFamily(string identifier)
        {
            Identifier = identifier;
        }

        public CommandFamily(string identifier, CommandFamily parent)
        {
            Identifier = identifier;
            parent.TryAddCommandFamily(this);
        }

        public async Task<bool> ParseOn(CommandContext context)
        {
            int index = context.Args.Index;

            string argument = context.Args.First;

            CommandFamily identifierMatchButNotEnoughArgs = null;

            foreach (CommandFamily family in nestedFamilies)
            {
                if (family.Identifier == argument)
                {
                    if (context.Args.Index + 1 < context.Args.TotalCount)
                    {
                        context.Args.Index++;
                        bool success = await family.ParseOn(context);
                        context.Args.Index--;
                        if (success)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        identifierMatchButNotEnoughArgs = family;
                    }
                }
            }

            int remainingArgs = context.Args.TotalCount - index - 1;

            foreach (Command command in commands)
            {
                switch (await command.TryHandleCommand(context))
                {
                    case Command.CommandMatchResult.CompleteMatch:
                        return true;
                    case Command.CommandMatchResult.IdentifiersMatch:
                        await context.Channel.SendEmbedAsync($"The command that matched requires more arguments: `{command.Syntax}`", true);
                        return true;
                }
            }

            if (identifierMatchButNotEnoughArgs != null)
            {
                await context.Channel.SendEmbedAsync($"Use `{CommandHandler.Prefix}help {identifierMatchButNotEnoughArgs.FullIdentifier}` for a list of all commands in the command family `{identifierMatchButNotEnoughArgs.FullIdentifier}`", true);
                return true;
            }

            return false;
        }

        public void FindCommandHelps(ref List<Command> helps, ref IndexArray<string> args, AccessLevel minAccessLevel)
        {
            int index = args.Index;

            if (args.Count > 0)
            {
                string argument = args.First;

                foreach (CommandFamily family in nestedFamilies)
                {
                    if (family.Identifier == argument && args.Index < args.TotalCount)
                    {
                        args.Index++;
                        family.FindCommandHelps(ref helps, ref args, minAccessLevel);
                        args.Index--;
                    }
                }

                foreach (Command command in commands)
                {
                    if (command.CheckCommandMatch(args, true) >= Command.CommandMatchResult.IdentifiersMatch && minAccessLevel >= command.RequireAccessLevel)
                    {
                        helps.Add(command);
                    }
                }
            }
            else
            {
                AllCommandHelps(ref helps, minAccessLevel);
            }
        }

        public void AllCommandHelps(ref List<Command> helps, AccessLevel minAccessLevel)
        {
            foreach (CommandFamily family in nestedFamilies)
            {
                family.AllCommandHelps(ref helps, minAccessLevel);
            }

            foreach (Command command in commands)
            {
                if (minAccessLevel >= command.RequireAccessLevel)
                {
                    helps.Add(command);
                }
            }
        }
    }
}

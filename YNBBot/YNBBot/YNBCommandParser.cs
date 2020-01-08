using BotCoreNET.BotVars;
using BotCoreNET.CommandHandling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace YNBBot
{
    class YNBCommandParser : BuiltInCommandParser
    {
        public override string CommandSyntax(string commandidentifier)
        {
            return $"{Prefix}{commandidentifier}";
        }

        public override string CommandSyntax(string commandidentifier, params Argument[] arguments)
        {
            if (arguments.Length == 0)
            {
                return $"{Prefix}{commandidentifier}";
            }
            else
            {
                return $"{Prefix}{commandidentifier} {arguments.Join(" ")}";
            }
        }

        public override string CommandSyntax(string commandidentifier, params string[] arguments)
        {
            if (arguments.Length == 0)
            {
                return $"{Prefix}{commandidentifier}";
            }
            else
            {
                return $"{Prefix}{commandidentifier} {arguments.Join(" ")}";
            }
        }

        public override bool IsPotentialCommand(string messageContent)
        {
            return messageContent.Length > Prefix.Length && messageContent.StartsWith(Prefix);
        }

        public override bool IsPotentialCommand(string messageContent, ulong guildId)
        {
            return IsPotentialCommand(messageContent);
        }

        public override ICommandContext ParseCommand(IGuildMessageContext guildContext)
        {
            return ParseCommand(guildContext as IMessageContext);
        }

        public override ICommandContext ParseCommand(IMessageContext dmContext)
        {
            string message = dmContext.Content.Substring(Prefix.Length);
            string commandIdentifier;
            string argSection;

            getCommandIdentifierAndArgSection(message, out commandIdentifier, out argSection);

            if (argSection.Length == 0)
            {
                CommandSearchResult searchResult = CommandCollection.TryFindCommand(commandIdentifier, 0, out Command command);
                return new CommandContext(command, searchResult, argSection, new IndexArray<string>(0));
            }
            else
            {
                var args = getArguments(argSection);
                CommandSearchResult searchResult = CommandCollection.TryFindCommand(commandIdentifier, args.Count, out Command command);
                return new CommandContext(command, searchResult, argSection, args);
            }
        }

        private void getCommandIdentifierAndArgSection(string message, out string commandIdentifier, out string argSection)
        {
            int index = message.IndexOf(character => { return char.IsWhiteSpace(character); });
            if (index == -1)
            {
                commandIdentifier = message;
                argSection = string.Empty;
            }
            else
            {
                commandIdentifier = message.Substring(0, index);
                argSection = message.Substring(index, message.Length - index);
            }
        }

        private IndexArray<string> getArguments(string argSection)
        {
            List<string> args = new List<string>(1);

            int argumentIndex = 0;
            bool inMultiWordArg = false;
            for (int i = 0; i < argSection.Length; i++)
            {
                char previous = getChar(argSection, i - 1);
                char next = getChar(argSection, i + 1);
                char current = argSection[i];
                if (current == '"')
                {
                    if (inMultiWordArg)
                    {
                        if (whiteSpaceOrNull(next))
                        {
                            args.Add(argSection.Substring(argumentIndex, i - argumentIndex));
                            argumentIndex = i + 2;
                            inMultiWordArg = false;
                        }
                    }
                    else if (whiteSpaceOrNull(previous))
                    {
                        argumentIndex = i + 1;
                        inMultiWordArg = true;
                    }
                }
                else if (!inMultiWordArg)
                {
                    if (whiteSpaceOrNull(current) && !whiteSpaceOrNull(previous) && previous != '"')
                    {
                        args.Add(argSection.Substring(argumentIndex, i - argumentIndex));
                    }
                    else if (!whiteSpaceOrNull(current) && whiteSpaceOrNull(previous))
                    {
                        argumentIndex = i;
                    }
                }
            }
            if (argumentIndex + 1 < argSection.Length)
            {
                args.Add(argSection.Substring(argumentIndex, argSection.Length - argumentIndex));
            }
            return new IndexArray<string>(args);
        }

        private static char getChar(string str, int index)
        {
            if (index < 0 || index >= str.Length)
            {
                return '\0';
            }
            return str[index];
        }

        private static bool whiteSpaceOrNull(char c)
        {
            return char.IsWhiteSpace(c) || c == '\0';
        }


        public override string RemoveArgumentsFront(int count, string argSection)
        {
            int args = 0;

            int argumentIndex = 0;
            bool inMultiWordArg = false;
            for (int i = 0; i < argSection.Length; i++)
            {
                char previous = getChar(argSection, i - 1);
                char next = getChar(argSection, i + 1);
                char current = argSection[i];
                if (current == '"')
                {
                    if (inMultiWordArg)
                    {
                        if (whiteSpaceOrNull(next))
                        {
                            args++;
                            inMultiWordArg = false;
                        }
                    }
                    else if (whiteSpaceOrNull(previous))
                    {
                        argumentIndex = i + 1;
                        inMultiWordArg = true;
                    }
                }
                else if (!inMultiWordArg)
                {
                    if (whiteSpaceOrNull(current) && !whiteSpaceOrNull(previous) && previous != '"')
                    {
                        args++;
                    }
                    else if (!whiteSpaceOrNull(current) && whiteSpaceOrNull(previous))
                    {
                        argumentIndex = i;
                    }
                }
                if (argumentIndex == i && count == args)
                {
                    return argSection.Substring(argumentIndex, argSection.Length - argumentIndex);
                }
            }
            return string.Empty;
        }
    }
}

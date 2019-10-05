using BotCoreNET.BotVars;
using BotCoreNET.CommandHandling;
using System;
using System.Collections.Generic;
using System.Text;

namespace YNBBot
{
    class YNBCommandParser : ICommandParser
    {
        internal string Prefix;
        public string CommandSyntax(string commandidentifier)
        {
            return $"{Prefix}{commandidentifier}";
        }

        public string CommandSyntax(string commandidentifier, params Argument[] arguments)
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

        public string CommandSyntax(string commandidentifier, params string[] arguments)
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

        public bool IsPotentialCommand(string messageContent)
        {
            return messageContent.Length > Prefix.Length && messageContent.StartsWith(Prefix);
        }

        public bool IsPotentialCommand(string messageContent, ulong guildId)
        {
            return IsPotentialCommand(messageContent);
        }

        public void OnBotVarSetup()
        {
            BotVarManager.GlobalBotVars.SubscribeToBotVarUpdateEvent(OnBotVarUpdated, "prefix");
        }

        private void OnBotVarUpdated(ulong guildId, BotVar botvar)
        {
            if (!string.IsNullOrWhiteSpace(botvar.String))
            {
                Prefix = botvar.String;
            }
        }

        public ICommandContext ParseCommand(IGuildMessageContext guildContext)
        {
            return ParseCommand(guildContext as IMessageContext);
        }

        public ICommandContext ParseCommand(IMessageContext dmContext)
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
                List<string> arguments = new List<string>();
                int lastArgStart = 0;
                bool inMultiWordArg = false;
                for (int i = 0; i < argSection.Length; i++)
                {
                    if (char.IsWhiteSpace(argSection[i]) && !inMultiWordArg)
                    {
                        if (lastArgStart < i - 1)
                        {
                            arguments.Add(argSection.Substring(lastArgStart, i - lastArgStart));
                        }
                        lastArgStart = i;
                    }
                    if (argSection[i] == '\"' && !inMultiWordArg)
                    {
                        if (inMultiWordArg)
                        {
                            bool nextCharValid;
                            if (i == argSection.Length - 1)
                            {
                                nextCharValid = true;
                            }
                            else
                            {
                                nextCharValid = char.IsWhiteSpace(argSection[i + 1]);
                            }
                            if (nextCharValid)
                            {
                                inMultiWordArg = false;
                                arguments.Add(argSection.Substring(lastArgStart, i - lastArgStart));
                                lastArgStart = i;
                            }
                        }
                        else
                        {
                            bool previousCharValid;
                            if (i == 0)
                            {
                                previousCharValid = true;
                            }
                            else
                            {
                                previousCharValid = char.IsWhiteSpace(argSection[i - 1]);
                            }
                            if (previousCharValid)
                            {
                                inMultiWordArg = true;
                                lastArgStart = i + 1;
                            }
                        }
                    }
                }
                CommandSearchResult searchResult = CommandCollection.TryFindCommand(commandIdentifier, arguments.Count, out Command command);
                return new CommandContext(command, searchResult, argSection, new IndexArray<string>(arguments));
            }
        }

        private static T[] Subset<T>(T[] array, int startIndex = 0, int length = 0)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Length == 0)
            {
                throw new ArgumentException($"Argument \"{nameof(array)}\" cannot be of length 0");
            }

            if (startIndex < 0 || startIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (length < 0 || length + startIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (length == 0)
            {
                length = array.Length - startIndex;
            }

            T[] result = new T[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = array[i + startIndex];
            }

            return result;
        }

        private static void getCommandIdentifierAndArgSection(string message, out string commandIdentifier, out string argSection)
        {
            string[] potentialCommandIdentifier = new string[2];

            int arrayPointer = 0;
            for (int i = 0; i < message.Length; i++)
            {
                if (message[i] == ' ')
                {
                    potentialCommandIdentifier[arrayPointer] = message.Substring(0, i);
                    arrayPointer++;
                    if (arrayPointer == potentialCommandIdentifier.Length)
                    {
                        break;
                    }
                }
            }

            if (potentialCommandIdentifier[1] == null)
            {
                commandIdentifier = potentialCommandIdentifier[0];
            }
            else
            {
                if (CommandCollection.TryFindCommand(potentialCommandIdentifier[1], out _))
                {
                    commandIdentifier = potentialCommandIdentifier[1];
                }
                else
                {
                    commandIdentifier = potentialCommandIdentifier[0];
                }
            }


            if (commandIdentifier.Length + 1 > message.Length)
            {
                argSection = message.Substring(commandIdentifier.Length + 1);
            }
            else
            {
                argSection = string.Empty;
            }
        }

        public string RemoveArgumentsFront(int count, string argSection)
        {
            int lastArgStart = 0;
            bool inMultiWordArg = false;
            for (int i = 0; i < argSection.Length; i++)
            {
                if (char.IsWhiteSpace(argSection[i]) && !inMultiWordArg)
                {
                    if (lastArgStart < i - 1)
                    {
                        count--;
                        if (count == 0)
                        {
                            return argSection.Substring(i, argSection.Length - i);
                        }
                    }
                    lastArgStart = i;
                }
                if (argSection[i] == '\"' && !inMultiWordArg)
                {
                    if (inMultiWordArg)
                    {
                        bool nextCharValid;
                        if (i == argSection.Length - 1)
                        {
                            nextCharValid = true;
                        }
                        else
                        {
                            nextCharValid = char.IsWhiteSpace(argSection[i + 1]);
                        }
                        if (nextCharValid)
                        {
                            inMultiWordArg = false;
                            count--;
                            if (count == 0)
                            {
                                return argSection.Substring(i, argSection.Length - i);
                            }
                            lastArgStart = i;
                        }
                    }
                    else
                    {
                        bool previousCharValid;
                        if (i == 0)
                        {
                            previousCharValid = true;
                        }
                        else
                        {
                            previousCharValid = char.IsWhiteSpace(argSection[i - 1]);
                        }
                        if (previousCharValid)
                        {
                            inMultiWordArg = true;
                            lastArgStart = i + 1;
                        }
                    }
                }
            }
            return string.Empty;
        }
    }
}

using Discord;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YNBBot.NestedCommands
{
    internal class HelpCommand : Command
    {
        public const string SUMMARY = "Provides help for specific commands and lists all available commands.";
        public const string REMARKS = "Lists all available commands if no arguments are provided";
        public const string LINK = "https://docs.google.com/document/d/1IdTQoq2l9YhF5Tlj5lBYz5Zcz56NQEgL3Hg5Dg2RyWs/edit#heading=h.7oz4bpjtg943";
        public static readonly Argument[] ARGS = new Argument[] { new Argument("Command Identifier", "A list of all keywords that identify the command(s) you want the help text for.", true, true) };

        private IndexArray<string> CommandKeys;

        public HelpCommand(string identifier) : base(identifier, OverriddenMethod.BasicSynchronous, OverriddenMethod.BasicAsync, arguments: ARGS, summary:SUMMARY, remarks:REMARKS, helplink:LINK)
        {
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            CommandKeys = (IndexArray<string>)context.Args.Clone();
            return ArgumentParseResult.SuccessfullParse;
        }

        private readonly EmbedFieldBuilder syntaxHelpField = new EmbedFieldBuilder() { Name = "Syntax Help", Value = "`key` = command identifier\n`<key>` = required argument\n`(key)` = optional argument\n`[key]` = multiple arguments possible" };

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            if (CommandKeys.Count > 0)
            {
                List<Command> matchedCommands = new List<Command>();
                CommandFamily matchedFamily = null;
                CommandHandler.BaseFamily.TryFindFamilyOrCommand(ref CommandKeys, ref matchedCommands, ref matchedFamily);
                if (matchedCommands.Count > 0 || matchedFamily != null)
                {
                    if (matchedCommands.Count > 0)
                    {
                        foreach (Command command in matchedCommands)
                        {
                            await CommandHelper.SendCommandHelp(context, command);
                        }
                    }
                    else if (matchedFamily != null)
                    {
                        await CommandHelper.SendCommandCollectionHelp(context, matchedFamily);
                    }
                }
                else
                {
                    await context.Channel.SendEmbedAsync("No matching commands found!", true);
                }
            }
            else
            {
                await CommandHelper.SendCommandCollectionHelp(context, CommandHandler.BaseFamily, "List of all Commands");
            }
        }

        private static async Task handleFamilyHelp(CommandContext context, CommandFamily matchedFamily)
        {
            string channelInformation;

            if (GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext))
            {
                if (guildContext.ChannelConfig.AllowShitposting)
                {
                    channelInformation = "Guild (shitposting)";
                }
                else
                {
                    channelInformation = "Guild";
                }
            }
            else
            {
                channelInformation = "Anywhere";
            }

            string embedTitle;
            if (matchedFamily == CommandHandler.BaseFamily)
            {
                embedTitle = $"List of all Commands";
            }
            else
            {
                embedTitle = $"Command Collection \"{CommandHandler.Prefix + string.Join(" ", context.Args)}\"";
            }
            string embedDescription = $"This list is generated based on your AccessLevel (`{context.UserAccessLevel}`), and the current channel context (`{channelInformation}`)\n{matchedFamily.Description}";

            List<EmbedFieldBuilder> commandFields = new List<EmbedFieldBuilder>();

            foreach (CommandFamily family in matchedFamily.NestedFamilies)
            {
                int availableCommands = family.CommandCount(context.IsGuildContext, context.UserAccessLevel);
                if (availableCommands > 0)
                {
                    commandFields.Add(Macros.EmbedField($"(Command Collection) {CommandHandler.Prefix + family.FullIdentifier}", $"{availableCommands} available commands.{(string.IsNullOrEmpty(family.Description) ? string.Empty : $" {family.Description}.")} Use `{CommandHandler.Prefix}help {family.FullIdentifier}` to see a summary of commands in this command family!", true));
                }
            }

            foreach (Command command in matchedFamily.Commands)
            {
                if (!(!context.IsGuildContext && command.RequireGuild) && context.UserAccessLevel >= command.RequiredAccessLevel)
                {
                    commandFields.Add(Macros.EmbedField(command.Syntax, command.HasDescription ? command.Description : "No Description Available", true));
                }
            }

            if (commandFields.Count > 0)
            {
                await context.Channel.SendSafeEmbedList(embedTitle, commandFields, embedDescription);
            }
            else
            {
                await context.Channel.SendEmbedAsync("You don't have sufficient access level to use any of the matched commands!", true);
            }
        }

        private async Task sendSpecificCommandhelp(CommandContext context, Command command)
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Title = $"Help For `{command.PrefixIdentifier}`",
                Color = Var.BOTCOLOR,
                Description = command.HasDescription ? command.Description : "No Description Available",
            };
            if (command.HasLink)
            {
                embed.AddField("Documentation", $"[Online Documentation for `{command.PrefixIdentifier}`]({command.Link})");
            }
            if (command.HasRemarks)
            {
                embed.AddField("Remarks", command.Remarks);
            }

            if (command.Arguments.Length > 0)
            {
                string[] argumentInfo = new string[command.Arguments.Length];
                for (int i = 0; i < command.Arguments.Length; i++)
                {
                    Argument argument = command.Arguments[i];
                    argumentInfo[i] = $"**{UnicodeEmoteService.TraingleRight}`{argument}`**\n{argument.Help}";
                }

                embed.AddField("Syntax", Macros.MultiLineCodeBlock(command.Syntax) + "\n" + string.Join("\n\n", argumentInfo));
                embed.AddField(syntaxHelpField);
            }
            else
            {
                embed.AddField("Syntax", Macros.MultiLineCodeBlock(command.Syntax));
            }

            string executionLocation;
            if (command.RequireGuild)
            {
                if (command.IsShitposting)
                {
                    executionLocation = "Guild channels (Shitposting)";
                }
                else
                {
                    executionLocation = "Guild channels";
                }
            }
            else
            {
                executionLocation = "Anywhere";
            }
            embed.AddField("Execution Requirements", $"Required Access Level: `{command.RequiredAccessLevel}`\nRequired Execution Location `{executionLocation}`");
            await context.Channel.SendEmbedAsync(embed);
        }
    }
}

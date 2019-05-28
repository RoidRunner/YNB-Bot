using Discord;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YNBBot.NestedCommands
{
    internal class HelpCommand : Command
    {
        public override string Identifier => "help";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicSynchronous;

        private IndexArray<string> CommandKeys;

        public HelpCommand()
        {
            List<CommandArgument> arguments = new List<CommandArgument>();
            arguments.Add(new CommandArgument("Command Identifier", "A list of all keywords that identify the command(s) you want the help text for.", true, true));
            InitializeHelp("Provides help for specific commands and lists all available commands.", arguments.ToArray(),
                remarks: "Lists all available commands if no arguments are provided");
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
                if (CommandHandler.BaseFamily.TryFindFamilyOrCommand(ref CommandKeys, ref matchedCommands, ref matchedFamily))
                {
                    if (matchedCommands.Count > 0)
                    {
                        foreach (Command command in matchedCommands)
                        {
                            await sendSpecificCommandhelp(context, command);
                        }
                    }
                    else if (matchedFamily != null)
                    {
                        await handleFamilyHelp(context, matchedFamily);
                    }
                }
            }
            else
            {
                await handleFamilyHelp(context, CommandHandler.BaseFamily);
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

            string embedTitle = $"Matching Commands for \"{CommandHandler.Prefix + string.Join(" ", context.Args)}\"";
            string embedDescription = $"This list is generated based on your AccessLevel (`{context.UserAccessLevel}`), and the current channel context (`{channelInformation}`)";

            List<EmbedFieldBuilder> commandFields = new List<EmbedFieldBuilder>();

            foreach (CommandFamily family in matchedFamily.NestedFamilies)
            {
                int availableCommands = family.CommandCount(context.IsGuildContext, context.UserAccessLevel);
                if (availableCommands > 0)
                {
                    commandFields.Add(Macros.EmbedField($"(Command Family) {CommandHandler.Prefix + family.FullIdentifier}", $"{availableCommands} available commands. Use `{CommandHandler.Prefix}help {family.FullIdentifier}` to see a summary of commands in this command family!", true));
                }
            }

            foreach (Command command in matchedFamily.Commands)
            {
                if (!(!context.IsGuildContext && command.RequireGuild) && context.UserAccessLevel >= command.RequireAccessLevel)
                {
                    commandFields.Add(Macros.EmbedField(command.Syntax, command.Description, true));
                }
            }

            await context.Channel.SendSafeEmbedList(embedTitle, commandFields, embedDescription);
        }

        private async Task sendSpecificCommandhelp(CommandContext context, Command command)
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Title = $"Help For `{command.PrefixIdentifier}`",
                Color = Var.BOTCOLOR,
                Description = command.Description,
            };
            if (command.HasRemarks)
            {
                embed.AddField("Remarks", command.Remarks);
            }

            if (command.Arguments.Length > 0)
            {
                string[] argumentInfo = new string[command.Arguments.Length];
                for (int i = 0; i < command.Arguments.Length; i++)
                {
                    CommandArgument argument = command.Arguments[i];
                    argumentInfo[i] = $"`{argument}`\n{argument.Help}";
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
            embed.AddField("Execution Requirements", $"Required Access Level: `{command.RequireAccessLevel}`\nRequired Execution Location `{executionLocation}`");
            if (command.HasLink)
            {
                embed.AddField("Documentation", $"[Online Documentation for `{command.PrefixIdentifier}`]({command.Link})");
            }
            await context.Channel.SendEmbedAsync(embed);
        }
    }
}

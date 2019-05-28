using Discord;
using System;
using System.Collections.Generic;
using System.Text;
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
            List<Command> helpList = new List<Command>();

            CommandHandler.BaseFamily.FindCommandHelps(ref helpList, ref CommandKeys, context.UserAccessLevel);

            if (helpList.Count == 0)
            {
                await context.Channel.SendEmbedAsync("No commands matching the criteria were found!");
                return;
            }

            bool GenerateList = CommandKeys.Count == 0 || helpList.Count > 2;


            if (GenerateList)
            {
                string title;
                if (CommandKeys.Count == 0)
                {
                    title = $"List of all commands available";
                }
                else
                {
                    title = $"List of all commands matching {string.Join(" ", context.Args)}";
                }
                string channel;

                GuildCommandContext guildContext = context as GuildCommandContext;

                if (context.IsGuildContext)
                {
                    if (guildContext.ChannelInfo.AllowShitposting)
                    {
                        channel = "A guild channel";
                    }
                    else
                    {
                        channel = "A guild channel that does not allow shitposting";
                    }
                }
                else
                {
                    channel = "Any channel or PM with the bot";
                }
                string description = $"This list is generated based on your AccessLevel (`{context.UserAccessLevel}`), and the current channel (`{channel}`)";

                List<EmbedField> embeds = new List<EmbedField>();

                foreach (Command command in helpList)
                {
                    if ((!command.RequireGuild || command.RequireGuild == context.IsGuildContext))
                    {
                        if (command.HasLink)
                        {
                            embeds.Add(new EmbedField(command.Syntax, $"{command.Description}\n[Online Documentation for `{command.PrefixIdentifier}`]({command.Link})", true));
                        }
                        else
                        {
                            embeds.Add(new EmbedField(command.Syntax, command.Description, true));
                        }
                    }
                }

                if (embeds.Count == 0)
                {
                    await context.Channel.SendEmbedAsync("No commands matching the criteria were found!", true);
                }
                else
                {
                    await context.Channel.SendSafeEmbedList(title, embeds, description);
                }
            }
            else
            {
                foreach (Command command in helpList)
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
    }
}

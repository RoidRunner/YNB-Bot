using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.NestedCommands
{
    static class CommandHelper
    {
        #region Command Collection Help

        public static Task SendCommandCollectionHelp(CommandContext context, CommandFamily collection, string embedTitle = null, ISocketMessageChannel outputchannel = null)
        {
            EmbedBuilder embed = GetCommandCollectionEmbed(context, collection, embedTitle);
            if (outputchannel == null)
            {
                outputchannel = context.Channel;
            }
            return outputchannel.SendEmbedAsync(embed);
        }

        public static EmbedBuilder GetCommandCollectionEmbed(CommandContext context, CommandFamily collection, string embedTitle = null)
        {
            string contextType = "";
            if (GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext))
            {
                if (guildContext.ChannelConfig.AllowShitposting)
                {
                    contextType = "Guild; Shitposting Enabled";
                }
                else
                {
                    contextType = "Guild";
                }
            }
            else
            {
                contextType = "PM";
            }

            if (string.IsNullOrEmpty(embedTitle))
            {
                embedTitle = $"Command Collection \"{CommandHandler.Prefix}{collection.FullIdentifier}\"";
            }
            string embedDesc = "This list only shows commands where all preconditions have been met!";

            List<EmbedFieldBuilder> helpFields = new List<EmbedFieldBuilder>();

            foreach (CommandFamily family in collection.NestedFamilies)
            {
                int availableCommands = family.CommandCount(context, guildContext);
                if (availableCommands > 0)
                {
                    helpFields.Add(Macros.EmbedField($"[Command Collection] {CommandHandler.Prefix + family.FullIdentifier}", $"{availableCommands} available commands.{(string.IsNullOrEmpty(family.Description) ? string.Empty : $" {family.Description}.")} Use `{CommandHandler.Prefix}help {family.FullIdentifier}` to see a summary of commands in this command family!", true));
                }
            }

            foreach (Command command in collection.Commands)
            {
                if (command.PreconditionCheck(context, guildContext, out _))
                {
                    helpFields.Add(Macros.EmbedField(command.Syntax, command.HasDescription ? command.Description : "No Description Available", true));
                }
            }

            if (helpFields.Count == 0)
            {
                embedDesc = "No command's precondition has been met!";
                return new EmbedBuilder() { Title = embedTitle, Description = embedDesc, Color = Var.ERRORCOLOR, Footer = new EmbedFooterBuilder() { Text = "Context: " + contextType } };
            }
            else
            {
                return new EmbedBuilder() { Title = embedTitle, Description = embedDesc, Color = Var.BOTCOLOR, Footer = new EmbedFooterBuilder() { Text = "Context: " + contextType }, Fields = helpFields };
            }
        }

        #endregion

        private static readonly EmbedFieldBuilder syntaxHelpField = new EmbedFieldBuilder() { Name = "Syntax Help", Value = "`key` = command identifier\n`<key>` = required argument\n`(key)` = optional argument\n`[key]` = multiple arguments possible" };

        public static Task SendCommandHelp(CommandContext context, Command command, ISocketMessageChannel outputchannel = null)
        {
            EmbedBuilder embed = GetCommandEmbed(context, command);
            if (outputchannel == null)
            {
                outputchannel = context.Channel;
            }
            return outputchannel.SendEmbedAsync(embed);
        }

        public static EmbedBuilder GetCommandEmbed(CommandContext context, Command command)
        {
            GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext);
            if (command.PreconditionCheck(context, guildContext, out List<string> failedPreconditionChecks))
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

                    string contextType;
                    if (command.RequireGuild)
                    {
                        if (command.IsShitposting)
                        {
                            contextType = "Guild; Shitposting Enabled";
                        }
                        else
                        {
                            contextType = "Guild";
                        }
                    }
                    else
                    {
                        contextType = "PM";
                    }
                    embed.AddField("Execution Requirements", $"\nRequired Execution Location `{contextType}`\n{command.Preconditions.Join("\n")}");
                    embed.Footer = new EmbedFooterBuilder() { Text = "Context: " + contextType };
                }
                else
                {
                    embed.AddField("Syntax", Macros.MultiLineCodeBlock(command.Syntax));
                }

                return embed;
            }
            else
            {
                return new EmbedBuilder() {
                    Title = $"Help For `{command.PrefixIdentifier}`",
                    Description = $"**Failed precondition check!**\n{failedPreconditionChecks.Join("\n")}",
                    Color = Var.ERRORCOLOR
                };
            }
        }
    }
}

using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.NestedCommands
{
    #region send

    class SendEmbedCommand : Command
    {
        public override string Identifier => "send";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicSynchronous;

        private SocketTextChannel channel;
        string messageContent = string.Empty;
        private EmbedBuilder embed;

        public SendEmbedCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;

            CommandArgument[] arguments = new CommandArgument[2];
            arguments[0] = new CommandArgument("Channel", ArgumentParsingHelper.GENERIC_PARSED_CHANNEL);
            arguments[1] = new CommandArgument("EmbedJSON", "The embed, formatted as a JSON", multiple: true);
            InitializeHelp("Sends a fully featured embed to a guild text channel", arguments, "Good tool for creating JSON formatted embeds: [MagicBots](https://discord.club/embedg/)");
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            if (!ArgumentParsingHelper.TryParseGuildTextChannel(context, context.Args[0], out channel))
            {
                return new ArgumentParseResult(Arguments[0], "Failed to parse to a guild text channel!");
            }

            if (context.Message.Content.Length > FullIdentifier.Length + context.Args.First.Length + 2)
            {
                string embedText = context.Message.Content.Substring(FullIdentifier.Length + context.Args[0].Length + 2).Replace("[3`]", "```");

                JSONObject json = new JSONObject(embedText);

                return EmbedHelper.TryParseEmbedFromJSONObject(json, out embed, out messageContent);
            }
            else
            {
                embed = null;
                return new ArgumentParseResult("Internal Error: " + Macros.GetCodeLocation());
            }
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            if (embed == null && messageContent != null)
            {
                await channel.SendMessageAsync(messageContent);
            }
            else if (embed != null && messageContent == null)
            {
                await channel.SendEmbedAsync(embed);
            }
            else if (embed != null && messageContent != null)
            {
                await channel.SendMessageAsync(text: messageContent, embed: embed.Build());
            }
            else
            {
                await context.Channel.SendEmbedAsync("The json you provided had no information or could not be parsed!", true);
                return;
            }
            await context.Channel.SendEmbedAsync("Done. Check it out here: " + channel.Mention);
        }
    }

    #endregion
    #region preview

    class PreviewEmbedCommand : Command
    {
        public override string Identifier => "preview";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicSynchronous;

        string messageContent = string.Empty;
        private EmbedBuilder embed;

        public PreviewEmbedCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;

            CommandArgument[] arguments = new CommandArgument[1];
            arguments[0] = new CommandArgument("EmbedJSON", "The embed, formatted as a JSON", multiple: true);
            InitializeHelp("Previews an embed in the channel the command is issued from", arguments, "Good tool for creating JSON formatted embeds: [MagicBots](https://discord.club/embedg/)");
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            if (context.Message.Content.Length > FullIdentifier.Length + 1)
            {
                string embedText = context.Message.Content.Substring(FullIdentifier.Length + 1).Replace("[3`]", "```");

                JSONObject json = new JSONObject(embedText);

                return EmbedHelper.TryParseEmbedFromJSONObject(json, out embed, out messageContent);
            }
            else
            {
                embed = null;
                return new ArgumentParseResult("Internal Error: " + Macros.GetCodeLocation());
            }
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            if (embed == null && messageContent != null)
            {
                await context.Channel.SendMessageAsync(messageContent);
            }
            else if (embed != null && messageContent == null)
            {
                await context.Channel.SendEmbedAsync(embed);
            }
            else if (embed != null && messageContent != null)
            {
                await context.Channel.SendMessageAsync(text: messageContent, embed: embed.Build());
            }
            else
            {
                await context.Channel.SendEmbedAsync("The json you provided had no information or could not be parsed!", true);
            }
        }
    }

    #endregion
    #region get

    class GetEmbedCommand : Command
    {
        public override string Identifier => "get";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicAsync;

        public GetEmbedCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;

            CommandArgument[] arguments = new CommandArgument[2];
            arguments[0] = new CommandArgument("MessageLink", "A discord message link to select the source");
            arguments[1] = new CommandArgument("Options", $"Command execution options. Available are:\n`{ExecutionOptions.pretty}` = Include some nice formatting in the embed JSON\n" +
                $"`{ExecutionOptions.remove}` = Remove the source message after retrieving the embed", true, true);
            InitializeHelp("Formats a JSON from a given message, including embeds", arguments);
        }

        private SocketGuild guild;
        private SocketTextChannel channel;
        private IMessage message;
        private List<ExecutionOptions> options = new List<ExecutionOptions>();

        protected override async Task<ArgumentParseResult> TryParseArgumentsAsync(CommandContext context)
        {
            if (!context.Args[0].StartsWith("https://discordapp.com/channels/") || context.Args[0].Length < 40)
            {
                return new ArgumentParseResult(Arguments[0], "Not a valid message link! Failed Startswith or length test");
            }

            string[] messageIdentifiers = context.Args[0].Substring(32).Split('/');

            if (messageIdentifiers.Length != 3)
            {
                return new ArgumentParseResult(Arguments[0], "Not a valid message link! Failed split test");
            }

            if (!ulong.TryParse(messageIdentifiers[0], out ulong guildId) || !ulong.TryParse(messageIdentifiers[1], out ulong channelId) || !ulong.TryParse(messageIdentifiers[2], out ulong messageId))
            {
                return new ArgumentParseResult(Arguments[0], "Not a valid message link! Failed id parse test");
            }

            guild = Var.client.GetGuild(guildId);

            if (guild != null)
            {
                channel = guild.GetTextChannel(channelId);

                if (channel != null)
                {
                    message = await channel.GetMessageAsync(messageId);

                    if (message == null)
                    {
                        return new ArgumentParseResult(Arguments[0], "Found correct guild and correct channel, but not correct message! Has the message been deleted?");
                    }
                }
                else
                {
                    return new ArgumentParseResult(Arguments[0], "Found correct guild, but not the channel!");
                }
            }
            else
            {
                return new ArgumentParseResult(Arguments[0], "Could not find the correct guild!");
            }

            options.Clear();
            if (context.Args.Count > 1)
            {

                context.Args.Index++;

                bool parseError = false;
                foreach (string arg in context.Args)
                {
                    if (Enum.TryParse(arg, out ExecutionOptions option))
                    {
                        if (!options.Contains(option))
                        {
                            options.Add(option);
                        }
                    }
                    else
                    {
                        parseError = true;
                    }
                }

                context.Args.Index--;

                if (parseError)
                {
                    return new ArgumentParseResult(Arguments[1], $"Not a valid execution option! Available are: `{ string.Join(", ", Enum.GetNames(typeof(ExecutionOptions))) }`");
                }
            }
            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            EmbedHelper.GetJSONFromUserMessage(message, out JSONObject json, out bool isAdminCommandLog);
            IReadOnlyCollection<IAttachment> attachments = message.Attachments;

            bool pretty = options.Contains(ExecutionOptions.pretty);
            bool remove = options.Contains(ExecutionOptions.remove);

            EmbedBuilder embed;
            if (pretty)
            {

                embed = new EmbedBuilder()
                {
                    Color = Var.BOTCOLOR,
                    Title = $"Message JSON for original message in {guild.Name} - {channel.Name} by {message.Author.Username}#{message.Author.Discriminator}",
                    Description = Macros.MaxLength("```json\n" + json.Print(true).Replace("```", "[3`]"), EmbedHelper.EMBEDDESCRIPTION_MAX - 8) + "```",
                };
            }
            else
            {
                embed = new EmbedBuilder()
                {
                    Color = Var.BOTCOLOR,
                    Title = $"Message JSON for original message in {guild.Name} - {channel.Name} by {message.Author.Username}#{message.Author.Discriminator}",
                    Footer = new EmbedFooterBuilder()
                    {
                        Text = Macros.MaxLength(json.Print(false), EmbedHelper.EMBEDFOOTERTEXT_MAX)
                    }
                };
            }
            if (attachments.Count > 0)
            {
                StringBuilder attachments_str = new StringBuilder();
                foreach (IAttachment attachment in attachments)
                {
                    if (Macros.IsValidImageURL(attachment.Url) && string.IsNullOrEmpty(embed.ImageUrl))
                    {
                        embed.ImageUrl = attachment.Url;
                    }
                    attachments_str.AppendLine($"[{attachment.Filename}]({attachment.Url})");
                }
                embed.AddField("Attachments", attachments_str.ToString());
            }
            await context.Channel.SendEmbedAsync(embed);

            if (remove)
            {
                if (isAdminCommandLog)
                {
                    await context.Channel.SendEmbedAsync("Can not remove admin command log messages!", true);
                }
                else
                {
                    try
                    {
                        await channel.DeleteMessageAsync(message);
                    }
                    catch (Exception e)
                    {
                        await context.Channel.SendEmbedAsync($"Failed to remove the message. Probably missing permissions! Exception: {e.GetType()} - {e.Message}", true);
                    }
                }
            }
        }

        enum ExecutionOptions
        {
            pretty,
            remove
        }
    }

    #endregion
    #region replace

    class ReplaceEmbedCommand : Command
    {
        public override string Identifier => "replace";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicAsync;

        private IUserMessage message;
        private string messageContent;
        private EmbedBuilder embed;

        public ReplaceEmbedCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;

            CommandArgument[] arguments = new CommandArgument[2];
            arguments[0] = new CommandArgument("MessageLink", "A discord message link to select the source");
            arguments[1] = new CommandArgument("EmbedJSON", "The embed, formatted as a JSON", multiple: true);
            InitializeHelp("Edits a message to follow a new embedjson", arguments, "The message author has to be the bot used to modify!");
        }

        protected override async Task<ArgumentParseResult> TryParseArgumentsAsync(CommandContext context)
        {
            if (!context.Args[0].StartsWith("https://discordapp.com/channels/") || context.Args[0].Length < 40)
            {
                return new ArgumentParseResult(Arguments[0], "Not a valid message link! Failed Startswith or length test");
            }

            string[] messageIdentifiers = context.Args[0].Substring(32).Split('/');

            if (messageIdentifiers.Length != 3)
            {
                return new ArgumentParseResult(Arguments[0], "Not a valid message link! Failed split test");
            }

            if (!ulong.TryParse(messageIdentifiers[0], out ulong guildId) || !ulong.TryParse(messageIdentifiers[1], out ulong channelId) || !ulong.TryParse(messageIdentifiers[2], out ulong messageId))
            {
                return new ArgumentParseResult(Arguments[0], "Not a valid message link! Failed id parse test");
            }

            SocketGuild guild = Var.client.GetGuild(guildId);

            if (guild != null)
            {
                SocketTextChannel channel = guild.GetTextChannel(channelId);

                if (channel != null)
                {
                    message = await channel.GetMessageAsync(messageId) as IUserMessage;

                    if (message == null)
                    {
                        return new ArgumentParseResult(Arguments[0], "Found correct guild and correct channel, but not correct message! Has the message been deleted?");
                    }
                    else if (message.Author.Id != Var.client.CurrentUser.Id)
                    {
                        return new ArgumentParseResult(Arguments[0], "Can not edit a message the bot didn't post itself");
                    }
                }
                else
                {
                    return new ArgumentParseResult(Arguments[0], "Found correct guild, but not the channel!");
                }
            }
            else
            {
                return new ArgumentParseResult(Arguments[0], "Could not find the correct guild!");
            }

            if (context.Message.Content.Length > FullIdentifier.Length + context.Args.First.Length + 2)
            {
                string embedText = context.Message.Content.Substring(FullIdentifier.Length + context.Args[0].Length + 2).Replace("[3`]", "```");

                JSONObject json = new JSONObject(embedText);

                return EmbedHelper.TryParseEmbedFromJSONObject(json, out embed, out messageContent);
            }
            else
            {
                embed = null;
                return new ArgumentParseResult("Internal Error: " + Macros.GetCodeLocation());
            }
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            EmbedHelper.GetJSONFromUserMessage(message, out JSONObject json, out bool isAdminCommandLog);

            if (isAdminCommandLog)
            {
                await context.Channel.SendEmbedAsync("Can not modify admin command log messages!", true);
                return;
            }

            await message.ModifyAsync(message =>
            {
                message.Content = messageContent; message.Embed = embed.Build();
            });
            await context.Channel.SendEmbedAsync("Edit done!");
        }
    }

    #endregion
    #region EmbedHelper

    public static class EmbedHelper
    {
        public const string MESSAGECONTENT = "content";
        public const string EMBED = "embed";
        public const string TITLE = "title";
        public const string DESCRIPTION = "description";
        public const string URL = "url";
        public const string AUTHOR = "author";
        public const string NAME = "name";
        public const string ICON_URL = "icon_url";
        public const string THUMBNAIL = "thumbnail";
        public const string IMAGE = "image";
        public const string FOOTER = "footer";
        public const string TEXT = "text";
        public const string TIMESTAMP = "timestamp";
        public const string FIELDS = "fields";
        public const string VALUE = "value";
        public const string INLINE = "inline";
        public const string COLOR = "color";

        public const int MESSAGECONTENT_MAX = 2000;
        public const int EMBEDTITLE_MAX = 256;
        public const int EMBEDDESCRIPTION_MAX = 2048;
        public const int EMBEDFIELDCOUNT_MAX = 25;
        public const int EMBEDFIELDNAME_MAX = 256;
        public const int EMBEDFIELDVALUE_MAX = 1024;
        public const int EMBEDFOOTERTEXT_MAX = 2048;
        public const int EMBEDAUTHORNAME_MAX = 256;
        public const int EMBEDTOTALLENGTH_MAX = 6000;

        public static ArgumentParseResult TryParseEmbedFromJSONObject(JSONObject json, out EmbedBuilder embed, out string messageContent)
        {
            embed = null;
            messageContent = null;

            JSONObject messageContentJSON = json[MESSAGECONTENT];
            JSONObject embedJSON = json[EMBED];

            if (messageContentJSON == null && embedJSON == null)
            {
                return new ArgumentParseResult("Neither message nor embed could be found!");
            }

            if ((messageContentJSON != null) && messageContentJSON.IsString)
            {
                if (!string.IsNullOrEmpty(messageContentJSON.str))
                {
                    if (messageContentJSON.str.Length > MESSAGECONTENT_MAX)
                    {
                        return new ArgumentParseResult($"The message content may not exceed {MESSAGECONTENT_MAX} characters!");
                    }
                    messageContent = RemoveJSONCompatibilitySymbols(messageContentJSON.str);
                }
            }
            else
            {
                messageContent = string.Empty;
            }

            if (embedJSON != null)
            {
                embed = new EmbedBuilder();

                // Parse TITLE, DESCRIPTION, TITLE_URL, TIMESTAMP

                if (embedJSON.GetField(out string embedTitle, TITLE, null))
                {
                    if (!string.IsNullOrEmpty(embedTitle))
                    {
                        if (embedTitle.Length > EMBEDTITLE_MAX)
                        {
                            return new ArgumentParseResult($"The embed title may not exceed {EMBEDTITLE_MAX} characters!");
                        }
                        embed.Title = RemoveJSONCompatibilitySymbols(embedTitle);
                    }
                }
                if (embedJSON.GetField(out string embedDescription, DESCRIPTION, null))
                {
                    if (!string.IsNullOrEmpty(embedDescription))
                    {
                        if (embedDescription.Length > EMBEDDESCRIPTION_MAX)
                        {
                            return new ArgumentParseResult($"The embed title may not exceed {EMBEDDESCRIPTION_MAX} characters!");
                        }
                        embed.Description = RemoveJSONCompatibilitySymbols(embedDescription);
                    }
                }
                if (embedJSON.GetField(out string embedURL, URL, null))
                {
                    if (!string.IsNullOrEmpty(embedURL))
                    {
                        if (Uri.IsWellFormedUriString(embedURL, UriKind.Absolute))
                        {
                            embed.Url = embedURL;
                        }
                        else
                        {
                            return new ArgumentParseResult("The url for the embed title is not a well formed url!");
                        }
                    }
                }
                if (embedJSON.GetField(out string embedFooterTimestamp, TIMESTAMP, null))
                {
                    if (!string.IsNullOrEmpty(embedFooterTimestamp))
                    {
                        if (DateTimeOffset.TryParse(embedFooterTimestamp, out DateTimeOffset timestamp))
                        {
                            embed.Timestamp = timestamp;
                        }
                        else
                        {
                            return new ArgumentParseResult("Could not parse the timestamp to a DateTimeOffset");
                        }
                    }
                }

                // Parse AUTHOR

                JSONObject authorJSON = embedJSON[AUTHOR];
                if (authorJSON != null)
                {
                    EmbedAuthorBuilder author = new EmbedAuthorBuilder();

                    if (authorJSON.GetField(out string authorName, NAME, null))
                    {
                        if (!string.IsNullOrEmpty(authorName))
                        {
                            if (authorName.Length > EMBEDAUTHORNAME_MAX)
                            {
                                return new ArgumentParseResult($"The embed author name may not exceed {EMBEDAUTHORNAME_MAX} characters!");
                            }
                            author.Name = RemoveJSONCompatibilitySymbols(authorName);
                        }
                    }
                    if (authorJSON.GetField(out string authorIconUrl, ICON_URL, null))
                    {
                        if (!string.IsNullOrEmpty(authorIconUrl))
                        {
                            author.IconUrl = authorIconUrl;
                        }
                    }
                    if (authorJSON.GetField(out string authorUrl, URL, null))
                    {
                        if (!string.IsNullOrEmpty(authorUrl))
                        {
                            author.Url = authorUrl;
                        }
                    }

                    embed.Author = author;
                }

                // Parse THUMBNAIL, IMAGE

                JSONObject thumbnailJSON = embedJSON[THUMBNAIL];
                if ((thumbnailJSON != null) && thumbnailJSON.GetField(out string thumbnailUrl, URL, null))
                {
                    if (Uri.IsWellFormedUriString(thumbnailUrl, UriKind.Absolute))
                    {
                        embed.ThumbnailUrl = thumbnailUrl;
                    }
                    else
                    {
                        return new ArgumentParseResult("The url for the embed thumbnail is not a well formed url!");
                    }
                }
                JSONObject imageJSON = embedJSON[IMAGE];
                if ((imageJSON != null) && imageJSON.GetField(out string imageUrl, URL, null))
                {
                    if (Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                    {
                        embed.ImageUrl = imageUrl;
                    }
                    else
                    {
                        return new ArgumentParseResult("The url for the embed image is not a well formed url!");
                    }
                }

                // Parse Color

                if (embedJSON.GetField(out int color, COLOR, 0))
                {
                    Discord.Color embedColor = new Color((uint)color);
                    embed.Color = embedColor;
                }

                // Parse Footer

                JSONObject footerJSON = embedJSON[FOOTER];
                if (footerJSON != null)
                {
                    EmbedFooterBuilder footer = new EmbedFooterBuilder();

                    if (footerJSON.GetField(out string footerText, TEXT, null))
                    {
                        if (!string.IsNullOrEmpty(footerText))
                        {
                            if (footerText.Length > EMBEDFOOTERTEXT_MAX)
                            {
                                return new ArgumentParseResult($"The embed footer text may not exceed {EMBEDFOOTERTEXT_MAX} characters!");
                            }
                            footer.Text = RemoveJSONCompatibilitySymbols(footerText);
                        }
                    }
                    if (footerJSON.GetField(out string footerIconUrl, ICON_URL, null))
                    {
                        if (!string.IsNullOrEmpty(footerIconUrl))
                        {
                            if (Uri.IsWellFormedUriString(footerIconUrl, UriKind.Absolute))
                            {
                                footer.IconUrl = footerIconUrl;
                            }
                            else
                            {
                                return new ArgumentParseResult("The url for the embed footer icon is not a well formed url!");
                            }
                        }
                    }

                    embed.Footer = footer;
                }

                // Parse Fields

                JSONObject fieldsListJSON = embedJSON[FIELDS];

                if ((fieldsListJSON != null) && fieldsListJSON.IsArray && fieldsListJSON.list.Count > 0)
                {
                    if (fieldsListJSON.list.Count > EMBEDFIELDCOUNT_MAX)
                    {
                        return new ArgumentParseResult($"The embed can not have more than {EMBEDFIELDCOUNT_MAX} fields!");
                    }
                    foreach (JSONObject fieldJSON in fieldsListJSON)
                    {
                        if (fieldJSON.GetField(out string fieldName, NAME, null) && fieldJSON.GetField(out string fieldValue, VALUE, null))
                        {
                            fieldJSON.GetField(out bool fieldInline, INLINE, false);
                            if (fieldName != null && fieldValue != null)
                            {
                                if (fieldName.Length > EMBEDFIELDNAME_MAX)
                                {
                                    return new ArgumentParseResult($"A field name may not exceed {EMBEDFIELDNAME_MAX} characters!");
                                }
                                if (fieldValue.Length > EMBEDFIELDVALUE_MAX)
                                {
                                    return new ArgumentParseResult($"A field value may not exceed {EMBEDFIELDVALUE_MAX} characters!");
                                }
                                embed.AddField(RemoveJSONCompatibilitySymbols(fieldName), RemoveJSONCompatibilitySymbols(fieldValue), fieldInline);
                            }
                        }
                    }
                }

                if (embed.Length > EMBEDTOTALLENGTH_MAX)
                {
                    return new ArgumentParseResult($"The total length of the embed may not exceed {EMBEDTOTALLENGTH_MAX} characters!");
                }
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        public static void GetJSONFromUserMessage(IMessage message, out JSONObject json, out bool isAdminCommandLog)
        {
            string messageContent = message.Content;
            IEmbed embed = null;

            IReadOnlyCollection<IEmbed> embeds = message.Embeds;

            if ((embeds != null) && embeds.Count > 0)
            {
                foreach (IEmbed iembed in embeds)
                {
                    embed = iembed;
                    break;
                }
            }

            GetJSONFromMessageContentAndEmbed(messageContent, embed, out json, out isAdminCommandLog);
        }

        public static void GetJSONFromMessageContentAndEmbed(string messageContent, IEmbed embed, out JSONObject json, out bool isAdminCommandLog)
        {
            json = new JSONObject();
            isAdminCommandLog = false;

            if (messageContent != null)
            {
                json.AddField(MESSAGECONTENT, MakeMagicBotJSONParserSafe(messageContent));
            }

            if (embed != null)
            {
                JSONObject embedJSON = new JSONObject();

                // Insert TITLE, DESCRIPTION, TITLE_URL, TIMESTAMP

                if (!string.IsNullOrEmpty(embed.Title))
                {
                    embedJSON.AddField(TITLE, MakeMagicBotJSONParserSafe(embed.Title));
                    if (embed.Title.StartsWith("Admin-Only command used by"))
                    {
                        isAdminCommandLog = true;
                    }
                }
                if (!string.IsNullOrEmpty(embed.Description))
                {
                    embedJSON.AddField(DESCRIPTION, MakeMagicBotJSONParserSafe(embed.Description));
                }
                if (!string.IsNullOrEmpty(embed.Url))
                {
                    embedJSON.AddField(URL, embed.Url);
                }
                if (embed.Timestamp != null)
                {
                    embedJSON.AddField(TIMESTAMP, embed.Timestamp?.ToString("u"));
                }

                // Insert AUTHOR

                if (embed.Author != null)
                {
                    EmbedAuthor author = embed.Author.Value;
                    JSONObject authorJSON = new JSONObject();

                    if (!string.IsNullOrEmpty(author.Name))
                    {
                        authorJSON.AddField(NAME, MakeMagicBotJSONParserSafe(author.Name));
                    }
                    if (!string.IsNullOrEmpty(author.IconUrl))
                    {
                        authorJSON.AddField(ICON_URL, author.IconUrl);
                    }
                    if (!string.IsNullOrEmpty(author.Url))
                    {
                        authorJSON.AddField(URL, author.Url);
                    }

                    embedJSON.AddField(AUTHOR, authorJSON);
                }

                // Insert THUMBNAIL, IMAGE

                if (embed.Thumbnail != null)
                {
                    if (!string.IsNullOrEmpty(embed.Thumbnail.Value.Url))
                    {
                        JSONObject thumbnailJSON = new JSONObject();
                        thumbnailJSON.AddField(URL, embed.Thumbnail.Value.Url);
                        embedJSON.AddField(THUMBNAIL, thumbnailJSON);
                    }
                }
                if (embed.Image != null)
                {
                    if (!string.IsNullOrEmpty(embed.Image.Value.Url))
                    {
                        JSONObject imagJSON = new JSONObject();
                        imagJSON.AddField(URL, embed.Image.Value.Url);
                        embedJSON.AddField(IMAGE, imagJSON);
                    }
                }

                // Insert Color

                if (embed.Color != null)
                {
                    if (embed.Color.Value.RawValue != 0)
                    {
                        embedJSON.AddField(COLOR, embed.Color.Value.RawValue);
                    }
                }

                // Insert Footer

                if (embed.Footer != null)
                {
                    EmbedFooter footer = embed.Footer.Value;
                    JSONObject footerJSON = new JSONObject();

                    if (!string.IsNullOrEmpty(footer.Text))
                    {
                        footerJSON.AddField(TEXT, MakeMagicBotJSONParserSafe(footer.Text));
                    }
                    if (!string.IsNullOrEmpty(footer.IconUrl))
                    {
                        footerJSON.AddField(ICON_URL, footer.IconUrl);
                    }

                    embedJSON.AddField(FOOTER, footerJSON);
                }

                // Insert Fields

                if ((embed.Fields != null) && embed.Fields.Length > 0)
                {
                    JSONObject fieldsJSON = new JSONObject();

                    foreach (Discord.EmbedField embedField in embed.Fields)
                    {
                        JSONObject fieldJSON = new JSONObject();
                        fieldJSON.AddField(NAME, MakeMagicBotJSONParserSafe(embedField.Name));
                        fieldJSON.AddField(VALUE, MakeMagicBotJSONParserSafe(embedField.Value));
                        fieldJSON.AddField(INLINE, embedField.Inline);
                        fieldsJSON.Add(fieldJSON);
                    }

                    embedJSON.AddField(FIELDS, fieldsJSON);
                }

                json.AddField(EMBED, embedJSON);
            }
        }

        public static string MakeMagicBotJSONParserSafe(string input)
        {
            return JSONObject.GetSafelyFormattedString(input);
        }

        public static string RemoveJSONCompatibilitySymbols(string input)
        {
            return JSONObject.GetOriginalFormat(input);
        }
    }

    #endregion
}
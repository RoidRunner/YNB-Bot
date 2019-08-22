using Discord;
using Discord.WebSocket;
using JSON;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.NestedCommands
{
    #region send

    class SendEmbedCommand : Command
    {
        public const string SUMMARY = "Sends a fully featured embed to a guild text channel";
        public const string REMARKS = "Good tool for creating JSON formatted embeds: [MagicBots](https://discord.club/embedg/)";
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.mny46gohtu1e";
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("Channel", ArgumentParsing.GENERIC_PARSED_CHANNEL),
            new Argument("EmbedJSON", "The embed, formatted as a JSON", multiple: true)
        };
        public static readonly Precondition[] PRECONDITIONS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        private SocketTextChannel channel;
        string messageContent = string.Empty;
        private EmbedBuilder embed;

        public SendEmbedCommand(string identifier) : base(identifier, OverriddenMethod.BasicSynchronous, OverriddenMethod.BasicAsync, false, ARGS, PRECONDITIONS, SUMMARY, REMARKS, LINK)
        {
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            if (!ArgumentParsing.TryParseGuildTextChannel(context, context.Args[0], out channel))
            {
                return new ArgumentParseResult(ARGS[0], "Failed to parse to a guild text channel!");
            }

            if (context.Message.Content.Length > FullIdentifier.Length + context.Args.First.Length + 2)
            {
                string embedText = context.Message.Content.Substring(FullIdentifier.Length + context.Args[0].Length + 2).Replace("[3`]", "```");

                if (JSONContainer.TryParse(embedText, out JSONContainer json, out string errormessage))
                {
                    return EmbedHelper.TryParseEmbedFromJSONObject(json, out embed, out messageContent);
                }
                else
                {
                    return new ArgumentParseResult(ARGS[1], $"Unable to parse JSON text to a json data structure! Error: `{errormessage}`");
                }
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
        public const string SUMMARY = "Previews an embed in the channel the command is issued from";
        public const string REMARKS = "Good tool for creating JSON formatted embeds: [MagicBots](https://discord.club/embedg/)";
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.yzc4ios44r6";
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("EmbedJSON", "The embed, formatted as a JSON", multiple: true)
        };
        public static readonly Precondition[] PRECONDITIONS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        string messageContent = string.Empty;
        private EmbedBuilder embed;

        public PreviewEmbedCommand(string identifier) : base(identifier, OverriddenMethod.BasicSynchronous, OverriddenMethod.BasicAsync, false, ARGS, PRECONDITIONS, SUMMARY, REMARKS, LINK)
        {
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            if (context.Message.Content.Length > FullIdentifier.Length + 1)
            {
                string embedText = context.Message.Content.Substring(FullIdentifier.Length + 1).Replace("[3`]", "```");

                if (JSONContainer.TryParse(embedText, out JSONContainer json, out string errormessage))
                {
                    return EmbedHelper.TryParseEmbedFromJSONObject(json, out embed, out messageContent);
                }
                else
                {
                    return new ArgumentParseResult(ARGS[0], $"Unable to parse JSON text to a json data structure! Error: `{errormessage}`");
                }
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
        public const string SUMMARY = "Formats a JSON from a given message, including embeds";
        public const string REMARKS = "Good tool for creating JSON formatted embeds: [MagicBots](https://discord.club/embedg/)";
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.au90cahsqtfp";
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("MessageLink", "A discord message link to select the source"),
            new Argument("Options", $"Command execution options. Available are:\n`{ExecutionOptions.pretty}` = Include some nice formatting in the embed JSON\n" +
                $"`{ExecutionOptions.remove}` = Remove the source message after retrieving the embed", true, true)
        };
        public static readonly Precondition[] PRECONDITIONS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public GetEmbedCommand(string identifier) : base(identifier, OverriddenMethod.BasicAsync, OverriddenMethod.BasicAsync, false, ARGS, PRECONDITIONS, SUMMARY, REMARKS, LINK)
        {
        }

        private SocketGuild guild;
        private SocketTextChannel channel;
        private IMessage message;
        private List<ExecutionOptions> options = new List<ExecutionOptions>();

        protected override async Task<ArgumentParseResult> TryParseArgumentsAsync(CommandContext context)
        {
            if (!context.Args[0].StartsWith("https://discordapp.com/channels/") || context.Args[0].Length < 40)
            {
                return new ArgumentParseResult(ARGS[0], "Not a valid message link! Failed Startswith or length test");
            }

            string[] messageIdentifiers = context.Args[0].Substring(32).Split('/');

            if (messageIdentifiers.Length != 3)
            {
                return new ArgumentParseResult(ARGS[0], "Not a valid message link! Failed split test");
            }

            if (!ulong.TryParse(messageIdentifiers[0], out ulong guildId) || !ulong.TryParse(messageIdentifiers[1], out ulong channelId) || !ulong.TryParse(messageIdentifiers[2], out ulong messageId))
            {
                return new ArgumentParseResult(ARGS[0], "Not a valid message link! Failed id parse test");
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
                        return new ArgumentParseResult(ARGS[0], "Found correct guild and correct channel, but not correct message! Has the message been deleted?");
                    }
                }
                else
                {
                    return new ArgumentParseResult(ARGS[0], "Found correct guild, but not the channel!");
                }
            }
            else
            {
                return new ArgumentParseResult(ARGS[0], "Could not find the correct guild!");
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
                    return new ArgumentParseResult(ARGS[1], $"Not a valid execution option! Available are: `{ string.Join(", ", Enum.GetNames(typeof(ExecutionOptions))) }`");
                }
            }
            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            EmbedHelper.GetJSONFromUserMessage(message, out JSONContainer json, out bool isAdminCommandLog);
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
                    Description = Macros.MaxLength("```json\n" + json.Build(true).Replace("```", "[3`]"), EmbedHelper.EMBEDDESCRIPTION_MAX - 8) + "```",
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
                        Text = Macros.MaxLength(json.Build(false), EmbedHelper.EMBEDFOOTERTEXT_MAX)
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
        public const string SUMMARY = "Edits a message to follow a new embedjson";
        public const string REMARKS = "The message author has to be by the bot used to modify!";
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.9t1eqpeq952a";
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("MessageLink", "A discord message link to select the source"),
            new Argument("EmbedJSON", "The embed, formatted as a JSON", multiple: true)
        };
        public static readonly Precondition[] PRECONDITIONS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        private IUserMessage message;
        private string messageContent;
        private EmbedBuilder embed;

        public ReplaceEmbedCommand(string identifier) : base(identifier, OverriddenMethod.BasicAsync, OverriddenMethod.BasicAsync, false, ARGS, PRECONDITIONS, SUMMARY, REMARKS, LINK)
        {
        }

        protected override async Task<ArgumentParseResult> TryParseArgumentsAsync(CommandContext context)
        {
            if (!context.Args[0].StartsWith("https://discordapp.com/channels/") || context.Args[0].Length < 40)
            {
                return new ArgumentParseResult(ARGS[0], "Not a valid message link! Failed Startswith or length test");
            }

            string[] messageIdentifiers = context.Args[0].Substring(32).Split('/');

            if (messageIdentifiers.Length != 3)
            {
                return new ArgumentParseResult(ARGS[0], "Not a valid message link! Failed split test");
            }

            if (!ulong.TryParse(messageIdentifiers[0], out ulong guildId) || !ulong.TryParse(messageIdentifiers[1], out ulong channelId) || !ulong.TryParse(messageIdentifiers[2], out ulong messageId))
            {
                return new ArgumentParseResult(ARGS[0], "Not a valid message link! Failed id parse test");
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
                        return new ArgumentParseResult(ARGS[0], "Found correct guild and correct channel, but not correct message! Has the message been deleted?");
                    }
                    else if (message.Author.Id != Var.client.CurrentUser.Id)
                    {
                        return new ArgumentParseResult(ARGS[0], "Can not edit a message the bot didn't post itself");
                    }
                }
                else
                {
                    return new ArgumentParseResult(ARGS[0], "Found correct guild, but not the channel!");
                }
            }
            else
            {
                return new ArgumentParseResult(ARGS[0], "Could not find the correct guild!");
            }

            if (context.Message.Content.Length > FullIdentifier.Length + context.Args.First.Length + 2)
            {
                string embedText = context.Message.Content.Substring(FullIdentifier.Length + context.Args[0].Length + 2).Replace("[3`]", "```");

                if (JSONContainer.TryParse(embedText, out JSONContainer json, out string errormessage))
                {
                    return EmbedHelper.TryParseEmbedFromJSONObject(json, out embed, out messageContent);
                }
                else
                {
                    return new ArgumentParseResult(ARGS[1], $"Unable to parse JSON text to a json data structure! Error: `{errormessage}`");
                }
            }
            else
            {
                embed = null;
                return new ArgumentParseResult("Internal Error: " + Macros.GetCodeLocation());
            }
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            EmbedHelper.GetJSONFromUserMessage(message, out JSONContainer json, out bool isAdminCommandLog);

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

        public static ArgumentParseResult TryParseEmbedFromJSONObject(JSONContainer json, out EmbedBuilder embed, out string messageContent)
        {
            embed = null;
            messageContent = null;

            json.TryGetField(MESSAGECONTENT, out JSONField messageContentJSON);
            json.TryGetField(EMBED, out JSONContainer embedJSON);

            if (messageContentJSON == null && embedJSON == null)
            {
                return new ArgumentParseResult("Neither message nor embed could be found!");
            }

            if ((messageContentJSON != null) && messageContentJSON.IsString)
            {
                if (!string.IsNullOrEmpty(messageContentJSON.String))
                {
                    if (messageContentJSON.String.Length > MESSAGECONTENT_MAX)
                    {
                        return new ArgumentParseResult($"The message content may not exceed {MESSAGECONTENT_MAX} characters!");
                    }
                    messageContent = messageContentJSON.String;
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

                if (embedJSON.TryGetField(TITLE, out string embedTitle))
                {
                    if (!string.IsNullOrEmpty(embedTitle))
                    {
                        if (embedTitle.Length > EMBEDTITLE_MAX)
                        {
                            return new ArgumentParseResult($"The embed title may not exceed {EMBEDTITLE_MAX} characters!");
                        }
                        embed.Title = embedTitle;
                    }
                }
                if (embedJSON.TryGetField(DESCRIPTION, out string embedDescription))
                {
                    if (!string.IsNullOrEmpty(embedDescription))
                    {
                        if (embedDescription.Length > EMBEDDESCRIPTION_MAX)
                        {
                            return new ArgumentParseResult($"The embed title may not exceed {EMBEDDESCRIPTION_MAX} characters!");
                        }
                        embed.Description = embedDescription;
                    }
                }
                if (embedJSON.TryGetField(URL, out string embedURL))
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
                if (embedJSON.TryGetField(TIMESTAMP, out string embedFooterTimestamp))
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

                if (embedJSON.TryGetField(AUTHOR, out JSONContainer authorJSON))
                {
                    EmbedAuthorBuilder author = new EmbedAuthorBuilder();

                    if (authorJSON.TryGetField(NAME, out string authorName))
                    {
                        if (!string.IsNullOrEmpty(authorName))
                        {
                            if (authorName.Length > EMBEDAUTHORNAME_MAX)
                            {
                                return new ArgumentParseResult($"The embed author name may not exceed {EMBEDAUTHORNAME_MAX} characters!");
                            }
                            author.Name = authorName;
                        }
                    }
                    if (authorJSON.TryGetField(ICON_URL, out string authorIconUrl))
                    {
                        if (!string.IsNullOrEmpty(authorIconUrl))
                        {
                            author.IconUrl = authorIconUrl;
                        }
                    }
                    if (authorJSON.TryGetField(URL, out string authorUrl))
                    {
                        if (!string.IsNullOrEmpty(authorUrl))
                        {
                            author.Url = authorUrl;
                        }
                    }

                    embed.Author = author;
                }

                // Parse THUMBNAIL, IMAGE

                if (embedJSON.TryGetField(THUMBNAIL, out JSONContainer thumbnailJSON))
                {
                    if (thumbnailJSON.TryGetField(URL, out string thumbnailUrl))
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
                }
                if (embedJSON.TryGetField(IMAGE, out JSONContainer imageJSON))
                {
                    if (imageJSON.TryGetField(URL, out string imageUrl))
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
                }

                // Parse Color

                if (embedJSON.TryGetField(COLOR, out int color))
                {
                    Discord.Color embedColor = new Color((uint)color);
                    embed.Color = embedColor;
                }

                // Parse Footer

                if (embedJSON.TryGetField(FOOTER, out JSONContainer footerJSON))
                {
                    EmbedFooterBuilder footer = new EmbedFooterBuilder();

                    if (footerJSON.TryGetField(TEXT, out string footerText))
                    {
                        if (!string.IsNullOrEmpty(footerText))
                        {
                            if (footerText.Length > EMBEDFOOTERTEXT_MAX)
                            {
                                return new ArgumentParseResult($"The embed footer text may not exceed {EMBEDFOOTERTEXT_MAX} characters!");
                            }
                            footer.Text = footerText;
                        }
                    }
                    if (footerJSON.TryGetField(ICON_URL, out string footerIconUrl))
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

                if (embedJSON.TryGetField(FIELDS, out IReadOnlyList<JSONField> fieldsList))
                { 
                    if (fieldsList.Count > EMBEDFIELDCOUNT_MAX)
                    {
                        return new ArgumentParseResult($"The embed can not have more than {EMBEDFIELDCOUNT_MAX} fields!");
                    }
                    foreach (JSONField fieldJSON in fieldsList)
                    {
                        if (fieldJSON.IsObject && fieldJSON.Container != null)
                        {
                            if (fieldJSON.Container.TryGetField(NAME, out string fieldName) && fieldJSON.Container.TryGetField(VALUE, out string fieldValue))
                            {
                                fieldJSON.Container.TryGetField(INLINE, out bool fieldInline, false);
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
                                    embed.AddField(fieldName, fieldValue, fieldInline);
                                }
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

        public static void GetJSONFromUserMessage(IMessage message, out JSONContainer json, out bool isAdminCommandLog)
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

        public static void GetJSONFromMessageContentAndEmbed(string messageContent, IEmbed embed, out JSONContainer json, out bool isAdminCommandLog)
        {
            json = JSONContainer.NewObject();
            isAdminCommandLog = false;

            if (messageContent != null)
            {
                json.TryAddField(MESSAGECONTENT, messageContent);
            }

            if (embed != null)
            {
                JSONContainer embedJSON = JSONContainer.NewObject();

                // Insert TITLE, DESCRIPTION, TITLE_URL, TIMESTAMP

                if (!string.IsNullOrEmpty(embed.Title))
                {
                    embedJSON.TryAddField(TITLE, embed.Title);
                    if (embed.Title.StartsWith("Admin-Only command used by"))
                    {
                        isAdminCommandLog = true;
                    }
                }
                if (!string.IsNullOrEmpty(embed.Description))
                {
                    embedJSON.TryAddField(DESCRIPTION, embed.Description);
                }
                if (!string.IsNullOrEmpty(embed.Url))
                {
                    embedJSON.TryAddField(URL, embed.Url);
                }
                if (embed.Timestamp != null)
                {
                    embedJSON.TryAddField(TIMESTAMP, embed.Timestamp?.ToString("u"));
                }

                // Insert AUTHOR

                if (embed.Author != null)
                {
                    EmbedAuthor author = embed.Author.Value;
                    JSONContainer authorJSON = JSONContainer.NewObject();

                    if (!string.IsNullOrEmpty(author.Name))
                    {
                        authorJSON.TryAddField(NAME, author.Name);
                    }
                    if (!string.IsNullOrEmpty(author.IconUrl))
                    {
                        authorJSON.TryAddField(ICON_URL, author.IconUrl);
                    }
                    if (!string.IsNullOrEmpty(author.Url))
                    {
                        authorJSON.TryAddField(URL, author.Url);
                    }

                    embedJSON.TryAddField(AUTHOR, authorJSON);
                }

                // Insert THUMBNAIL, IMAGE

                if (embed.Thumbnail != null)
                {
                    if (!string.IsNullOrEmpty(embed.Thumbnail.Value.Url))
                    {
                        JSONContainer thumbnailJSON = JSONContainer.NewObject();
                        thumbnailJSON.TryAddField(URL, embed.Thumbnail.Value.Url);
                        embedJSON.TryAddField(THUMBNAIL, thumbnailJSON);
                    }
                }
                if (embed.Image != null)
                {
                    if (!string.IsNullOrEmpty(embed.Image.Value.Url))
                    {
                        JSONContainer imagJSON = JSONContainer.NewObject();
                        imagJSON.TryAddField(URL, embed.Image.Value.Url);
                        embedJSON.TryAddField(IMAGE, imagJSON);
                    }
                }

                // Insert Color

                if (embed.Color != null)
                {
                    if (embed.Color.Value.RawValue != 0)
                    {
                        embedJSON.TryAddField(COLOR, embed.Color.Value.RawValue);
                    }
                }

                // Insert Footer

                if (embed.Footer != null)
                {
                    EmbedFooter footer = embed.Footer.Value;
                    JSONContainer footerJSON = JSONContainer.NewObject();

                    if (!string.IsNullOrEmpty(footer.Text))
                    {
                        footerJSON.TryAddField(TEXT, footer.Text);
                    }
                    if (!string.IsNullOrEmpty(footer.IconUrl))
                    {
                        footerJSON.TryAddField(ICON_URL, footer.IconUrl);
                    }

                    embedJSON.TryAddField(FOOTER, footerJSON);
                }

                // Insert Fields

                if ((embed.Fields != null) && embed.Fields.Length > 0)
                {
                    JSONContainer fieldsJSON = JSONContainer.NewArray();

                    foreach (Discord.EmbedField embedField in embed.Fields)
                    {
                        JSONContainer fieldJSON = JSONContainer.NewObject();
                        fieldJSON.TryAddField(NAME, embedField.Name);
                        fieldJSON.TryAddField(VALUE, embedField.Value);
                        fieldJSON.TryAddField(INLINE, embedField.Inline);
                        fieldsJSON.Add(fieldJSON);
                    }

                    embedJSON.TryAddField(FIELDS, fieldsJSON);
                }

                json.TryAddField(EMBED, embedJSON);
            }
        }
    }

    #endregion
}
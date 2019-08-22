using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using YNBBot.NestedCommands;

namespace YNBBot
{
    static class Macros
    {
        public static Random Rand = new Random();

        #region Markdown Helpers

        private const string CODEBLOCKBASESTRING = "``````";
        private const string INLINECODEBLOCKBASESTRING = "``";
        private const string FATBASESTRING = "****";

        /// <summary>
        /// Adds multiline codeblock markdown syntax around the given input
        /// </summary>
        /// <param name="input">Any object whichs .ToString() function is used as the text</param>
        /// <returns></returns>
        public static string MultiLineCodeBlock(object input)
        {
            return CODEBLOCKBASESTRING.Insert(3, input.ToString());
        }

        /// <summary>
        /// Adds inline codeblock markdown syntax around the given input
        /// </summary>
        /// <param name="input">Any object whichs .ToString() function is used as the text</param>
        /// <returns></returns>
        public static string InlineCodeBlock(object input)
        {
            return INLINECODEBLOCKBASESTRING.Insert(1, input.ToString());
        }

        /// <summary>
        /// Adds fat markdown syntax around the given input
        /// </summary>
        /// <param name="input">Any object whichs .ToString() function is used as the text</param>
        /// <returns></returns>
        public static string Fat(object input)
        {
            return FATBASESTRING.Insert(2, input.ToString());
        }

        /// <summary>
        /// Creates a mention for a given role Id
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public static string Mention_Role(ulong roleId)
        {
            return $"<@&{roleId}>";
        }

        internal static string Mention_User(ulong userId)
        {
            return $"<@{userId}>";
        }

        #endregion
        #region Embed Sending Extension Methods

        /// <summary>
        /// Sends an embedded message to this channel
        /// </summary>
        /// <param name="message">the string message to send</param>
        /// <param name="error">if true, the bots error color is used as the embed color instead of the normal bot color</param>
        /// <returns>The embed message created by using this method</returns>
        public async static Task<Discord.Rest.RestUserMessage> SendEmbedAsync(this ISocketMessageChannel channel, string message, bool error = false)
        {
            EmbedBuilder embed = new EmbedBuilder();
            if (error)
            {
                embed.Color = Var.ERRORCOLOR;
            }
            else
            {
                embed.Color = Var.BOTCOLOR;
            }
            embed.Description = message;
            return await channel.SendMessageAsync(string.Empty, embed: embed.Build());
        }

        /// <summary>
        /// Sends an embedded message to this channel
        /// </summary>
        /// <param name="messageContent">the unembedded part of the message</param>
        /// <param name="embeddedmessage">the embedded part of the message</param>
        /// <param name="error">if true, the bots error color is used as the embed color instead of the normal bot color</param>
        /// <returns>The embed message created by using this method</returns>
        public async static Task<Discord.Rest.RestUserMessage> SendEmbedAsync(this ISocketMessageChannel channel, string messageContent, string embeddedmessage, bool error = false)
        {
            EmbedBuilder embed = new EmbedBuilder();
            if (error)
            {
                embed.Color = Var.ERRORCOLOR;
            }
            else
            {
                embed.Color = Var.BOTCOLOR;
            }
            embed.Description = embeddedmessage;
            return await channel.SendMessageAsync(messageContent, embed: embed.Build());
        }

        /// <summary>
        /// Sends an embed to this channel
        /// </summary>
        /// <param name="embed">The embed builder carrying the embed data</param>
        /// <returns>The embed message created by using this method</returns>
        public async static Task<Discord.Rest.RestUserMessage> SendEmbedAsync(this ISocketMessageChannel channel, EmbedBuilder embed)
        {
            return await channel.SendMessageAsync(embed: embed.Build());
        }

        /// <summary>
        /// Sends as many embeds as necessary based on a list of embed fields. Adhering to the maximum of 25 embed fields per embed
        /// </summary>
        /// <param name="title">The title applied to all embeds</param>
        /// <param name="embeds">List of embed field builders</param>
        /// <param name="description">The description applied to all embeds</param>
        public async static Task SendSafeEmbedList(this ISocketMessageChannel channel, string title, List<EmbedFieldBuilder> embeds, string description = null)
        {
            List<EmbedBuilder> embedMessages = new List<EmbedBuilder>();
            EmbedBuilder CurrentBuilder = null;
            for (int i = 0; i < embeds.Count; i++)
            {
                if (i % 25 == 0)
                {
                    CurrentBuilder = new EmbedBuilder
                    {
                        Color = Var.BOTCOLOR,
                        Title = title
                    };
                    if (!string.IsNullOrEmpty(description))
                    {
                        CurrentBuilder.Description = description;
                    }
                    embedMessages.Add(CurrentBuilder);
                }

                EmbedFieldBuilder embed = embeds[i];
                if (CurrentBuilder != null)
                {
                    CurrentBuilder.AddField(embed);
                }
            }

            foreach (EmbedBuilder embedMessage in embedMessages)
            {
                await channel.SendEmbedAsync(embedMessage);
            }
        }

        #endregion
        #region Discord Client Extension Methods

        /// <summary>
        /// Retrieves access level (based on botadmin list and roles) for a given user Id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static AccessLevel GetAccessLevel(this DiscordSocketClient client, ulong userId)
        {
            if (SettingsModel.UserIsBotAdmin(userId))
            {
                return AccessLevel.BotAdmin;
            }
            AccessLevel level = AccessLevel.Basic;
            foreach (SocketGuild guild in client.Guilds)
            {
                SocketGuildUser userInGuild = guild.GetUser(userId);
                if (userInGuild != null)
                {
                    foreach (SocketRole role in userInGuild.Roles)
                    {
                        if (role.Id == SettingsModel.AdminRole)
                        {
                            return AccessLevel.Admin;
                        }
                        else if (role.Id == SettingsModel.MinecraftBranchRole)
                        {
                            level = AccessLevel.Minecraft;
                        }
                    }
                }
            }
            return level;
        }

        public static bool TryGetRole(this DiscordSocketClient client, ulong roleId, out SocketRole result)
        {
            result = null;
            foreach (SocketGuild guild in client.Guilds)
            {
                result = guild.GetRole(roleId);
                if (result != null)
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task<SocketUserMessage> GetMessage(this DiscordSocketClient client, ulong guildId, ulong channelId, ulong messageId)
        {
            SocketUserMessage message = null;
            SocketGuild guild = client.GetGuild(guildId);
            if (guild != null)
            {
                SocketTextChannel channel = guild.GetTextChannel(channelId);
                if (channel != null)
                {
                    message = await channel.GetMessageAsync(messageId) as SocketUserMessage;
                }
            }
            return message;
        }

        #endregion
        #region String Extension Methods

        /// <summary>
        /// Prunes to a maximum length if it is exceeded
        /// </summary>
        /// <param name="maxLength">Maximum length the string is allowed to reach</param>
        /// <returns>A string guaranteed to not exceed maximum length</returns>
        public static string MaxLength(this string str, int maxLength)
        {
            if (str.Length <= maxLength)
            {
                return str;
            }
            else
            {
                return str.Substring(0, maxLength);
            }
        }

        /// <summary>
        /// Checks wether the string is a valid image url (http protocol and image file endings)
        /// </summary>
        /// <returns>True, if a valid image url</returns>
        public static bool IsValidImageURL(this string str)
        {
            return str.StartsWith("http") && str.Contains("://") && (str.EndsWith(".png") || str.EndsWith(".jpg") || str.EndsWith(".jpeg") || str.EndsWith(".gif") || str.EndsWith(".webp"));
        }

        #endregion
        #region Misc Extension Methods

        /// <summary>
        /// Formats a discord compatible message url
        /// </summary>
        /// <param name="guildId">The guild where this message was sent</param>
        public static string GetMessageURL(this IMessage message, ulong guildId)
        {
            return string.Format("https://discordapp.com/channels/{0}/{1}/{2}", guildId, message.Channel.Id, message.Id);
        }

        /// <summary>
        /// Formats to a shorted string (maximum data density in short human readable format)
        /// </summary>
        /// <returns></returns>
        public static string ToShortString(this float n)
        {
            if (n < 0)
            {
                throw new ArgumentException("This parameter cannot be smaller than 0", "n");
            }
            int lvl = (int)(MathF.Log10(n)) / 3;
            n = n / MathF.Pow(10, lvl * 3);
            char lvlId;
            switch (lvl)
            {
                case 0:
                    lvlId = ' ';
                    break;
                case 1:
                    lvlId = 'K';
                    break;
                case 2:
                    lvlId = 'M';
                    break;
                case 3:
                    lvlId = 'B';
                    break;
                case 4:
                    lvlId = 'T';
                    break;
                default:
                    lvlId = '?';
                    break;
            }
            string nstr;
            if (n >= 100)
            {
                nstr = n.ToString("000");
            }
            else if (n >= 10)
            {
                nstr = n.ToString("00.0");
            }
            else if (n >= 1)
            {
                nstr = n.ToString("0.00");
            }
            else
            {
                nstr = n.ToString("0.000");
            }
            if (lvlId == ' ')
            {
                return nstr;
            }
            else
            {
                return string.Format("{0} {1}", nstr, lvlId);
            }
        }

        /// <summary>
        /// Converts <see cref="TimeSpan"/> objects to a simple human-readable string.  Examples: 3.1 seconds, 2 minutes, 4.23 hours, etc.
        /// </summary>
        /// <param name="span">The timespan.</param>
        /// <param name="significantDigits">Significant digits to use for output.</param>
        /// <returns></returns>
        public static string ToHumanTimeString(this TimeSpan span, int significantDigits = 3)
        {
            var format = "G" + significantDigits;
            return span.TotalMilliseconds < 1000 ? span.TotalMilliseconds.ToString(format) + " milliseconds"
                : (span.TotalSeconds < 60 ? span.TotalSeconds.ToString(format) + " seconds"
                    : (span.TotalMinutes < 60 ? span.TotalMinutes.ToString(format) + " minutes"
                        : (span.TotalHours < 24 ? span.TotalHours.ToString(format) + " hours"
                                                : span.TotalDays.ToString(format) + " days")));
        }

        public static bool TryParseHumanTimeString(string str, out TimeSpan span)
        {
            if (string.IsNullOrEmpty(str))
            {
                span = default;
                return false;
            }
            str.Trim();

            for (int i = 0; i < str.Length; i++)
            {
                char current = str[i];
                switch (current)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        continue;
                    case 'h':
                    case 'H':
                        if (i == 0)
                        {
                            span = default;
                            return false;
                        }
                        else
                        {
                            if (uint.TryParse(str.Substring(0, i), out uint hours))
                            {
                                span = TimeSpan.FromHours(hours);
                                return true;
                            }
                            else
                            {
                                span = default;
                                return false;
                            }
                        }

                    case 'm':
                    case 'M':
                        if (i == 0)
                        {
                            span = default;
                            return false;
                        }
                        else
                        {
                            if (uint.TryParse(str.Substring(0, i), out uint minutes))
                            {
                                span = TimeSpan.FromMinutes(minutes);
                                return true;
                            }
                            else
                            {
                                span = default;
                                return false;
                            }
                        }
                    default:
                        span = default;
                        return false;
                }
            }

            span = default;
            return false;
        }

        /// <summary>
        /// Checks for an index being inside array bounds
        /// </summary>
        /// <returns>True, if index is withing array bounds</returns>
        public static bool WithinBounds(this Array array, int index)
        {
            return index > 0 && index < array.Length;
        }

        /// <summary>
        /// Functions similarly to string.Join(), but with the addition of performing an operation to select the object represantation string instead of calling object.ToString()
        /// </summary>
        /// <param name="separator">Separator string that separates items in the result</param>
        /// <param name="operation">Operation taking TSource as input, returning a string</param>
        /// <returns>Concatenated string of all results of calling operation once for each item, with separator string inbetween</returns>
        public static string OperationJoin<TSource>(this ICollection<TSource> source, string separator, Func<TSource, string> operation)
        {
            StringBuilder builder = new StringBuilder();

            IEnumerator<TSource> enumerator = source.GetEnumerator();
            for (int i = 0; i < source.Count; i++)
            {
                if (!enumerator.MoveNext())
                {
                    throw new IndexOutOfRangeException("Collection did not contain expected count of items!");
                }
                builder.Append(operation(enumerator.Current));
                if (i < source.Count - 1)
                {
                    builder.Append(separator);
                }
            }

            return builder.ToString();
        }

        public static string Join<TSource>(this ICollection<TSource> source, string separator)
        {
            StringBuilder builder = new StringBuilder();

            IEnumerator<TSource> enumerator = source.GetEnumerator();
            for (int i = 0; i < source.Count; i++)
            {
                if (!enumerator.MoveNext())
                {
                    throw new IndexOutOfRangeException("Collection did not contain expected count of items!");
                }
                builder.Append(enumerator.Current.ToString());
                if (i < source.Count - 1)
                {
                    builder.Append(separator);
                }
            }

            return builder.ToString();
        }

        #endregion
        #region Misc Methods

        /// <summary>
        /// Schedules messages for deletion
        /// </summary>
        /// <param name="delay">The delay in milliseconds to wait until the messages are to be deleted</param>
        /// <param name="messages">All messages that are meant to be deleted</param>
        public static void ScheduleMessagesForDeletion(long delay, params IUserMessage[] messages)
        {
            TimingThread.AddScheduleDelegate(async () => 
            {
                try
                {
                    foreach (IUserMessage message in messages)
                    {
                        await message.DeleteAsync();
                    }
                }
                catch(Exception)
                {
                    // No handling, as an exception would only mean that we lack permission!
                }
            }, delay);
        }

        /// <summary>
        /// Sweeps a text for contained image urls
        /// </summary>
        /// <param name="text">The text to search for an image url</param>
        /// <param name="url">The image url if a result was found</param>
        /// <returns>True, if an image url has been found</returns>
        public static bool TryGetImageURLFromText(string text, out string url)
        {
            url = null;
            foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                word.Trim();
                if (word.IsValidImageURL())
                {
                    url = word;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets code location (for debug purposes)
        /// </summary>
        public static string GetCodeLocation([CallerFilePath] string file = "File Not Found", [CallerLineNumber] int lineNumber = 0)
        {
            return $"{file} at line {lineNumber}";
        }

        /// <summary>
        /// Returns an embed builder with title, description and color set based on an Exception
        /// </summary>
        /// <param name="e">The exception to base the embed on</param>
        public static EmbedBuilder EmbedFromException(Exception e)
        {
            EmbedBuilder result = new EmbedBuilder()
            {
                Title = MaxLength($"{e.GetType()} - {e.Message}", EmbedHelper.EMBEDTITLE_MAX),
                Description = MaxLength("```" + e.StackTrace, EmbedHelper.EMBEDDESCRIPTION_MAX - 3) + "```",
                Color = Var.ERRORCOLOR
            };
            return result;
        }

        /// <summary>
        /// Get a list string of all enum names
        /// </summary>
        /// <typeparam name="T">Enum type to get the name list of</typeparam>
        /// <returns></returns>
        public static string GetEnumNames<T>() where T : Enum
        {
            return string.Join(", ", Enum.GetNames(typeof(T)));
        }

        /// <summary>
        /// Returns an embed field builder given embed field parameters
        /// </summary>
        /// <param name="title">The title of the embed field</param>
        /// <param name="value">The value of the embed field</param>
        /// <param name="inLine">Wether the field can be displayed inline or not</param>
        public static EmbedFieldBuilder EmbedField(string title, object value, bool inLine = false)
        {
            return new EmbedFieldBuilder() { Name = title, Value = value, IsInline = inLine };
        }

        #endregion
    }
}

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

        private const string CODEBLOCKBASESTRING = "``````";
        private const string INLINECODEBLOCKBASESTRING = "``";
        private const string FATBASESTRING = "****";

        public static string MultiLineCodeBlock(object input)
        {
            return CODEBLOCKBASESTRING.Insert(3, input.ToString());
        }

        public static string InlineCodeBlock(object input)
        {
            return INLINECODEBLOCKBASESTRING.Insert(1, input.ToString());
        }

        public static string Fat(object input)
        {
            return FATBASESTRING.Insert(2, input.ToString());
        }

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

        public async static Task<Discord.Rest.RestUserMessage> SendEmbedAsync(this ISocketMessageChannel channel, string message, string embeddedmessage, bool error = false)
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
            return await channel.SendMessageAsync(message, embed: embed.Build());
        }

        public async static Task<Discord.Rest.RestUserMessage> SendEmbedAsync(this ISocketMessageChannel channel, EmbedBuilder embed)
        {
            return await channel.SendMessageAsync(string.Empty, embed: embed.Build());
        }

        public async static Task SendSafeEmbedList(this ISocketMessageChannel channel, string title, List<EmbedField> embeds, string description = null)
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

                EmbedField embed = embeds[i];
                if (CurrentBuilder != null && !string.IsNullOrEmpty(embed.Title) && embed.Value != null && !string.IsNullOrEmpty(embed.Value.ToString()))
                {
                    CurrentBuilder.AddField(embed.Title, embed.Value, embed.InLine);
                }
                else if (CurrentBuilder != null)
                {
                    CurrentBuilder.AddField("Warning!", $"Failed to add field `{embed.Title} - {embed.Value?.ToString()}`");
                }
            }

            foreach (EmbedBuilder embedMessage in embedMessages)
            {
                await channel.SendEmbedAsync(embedMessage);
            }
        }

        public static string GetMessageURL(this IMessage message, ulong guildId)
        {
            return string.Format("https://discordapp.com/channels/{0}/{1}/{2}", guildId, message.Channel.Id, message.Id);
        }

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

        public static bool TryParseChannelId(string channel, out ulong channelId, ulong channelself = ulong.MaxValue)
        {
            channelId = 0;
            if (ulong.TryParse(channel, out ulong Id))
            {
                channelId = Id;
                return true;
            }
            else if (channel.StartsWith("<#") && channel.EndsWith('>') && channel.Length > 3)
            {
                if (ulong.TryParse(channel.Substring(2, channel.Length - 3), out ulong Id2))
                {
                    channelId = Id2;
                    return true;
                }
            }
            else if (channel.Equals("this"))
            {
                channelId = channelself;
                return true;
            }
            return false;
        }

        public static bool TryParseUserId(string user, out ulong userId, ulong userself)
        {
            userId = 0;
            if (user.Equals("self"))
            {
                userId = userself;
                return true;
            }
            else if (user.StartsWith("<@") && user.EndsWith('>') && user.Length > 3)
            {
                if (ulong.TryParse(user.Substring(2, user.Length - 3), out userId))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (ulong.TryParse(user, out userId))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string FirstToUpper(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            else
            {
                return str.Substring(0, 1).ToUpper() + str.Substring(1);
            }
        }

        public static bool IsValidImageURL(this string str)
        {
            return str.StartsWith("http") && str.Contains("://") && (str.EndsWith(".png") || str.EndsWith(".jpg") || str.EndsWith(".jpeg") || str.EndsWith(".gif") || str.EndsWith(".webp"));
        }

        public static string BuildListString<T>(IList<T> list)
        {
            if ((list == null) || list.Count == 0)
            {
                return "none";
            }
            else if (list.Count == 1)
            {
                return list[0].ToString();
            }
            else
            {
                StringBuilder result = new StringBuilder();
                for (int i = 0; i < list.Count - 1; i++)
                {
                    result.Append(list[i].ToString());
                    result.Append(", ");
                }
                result.Append(list[list.Count - 1].ToString());
                return result.ToString();
            }
        }

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

        public static bool ContainsAny(this string str, char[] testers)
        {
            foreach (char ch in testers)
            {
                if (str.Contains(ch.ToString()))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ContainsOnly(this string str, char[] testers)
        {
            foreach (char c in str)
            {
                if (!testers.Contains(c))
                {
                    return false;
                }
            }
            return true;
        }

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

        public static SocketTextChannel GetTextChannel(this DiscordSocketClient client, ulong id)
        {
            SocketTextChannel result = client.GetChannel(id) as SocketTextChannel;
            return result;
        }

        public static SocketGuildUser GetGuildUser(this DiscordSocketClient client, ulong id)
        {
            SocketGuildUser result;
            foreach (SocketGuild guild in client.Guilds)
            {
                result = guild.GetUser(id);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        public static SocketRole GetRole(this DiscordSocketClient client, ulong id)
        {
            SocketRole result = null;
            foreach (SocketGuild guild in client.Guilds)
            {
                result = guild.GetRole(id);
                if (result != null)
                {
                    break;
                }
            }
            return result;
        }

        public static AccessLevel GetAccessLevel(this DiscordSocketClient client, ulong userId)
        {
            if (SettingsModel.UserIsBotAdmin(userId))
            {
                return AccessLevel.BotAdmin;
            }
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
                    }
                }
            }
            return AccessLevel.Basic;
        }

        public static bool WithinBounds(this Array array, int index)
        {
            return index > 0 && index < array.Length;
        }

        public static string GetCodeLocation([CallerFilePath] string file = "File Not Found", [CallerLineNumber] int lineNumber = 0)
        {
            return $"{file} at line {lineNumber}";
        }

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

        public static string GetEnumNames<T>() where T : Enum
        {
            return string.Join(", ", Enum.GetNames(typeof(T)));
        }
    }

    public struct EmbedField
    {
        public string Title;
        public object Value;
        public bool InLine;

        public EmbedField(string title, object value, bool inLine = false)
        {
            Title = title;
            Value = value;
            InLine = inLine;
        }
    }
}

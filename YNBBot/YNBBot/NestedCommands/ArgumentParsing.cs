using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace YNBBot.NestedCommands
{
    public static class ArgumentParsingHelper
    {
        public const string GENERIC_PARSED_USER = "A Discord User, specified either by a mention, the Discord Snowflake Id or the keyword \"self\"";
        public const string GENERIC_PARSED_ROLE = "A Guild Role, specified either by a mention or the Discord Snowflake Id";
        public const string GENERIC_PARSED_CHANNEL = "A Guild Channel, specified either by a mention, the Discord Snowflake Id or the keyword \"this\"";

        public static async Task<SocketUser> ParseUser(CommandContext context, string argument, bool allowMention = true, bool allowSelf = true, bool allowId = true)
        {
            SocketUser result = null;
            if (allowSelf && argument.Equals("self"))
            {
                result = context.User;
            }
            else if (allowMention && argument.StartsWith("<@") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(2, argument.Length - 3), out ulong userId))
                {
                    result = await context.Channel.GetUserAsync(userId) as SocketUser;
                }
            }
            else if (allowMention && argument.StartsWith("<@!") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(3, argument.Length - 3), out ulong userId))
                {
                    result = await context.Channel.GetUserAsync(userId) as SocketUser;
                }
            }
            else if (allowId && ulong.TryParse(argument, out ulong userId))
            {
                result = await context.Channel.GetUserAsync(userId) as SocketUser;
            }

            return result;
        }

        public static bool TryParseGuildUser(GuildCommandContext context, string argument, out SocketGuildUser result, bool allowMention = true, bool allowSelf = true, bool allowId = true)
        {
            result = null;
            if (allowSelf && argument.Equals("self"))
            {
                result = context.GuildUser;
                return true;
            }
            else if (allowMention && argument.StartsWith("<@") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(2, argument.Length - 3), out ulong userId))
                {
                    result = context.Guild.GetUser(userId);
                    return result != null;
                }
            }
            else if (allowMention && argument.StartsWith("<@!") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(3, argument.Length - 3), out ulong userId))
                {
                    result = context.Guild.GetUser(userId);
                    return result != null;
                }
            }
            else if (allowId && ulong.TryParse(argument, out ulong userId))
            {
                result = context.Guild.GetUser(userId);
                return result != null;
            }

            return false;
        }

        public static bool TryParseRole(GuildCommandContext context, string argument, out SocketRole result, bool allowMention = true, bool allowId = true)
        {
            result = null;

            if (allowMention && argument.StartsWith("<@&") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(3, argument.Length - 4), out ulong roleId))
                {
                    result = context.Guild.GetRole(roleId);
                    return result != null;
                }
            }
            else if (allowId && ulong.TryParse(argument, out ulong roleId))
            {
                result = context.Guild.GetRole(roleId);
                return result != null;
            }

            return false;
        }

        public static bool TryParseGuildChannel(GuildCommandContext context, string argument, out SocketGuildChannel result, bool allowMention = true, bool allowThis = true, bool allowId = true)
        {
            result = null;
            if (allowId && ulong.TryParse(argument, out ulong Id))
            {
                result = context.Guild.GetChannel(Id);
                return result != null;
            }
            if (allowMention && argument.StartsWith("<#") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(2, argument.Length - 3), out ulong Id2))
                {
                    result = context.Guild.GetTextChannel(Id2);
                    return result != null;
                }
            }
            if (allowThis && argument.Equals("this"))
            {
                result = context.GuildChannel;
                return true;
            }
            return false;
        }

        public static bool TryParseGuildTextChannel(CommandContext context, string argument, out SocketTextChannel result, bool allowMention = true, bool allowThis = true, bool allowId = true)
        {
            result = null;
            if (allowId && ulong.TryParse(argument, out ulong Id))
            {
                result = Var.client.GetChannel(Id) as SocketTextChannel;
                return result != null;
            }
            if (allowMention && argument.StartsWith("<#") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(2, argument.Length - 3), out ulong Id2))
                {
                    result = Var.client.GetChannel(Id2) as SocketTextChannel;
                    return result != null;
                }
            }
            if (allowThis && argument.Equals("this") && context.IsGuildContext)
            {
                result = (context as GuildCommandContext)?.GuildChannel;
                return result != null;
            }
            return false;
        }


    }
}
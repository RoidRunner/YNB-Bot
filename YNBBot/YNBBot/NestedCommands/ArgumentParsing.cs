using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace YNBBot.NestedCommands
{
    //public interface IArgument<T>
    //{
    //    string Error { get; }
    //    bool HasDefault { get; }
    //    bool TryParseArgument(CommandContext context, string argument, out T result);
    //    T GetDefault(CommandContext context);
    //}

    //public interface IGuildArgument<T> : IArgument<T>
    //{
    //    new string Error { get; }
    //    new bool HasDefault { get; }
    //    bool TryParseArgument(GuildCommandContext context, string argument, out T result);
    //    T GetDefault(GuildCommandContext context);
    //}

    //public class LiteralArgument : IArgument<string>
    //{
    //    public string CheckWord { get; private set; }

    //    public string Error => string.Empty;

    //    public bool HasDefault => false;

    //    public LiteralArgument(string checkWord)
    //    {
    //        if (checkWord.Contains(" "))
    //        {
    //            throw new ArgumentException("May not contain whitespaces!", "checkWord");
    //        }
    //        CheckWord = checkWord;
    //    }

    //    public string GetDefault(CommandContext context)
    //    {
    //        return default(string);
    //    }

    //    public bool TryParseArgument(CommandContext context, string argument, out string result)
    //    {
    //        result = string.Empty;
    //        return argument == CheckWord;
    //    }
    //}

    //#region Basic Value Arguments 

    //public class LongArgument : IArgument<long>
    //{
    //    public bool AllowNegative { get; private set; }

    //    public string Error => "Could not parse {0} to an Integer Number";

    //    public bool HasDefault => false;

    //    public LongArgument(bool allowNegative)
    //    {
    //        AllowNegative = allowNegative;
    //    }

    //    public bool TryParseArgument(CommandContext context, string argument, out long result)
    //    {
    //        if (long.TryParse(argument, out result))
    //        {
    //            return AllowNegative || result >= 0;
    //        }
    //        else
    //        {
    //            result = default(long);
    //            return false;
    //        }
    //    }

    //    public long GetDefault(CommandContext context)
    //    {
    //        return default(long);
    //    }
    //}

    //public class DoubleArgument : IArgument<double>
    //{
    //    public bool AllowNegative { get; private set; }

    //    public string Error => "Could not parse {0} to a double precision Float Number";

    //    public bool HasDefault => false;

    //    public DoubleArgument(bool allowNegative)
    //    {
    //        AllowNegative = allowNegative;
    //    }

    //    public double GetDefault(CommandContext context)
    //    {
    //        return default(double);
    //    }

    //    public bool TryParseArgument(CommandContext context, string argument, out double result)
    //    {
    //        if (double.TryParse(argument, out result))
    //        {
    //            return AllowNegative || result >= 0;
    //        }
    //        else
    //        {
    //            result = default(long);
    //            return false;
    //        }
    //    }
    //}

    //public class StringArgument : IArgument<string>
    //{
    //    public int? MinLength;
    //    public int? MaxLength;

    //    public string Error => "The argument {0} did not meet the length requirements for the command. " + $"{ (MinLength != null ? $" Min Length: {MinLength} " : "") }{ (MaxLength != null ? $" Max Length: {MaxLength}" : "") }";

    //    public bool HasDefault => false;

    //    public StringArgument(int? minLength = null, int? maxLength = null)
    //    {
    //        MinLength = minLength;
    //        MaxLength = maxLength;
    //    }

    //    public string GetDefault(CommandContext context)
    //    {
    //        return default(string);
    //    }

    //    public bool TryParseArgument(CommandContext context, string argument, out string result)
    //    {
    //        result = string.Empty;
    //        if ((MinLength != null) && argument.Length < MinLength)
    //        {
    //            return false;
    //        }
    //        else if ((MaxLength != null) && argument.Length > MaxLength)
    //        {
    //            return false;
    //        }
    //        else
    //        {
    //            result = argument;
    //            return true;
    //        }
    //    }
    //}

    //#endregion
    //#region Guild Object Arguments

    //public class GuildTextChannelArgument : IGuildArgument<SocketTextChannel>
    //{
    //    public bool AllowMention { get; private set; }
    //    public bool AllowThis { get; private set; }
    //    public bool AllowId { get; private set; }

    //    public string Error => "Could not parse {0} to a Discord Channel";
    //    public bool HasDefault => AllowThis;

    //    public GuildTextChannelArgument(bool allowMention = true, bool allowThis = true, bool allowId = true)
    //    {
    //        if (!allowMention && !allowId && !allowThis)
    //        {
    //            throw new ArgumentException("Atleast one parse option has to be allowed!");
    //        }
    //        AllowMention = allowMention;
    //        AllowThis = allowThis;
    //        AllowId = allowId;
    //    }

    //    public bool TryParseArgument(GuildCommandContext context, string argument, out SocketTextChannel result)
    //    {
    //        result = null;
    //        if (AllowId && ulong.TryParse(argument, out ulong Id))
    //        {
    //            result = context.Guild.GetChannel(Id) as SocketTextChannel;
    //            return true;
    //        }
    //        if (AllowMention && argument.StartsWith("<#") && argument.EndsWith('>') && argument.Length > 3)
    //        {
    //            if (ulong.TryParse(argument.Substring(2, argument.Length - 3), out ulong Id2))
    //            {
    //                result = context.Guild.GetChannel(Id2) as SocketTextChannel;
    //                return true;
    //            }
    //        }
    //        if (AllowThis && argument.Equals("this"))
    //        {
    //            result = context.GuildChannel;
    //            return true;
    //        }
    //        return false;
    //    }

    //    public override string ToString()
    //    {
    //        List<string> parseOptions = new List<string>(3);
    //        if (AllowMention)
    //        {
    //            parseOptions.Add("Channel Mention");
    //        }
    //        if (AllowThis)
    //        {
    //            parseOptions.Add("The keyword \"this\"");
    //        }
    //        if (AllowId)
    //        {
    //            parseOptions.Add("Channel Id (unsigned 64bit integer)");
    //        }
    //        return $"A Discord Server Text Channel that can be specified one of the following ways: {Macros.BuildListString(parseOptions)}";
    //    }

    //    public SocketTextChannel GetDefault(GuildCommandContext context)
    //    {
    //        return context.GuildChannel;
    //    }

    //    public bool TryParseArgument(CommandContext context, string argument, out SocketTextChannel result)
    //    {
    //        result = default(SocketTextChannel);
    //        return false;
    //    }

    //    public SocketTextChannel GetDefault(CommandContext context)
    //    {
    //        return default(SocketTextChannel);
    //    }
    //}

    //public class GuildUserArgument : IGuildArgument<SocketGuildUser>
    //{
    //    public bool AllowMention { get; private set; }
    //    public bool AllowSelf { get; private set; }
    //    public bool AllowId { get; private set; }

    //    public string Error => "Could not parse {0} to a User of this Guild";

    //    public bool HasDefault => AllowSelf;

    //    public GuildUserArgument(bool allowMention, bool allowSelf, bool allowId)
    //    {
    //        if (!allowMention && !allowId && !allowSelf)
    //        {
    //            throw new ArgumentException("Atleast one parse option has to be allowed!");
    //        }
    //        AllowMention = allowMention;
    //        AllowSelf = allowSelf;
    //        AllowId = allowId;
    //    }

    //    public SocketGuildUser GetDefault(GuildCommandContext context)
    //    {
    //        return context.GuildUser;
    //    }

    //    public bool TryParseArgument(GuildCommandContext context, string argument, out SocketGuildUser result)
    //    {
    //        result = null;
    //        if (AllowSelf && argument.Equals("self"))
    //        {
    //            result = context.GuildUser;
    //            return true;
    //        }
    //        else if (AllowMention && argument.StartsWith("<@") && argument.EndsWith('>') && argument.Length > 3)
    //        {
    //            if (ulong.TryParse(argument.Substring(2, argument.Length - 3), out ulong userId))
    //            {
    //                result = context.Guild.GetUser(userId);
    //                return result != null;
    //            }
    //        }
    //        else if (AllowMention && argument.StartsWith("<@!") && argument.EndsWith('>') && argument.Length > 3)
    //        {
    //            if (ulong.TryParse(argument.Substring(3, argument.Length - 3), out ulong userId))
    //            {
    //                result = context.Guild.GetUser(userId);
    //                return result != null;
    //            }
    //        }
    //        else if (AllowId && ulong.TryParse(argument, out ulong userId))
    //        {
    //            result = context.Guild.GetUser(userId);
    //            return result != null;
    //        }

    //        return false;
    //    }

    //    public bool TryParseArgument(CommandContext context, string argument, out SocketGuildUser result)
    //    {
    //        result = default(SocketGuildUser);
    //        return false;
    //    }

    //    public SocketGuildUser GetDefault(CommandContext context)
    //    {
    //        return default(SocketGuildUser);
    //    }
    //}

    //public class RoleArgument : IGuildArgument<SocketRole>
    //{
    //    public bool AllowMention { get; private set; }
    //    public bool AllowId { get; private set; }

    //    public string Error => "Could not parse {0} to a Role on this Guild";

    //    public bool HasDefault => false;

    //    public RoleArgument(bool allowMention, bool allowId)
    //    {
    //        if (!allowMention && !allowId)
    //        {
    //            throw new ArgumentException("Atleast one parse option has to be allowed!");
    //        }
    //        AllowMention = allowMention;
    //        AllowId = allowId;
    //    }

    //    public SocketRole GetDefault(GuildCommandContext context)
    //    {
    //        return null;
    //    }

    //    public bool TryParseArgument(GuildCommandContext context, string argument, out SocketRole result)
    //    {
    //        result = null;

    //        if (argument.StartsWith("<@&") && argument.EndsWith('>') && argument.Length > 3)
    //        {
    //            if (ulong.TryParse(argument.Substring(3, argument.Length - 3), out ulong roleId))
    //            {
    //                result = context.Guild.GetRole(roleId);
    //                return result != null;
    //            }
    //        }
    //        else if (ulong.TryParse(argument, out ulong roleId))
    //        {
    //            result = context.Guild.GetRole(roleId);
    //            return result != null;
    //        }

    //        return false;
    //    }

    //    public bool TryParseArgument(CommandContext context, string argument, out SocketRole result)
    //    {
    //        result = default(SocketRole);
    //        return false;
    //    }

    //    public SocketRole GetDefault(CommandContext context)
    //    {
    //        return default(SocketRole);
    //    }
    //}

    //#endregion

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
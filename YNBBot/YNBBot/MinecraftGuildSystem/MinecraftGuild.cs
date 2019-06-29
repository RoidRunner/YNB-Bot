using Discord;
using Discord.WebSocket;
using JSON;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using YNBBot.PagedStorageService;

namespace YNBBot.MinecraftGuildSystem
{
    class MinecraftGuild : IJSONSerializable
    {
        #region Fields and Properties

        private bool nameAndColorRetrieved = false;

        /// <summary>
        /// Channel Id of the Guilds private channel
        /// </summary>
        public ulong ChannelId;
        /// <summary>
        /// Role Id of the Guilds Role
        /// </summary>
        public ulong RoleId;
        /// <summary>
        /// Color assigned to the Guilds Role
        /// </summary>
        public GuildColor Color;
        /// <summary>
        /// Color converted to discords color system
        /// </summary>
        public Color DiscordColor
        {
            get
            {
                return ToDiscordColor(Color);
            }
        }

        /// <summary>
        /// Name of the Guild
        /// </summary>
        public string Name = "Name Not Found";
        public string Name_CommandSafe
        {
            get
            {
                if (Name.Contains(' '))
                {
                    return $"\"{Name}\"";
                }
                else
                {
                    return Name;
                }
            }
        }
        /// <summary>
        /// User Id of the Captain
        /// </summary>
        public ulong CaptainId;
        /// <summary>
        /// User Ids of the mates
        /// </summary>
        public List<ulong> MateIds = new List<ulong>();
        /// <summary>
        /// User Ids of the members
        /// </summary>
        public List<ulong> MemberIds = new List<ulong>();
        /// <summary>
        /// Timestamp when the guild was founded
        /// </summary>
        public DateTimeOffset FoundingTimestamp = DateTimeOffset.MinValue;

        #endregion
        #region Constructors

        public MinecraftGuild()
        {

        }

        public MinecraftGuild(ulong channelId, ulong roleId, GuildColor color, string name, ulong captainId)
        {
            ChannelId = channelId;
            RoleId = roleId;
            Color = color;
            Name = name;
            CaptainId = captainId;
            nameAndColorRetrieved = true;
            FoundingTimestamp = DateTimeOffset.UtcNow;
        }

        #endregion
        #region Name And Colors

        private const int DISCORDBLACK = 0x010000;

        /// <summary>
        /// Wether the name and color of the guild had been found. This fails when the client hadn't loaded the role when checking or the role got deleted
        /// </summary>
        public bool NameAndColorFound
        {
            get
            {
                if (nameAndColorRetrieved)
                {
                    return true;
                }
                return TryFindNameAndColor();
            }
        }

        /// <summary>
        /// Tries to find Name and Color by retrieving the guilds role
        /// </summary>
        /// <returns>True if name and color could be retrieved</returns>
        public bool TryFindNameAndColor()
        {
            if (Var.client.TryGetRole(RoleId, out SocketRole guildRole) && !nameAndColorRetrieved)
            {
                Color = (GuildColor)guildRole.Color.RawValue;
                if (guildRole.Color.RawValue == DISCORDBLACK)
                {
                    Color = GuildColor.black;
                }
                Name = guildRole.Name;
                nameAndColorRetrieved = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Converts a guild color to a discord color
        /// </summary>
        public static Color ToDiscordColor(GuildColor color)
        {
            if (color == GuildColor.black)
            {
                return new Color(DISCORDBLACK);
            }
            return new Color((uint)color);
        }

        #endregion
        #region Misc

        public bool UserIsInGuild(ulong userId)
        {
            return CaptainId == userId || MateIds.Contains(userId) || MemberIds.Contains(userId);
        }

        #endregion
        #region JSON

        private const string JSON_MEMBERIDS = "MemberIds";
        private const string JSON_MATEIDS = "MateIds";
        private const string JSON_CHANNELIDS = "ChannelId";
        private const string JSON_ROLEID = "RoleId";
        private const string JSON_CAPTAINID = "CaptainId";
        private const string JSON_FOUNDINGTIMESTAMP = "Founded";


        public bool FromJSON(JSONContainer json)
        {
            MemberIds.Clear();

            if (json.TryGetField(JSON_CHANNELIDS, out ChannelId) && json.TryGetField(JSON_ROLEID, out RoleId) && json.TryGetField(JSON_CAPTAINID, out CaptainId) && json.TryGetField(JSON_MEMBERIDS, out IReadOnlyList<JSONField> memberIdList))
            {
                foreach (JSONField memberIdJson in memberIdList)
                {
                    if (memberIdJson.IsNumber && !memberIdJson.IsSigned && !memberIdJson.IsFloat && !MemberIds.Contains(memberIdJson.Unsigned_Int64) && memberIdJson.Unsigned_Int64 != CaptainId)
                    {
                        MemberIds.Add(memberIdJson.Unsigned_Int64);
                    }
                }
                if (json.TryGetField(JSON_MATEIDS, out IReadOnlyList<JSONField> mateIdList))
                {
                    foreach (JSONField mateIdJson in mateIdList)
                    {
                        if (mateIdJson.IsNumber && !mateIdJson.IsSigned && !mateIdJson.IsFloat && !MateIds.Contains(mateIdJson.Unsigned_Int64))
                        {
                            MateIds.Add(mateIdJson.Unsigned_Int64);
                            if (MemberIds.Contains(mateIdJson.Unsigned_Int64))
                            {
                                MemberIds.Remove(mateIdJson.Unsigned_Int64);
                            }
                        }
                    }
                }
                if (json.TryGetField(JSON_FOUNDINGTIMESTAMP, out string timestamp_str))
                {
                    if (!DateTimeOffset.TryParseExact(timestamp_str, "u", CultureInfo.InvariantCulture, DateTimeStyles.None, out FoundingTimestamp))
                    {
                        FoundingTimestamp = DateTimeOffset.MinValue;
                    }
                }

                TryFindNameAndColor();

                return true;
            }
            return false;
        }

        public JSONContainer ToJSON()
        {
            JSONContainer result = JSONContainer.NewObject();

            result.TryAddField(JSON_CHANNELIDS, ChannelId);
            result.TryAddField(JSON_ROLEID, RoleId);
            result.TryAddField(JSON_CAPTAINID, CaptainId);
            result.TryAddField(JSON_FOUNDINGTIMESTAMP, FoundingTimestamp.ToString("u"));
            JSONContainer memberIdList = JSONContainer.NewArray();
            foreach (ulong id in MemberIds)
            {
                memberIdList.Add(id);
            }
            result.TryAddField(JSON_MEMBERIDS, memberIdList);
            JSONContainer mateIdList = JSONContainer.NewArray();
            foreach (ulong id in MateIds)
            {
                mateIdList.Add(id);
            }
            result.TryAddField(JSON_MATEIDS, mateIdList);
            return result;
        }

        #endregion
    }

    /// <summary>
    /// Enum of color codes Minecraft can display
    /// </summary>
    enum GuildColor
    {
        black = 0x000000,
        dark_blue = 0x0000AA,
        dark_green = 0x00AA00,
        dark_aqua = 0x00AAAA,
        dark_red = 0xAA0000,
        dark_purple = 0xAA00AA,
        gold = 0xFFAA00,
        gray = 0xAAAAAA,
        dark_gray = 0x555555,
        blue = 0x5555FF,
        green = 0x55FF55,
        aqua = 0x55FFFF,
        red = 0xFF5555,
        light_purple = 0xFF55FF,
        yellow = 0xFFFF55,
        white = 0xFFFFFFF
    }
}

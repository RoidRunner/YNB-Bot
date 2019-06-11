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
        public bool NameAndColorRetrieved { get; private set; } = false;

        public ulong ChannelId;
        public ulong RoleId;
        public GuildColor Color;
        public Color DiscordColor => new Color((uint)Color);
        public string Name;
        public ulong CaptainId;
        public List<ulong> MemberIds = new List<ulong>();
        public DateTimeOffset FoundingTimestamp = DateTimeOffset.MinValue;

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
            NameAndColorRetrieved = true;
            FoundingTimestamp = DateTimeOffset.UtcNow;
        }


        #region JSON

        private const string JSON_MEMBERIDS = "MemberIds";
        private const string JSON_CHANNELIDS = "ChannelId";
        private const string JSON_ROLEID = "RoleId";
        private const string JSON_CAPTAINID = "CaptainId";
        private const string JSON_FOUNDINGTIMESTAMP = "Founded";

        public bool TryRetrieveNameAndColor()
        {
            if (NameAndColorRetrieved)
            {
                return true;
            }
            if (Var.client.TryGetRole(RoleId, out SocketRole guildRole) && !NameAndColorRetrieved)
            {
                Color = (GuildColor)guildRole.Color.RawValue;
                Name = guildRole.Name;
                NameAndColorRetrieved = true;
                return true;
            }
            return false;
        }

        public bool FromJSON(JSONContainer json)
        {
            MemberIds.Clear();

            if (json.TryGetField(JSON_CHANNELIDS, out ChannelId) && json.TryGetField(JSON_ROLEID, out RoleId) && json.TryGetField(JSON_CAPTAINID, out CaptainId) && json.TryGetField(JSON_MEMBERIDS, out IReadOnlyList<JSONField> memberIdList))
            {
                foreach (JSONField memberIdJson in memberIdList)
                {
                    if (memberIdJson.IsNumber && !memberIdJson.IsSigned && !memberIdJson.IsFloat)
                    {
                        MemberIds.Add(memberIdJson.Unsigned_Int64);
                    }
                }
                if (json.TryGetField(JSON_FOUNDINGTIMESTAMP, out string timestamp_str))
                {
                    if (!DateTimeOffset.TryParseExact(timestamp_str, "u", CultureInfo.InvariantCulture, DateTimeStyles.None, out FoundingTimestamp))
                    {
                        FoundingTimestamp = DateTimeOffset.MinValue;
                    }
                }

                TryRetrieveNameAndColor();

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
            return result;
        }

        #endregion
    }

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

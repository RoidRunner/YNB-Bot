using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using YNBBot.PagedStorageService;

namespace YNBBot.GuildSystem
{
    class MinecraftGuild : PageStorable
    {
        public ulong ChannelId;
        public ulong RoleId;
        public GuildColor Color;
        public string Name;
        public ulong CaptainId;
        public List<ulong> MemberIds;

        public MinecraftGuild()
        {

        }

        #region JSON

        private const string JSON_MEMBERIDS = "MemberIds";
        private const string JSON_CHANNELIDS = "ChannelId";
        private const string JSON_ROLEID = "RoleId";
        private const string JSON_CAPTAINID = "CaptainId";

        internal override bool FromJSON(JSONObject json)
        {
            MemberIds.Clear();

            JSONObject memberIdList = json[JSON_MEMBERIDS];
            if (json.GetField(out string channelId_str, JSON_CHANNELIDS, null) && json.GetField(out string roleId_str, JSON_ROLEID, null) && json.GetField(out string captainId_str, JSON_CAPTAINID, null) && memberIdList != null)
            {
                if (ulong.TryParse(channelId_str, out ChannelId) && ulong.TryParse(roleId_str, out RoleId) && ulong.TryParse(captainId_str, out CaptainId) && memberIdList.IsArray)
                {
                    if (Var.client.TryGetRole(RoleId, out SocketRole guildRole))
                    {
                        Color = (GuildColor)guildRole.Color.RawValue;
                        Name = guildRole.Name;

                        foreach (JSONObject memberIdJson in memberIdList)
                        {
                            if (memberIdJson.IsString)
                            {
                                if (ulong.TryParse(memberIdJson.str, out ulong memberId))
                                {
                                    MemberIds.Add(memberId);
                                }
                            }
                        }

                        return MemberIds.Count >= 2;
                    }
                }
            }
            return false;
        }

        internal override JSONObject ToJSON()
        {
            JSONObject result = IdJSON;

            result.AddField(JSON_CHANNELIDS, ChannelId.ToString());
            result.AddField(JSON_ROLEID, RoleId.ToString());
            result.AddField(JSON_CAPTAINID, CaptainId.ToString());
            JSONObject memberIdList = new JSONObject();
            foreach (ulong id in MemberIds)
            {
                memberIdList.Add(id.ToString());
            }
            result.AddField(JSON_MEMBERIDS, memberIdList);
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

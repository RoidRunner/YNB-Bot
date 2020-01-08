using BotCoreNET.BotVars;
using JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YNBBot.Moderation;

namespace YNBBot.EventLogging
{
    class EventLoggerSettings : IGenericBotVar
    {
        public Dictionary<DiscordEventType, ulong> EventLogChannels = new Dictionary<DiscordEventType, ulong>();
        public Dictionary<ModerationType, ulong> UserModLogChannels = new Dictionary<Moderation.ModerationType, ulong>();
        public Dictionary<ChannelModerationType, ulong> ChannelModLogChannels = new Dictionary<ChannelModerationType, ulong>();

        public EventLoggerSettings()
        {

        }

        public bool ApplyJSON(JSONContainer json)
        {
            EventLogChannels.Clear();
            UserModLogChannels.Clear();
            ChannelModLogChannels.Clear();
            if (json.TryGetObjectField("EventLogChannels", out JSONContainer eventChannelsJSON))
            {
                foreach (JSONField field in eventChannelsJSON.Fields)
                {
                    if (Enum.TryParse(field.Identifier, out DiscordEventType type))
                    {
                        EventLogChannels.Add(type, field.Unsigned_Int64);
                    }
                }
            }
            if (json.TryGetObjectField("UserModLogChannels", out JSONContainer modChannelsJSON))
            {
                foreach (JSONField field in modChannelsJSON.Fields)
                {
                    if (Enum.TryParse(field.Identifier, out ModerationType type))
                    {
                        UserModLogChannels.Add(type, field.Unsigned_Int64);
                    }
                }
            }
            if (json.TryGetObjectField("ChannelModLogChannels", out JSONContainer channelsLogJSON))
            {
                foreach (JSONField field in channelsLogJSON.Fields)
                {
                    if (Enum.TryParse(field.Identifier, out ChannelModerationType type))
                    {
                        ChannelModLogChannels.Add(type, field.Unsigned_Int64);
                    }
                }
            }
            return true;
        }

        public JSONContainer ToJSON()
        {
            JSONContainer result = JSONContainer.NewObject();
            JSONContainer eventChannelsJSON = JSONContainer.NewObject();
            foreach (var channel in EventLogChannels)
            {
                eventChannelsJSON.TryAddField(channel.Key.ToString(), channel.Value);
            }
            result.TryAddField("EventLogChannels", eventChannelsJSON);
            JSONContainer userModChannelsJSON = JSONContainer.NewObject();
            foreach (var channel in UserModLogChannels)
            {
                userModChannelsJSON.TryAddField(channel.Key.ToString(), channel.Value);
            }
            result.TryAddField("UserModLogChannels", userModChannelsJSON);
            JSONContainer channelModChannelsJSON = JSONContainer.NewObject();
            foreach (var channel in ChannelModLogChannels)
            {
                channelModChannelsJSON.TryAddField(channel.Key.ToString(), channel.Value);
            }
            result.TryAddField("ChannelModLogChannels", channelModChannelsJSON);
            return result;
        }
    }
}

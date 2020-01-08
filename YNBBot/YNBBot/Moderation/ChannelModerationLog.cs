using JSON;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.Moderation
{
    class ChannelModerationLog : IJSONSerializable
    {
        public readonly GuildModerationLog Parent;
        public ulong ChannelId;
        private List<ChannelModerationEntry> moderationEntries = new List<ChannelModerationEntry>();
        public IReadOnlyList<ChannelModerationEntry> ModerationEntries => moderationEntries.AsReadOnly();

        public ChannelModerationLog(GuildModerationLog parent, ulong channelId = 0)
        {
            Parent = parent;
            ChannelId = channelId;
        }

        public Task Save()
        {
            JSONContainer json = ToJSON();
            return ResourcesModel.WriteJSONObjectToFile($"{Parent.ChannelDirectory}/{ChannelId}.json", json);
        }

        #region Locking
        #endregion
        #region JSON

        private const string JSON_CHANNELID = "Id";
        private const string JSON_MODENTRIES = "ModEntries";

        public bool FromJSON(JSONContainer json)
        {
            if (json.TryGetField(JSON_CHANNELID, out ChannelId))
            {
                if (json.TryGetArrayField(JSON_MODENTRIES, out JSONContainer jsonModEntries))
                {
                    foreach (JSONField field in jsonModEntries.Array)
                    {
                        if (field.IsObject)
                        {
                            ChannelModerationEntry entry = new ChannelModerationEntry(Parent.GuildId);
                            if (entry.FromJSON(field.Container))
                            {
                                moderationEntries.Add(entry);
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public JSONContainer ToJSON()
        {
            JSONContainer json = JSONContainer.NewObject();
            json.TryAddField(JSON_CHANNELID, ChannelId);
            if (moderationEntries.Count > 0)
            {
                JSONContainer jsonModEntries = JSONContainer.NewArray();
                foreach (ChannelModerationEntry entry in moderationEntries)
                {
                    jsonModEntries.Add(entry.ToJSON());
                }
                json.TryAddField(JSON_MODENTRIES, jsonModEntries);
            }
            return json;
        }

        #endregion
    }
}

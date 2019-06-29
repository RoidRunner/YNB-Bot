using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.MinecraftGuildSystem
{
    interface DesyncOption
    {
        string Description { get; }
        Task ExecuteAsync();
    }

    class DismissDesyncOption : DesyncOption
    {
        public string Description => "Dismiss";

        public Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }
    }

    class RemoveMemberDatasetDesyncOption : DesyncOption
    {
        public string Description {get; private set;}
        public MinecraftGuild Guild { get; private set; }
        public ulong UserId { get; private set; }

        public RemoveMemberDatasetDesyncOption(MinecraftGuild guild, ulong userId)
        {
            Guild = guild;
            UserId = userId;
            Description = $"Remove {userId} from memberlist of Guild \"{guild.Name}\"";
        }

        public Task ExecuteAsync()
        {
            Guild.MemberIds.Remove(UserId);
            Guild.MateIds.Remove(UserId);
            return MinecraftGuildModel.SaveAll();
        }
    }

    class DeleteGuildDatasetOption : DesyncOption
    {
        public string Description { get; private set; }
        public MinecraftGuild Guild { get; private set; }

        public DeleteGuildDatasetOption(MinecraftGuild guild)
        {
            Guild = guild;
            Description = $"Delete dataset of guild \"{guild.Name}\"";
        }

        public Task ExecuteAsync()
        {
            return MinecraftGuildModel.DeleteGuildDatasetAsync(Guild);
        }
    }

    class AddUserToDatasetOption : DesyncOption
    {
        public string Description { get; private set; }
        public MinecraftGuild Guild { get; private set; }
        public ulong UserId { get; private set; }

        public AddUserToDatasetOption(MinecraftGuild guild, ulong userId)
        {
            Guild = guild;
            UserId = userId;
            Description = $"Add {Macros.Mention_User(userId)} to dataset of guild \"{guild.Name}\"";
        }

        public Task ExecuteAsync()
        {
            Guild.MemberIds.Add(UserId);
            return MinecraftGuildModel.SaveAll();
        }
    }
}

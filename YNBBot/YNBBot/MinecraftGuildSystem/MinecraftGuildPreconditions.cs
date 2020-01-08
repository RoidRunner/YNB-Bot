using BotCoreNET.CommandHandling;
using System;
using System.Collections.Generic;
using System.Text;

namespace YNBBot.MinecraftGuildSystem
{
    class MinecraftGuildRankPrecondition : Precondition
    {
        private GuildRank RequiredRank;

        public MinecraftGuildRankPrecondition(GuildRank rank) : base(false, $"Have a rank of `{rank}` or higher in your Guild")
        {
            RequiredRank = rank;
        }

        public override bool PreconditionCheck(IDMCommandContext context, out string message)
        {
            if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out MinecraftGuild userGuild, true))
            {
                if (userGuild.Active)
                {
                    if (userGuild.GetMemberRank(context.User.Id) >= RequiredRank)
                    {
                        message = null;
                        return true;
                    }
                    else
                    {
                        message = $"You do not have the required rank of `{RequiredRank}` in {userGuild.Name}";
                        return false;
                    }
                }
                else
                {
                    message = $"Your guild {userGuild.Name} is inactive!";
                    return false;
                }
            }
            else
            {
                message = "You are not in a guild!";
                return false;
            }
        }
    }
}

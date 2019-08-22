using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using YNBBot.MinecraftGuildSystem;

namespace YNBBot.NestedCommands
{
    /// <summary>
    /// Represents a condition that is tested pre- command execution
    /// </summary>
    abstract class Precondition
    {
        /// <summary>
        /// Wether the precondition can only be tested in a guild context
        /// </summary>
        public readonly bool RequireGuild;
        /// <summary>
        /// A description describing the precondition
        /// </summary>
        public readonly string Description;

        public static readonly string AUTHCHECKPASSED = "AuthCheck passed!";

        /// <summary>
        /// Tests a context versus a precondition
        /// </summary>
        /// <param name="context">The context to check against</param>
        /// <param name="message">Errormessage that is set if the test fails</param>
        /// <returns>True, if the context could be validated</returns>
        public virtual bool IsAuthorized (CommandContext context, out string message)
        {
            throw new UnpopulatedMethodException("The base authorization check method has not been overriden!");
        }

        /// <summary>
        /// Tests a context versus a precondition
        /// </summary>
        /// <param name="context">The context to check against</param>
        /// <param name="message">Errormessage that is set if the test fails</param>
        /// <returns>True, if the context could be validated</returns>
        public virtual bool IsAuthorizedGuild (GuildCommandContext context, out string message)
        {
            throw new UnpopulatedMethodException("The guild authorization check method has not been overriden!");
        }

        public Precondition(bool requireGuild, string description)
        {
            RequireGuild = requireGuild;
            Description = description;
        }

        public override string ToString()
        {
            return Description;
        }
    }

     class AccessLevelAuthPrecondition : Precondition
    {
        public readonly AccessLevel RequiredAccessLevel;

        public AccessLevelAuthPrecondition(AccessLevel requiredAccessLevel) : base(false, $"Minimum Access Level: `{requiredAccessLevel}`")
        {
            RequiredAccessLevel = requiredAccessLevel;
        }

        public override bool IsAuthorized(CommandContext context, out string message)
        {
            if (context.UserAccessLevel >= RequiredAccessLevel)
            {
                message = AUTHCHECKPASSED;
                return true;
            }
            else
            {
                message = $"You don't have permission to use this command! It requires `{RequiredAccessLevel}` access, but you have only `{context.UserAccessLevel}` access!";
                return false;
            }
        }

        public static readonly AccessLevelAuthPrecondition MINECRAFT = new AccessLevelAuthPrecondition(AccessLevel.Minecraft);
        public static readonly AccessLevelAuthPrecondition ADMIN = new AccessLevelAuthPrecondition(AccessLevel.Admin);
    }

    class MinecraftGuildRankAuthPrecondition : Precondition
    {
        public readonly GuildRank RequiredGuildRank;

        public MinecraftGuildRankAuthPrecondition(GuildRank requiredGuildRank) : base(false, $"Minimum Guild Rank: `{requiredGuildRank}`")
        {
            RequiredGuildRank = requiredGuildRank;
        }

        public override bool IsAuthorized(CommandContext context, out string message)
        {
            if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out MinecraftGuild guild))
            {
                GuildRank userRank = guild.GetMemberRank(context.User.Id);
                if (userRank >= RequiredGuildRank)
                {
                    message = AUTHCHECKPASSED;
                    return true;
                }
                else
                {
                    message = $"You don't have permission to use this command! It requires a guild rank of `{RequiredGuildRank}`, but you are only `{userRank}`!";
                    return false;
                }
            }
            else
            {
                message = "You have to be in a guild to use this command!";
                return false;
            }
        }

        public static MinecraftGuildRankAuthPrecondition MATE = new MinecraftGuildRankAuthPrecondition(GuildRank.Mate);
        public static MinecraftGuildRankAuthPrecondition CAPTAIN = new MinecraftGuildRankAuthPrecondition(GuildRank.Captain);
    }
}

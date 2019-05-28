using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.Reactions
{
    static class UtilityReactionCommand
    {
        public static void Init()
        {
            ReactionService.AddReactionCommand(new ReactionCommand("getmessagelink", AccessLevel.Basic, HandleGetMessageLinkReaction));
        }

        public static async Task HandleGetMessageLinkReaction(ReactionContext context)
        {
            string messagelink = $"https://discordapp.com/channels/{context.Channel.Guild.Id}/{context.Channel.Id}/{context.Message.Id}";
            IDMChannel dmChannel = await context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(embed: new EmbedBuilder() { Title = $"Message link to requested message in #{context.Channel.Name}", Description = $"[{messagelink}]({messagelink})", Color = Var.BOTCOLOR }.Build());
        }
    }
}
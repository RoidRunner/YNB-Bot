using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YNBBot.NestedCommands;

namespace YNBBot.Reactions
{
    static class UtilityReactionCommand
    {
        public static void Init()
        {
            ReactionService.AddReactionCommand(new ReactionCommand("getmessagelink", AccessLevel.Basic, HandleGetMessageLinkReaction));
            ReactionService.AddReactionCommand(new ReactionCommand("getmessagecontent", AccessLevel.Basic, HandleGetMessageContentReaction));
        }

        public static async Task HandleGetMessageLinkReaction(ReactionContext context)
        {
            string messagelink = context.Message.GetMessageURL(context.Channel.Guild.Id);
            IDMChannel dmChannel = await context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(embed: new EmbedBuilder() { Title = $"Message link to requested message in #{context.Channel.Name}", Description = $"[{messagelink}]({messagelink})", Color = Var.BOTCOLOR }.Build());
        }

        public static async Task HandleGetMessageContentReaction(ReactionContext context)
        {
            string messageContent = context.Message.Content;
            if (string.IsNullOrWhiteSpace(messageContent))
            {
                messageContent = "Empty Message";
            }
            EmbedBuilder embed = new EmbedBuilder()
            {
                Title = $"Messagecontent of requested message in #{context.Channel.Name}",
                Description = Macros.MultiLineCodeBlock(Macros.MaxLength(messageContent.Replace("```", "[3`]"), EmbedHelper.EMBEDDESCRIPTION_MAX - 6)),
                Footer = new EmbedFooterBuilder()
                {
                    Text = "Multiline codeblock markers \"```\" are replaced with \"[3`]\""
                }
            };
            embed.AddField("Message Link", context.Message.GetMessageURL(context.Channel.Guild.Id));
            IDMChannel dmChannel = await context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(embed: embed.Build());
        }
    }
}
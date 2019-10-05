using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BotCoreNET;
using Discord;
using Discord.WebSocket;
using YNBBot.MinecraftGuildSystem;

namespace YNBBot.Interactive
{
    /// <summary>
    /// Represents a generic message that can be confirmed
    /// </summary>
    internal class GuildInvitationInteractiveMessage : InteractiveMessage
    {
        private MinecraftGuild Guild;
        private SocketGuildUser NewMember;

        public GuildInvitationInteractiveMessage(MinecraftGuild guild, SocketGuildUser newMember, IUserMessage message, ICollection<EmoteInteraction> interactions, long expirationTime = -1) : base(message, interactions, expirationTime)
        {
            Guild = guild;
            NewMember = newMember;
            AddMessageInteractionParams(new EmoteInteraction(UnicodeEmoteService.Checkmark, OnConfirm, false), new EmoteInteraction(UnicodeEmoteService.Cross, OnDeny, false));
        }

        private async Task<bool> OnConfirm(MessageInteractionContext context)
        {
            if (context.User.Id == NewMember.Id)
            {
                if (MinecraftGuildModel.TryGetGuildOfUser(NewMember.Id, out MinecraftGuild existingGuild, true))
                {
                    EmbedBuilder failure = new EmbedBuilder()
                    {
                        Title = "Failed",
                        Color = BotCore.ErrorColor,
                        Description = $"Already in guild \"{(existingGuild.NameAndColorFound ? existingGuild.Name : existingGuild.ChannelId.ToString())}\""
                    };
                    await context.Message.ModifyAsync(MessageProperties =>
                    {
                        MessageProperties.Embed = failure.Build();
                    });
                }
                else if (Guild != null)
                {
                    await MinecraftGuildModel.MemberJoinGuildAsync(Guild, NewMember);
                    EmbedBuilder success = new EmbedBuilder()
                    {
                        Title = "Success",
                        Color = Guild.DiscordColor,
                        Description = $"{NewMember.Mention} joined \"{Guild.Name}\""
                    };
                    await context.Message.ModifyAsync(MessageProperties =>
                    {
                        MessageProperties.Embed = success.Build();
                    });
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<bool> OnDeny(MessageInteractionContext context)
        {
            if (context.User.Id == NewMember.Id)
            {
                await GenericInteractionEnd(context.Message, "Invitation Dismissed");
                return true;
            }
            else if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out MinecraftGuild UserGuild))
            {
                if (Guild.CaptainId == context.User.Id || Guild.MateIds.Contains(context.User.Id))
                {
                    await GenericInteractionEnd(context.Message, "Invitation Retracted");
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Creates a new Message asking for confirmation by the user
        /// </summary>
        public static async Task<GuildInvitationInteractiveMessage> CreateConfirmationMessage(MinecraftGuild guild, SocketGuildUser newMember, Color color)
        {
            if (GuildChannelHelper.TryGetChannel(GuildChannelHelper.InteractiveMessagesChannelId, out SocketTextChannel channel))
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Title = $"Confirm you want to join \"{guild.Name}\"",
                    Color = color,
                };
                embed.AddField("Choices", $"{UnicodeEmoteService.Checkmark} - Confirm\n{UnicodeEmoteService.Cross} - Deny");
                var message = await channel.SendMessageAsync($"{ newMember.Mention} Invitation to join Guild \"{guild.Name}\"", embed: embed.Build());
                List<EmoteInteraction> interactions = new List<EmoteInteraction>(2);
                var result = new GuildInvitationInteractiveMessage(guild, newMember, message as IUserMessage, interactions);
                await message.AddReactionsAsync(new IEmote[] { UnicodeEmoteService.Checkmark, UnicodeEmoteService.Cross });

                return result;
            }
            else
            {
                return null;
            }
        }
    }
}

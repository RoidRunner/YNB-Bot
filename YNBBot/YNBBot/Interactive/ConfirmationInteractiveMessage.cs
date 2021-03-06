﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BotCoreNET;
using Discord;
using Discord.WebSocket;

namespace YNBBot.Interactive
{
    /// <summary>
    /// Represents a generic message that can be confirmed
    /// </summary>
    internal class ConfirmationInteractiveMessage : InteractiveMessage
    {
        public ConfirmationInteractiveMessage(IUserMessage message, ICollection<EmoteInteraction> interactions, long expirationTime = -1) : base(message, interactions, expirationTime)
        {
        }

        /// <summary>
        /// Creates a new Message asking for confirmation by the user
        /// </summary>
        public static Task<ConfirmationInteractiveMessage> CreateConfirmationMessage(string messageContent, string title, string description, MessageInteractionDelegate onConfirm, MessageInteractionDelegate onDeny)
        {
            return CreateConfirmationMessage(messageContent, title, BotCore.EmbedColor, description, UnicodeEmoteService.Checkmark, UnicodeEmoteService.Cross, onConfirm, onDeny);
        }

        /// <summary>
        /// Creates a new Message asking for confirmation by the user
        /// </summary>
        public static async Task<ConfirmationInteractiveMessage> CreateConfirmationMessage(string messageContent, string title, Color color, string description, IEmote confirmEmote, IEmote denyEmote, MessageInteractionDelegate onConfirm, MessageInteractionDelegate onDeny)
        {
            if (GuildChannelHelper.TryGetChannel(GuildChannelHelper.InteractiveMessagesChannelId, out SocketTextChannel channel))
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Title = title,
                    Color = color,
                    Description = description
                };
                embed.AddField("Choices", $"{confirmEmote.Name} - Confirm\n{denyEmote.Name} - Deny");
                var message = await channel.SendMessageAsync(messageContent, embed:embed.Build());
                List<EmoteInteraction> interactions = new List<EmoteInteraction>(2);
                interactions.Add(new EmoteInteraction(confirmEmote, onConfirm, false));
                interactions.Add(new EmoteInteraction(denyEmote, onDeny, false));
                var result = new ConfirmationInteractiveMessage(message as IUserMessage, interactions);
                await message.AddReactionsAsync(new IEmote[] { confirmEmote, denyEmote });

                return result;
            }
            else
            {
                return null;
            }
        }
    }
}

using BotCoreNET;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.Interactive
{
    /// <summary>
    /// Represents an individual specialized handler for emote reactions added to an assigned message
    /// </summary>
    class InteractiveMessage
    {
        /// <summary>
        /// The Id of the assigned message
        /// </summary>
        public ulong MessageId { get; private set; }
        /// <summary>
        /// The Id of the channel where the assigned message was posted
        /// </summary>
        public ulong ChannelId { get; private set; }
        /// <summary>
        /// The Id of the guild where the channel and message reside in
        /// </summary>
        public ulong GuildId { get; private set; }

        /// <summary>
        /// Time in millis since bot startup for when the message expires
        /// </summary>
        public long ExpirationTime { get; protected set; }

        private Dictionary<string, EmoteInteraction> Interactions = new Dictionary<string, EmoteInteraction>();

        public InteractiveMessage(IUserMessage message, ICollection<EmoteInteraction> interactions = null, long expirationTime = -1)
        {
            MessageId = message.Id;
            ChannelId = message.Channel.Id;
            GuildId = (ulong)(message.Channel as SocketTextChannel)?.Guild.Id;
            if (expirationTime == -1)
            {
                ExpirationTime = -1;
            }
            else
            {
                ExpirationTime = TimingThread.Millis + expirationTime;
            }
            if (interactions != null)
            {
                AddMessageInteractions(interactions);
            }

            InteractiveMessageService.AddInteractiveMessage(this);
        }

        /// <summary>
        /// Adds MessageInteractions
        /// </summary>
        protected void AddMessageInteractionParams(params EmoteInteraction[] interactions)
        {
            AddMessageInteractions(interactions);
        }

        /// <summary>
        /// Adds MessageInteractions
        /// </summary>
        protected void AddMessageInteractions(ICollection<EmoteInteraction> interactions)
        {
            foreach (EmoteInteraction interaction in interactions)
            {
                AddMessageInteraction(interaction);
            }
        }

        /// <summary>
        /// Adds a MessageInteraction
        /// </summary>
        protected void AddMessageInteraction(EmoteInteraction interaction)
        {
            interaction.MessageId = MessageId;
            Interactions.Add(interaction.Emote.Name, interaction);
        }

        /// <summary>
        /// Handles an interaction with this message
        /// </summary>
        /// <param name="context">MessageInteraction Context</param>
        public async Task HandleInteraction(MessageInteractionContext context)
        {
            if (ExpirationTime < TimingThread.Millis && ExpirationTime >= 0)
            {
                InteractiveMessageService.RemoveInteractiveMessage(MessageId);
                await OnMessageExpire(context);
            }
            else
            {
                if (await OnAnyEmoteAdded(context))
                {
                    InteractiveMessageService.RemoveInteractiveMessage(MessageId);
                }
                else if (Interactions.TryGetValue(context.Emote.Name, out EmoteInteraction interaction))
                {
                    await interaction.HandleAction(context);
                }
            }
        }

        public virtual Task OnMessageExpire(MessageInteractionContext context)
        {
            return context.Message.ModifyAsync(MessageProperties => { MessageProperties.Embed = GenericExpired.Build(); });
        }

        protected virtual Task<bool> OnAnyEmoteAdded(MessageInteractionContext context)
        {
            return Task.FromResult(false);
        }

        internal static readonly EmbedBuilder GenericSuccess = new EmbedBuilder() { Title = "Success", Color = BotCore.EmbedColor };
        internal static readonly EmbedBuilder GenericExpired = new EmbedBuilder() { Title = "Expired", Color = BotCore.ErrorColor };
        internal static readonly EmbedBuilder GenericFailure = new EmbedBuilder() { Title = "Failure", Color = BotCore.ErrorColor };

        internal static async Task GenericInteractionEnd(IUserMessage message, EmbedBuilder embed)
        {
            if (InteractiveMessageService.RemoveInteractiveMessage(message.Id))
            {
                await message.ModifyAsync(MessageProperties =>
                {
                    MessageProperties.Embed = embed.Build();
                });
            }
        }
        internal static async Task GenericInteractionEnd(IUserMessage message, string title)
        {
            if (InteractiveMessageService.RemoveInteractiveMessage(message.Id))
            {
                EmbedBuilder success = new EmbedBuilder() { Title = title, Color = BotCore.EmbedColor };
                await message.ModifyAsync(MessageProperties =>
                {
                    MessageProperties.Embed = success.Build();
                });
            }
        }
    }
}

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
            ExpirationTime = expirationTime;

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
                await OnMessageExpire();
            }
            else
            {
                if (Interactions.TryGetValue(context.Emote.Name, out EmoteInteraction interaction))
                {
                    await interaction.HandleAction(context);
                }
            }
        }

        public virtual Task OnMessageExpire() { return Task.CompletedTask; }

        internal static readonly EmbedBuilder GenericSuccess = new EmbedBuilder() { Title = "Success", Color = Var.BOTCOLOR };
        internal static readonly EmbedBuilder GenericExpired = new EmbedBuilder() { Title = "Expired", Color = Var.ERRORCOLOR };
        internal static readonly EmbedBuilder GenericFailure = new EmbedBuilder() { Title = "Failure", Color = Var.ERRORCOLOR };

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
                EmbedBuilder success = new EmbedBuilder() { Title = title, Color = Var.BOTCOLOR };
                await message.ModifyAsync(MessageProperties =>
                {
                    MessageProperties.Embed = success.Build();
                });
            }
        }
    }
}

using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.Interactive
{
    class InteractiveMessage
    {
        public ulong MessageId { get; private set; }
        public ulong ChannelId { get; private set; }
        public ulong GuildId { get; private set; }

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

        protected void AddMessageInteractionParams(params EmoteInteraction[] interactions)
        {
            AddMessageInteractions(interactions);
        }

        protected void AddMessageInteractions(ICollection<EmoteInteraction> interactions)
        {
            foreach (EmoteInteraction interaction in interactions)
            {
                AddMessageInteraction(interaction);
            }
        }

        protected void AddMessageInteraction(EmoteInteraction interaction)
        {
            interaction.MessageId = MessageId;
            Interactions.Add(interaction.Emote.Name, interaction);
        }

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

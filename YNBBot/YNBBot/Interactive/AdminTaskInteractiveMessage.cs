using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace YNBBot.Interactive
{
    class AdminTaskInteractiveMessage : InteractiveMessage
    {
        public static readonly Color Red = new Color(1f, 0f, 0f);
        public static readonly Color Green = new Color(0f, 1f, 0f);
        public static readonly EmbedFooterBuilder Footer = new EmbedFooterBuilder() { Text = $"{UnicodeEmoteService.Checkmark} = Done, {UnicodeEmoteService.Cross} = Remove Message" };

        public string Title { get; private set; }
        public bool Completed { get; private set; }

        public AdminTaskInteractiveMessage(IUserMessage message, string title) : base(message)
        {
            Title = title;
            Completed = false;
            AddMessageInteractionParams(new EmoteInteraction(UnicodeEmoteService.Checkmark, MarkTaskAsDone), new EmoteInteraction(UnicodeEmoteService.Cross, RemoveMessage));
        }

        private async Task<bool> MarkTaskAsDone(MessageInteractionContext context)
        {
            if (!Completed)
            {
                Completed = true;
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Title = Title,
                    Description = "Completed by " + context.User.Mention,
                    Color = Green
                };
                await context.Message.ModifyAsync(message => { message.Embed = embed.Build(); });
            }
            return false;
        }

        private async Task<bool> RemoveMessage(MessageInteractionContext context)
        {
            await context.Message.DeleteAsync();
            return true;
        }

        public static async Task<AdminTaskInteractiveMessage> CreateAdminTaskMessage(string taskTitle, string taskDescription)
        {
            if (GuildChannelHelper.TryGetChannel(GuildChannelHelper.AdminNotificationChannelId, out SocketTextChannel channel))
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Title = taskTitle,
                    Description = taskDescription,
                    Color = Red,
                    Footer = Footer
                };
                Discord.Rest.RestUserMessage message = await channel.SendEmbedAsync(embed);
                AdminTaskInteractiveMessage result = new AdminTaskInteractiveMessage(message as IUserMessage, taskTitle);
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

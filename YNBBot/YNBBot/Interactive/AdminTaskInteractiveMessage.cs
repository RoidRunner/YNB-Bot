using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace YNBBot.Interactive
{
    /// <summary>
    /// Represents a generic admin task that can be completed or dismissed with emote reactions
    /// </summary>
    class AdminTaskInteractiveMessage : InteractiveMessage
    {
        private static readonly Color Red = new Color(1f, 0f, 0f);
        private static readonly Color Green = new Color(0f, 1f, 0f);
        private static readonly EmbedFooterBuilder Footer = new EmbedFooterBuilder() { Text = $"{UnicodeEmoteService.Checkmark} = Done, {UnicodeEmoteService.Cross} = Remove Message" };

        /// <summary>
        /// Title of this task
        /// </summary>
        public string Title { get; private set; }
        /// <summary>
        /// Wether the task has been marked as complete
        /// </summary>
        public bool Completed { get; private set; }

        public AdminTaskInteractiveMessage(IUserMessage message, string title) : base(message)
        {
            Title = title;
            Completed = false;
            AddMessageInteractionParams(new EmoteInteraction(UnicodeEmoteService.Checkmark, MarkTaskAsComplete), new EmoteInteraction(UnicodeEmoteService.Cross, RemoveMessage));
        }

        private async Task<bool> MarkTaskAsComplete(MessageInteractionContext context)
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
            bool userIsAdmin = context.User.Id == context.Guild.OwnerId || context.User.Roles.Any(role => { return role.Permissions.Administrator == true; });

            if (userIsAdmin)
            {
                await context.Message.DeleteAsync();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Sends a new admin task
        /// </summary>
        /// <param name="taskTitle">Title of the task</param>
        /// <param name="taskDescription">Further information</param>
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

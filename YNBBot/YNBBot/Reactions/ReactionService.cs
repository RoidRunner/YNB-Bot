using BotCoreNET;
using BotCoreNET.Helpers;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.Reactions
{
    static class ReactionService
    {
        internal static Dictionary<string, ReactionCommand> ReactionCommands;

        static ReactionService()
        {
            ReactionCommands = new Dictionary<string, ReactionCommand>();
        }

        internal static void AddReactionCommand(ReactionCommand command)
        {
            ReactionCommands.TryAdd(command.Emote, command);
        }

        internal static async Task HandleReactionAdded(SocketTextChannel channel, SocketReaction reaction)
        {
            if (ReactionCommands.TryGetValue(reaction.Emote.Name, out ReactionCommand reactionCommand))
            {
                SocketGuildUser user = channel.Guild.GetUser(reaction.UserId);

                if (user != null)
                {
                    IUserMessage message = await channel.GetMessageAsync(reaction.MessageId) as IUserMessage;
                    if (message != null)
                    {
                        ReactionContext context = new ReactionContext(message, user, channel, reaction);
                        try
                        {
                            if (!reactionCommand.IsShitposting)
                            {
                                await reactionCommand.HandleReaction(context);
                            }
                        }
                        catch (Exception e)
                        {
                            await SendCommandExecutionExceptionMessage(e, context, reactionCommand);
                        }
                    }
                }
            }
        }

        private static async Task SendCommandExecutionExceptionMessage(Exception e, ReactionContext context, ReactionCommand command)
        {
            await context.Channel.SendEmbedAsync("Something went horribly wrong trying to execute your emojicommand! I have contacted my creators to help fix this issue!", true);
            if (GuildChannelHelper.TryGetChannel(GuildChannelHelper.DebugChannelId, out SocketTextChannel channel))
            {
                EmbedBuilder embed = new EmbedBuilder();
                embed.Color = BotCore.ErrorColor;
                embed.Title = "**__Exception__**";
                embed.AddField("Command", command.Emote);
                embed.AddField("Location", context.Channel.Mention);
                embed.AddField("Message", Markdown.MultiLineCodeBlock(e.Message));
                string stacktrace;
                if (e.StackTrace.Length <= 500)
                {
                    stacktrace = e.StackTrace;
                }
                else
                {
                    stacktrace = e.StackTrace.Substring(0, 500);
                }
                embed.AddField("StackTrace", Markdown.MultiLineCodeBlock(stacktrace));
                await channel.SendMessageAsync(embed: embed.Build());
            }
            await YNBBotCore.Logger(new LogMessage(LogSeverity.Error, "CMDSERVICE", string.Format("An Exception occured while trying to execute command `/{0}`.Message: '{1}'\nStackTrace {2}", command.Emote, e.Message, e.StackTrace)));
        }
    }

    internal delegate Task HandleReaction(ReactionContext context);

}

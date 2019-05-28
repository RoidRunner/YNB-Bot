using Discord;
using System;
using Discord.WebSocket;
using System.Collections;
using System.Collections.Generic;

namespace YNBBot.NestedCommands
{
    /// <summary>
    /// Represents a Command Context in any message channel (PM with the bot or guild text channel)
    /// </summary>
    public class CommandContext
    {
        /// <summary>
        /// The user that sent the command message
        /// </summary>
        public SocketUser User { get; private set; }
        /// <summary>
        /// The access level determined for the User
        /// </summary>
        public AccessLevel UserAccessLevel { get; private set; } = AccessLevel.Basic;
        /// <summary>
        /// The channel the command message was sent into
        /// </summary>
        public ISocketMessageChannel Channel { get; private set; }
        /// <summary>
        /// The command message
        /// </summary>
        public SocketUserMessage Message { get; private set; }
        /// <summary>
        /// All arguments of the command, stored in an index array
        /// </summary>
        public IndexArray<string> Args { get; private set; }
        /// <summary>
        /// True, if the command context is a guild context
        /// </summary>
        public bool IsGuildContext { get; protected set; }

        /// <summary>
        /// Creates a new command context based on a SocketUserMessage
        /// </summary>
        /// <param name="message">The socket user message that was identified as a command</param>
        public CommandContext(SocketUserMessage message)
        {
            User = message.Author;
            if (User != null)
            {
                UserAccessLevel = Var.client.GetAccessLevel(User.Id);
            }
            Channel = message.Channel;
            Message = message;
            Args = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (Args.TotalCount >= 1)
            {
                if (Args[0].Length > 0)
                {
                    Args[0] = Args[0].Substring(1);
                }
            }
            IsGuildContext = false;
        }

        /// <summary>
        /// True, if all required Fields are set
        /// </summary>
        public virtual bool IsDefined { get { return User != null && Channel != null && Message != null && Args != null; } }
    }

    /// <summary>
    /// Represents a Command Context in a Guild Text Channel
    /// </summary>
    public class GuildCommandContext : CommandContext
    {
        /// <summary>
        /// The Guild User that sent the command message
        /// </summary>
        public SocketGuildUser GuildUser { get; private set; }
        /// <summary>
        /// The Guild Channel the message was sent in
        /// </summary>
        public SocketTextChannel GuildChannel { get; private set; }
        /// <summary>
        /// Channelconfiguration of the GuildChannel
        /// </summary>
        public GuildChannelConfiguration ChannelConfig { get; private set; }
        /// <summary>
        /// The guild the message was sent in
        /// </summary>
        public SocketGuild Guild { get; private set; }

        /// <summary>
        /// Creates a GuildCommandContext based on a SocketUserMessage and a SocketGuild
        /// </summary>
        /// <param name="message">The socket user message that was identified as a command</param>
        /// <param name="guild">The guild where the user message was sent</param>
        public GuildCommandContext(SocketUserMessage message, SocketGuild guild) : base(message)
        {
            if (base.IsDefined)
            {
                GuildUser = guild.GetUser(message.Author.Id);
                GuildChannel = guild.GetTextChannel(Channel.Id);
                Guild = guild;
                IsGuildContext = true;
                ChannelConfig = GuildChannelHelper.GetChannelConfigOrDefault(GuildChannel);
            }
        }

        /// <summary>
        /// True, if all required Fields are set
        /// </summary>
        public override bool IsDefined { get { return base.IsDefined && GuildUser != null && GuildChannel != null && Guild != null; } }

        /// <summary>
        /// Attempts to convert a normal command context to a guild command context
        /// </summary>
        /// <param name="context">The normal CommandContext to convert</param>
        /// <param name="guildContext">The resulting GuildCommandContext</param>
        /// <returns>True, if conversion was successful</returns>
        public static bool TryConvert(CommandContext context, out GuildCommandContext guildContext)
        {
            guildContext = context as GuildCommandContext;
            return guildContext != null;
        }
    }
}
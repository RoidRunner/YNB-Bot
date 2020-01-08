namespace YNBBot.EventLogging
{
    public enum DiscordEventType
    {
        ChannelCreated,
        ChannelDestroyed,
        ChannelUpdated,

        GuildMemberUpdated,
        GuildUpdated,

        MessageDeleted,
        MessageBulkDeleted,
        MessageUpdated,

        RoleCreated,
        RoleDeleted,
        RoleUpdated,

        UserJoined,
        UserLeft,
        UserBanned,
        UserUnbanned,

        UserVoiceStatusUpdated
    }


}
namespace YNBBot.Moderation
{
    public enum ModerationType : byte
    {
        Note = 0,
        Warning = 1,
        Muted = 10,
        UnMuted = 11,
        Kicked = 20,
        Banned = 30,
        UnBanned = 31,
        Undefined = byte.MaxValue
    }

    public enum ChannelModerationType : byte
    {
        Locked = 0,
        Unlocked = 1,
        Purged = 2
    }
}

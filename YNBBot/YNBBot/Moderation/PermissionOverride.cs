using System;
using System.Collections.Generic;
using System.Text;

namespace YNBBot.Moderation
{
    struct PermissionOverride
    {
        public ulong TargetId;
        public bool IsUser;
        public ulong AllowPerms;
        public ulong DenyPerms;
    }
}

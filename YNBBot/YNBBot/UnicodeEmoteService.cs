using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace YNBBot
{
    static class UnicodeEmoteService
    {
        internal static readonly Emote Question = new Emote(Emotes.question);
        internal static readonly Emote Checkmark = new Emote(Emotes.checkmark);
        internal static readonly Emote Cross = new Emote(Emotes.cross);

        internal static readonly Emote[] Numbers = { new Emote(Emotes.zero), new Emote(Emotes.one), new Emote(Emotes.two), new Emote(Emotes.three), new Emote(Emotes.four), new Emote(Emotes.five), new Emote(Emotes.six), new Emote(Emotes.seven), new Emote(Emotes.eight), new Emote(Emotes.nine), new Emote(Emotes.ten) };

        internal static string GetEmote(Emotes emote)
        {
            switch (emote)
            {
                case Emotes.question:
                    return "\u2753";
                case Emotes.checkmark:
                    return "\u2705";
                case Emotes.cross:
                    return "\u274c";
                case Emotes.zero:
                    return "\u0030\u20E3";
                case Emotes.one:
                    return "\u0031\u20E3";
                case Emotes.two:
                    return "\u0032\u20E3";
                case Emotes.three:
                    return "\u0033\u20E3";
                case Emotes.four:
                    return "\u0034\u20E3";
                case Emotes.five:
                    return "\u0035\u20E3";
                case Emotes.six:
                    return "\u0036\u20E3";
                case Emotes.seven:
                    return "\u0037\u20E3";
                case Emotes.eight:
                    return "\u0038\u20E3";
                case Emotes.nine:
                    return "\u0039\u20E3";
                case Emotes.ten:
                    return "\u1F51F";
                default:
                    return null;
            }
        }

        public static bool TryParseEmoteToInt(IEmote emote, out int number)
        {
            switch (emote.Name)
            {
                case "\u0030\u20E3":
                    number = 0;
                    break;
                case "\u0031\u20E3":
                    number = 1;
                    break;
                case "\u0032\u20E3":
                    number = 2;
                    break;
                case "\u0033\u20E3":
                    number = 3;
                    break;
                case "\u0034\u20E3":
                    number = 4;
                    break;
                case "\u0035\u20E3":
                    number = 5;
                    break;
                case "\u0036\u20E3":
                    number = 6;
                    break;
                case "\u0037\u20E3":
                    number = 7;
                    break;
                case "\u0038\u20E3":
                    number = 8;
                    break;
                case "\u0039\u20E3":
                    number = 9;
                    break;
                case "\u1F51F":
                    number = 10;
                    break;
                default:
                    number = -1;
                    return false;
            }
            return true;
        }
    }

    public class Emote : IEmote
    {
        public string Name { get; private set; }

        public Emote (string emote)
        {
            Name = emote;
        }

        public Emote (Emotes emote)
        {
            Name = UnicodeEmoteService.GetEmote(emote);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public enum Emotes
    {
        question,
        checkmark,
        cross,
        zero,
        one,
        two,
        three,
        four,
        five,
        six,
        seven,
        eight,
        nine,
        ten
    }
}

namespace YNBBot.NestedCommands
{
    /// <summary>
    /// Contains parsing and help information for a command argument
    /// </summary>
    public class Argument
    {
        /// <summary>
        /// String identifier that represents the argument in syntax and help
        /// </summary>
        public readonly string Identifier;
        /// <summary>
        /// Help text that provides information on usage of the argument
        /// </summary>
        public readonly string Help;
        /// <summary>
        /// Wether the argument is optional or not
        /// </summary>
        public readonly bool Optional;
        /// <summary>
        /// Wether multiple arguments are allowed or not
        /// </summary>
        public readonly bool Multiple;

        /// <summary>
        /// Creates a new CommandArgument object
        /// </summary>
        /// <param name="identifier">String representation of the argument in syntax and help</param>
        /// <param name="help">Help text that provides information on usage of the argument</param>
        /// <param name="optional">Wether the argument is optional or not</param>
        /// <param name="multiple">Wether multiple arguments are allowed or not</param>
        public Argument(string identifier, string help, bool optional = false, bool multiple = false)
        {
            Identifier = identifier;
            Help = help;
            Optional = optional;
            Multiple = multiple;
        }

        public override string ToString()
        {
            string result = Identifier;
            if (Multiple)
            {
                result = $"[{result}]";
            }

            if (Optional)
            {
                result = $"({result})";
            }
            else
            {
                result = $"<{result}>";
            }
            return result;
        }
    }
}

using System;

namespace SenX_KOTH_Plugin.DiscordAPI
{
    // This is based off N4T4NM work => https://github.com/N4T4NM/CSharpDiscordWebhook
    public sealed class DiscordException : Exception
    {
        public DiscordException()
        {
        }

        public DiscordException(string message)
            : base(message)
        {
        }

        public DiscordException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

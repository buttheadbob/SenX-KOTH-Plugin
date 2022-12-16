using System.Drawing;

namespace SenX_KOTH_Plugin.DiscordAPI
{
    // This is based off N4T4NM work => https://github.com/N4T4NM/CSharpDiscordWebhook
    public static class Extensions
    {
        public static int? ToHex(this Color? color)
        {
            string HS =
                color?.R.ToString("X2") +
                color?.G.ToString("X2") +
                color?.B.ToString("X2");

            if (int.TryParse(HS, System.Globalization.NumberStyles.HexNumber, null, out int hex))
                return hex;

            return null;
        }

        public static Color? ToColor(this int? hex)
        {
            if (hex == null)
                return null;

            return ColorTranslator.FromHtml(hex?.ToString("X6"));
        }
    }
}

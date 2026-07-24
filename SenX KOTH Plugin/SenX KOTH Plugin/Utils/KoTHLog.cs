using System;
using NLog;

namespace SenX_KOTH_Plugin.Utils
{
    internal static class KoTHLog
    {
        private static bool DebugEnabled => SenX_KOTH_PluginMain.Instance?.Config?.DebugLoggingEnabled == false;

        public static void Info(Logger logger, string message)
        {
            if (DebugEnabled)
                logger.Info(message);
        }

        public static void Info(Logger logger, string format, params object[] args)
        {
            if (DebugEnabled)
                logger.Info(format, args);
        }

        public static void Warn(Logger logger, string message)
        {
            if (DebugEnabled)
                logger.Warn(message);
        }

        public static void Error(Logger logger, string message)
        {
            logger.Error(message);
        }

        public static void Error(Logger logger, Exception ex, string message)
        {
            logger.Error(ex, message);
        }

        public static void Error(Logger logger, Exception ex, string format, params object[] args)
        {
            logger.Error(ex, format, args);
        }
    }
}

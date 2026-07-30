using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using NiceIO;

namespace UnityZed
{
    public class ZedLogger
    {
        public static ILogger Create([CallerFilePath] string filePath = null)
        {
            var path = new NPath(filePath);
            var tag = path.FileNameWithoutExtension;

#if UNITY_ZED_DEBUG
            var handler = new LogHandler(tag, Debug.unityLogger.logHandler, true);
#else
            var handler = new LogHandler(tag, Debug.unityLogger.logHandler, false);
#endif

            return new Logger(handler);
        }

        private class LogHandler : ILogHandler
        {
            private readonly ILogHandler m_LogHandler;
            private readonly string m_Tag;
            private readonly bool m_LogMessages;

            public LogHandler(string tag, ILogHandler logHandler, bool logMessages)
            {
                m_Tag = tag;
                m_LogHandler = logHandler;
                m_LogMessages = logMessages;
            }

            public void LogException(Exception exception, UnityEngine.Object context)
                => m_LogHandler?.LogException(exception, context);

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                if (m_LogMessages || logType != LogType.Log)
                    m_LogHandler.LogFormat(logType, context, $"[{m_Tag}] {format}", args);
            }
        }
    }
}

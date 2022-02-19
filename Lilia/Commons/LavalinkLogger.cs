using System;
using Microsoft.Extensions.Logging;
using ILogger = Lavalink4NET.Logging.ILogger;
using LogLevel = Lavalink4NET.Logging.LogLevel;

namespace Lilia.Commons;

public class LavalinkLogger : ILogger
{
    private Microsoft.Extensions.Logging.ILogger _logger;

    public LavalinkLogger(Microsoft.Extensions.Logging.ILogger logger)
    {
        _logger = logger;
    }
#nullable enable
    public void Log(object source, string message, LogLevel level = LogLevel.Information, Exception? exception = null)
#nullable disable
    {
        switch (level)
        {
            case LogLevel.Trace:
                _logger.LogTrace(message);
                break;
            case LogLevel.Debug:
                _logger.LogDebug(message);
                break;
            case LogLevel.Information:
                _logger.LogInformation(message);
                break;
            case LogLevel.Error:
                _logger.LogError(exception, message);
                break;
            case LogLevel.Warning:
                _logger.LogWarning(message);
                break;
        }
    }
}
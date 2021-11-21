using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.IO;

namespace dotnet_i18n_translate;

internal sealed class SuperSimpleConsoleFormatter : ConsoleFormatter, IDisposable
{
	private readonly IDisposable _optionsReloadToken;
	private ConsoleFormatterOptions FormatterOptions { get; set; }

	public SuperSimpleConsoleFormatter(IOptionsMonitor<ConsoleFormatterOptions> options)
		: base(nameof(SuperSimpleConsoleFormatter))
	{
		FormatterOptions = options.CurrentValue;
		_optionsReloadToken = options.OnChange(o => FormatterOptions = o);
	}

	public void Dispose()
	{
		_optionsReloadToken?.Dispose();
	}

	public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
    {
        string text = logEntry.Formatter!(logEntry.State, logEntry.Exception);
        if (logEntry.Exception == null && text == null)
        {
            return;
        }
        
        LogLevel logLevel = logEntry.LogLevel;
        Exception? exception = logEntry.Exception;

        string? syslogSeverityString = GetLogLevelString(logLevel);
        if (syslogSeverityString is not null)
        {
            textWriter.Write(syslogSeverityString);
            textWriter.Write(": ");
        }

        string timestampFormat = FormatterOptions.TimestampFormat;

        if (timestampFormat is not null)
        {
            textWriter.Write(GetCurrentDateTime().ToString(timestampFormat));
            textWriter.Write(' ');
        }

        if (!string.IsNullOrEmpty(text))
        {
            WriteReplacingNewLine(textWriter, text);
        }

        if (exception is not null)
        {
            textWriter.Write(' ');
            WriteReplacingNewLine(textWriter, exception.ToString());
        }

        textWriter.Write(Environment.NewLine);

        static void WriteReplacingNewLine(TextWriter writer, string message)
        {
            string value = message.Replace(Environment.NewLine, " ");
            writer.Write(value);
        }
    }

    private DateTimeOffset GetCurrentDateTime() => FormatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;

    private static string? GetLogLevelString(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "trce",
        LogLevel.Debug => "dbug",
        //LogLevel.Information => "[info]",
        LogLevel.Warning => "warn",
        LogLevel.Error => "fail",
        LogLevel.Critical => "crit",
        _ => null,
    };
}
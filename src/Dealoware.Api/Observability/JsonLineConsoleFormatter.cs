using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Dealoware.Api.Observability;

/// <summary>
/// One JSON object per log line: <c>Time</c>, <c>Level</c>, <c>Category</c>, <c>Message</c> and
/// <c>TraceId</c> (the request ID, see <see cref="RequestIdMiddleware"/>), plus
/// <c>ExceptionType</c> and <c>StackTrace</c> when an exception is logged.
/// <para>
/// Field policy. Only the formatted message is written. The log entry's state (its named values)
/// and the scopes (which carry the request path and connection details) are never written, and a
/// logged exception gives only its type and stack trace, never its message: an exception message
/// can carry a value from a request or a database. EF Core is the one known logger that puts a
/// whole exception's text (with the database's own words, which can quote a value) inside its
/// message, after the first line; for its categories (<c>Microsoft.EntityFrameworkCore*</c>) the
/// message is cut at its first line break. The engine's own messages follow its rule: no email
/// address, name, token, password, code, key, IP address or request body is ever put in a log
/// message. LogPrivacyTests checks this.
/// </para>
/// <para>
/// Two safety nets on the message, for what the framework itself writes (its request lines, at
/// levels the engine does not normally log): a URL's query string is replaced by
/// <c>?&lt;query&gt;</c>, and anything shaped like an email address by <c>&lt;email&gt;</c>.
/// </para>
/// </summary>
public sealed class JsonLineConsoleFormatter : ConsoleFormatter
{
    public const string FormatterName = "dealoware-json";

    public JsonLineConsoleFormatter()
        : base(FormatterName)
    {
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message is null && logEntry.Exception is null)
        {
            return;
        }

        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteString("Time", DateTimeOffset.UtcNow.ToString("O"));
            json.WriteString("Level", logEntry.LogLevel.ToString());
            json.WriteString("Category", logEntry.Category);
            json.WriteString("Message", Redact(FirstLineFor(logEntry.Category, message ?? string.Empty)));
            var traceId = RequestIdMiddleware.CurrentTraceId();
            if (traceId is not null)
            {
                json.WriteString("TraceId", traceId);
            }

            if (logEntry.Exception is { } ex)
            {
                json.WriteString("ExceptionType", ExceptionTypes(ex));
                json.WriteString("StackTrace", ex.StackTrace ?? string.Empty);
            }

            json.WriteEndObject();
        }

        // Utf8JsonWriter escapes control characters, so a message can never break the line.
        textWriter.Write(System.Text.Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length));
        textWriter.Write(Environment.NewLine);
    }

    private static readonly Regex QueryString = new(
        @"((?:[A-Za-z][A-Za-z0-9+.\-]*://|/)[^\s?]*)\?\S*", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    private static readonly Regex EmailShaped = new(
        @"[A-Za-z0-9._%+\-]+(@|%40)[A-Za-z0-9.\-]+\.[A-Za-z]{2,}",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));

    public const string EfCoreCategoryPrefix = "Microsoft.EntityFrameworkCore";

    /// <summary>For EF Core's categories, the message up to its first line break; others unchanged.</summary>
    public static string FirstLineFor(string category, string message)
    {
        if (!category.StartsWith(EfCoreCategoryPrefix, StringComparison.Ordinal))
        {
            return message;
        }

        var end = message.IndexOfAny(['\r', '\n']);
        return end < 0 ? message : message[..end];
    }

    /// <summary>The message without query strings and email-shaped text (see the class notes).</summary>
    public static string Redact(string message)
    {
        try
        {
            var noQuery = message.Contains('?') ? QueryString.Replace(message, "$1?<query>") : message;
            return EmailShaped.Replace(noQuery, "<email>");
        }
        catch (RegexMatchTimeoutException)
        {
            return "<message withheld: too long to check>";
        }
    }

    /// <summary>The exception's type and its inner exceptions' types, outermost first. No messages.</summary>
    private static string ExceptionTypes(Exception ex)
    {
        var names = new List<string>();
        for (Exception? e = ex; e is not null && names.Count < 8; e = e.InnerException)
        {
            names.Add(e.GetType().FullName ?? e.GetType().Name);
        }

        return string.Join(" <- ", names);
    }
}

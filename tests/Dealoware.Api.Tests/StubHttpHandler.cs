using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace Dealoware.Api.Tests;

/// <summary>
/// An HTTP stand-in that records each request (method, address, headers and body, read when
/// sent) and answers with whatever <see cref="Respond"/> returns. No network is touched.
/// </summary>
public sealed class StubHttpHandler : HttpMessageHandler
{
    public sealed record Seen(HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers, string Body);

    private readonly ConcurrentQueue<Seen> _seen = new();

    public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage>? respond = null)
    {
        Respond = respond ?? (_ => new HttpResponseMessage(HttpStatusCode.OK));
    }

    public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; }

    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public IReadOnlyList<Seen> Requests => _seen.ToArray();

    public int Count => _seen.Count;

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
            headers[header.Key] = string.Join(",", header.Value);
        if (request.Content is not null)
        {
            foreach (var header in request.Content.Headers)
                headers[header.Key] = string.Join(",", header.Value);
        }

        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        _seen.Enqueue(new Seen(request.Method, request.RequestUri!, headers, body));

        if (Delay > TimeSpan.Zero)
            await Task.Delay(Delay, cancellationToken);

        return Respond(request);
    }
}

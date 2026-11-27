using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Dealoware.Domain.Participants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Dealoware.Api.Tests;

/// <summary>
/// What is not a fault of the engine keeps the server's own answer: a body over the limit is still
/// 413 (with the request ID), and a request the client gave up on writes no Error line. Neither may
/// raise the server-error or log-error alarms (T09 spec §4.4), which any client could otherwise do.
/// </summary>
public class RequestIdNotAFaultTests
{
    [Fact]
    public async Task ABodyOverTheLimit_IsStill413_WithTheId_AndNoErrorLine()
    {
        var capture = new FormatterCapture();
        using var baseFactory = new EnvironmentWebApplicationFactory("Production", EnvironmentWebApplicationFactory.TestSigningKey64);
        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(capture)));
        // A real Kestrel server on a free loopback port: the limit and its 413 are Kestrel's.
        factory.UseKestrel(options => options.Limits.MaxRequestBodySize = 10);
        factory.StartServer();
        using var client = factory.CreateClient();

        using var content = new StringContent("{\"displayName\":\"a body well over ten bytes\"}", System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/auth/register", content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Matches("^00-[0-9a-f]{32}-[0-9a-f]{16}-0[01]$", Assert.Single(response.Headers.GetValues("traceparent")));
        Assert.DoesNotContain(capture.Lines, l => JsonDocument.Parse(l).RootElement.GetProperty("Level").GetString() == "Error"
            && JsonDocument.Parse(l).RootElement.GetProperty("Category").GetString() == typeof(Dealoware.Api.Observability.RequestIdMiddleware).FullName);
    }

    [Fact]
    public async Task ABodyOverTheLimit_ReadInsideTheEndpoint_IsStill413_WithTheId_AndNoErrorLine()
    {
        // The read of an over-limit body inside an endpoint throws Kestrel's own
        // BadHttpRequestException (413). It must reach Kestrel, which answers it, not become a 500.
        var capture = new FormatterCapture();
        using var baseFactory = new EnvironmentWebApplicationFactory("Production", EnvironmentWebApplicationFactory.TestSigningKey64);
        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(capture));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IParticipantRepository>();
                services.AddScoped(_ => DispatchProxy.Create<IParticipantRepository, OverLimitReadProxy>());
            });
        });
        factory.UseKestrel(_ => { });
        factory.StartServer();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/auth/register", new { });

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Matches("^00-[0-9a-f]{32}-[0-9a-f]{16}-0[01]$", Assert.Single(response.Headers.GetValues("traceparent")));
        Assert.DoesNotContain(capture.Lines, l => JsonDocument.Parse(l).RootElement.GetProperty("Category").GetString() == typeof(Dealoware.Api.Observability.RequestIdMiddleware).FullName);
    }

    [Fact]
    public async Task ARequestTheClientGaveUpOn_WritesNoErrorLine()
    {
        var capture = new FormatterCapture();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var baseFactory = new EnvironmentWebApplicationFactory("Production", EnvironmentWebApplicationFactory.TestSigningKey64);
        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(capture));
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpContextAccessor();
                services.RemoveAll<IParticipantRepository>();
                services.AddScoped(sp =>
                {
                    var proxy = DispatchProxy.Create<IParticipantRepository, WaitForAbortProxy>();
                    var waiter = (WaitForAbortProxy)(object)proxy;
                    waiter.Accessor = sp.GetRequiredService<IHttpContextAccessor>();
                    waiter.Entered = entered;
                    return proxy;
                });
            });
        });
        using var client = factory.CreateClient();

        // Wait for this request's server span to stop: by then the whole pipeline has finished.
        var traceId = ActivityTraceId.CreateRandom();
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            // PropagationData: the host then makes its request activity from its source, so its stop is seen.
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.PropagationData,
            ActivityStopped = activity =>
            {
                if (activity.TraceId == traceId)
                {
                    finished.TrySetResult();
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        using var cancel = new CancellationTokenSource();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/register") { Content = JsonContent.Create(new { }) };
        request.Headers.TryAddWithoutValidation("traceparent", $"00-{traceId.ToHexString()}-00f067aa0ba902b7-01");
        var send = client.SendAsync(request, cancel.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(30));

        Assert.DoesNotContain(capture.Lines, l => JsonDocument.Parse(l).RootElement.GetProperty("Level").GetString() is "Error" or "Critical");
    }

    /// <summary>A repository whose every call fails the way an over-limit body read does.</summary>
    public class OverLimitReadProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => throw new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge);
    }

    /// <summary>A repository whose every call waits until the client gives up, then throws the cancellation.</summary>
    public class WaitForAbortProxy : DispatchProxy
    {
        public IHttpContextAccessor Accessor { get; set; } = null!;

        public TaskCompletionSource Entered { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var aborted = Accessor.HttpContext!.RequestAborted;
            Entered.TrySetResult();
            aborted.WaitHandle.WaitOne(TimeSpan.FromSeconds(30));
            aborted.ThrowIfCancellationRequested();
            throw new InvalidOperationException("the client did not give up");
        }
    }
}

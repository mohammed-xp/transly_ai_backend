using System.Net;

namespace TranslyAI.Api.Tests.Fakes;

public sealed class GeminiStubHandler : HttpMessageHandler
{
    private int _callCount;

    public int CallCount => Volatile.Read(ref _callCount);

    public Func<HttpResponseMessage> RespondWith { get; set; } =
        () => new HttpResponseMessage(HttpStatusCode.OK);

    public void Reset() => Interlocked.Exchange(ref _callCount, 0);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);
        return Task.FromResult(RespondWith());
    }
}

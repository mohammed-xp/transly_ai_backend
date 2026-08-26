namespace TranslyAI.Api.Tests.Fakes;

public sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    => Task.FromResult(response);
}
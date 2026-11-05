namespace TranslyAI.Api.Tests.Fakes;

public sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
{
    public string? RequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return response;
    }
}

using System.Net;
using System.Net.Http.Json;

namespace Predictorator.Tests.Helpers;

public class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly object _response;
    public int CallCount { get; private set; }

    public StubHttpMessageHandler(object response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        var msg = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(_response)
        };
        return Task.FromResult(msg);
    }
}

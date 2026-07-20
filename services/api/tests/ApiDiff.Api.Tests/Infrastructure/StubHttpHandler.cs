using System.Net;

namespace ApiDiff.Api.Tests.Infrastructure;

/// <summary>
/// Test HttpMessageHandler that records requests and replies from a scripted
/// per-route response map (matched by "METHOD path-prefix").
/// </summary>
public sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode status, string body)> _routes = new();

    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string> Bodies { get; } = [];

    public StubHttpHandler On(string method, string pathPrefix, HttpStatusCode status, string body)
    {
        _routes[$"{method} {pathPrefix}"] = (status, body);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));

        var path = request.RequestUri!.AbsolutePath;
        foreach (var (key, response) in _routes)
        {
            var parts = key.Split(' ', 2);
            if (request.Method.Method == parts[0] && path.StartsWith(parts[1], StringComparison.Ordinal))
            {
                return new HttpResponseMessage(response.status)
                {
                    Content = new StringContent(response.body),
                };
            }
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") };
    }
}

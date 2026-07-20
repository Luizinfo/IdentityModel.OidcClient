using IdentityModel.OidcClient.Browser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text;

namespace ConformanceTests;

internal sealed class SystemBrowser : IBrowser
{
    private readonly Uri _redirectUri;
    private readonly Action<string> _openBrowser;

    public SystemBrowser(Uri redirectUri, Action<string>? openBrowser = null)
    {
        _redirectUri = redirectUri;
        _openBrowser = openBrowser ?? OpenBrowser;
    }

    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken)
    {
        await using var listener = new LoopbackHttpListener(_redirectUri);
        _openBrowser(options.StartUrl);

        try
        {
            var response = await listener.WaitForCallbackAsync(TimeSpan.FromMinutes(5), cancellationToken);
            return string.IsNullOrWhiteSpace(response)
                ? new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = "Empty response." }
                : new BrowserResult { Response = response, ResultType = BrowserResultType.Success };
        }
        catch (OperationCanceledException exception)
        {
            return new BrowserResult { ResultType = BrowserResultType.Timeout, Error = exception.Message };
        }
        catch (TimeoutException exception)
        {
            return new BrowserResult { ResultType = BrowserResultType.Timeout, Error = exception.Message };
        }
        catch (Exception exception)
        {
            return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = exception.Message };
        }
    }

    private static void OpenBrowser(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}

internal sealed class LoopbackHttpListener : IAsyncDisposable
{
    private readonly Uri _redirectUri;
    private readonly IHost _host;
    private readonly TaskCompletionSource<string> _source =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public LoopbackHttpListener(Uri redirectUri)
    {
        if (!redirectUri.IsLoopback || redirectUri.Scheme != Uri.UriSchemeHttp)
        {
            throw new ArgumentException("The redirect URI must use HTTP on a loopback address.", nameof(redirectUri));
        }

        _redirectUri = redirectUri;
        _host = new HostBuilder()
            .ConfigureWebHost(builder =>
            {
                builder.UseKestrel();
                builder.UseUrls($"{redirectUri.Scheme}://{redirectUri.Authority}");
                builder.Configure(Configure);
            })
            .Build();
        _host.Start();
    }

    public Task<string> WaitForCallbackAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        return _source.Task.WaitAsync(timeout, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }

    private void Configure(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            if (!string.Equals(context.Request.Path, _redirectUri.AbsolutePath, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            string callback;
            if (HttpMethods.IsGet(context.Request.Method))
            {
                callback = context.Request.QueryString.Value ?? string.Empty;
            }
            else if (HttpMethods.IsPost(context.Request.Method))
            {
                if (!context.Request.HasFormContentType)
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    return;
                }

                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
                callback = await reader.ReadToEndAsync(context.RequestAborted);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return;
            }

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/html; charset=UTF-8";
            await context.Response.WriteAsync("<h1>You can now return to the application.</h1>", context.RequestAborted);
            _source.TrySetResult(callback);
        });
    }
}

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RockwellPlexServiceLibrary.Utils
{
    internal class HttpExecutionResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccessStatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public string Content { get; set; }
    }

    internal class HttpHandler : IDisposable
    {
        private const int MaxRetries = 3;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(100);

        private readonly HttpClient Client;
        private readonly ConnectionType ConnectionType;
        private readonly Authenticator Authenticator;
        private bool Disposed;

        public HttpHandler(Authenticator authenticator, ConnectionType connectionType)
        {
            Authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            ConnectionType = connectionType;

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            Client = new HttpClient(handler, true);
            Client.Timeout = Timeout;
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", BuildBasicAuthorizationValue(Authenticator));
        }

        public async Task<HttpExecutionResult> ExecuteDataSourceAsync(string datasourceId, string jsonBody, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(datasourceId))
            {
                throw new ArgumentException("Datasource ID is required.", nameof(datasourceId));
            }

            var requestUri = BuildDataSourceUri(datasourceId);
            var payload = string.IsNullOrWhiteSpace(jsonBody) ? "{}" : jsonBody;

            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using (var request = CreateRequest(requestUri, payload))
                    {
                        using (var response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false))
                        {
                            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                            if (ShouldRetry(response.StatusCode) && attempt < MaxRetries)
                            {
                                await DelayBeforeRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
                                continue;
                            }

                            return new HttpExecutionResult
                            {
                                StatusCode = response.StatusCode,
                                IsSuccessStatusCode = response.IsSuccessStatusCode,
                                ReasonPhrase = response.ReasonPhrase,
                                Content = content
                            };
                        }
                    }
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < MaxRetries)
                {
                    await DelayBeforeRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException) when (attempt < MaxRetries)
                {
                    await DelayBeforeRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    throw new HttpRequestException(
                        $"Error calling Plex datasource '{datasourceId}' at '{requestUri}'.",
                        ex);
                }
            }

            throw new HttpRequestException(
                $"Request to Plex datasource '{datasourceId}' failed after {MaxRetries} attempts.");
        }

        private HttpRequestMessage CreateRequest(string requestUri, string jsonBody)
        {
            return new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
        }

        private string BuildDataSourceUri(string datasourceId)
        {
            var host = ConnectionType == ConnectionType.Test
                ? "https://test.cloud.plex.com"
                : "https://cloud.plex.com";

            return $"{host}/api/datasources/{datasourceId}/execute?format=2";
        }

        private static bool ShouldRetry(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.RequestTimeout
                || (int)statusCode == 429
                || statusCode == HttpStatusCode.BadGateway
                || statusCode == HttpStatusCode.ServiceUnavailable
                || statusCode == HttpStatusCode.GatewayTimeout
                || (int)statusCode >= 500;
        }

        private static Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
        {
            var delay = TimeSpan.FromMilliseconds(250 * attempt);
            return Task.Delay(delay, cancellationToken);
        }

        private static string BuildBasicAuthorizationValue(Authenticator authenticator)
        {
            var credentials = $"{authenticator.Username}:{authenticator.Password}";
            var bytes = Encoding.UTF8.GetBytes(credentials);

            return Convert.ToBase64String(bytes);
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Client.Dispose();
            Disposed = true;
        }
    }
}

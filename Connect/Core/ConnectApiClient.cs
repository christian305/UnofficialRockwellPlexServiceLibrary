using RockwellPlexServiceLibrary.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RockwellPlexServiceLibrary.Connect.Core
{
    public class ConnectApiClient : IDisposable
    {
        private const int MaxRetries = 3;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(100);

        private readonly HttpClient Client;
        private readonly bool OwnsClient;
        private bool Disposed;

        public ConnectApiClient(Connection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            Client = CreateHttpClient(connection.Credentials);
            OwnsClient = true;
        }

        public ConnectApiClient(Authenticator authenticator, ConnectionType connectionType)
        {
            if (authenticator == null)
            {
                throw new ArgumentNullException(nameof(authenticator));
            }

            Client = CreateHttpClient(authenticator);
            OwnsClient = true;
        }

        public Task<ConnectApiResponse> GetAsync(
            string path,
            IDictionary<string, string> queryParameters = null,
            CancellationToken cancellationToken = default)
        {
            return SendAsync(HttpMethod.Get, path, null, queryParameters, cancellationToken);
        }

        public Task<ConnectApiResponse> PostAsync(
            string path,
            object body,
            IDictionary<string, string> queryParameters = null,
            CancellationToken cancellationToken = default)
        {
            return SendAsync(HttpMethod.Post, path, body, queryParameters, cancellationToken);
        }

        public Task<ConnectApiResponse> PutAsync(
            string path,
            object body,
            IDictionary<string, string> queryParameters = null,
            CancellationToken cancellationToken = default)
        {
            return SendAsync(HttpMethod.Put, path, body, queryParameters, cancellationToken);
        }

        public async Task<ConnectApiResponse> SendAsync(
            HttpMethod method,
            string path,
            object body = null,
            IDictionary<string, string> queryParameters = null,
            CancellationToken cancellationToken = default)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is required.", nameof(path));
            }

            var requestUri = BuildUri(path, queryParameters);
            var payload = body == null ? null : JsonSerializer.Serialize(body);

            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using (var request = CreateRequest(method, requestUri, payload))
                    {
                        using (var response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false))
                        {
                            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                            if (ShouldRetry(response.StatusCode) && attempt < MaxRetries)
                            {
                                await DelayBeforeRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
                                continue;
                            }

                            return new ConnectApiResponse
                            {
                                StatusCode = response.StatusCode,
                                IsSuccessStatusCode = response.IsSuccessStatusCode,
                                ReasonPhrase = response.ReasonPhrase,
                                Content = content
                            };
                        }
                    }
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < MaxRetries)
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
                        $"Error calling Connect API '{method} {requestUri}'.",
                        ex);
                }
            }

            throw new HttpRequestException($"Request to Connect API failed after {MaxRetries} attempts.");
        }

        private static HttpClient CreateHttpClient(Authenticator authenticator)
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var client = new HttpClient(handler, true);
            client.Timeout = Timeout;
            client.BaseAddress = new Uri("https://connect.plex.com/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", BuildBasicAuthorizationValue(authenticator));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RockwellPlexServiceLibrary/1.0");

            return client;
        }

        private static HttpRequestMessage CreateRequest(HttpMethod method, string requestUri, string payload)
        {
            var request = new HttpRequestMessage(method, requestUri);

            if (!string.IsNullOrWhiteSpace(payload))
            {
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            }

            return request;
        }

        private static string BuildUri(string path, IDictionary<string, string> queryParameters)
        {
            var normalizedPath = path.TrimStart('/');

            if (queryParameters == null || queryParameters.Count == 0)
            {
                return normalizedPath;
            }

            var query = string.Join(
                "&",
                queryParameters
                    .Where(i => !string.IsNullOrWhiteSpace(i.Key) && !string.IsNullOrWhiteSpace(i.Value))
                    .Select(i => $"{Uri.EscapeDataString(i.Key)}={Uri.EscapeDataString(i.Value)}"));

            return string.IsNullOrWhiteSpace(query)
                ? normalizedPath
                : $"{normalizedPath}?{query}";
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

            if (OwnsClient)
            {
                Client.Dispose();
            }

            Disposed = true;
        }
    }
}

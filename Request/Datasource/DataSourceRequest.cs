using RockwellPlexServiceLibrary.Utils;
using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RockwellPlexServiceLibrary.Request.Datasource
{
    public class DataSourceRequest : IDisposable
    {
        private readonly Connection Connection;
        private readonly bool OwnsConnection;
        private bool Disposed;


        public DataSourceRequest(Connection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            OwnsConnection = false;
        }

        public DataSourceRequest(Authenticator authenticator, ConnectionType connectionType)
            : this(new Connection(authenticator, connectionType))
        {
            OwnsConnection = true;
        }

        public Task<DataSourceResponse> ExecuteAsync(string datasourceId, object body, CancellationToken cancellationToken = default)
        {
            var jsonBody = JsonSerializer.Serialize(body ?? new object());
            return ExecuteAsync(datasourceId, jsonBody, cancellationToken);
        }

        public async Task<DataSourceResponse> ExecuteAsync(string datasourceId, string jsonBody, CancellationToken cancellationToken = default)
        {
            var result = await Connection.ExecuteDataSourceAsync(datasourceId, jsonBody, cancellationToken).ConfigureAwait(false);

            return new DataSourceResponse
            {
                StatusCode = result.StatusCode,
                IsSuccessStatusCode = result.IsSuccessStatusCode,
                ReasonPhrase = result.ReasonPhrase,
                Content = result.Content
            };
        }

        public Task<DataSourceResponse> ExecuteAsync(DataSourceDefinition definition, CancellationToken cancellationToken = default)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return ExecuteAsync(definition.DataSourceId, definition.ToBody(), cancellationToken);
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            if (OwnsConnection)
            {
                Connection.Dispose();
            }

            Disposed = true;
        }
    }

    public class DataSourceResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccessStatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public string Content { get; set; }
    }
}

using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RockwellPlexServiceLibrary.Utils
{
    //https://{if ConnectionType = Test ? Test : null}.cloud.plex.com/api/datasources/{datasourceId}/execute?format=2
    public enum ConnectionType
    {
        Production,
        Test
    }

    public class Connection : IDisposable
    {
        protected ConnectionType ConnectionType;
        protected Authenticator Authenticator;
        private readonly HttpHandler HttpHandler;
        private bool Disposed;

        internal ConnectionType Environment => ConnectionType;
        internal Authenticator Credentials => Authenticator;

        public Connection(Authenticator authenticator, ConnectionType connectionType)
        {
            Authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            ConnectionType = connectionType;
            HttpHandler = new HttpHandler(Authenticator, ConnectionType);
        }

        internal Task<HttpExecutionResult> ExecuteDataSourceAsync(string datasourceId, string jsonBody, CancellationToken cancellationToken = default)
        {
            return HttpHandler.ExecuteDataSourceAsync(datasourceId, jsonBody, cancellationToken);
        }

        public virtual void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            HttpHandler.Dispose();
            Disposed = true;
        }
    }
}

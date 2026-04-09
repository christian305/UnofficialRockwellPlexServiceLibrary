using RockwellPlexServiceLibrary.Connect.Core;
using RockwellPlexServiceLibrary.Utils;
using System;

namespace RockwellPlexServiceLibrary.Connect.Inventory.InventoryReceiving
{
    public class InventoryClient : IDisposable
    {
        private readonly ConnectApiClient ApiClient;
        private readonly bool OwnsApiClient;
        private bool Disposed;

        public InventoryReceivingClient Receiving { get; }

        public InventoryClient(Connection connection)
        {
            ApiClient = new ConnectApiClient(connection ?? throw new ArgumentNullException(nameof(connection)));
            OwnsApiClient = true;
            Receiving = new InventoryReceivingClient(ApiClient);
        }

        public InventoryClient(Authenticator authenticator, ConnectionType connectionType)
        {
            ApiClient = new ConnectApiClient(authenticator ?? throw new ArgumentNullException(nameof(authenticator)), connectionType);
            OwnsApiClient = true;
            Receiving = new InventoryReceivingClient(ApiClient);
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            if (OwnsApiClient)
            {
                ApiClient.Dispose();
            }

            Disposed = true;
        }
    }
}

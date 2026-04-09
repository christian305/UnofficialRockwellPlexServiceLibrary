using RockwellPlexServiceLibrary.Connect.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RockwellPlexServiceLibrary.Connect.Inventory.InventoryReceiving
{
    public class InventoryReceivingClient
    {
        private readonly ConnectApiClient ApiClient;

        public InventoryReceivingClient(ConnectApiClient apiClient)
        {
            ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public Task<ConnectApiResponse> GetReceiptAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Receipt ID is required.", nameof(id));
            }

            return ApiClient.GetAsync($"inventory/v1/inventory-receiving/receipts/{Uri.EscapeDataString(id)}", null, cancellationToken);
        }

        public Task<ConnectApiResponse> ListReceiptsAsync(IDictionary<string, string> queryParameters = null, CancellationToken cancellationToken = default)
        {
            return ApiClient.GetAsync("inventory/v1/inventory-receiving/receipts", queryParameters, cancellationToken);
        }

        public Task<ConnectApiResponse> CreateReceiptAsync(object body, CancellationToken cancellationToken = default)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            return ApiClient.PostAsync("inventory/v1/inventory-receiving/receipts", body, null, cancellationToken);
        }
    }
}
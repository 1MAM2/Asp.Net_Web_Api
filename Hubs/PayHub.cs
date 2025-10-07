using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;


namespace productApi.Hubs
{
    public class PayHub : Hub
    {
        public static readonly ConcurrentDictionary<string, string> TransactionConnections = new();

        public void RegisterTransaction(string id)
        {
            var connectionId = Context.ConnectionId;
            TransactionConnections[id] = connectionId;
        }
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            var item = TransactionConnections.FirstOrDefault(p => p.Value == connectionId);
            if (!string.IsNullOrEmpty(item.Key))
            {
                TransactionConnections.TryRemove(item.Key, out _);
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}
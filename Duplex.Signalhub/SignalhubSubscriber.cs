using System;
using System.Diagnostics;
using System.Linq;
using Duplex.EventSource;

namespace Duplex.Signalhub
{
    public class SignalhubSubscriber : IDisposable
    {
        private readonly EventSourceClient[] clients;

        public event EventHandler Disposed;

        public DateTime? LastHeartbeatReceived { get; protected set; } = null;

        public SignalhubSubscriber(string appName, string channel, Action<dynamic> callback, params Uri[] urls)
        {
            this.clients = urls.Select(uri => CreateClient(new Uri(uri, "/v1/" + appName + "/" + channel), callback)).ToArray();
        }

        private EventSourceClient CreateClient(Uri uri, Action<dynamic> callback)
        {
            var client = new EventSourceClient(uri);
            client.Event += (sender, e) => { Debug.WriteLine("[SignalhubSubscriber] Event Received"); callback(e.Data); };
            client.Heartbeat += (sender, e) => { LastHeartbeatReceived = DateTime.Now; Debug.WriteLine("[SignalhubSubscriber] Heartbeat"); };
            client.Start();
            return client;
        }

        ~SignalhubSubscriber()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (var i = 0; i < this.clients.Length; i++)
                    this.clients[i]?.Dispose();

                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}


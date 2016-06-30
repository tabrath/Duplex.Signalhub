using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Duplex.Signalhub
{
    public class SignalhubClient : IDisposable
    {
        private readonly Uri[] urls;
        private readonly List<SignalhubSubscriber> subscribers;
        private readonly string appName;

        public string Name { get; protected set; }
        public string Version { get; protected set; }
        public int Subscribers { get; protected set; }

        public SignalhubClient(string appName, params Uri[] urls)
        {
            this.appName = appName;
            this.urls = urls;
            this.subscribers = new List<SignalhubSubscriber>();

            GetServerInfo(urls)
                .ContinueWith(t =>
                {
                    Name = t.Result.name;
                    Version = t.Result.version;
                    Subscribers = t.Result.subscribers;
                }).Wait(1000);
        }

        ~SignalhubClient()
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
                while (this.subscribers.Count > 0)
                    this.subscribers.First().Dispose();
            }
        }

        public SignalhubSubscriber Subscribe(string channel, Action<dynamic> callback)
        {
            var subscriber = new SignalhubSubscriber(this.appName, channel, callback, this.urls);
            subscriber.Disposed += (sender, e) => this.subscribers.Remove((SignalhubSubscriber)sender);
            this.subscribers.Add(subscriber);
            return subscriber;
        }

        public Task<bool> Broadcast<T>(string channel, T message)
        {
            return Task.WhenAll(this.urls.Select(url => Broadcast(url, this.appName, channel, message))).ContinueWith(t => t.Result.All(r => r));
        }

        private static async Task<bool> Broadcast<T>(Uri uri, string appName, string channel, T message)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    await client.UploadStringTaskAsync(new Uri(uri, "/v1/" + appName + "/" + channel), "POST", JsonConvert.SerializeObject(message, Formatting.None));
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Task<dynamic> GetServerInfo(params Uri[] urls)
        {
            return Task.WhenAny(urls.Select(uri => GetServerInfo(uri))).Unwrap();
        }

        private static async Task<dynamic> GetServerInfo(Uri uri)
        {
            string json;
            using (var client = new WebClient())
            {
                json = await client.DownloadStringTaskAsync(uri);
            }
            return JsonConvert.DeserializeObject(json);
        }
    }
}


using System;
using Duplex.Signalhub;

namespace Duplex.SignalhubConsole
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (var hub = new SignalhubClient("testapp", new Uri("http://localhost:8080")))
            {
                Console.WriteLine("Name: {0}, Version: {1}, Subscribers: {2}", hub.Name, hub.Version, hub.Subscribers);
                hub.Subscribe("testchannel", (obj) => Console.WriteLine("Event: " + obj));
                hub.Broadcast("testchannel", new { hello = "world", from = "net" })
                   .ContinueWith(t =>
               {
                   if (t.IsFaulted)
                   {
                       Console.WriteLine("Broadcast faulted: " + t.Exception.Flatten());
                       return;
                   }

                   Console.WriteLine("Broadcast was sent: " + t.IsCompleted);
               });

                Console.ReadLine();
            }
        }
    }
}

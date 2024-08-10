using System.Net.Sockets;
using System.Net;

namespace Project1
{
    internal class HttpListenerManager(string protocol, string port, string ipFallback)
    {
        private readonly string _port = port;
        private readonly string _protocol = protocol;
        private readonly string _ipFallback = ipFallback;

        public HttpListener Listener()
        {
            HttpListener httpListener = new();
            string prefix = ResolveHttpPrefix();
            httpListener.Prefixes.Add(prefix);
            httpListener.Start();

            Console.WriteLine($"[Server]: Started at {prefix}");

            return httpListener;
        }

        private string ResolveHttpPrefix()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            string prefix = $"{_protocol}://{_ipFallback}:{_port}/";

            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    prefix = $"{_protocol}://{address}:{_port}/";
                    break;
                }
            }

            return prefix;
        }
    }
}

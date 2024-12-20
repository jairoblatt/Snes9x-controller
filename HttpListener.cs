﻿using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

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

            Console.WriteLine();
            Console.WriteLine(@" ___________            __              ._.");
            Console.WriteLine(@" \_   _____/ ____      |__| ____ ___.__.| |");
            Console.WriteLine(@"  |    __)_ /    \\    |  |/  _ |   |  || |");
            Console.WriteLine(@"  |        \   |  \\   |  (  <_> )___  | \|");
            Console.WriteLine(@"  \______  /___|  /\\__|  |\____// ____| __");
            Console.WriteLine(@"         \/     \/\_______|      \/      \/");
            Console.WriteLine();

            Console.WriteLine($" Your Snes9x controllers is available at:");

            string[] paths = Controller.GetPaths();

            for (var index = 0; index < paths.Length; index++)
            {
                Console.WriteLine($" {prefix}{paths[index].Replace("/", "")}");
            }

            Console.WriteLine();

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

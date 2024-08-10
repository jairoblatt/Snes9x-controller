﻿using System;
using System.ComponentModel.Design;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using static Program;

internal class Program
{
    enum Commands
    {
        Select = 1,
        Start = 2,
        Up = 3,
        Down = 4,
        Left = 5,
        Right = 6,
        A = 7,
        B = 8,
        Y = 9,
        X = 10
    }

    enum Actions
    {
        Up,
        Down
    }

    class SocketPayload
    {
        public Commands command { get; set; }
        public Actions action { get; set; }
        public string? timestamp { get; set; }
    }

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    internal static extern ushort MapVirtualKey(ushort wCode, uint wMapType);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public int type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    internal const int INPUT_KEYBOARD = 1;
    internal const uint KEYEVENTF_KEYDOWN = 0;
    internal const uint KEYEVENTF_KEYUP = 0x0002;

    internal const string HTTP_LISTENER_PORT = "9999";
    internal const string HTTP_LISTENER_PROTOCOL = "https";
    internal const string HTTP_LISTENER_IP_FALLBACK = "192.168.1.6";

    internal static async Task Main()
    {
        HttpListener httpListener = CreateHttpListener(
            HTTP_LISTENER_PROTOCOL,
            HTTP_LISTENER_PORT,  
            HTTP_LISTENER_IP_FALLBACK
          );

        while (true)
        {
            HttpListenerContext httpContext = await httpListener.GetContextAsync();

            string? absolutePath = null;

            if (httpContext.Request.Url != null)
            {
                absolutePath = httpContext.Request.Url.AbsolutePath;
            }

            if (httpContext.Request.IsWebSocketRequest)
            {
                await HandleWebSocketConnection(httpContext);
            }
            else if (absolutePath == "/controller")
            {
                await HandleController(httpContext);
            }
            else
            {
                httpContext.Response.StatusCode = 400;
                httpContext.Response.Close();
            }
        }
    }

    internal static HttpListener CreateHttpListener(string protocol, string port, string ipFallback)
    {
        HttpListener httpListener = new();
        string prefix = ResolveHttpPrefix(protocol, port, ipFallback);
        httpListener.Prefixes.Add(prefix);
        httpListener.Start();
        
        Console.WriteLine($"[Server]: Start at {prefix}");

        return httpListener;
    }

    internal static string ResolveHttpPrefix(string protocol, string port, string ipFallback)
    {
        string hostName = Dns.GetHostName();
        IPAddress[] addresses = Dns.GetHostAddresses(hostName);
        string prefix = $"{protocol}://{ipFallback}:{port}/";

        foreach (IPAddress address in addresses)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                prefix = $"{protocol}://{address}:{port}/";
            }
        }

        return prefix;
    }

    internal static async Task HandleController(HttpListenerContext httpContext)
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "controller.html");

        if (File.Exists(filePath))
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            httpContext.Response.ContentType = "text/html";
            httpContext.Response.ContentLength64 = fileBytes.Length;
            await httpContext.Response.OutputStream.WriteAsync(fileBytes, 0, fileBytes.Length);
            httpContext.Response.StatusCode = 200;
        }
        else
        {
            httpContext.Response.StatusCode = 404;
        }

        httpContext.Response.Close();
    }

    internal static async Task HandleWebSocketConnection(HttpListenerContext httpContext)
    {
        byte[] buffer = new byte[1024];
        HttpListenerWebSocketContext webSocketContext = await httpContext.AcceptWebSocketAsync(null);
        WebSocket webSocket = webSocketContext.WebSocket;

        Console.WriteLine("[Socket]: Client connected");

        try
        {

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("[Socket]: Client initiated close handshake");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    Console.WriteLine("[Socket]: Client disconnected");
                }
                else
                {
                    string payload = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    SocketPayload socketPayload = JsonSerializer.Deserialize<SocketPayload>(payload);

                    if (socketPayload == null)
                    {
                        Console.WriteLine("[Socket]: Payload malformed");
                    }
                    else
                    {

                        if (Enum.IsDefined(typeof(Commands), socketPayload.command) && Enum.IsDefined(typeof(Actions), socketPayload.action))
                        {
                            SimulateKeyPress(socketPayload);
                        }
                        else
                        {
                            Console.WriteLine("[Socket]: Invalid action and/or command");
                        }
                    }

                }
            }
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"WebSocket error: {ex.Message}");
            Console.WriteLine($"WebSocket state: {webSocket.State}");
        }
        finally
        {
            if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
            {
                Console.WriteLine("[Socket]: Closing WebSocket due to error or client disconnection");
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing due to error", CancellationToken.None);
            }
        }
    }

    static void SimulateKeyPress(SocketPayload payload)
    {
        Dictionary<Commands, ushort> CommandToVkCodeMap = new()
        {
            // VK_RETURN
            { Commands.Select, 0x0D },

            // VK_SPACE
            { Commands.Start, 0x20 },

            // VK_UP
            { Commands.Up, 0x26 },

            // VK_DOWN
            { Commands.Down, 0x28 },

            // VK_LEFT
            { Commands.Left, 0x25 },

            // VK_RIGHT
            { Commands.Right, 0x27 },

            // V
            { Commands.A, 0x56 },

            // C
            { Commands.B, 0x43 },

            // X
            { Commands.Y, 0x58 },

            // D
            { Commands.X, 0x44 }
        };

        if (!CommandToVkCodeMap.TryGetValue(payload.command, out ushort vkCode))
        {
            Console.WriteLine($"[Simulate]: Command {payload.command} not found in the map.");
        }
        else
        {

            IntPtr hWnd = GetSnesWindow();

            if (hWnd == IntPtr.Zero)
            {
                Console.WriteLine("[Simulate]: Unable to find Snes9x window.");
            }
            else
            {
                SetForegroundWindow(hWnd);

                Console.WriteLine($"[Simulate]: " +
                    $"\n Command: {payload.command} " +
                    $"\n Action: {payload.action} " +
                    $"\n Virtual Code: {vkCode} " +
                    $"\n Timestamp: {payload.timestamp} " +
                    $"\n");

                Thread.Sleep(1);
                SendInputCommand(payload.action == Actions.Down ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP, vkCode);
            }
        }
    }

    static void SendInputCommand(uint flag, ushort virtualCode)
    {
        INPUT input = new()
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    dwFlags = flag,
                    wVk = virtualCode
                }
            }
        };

        SendInput(1, [input], Marshal.SizeOf(typeof(INPUT)));
    }

    static IntPtr GetSnesWindow()
    {
        string? snesTitle = GetSnesWindowTitle();

        if (String.IsNullOrEmpty(snesTitle))
        {
            return IntPtr.Zero;

        }

        return FindWindow(null, snesTitle);
    }

    static string? GetSnesWindowTitle()
    {
        string? title = null;

        EnumWindows((hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd))
            {
                StringBuilder windowText = new(256);

                GetWindowText(hWnd, windowText, windowText.Capacity);

                string windowTitle = windowText.ToString();

                if (!string.IsNullOrEmpty(windowTitle) && windowTitle.Contains("Snes9x"))
                {
                    title = windowTitle;
                }
            }

            return true;

        }, IntPtr.Zero);


        return title;
    }
}

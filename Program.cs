using Project1;
using System;
using System.Net;
using System.Net.WebSockets;
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
        public string? player { get; set; }
        public string? token { get; set; }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

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

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYDOWN = 0;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const string HTTP_LISTENER_PORT = "9999";
    private const string HTTP_LISTENER_PROTOCOL = "https";
    private const string HTTP_LISTENER_IP_FALLBACK = "192.168.1.6";

    internal static async Task Main()
    {
        var snesWindowTitle = new Input("Snes9x", () => Snes9x.GetSnesWindowTitles()).RequestWindowTitle();

        if (!String.IsNullOrEmpty(snesWindowTitle))
        {
            var httpListener = new HttpListenerManager(
                HTTP_LISTENER_PROTOCOL,
                HTTP_LISTENER_PORT,
                HTTP_LISTENER_IP_FALLBACK
             ).Listener();

            while (true)
            {
                var httpContext = await httpListener.GetContextAsync();

                if (httpContext.Request.IsWebSocketRequest)
                {
                    await HandleWebSocketConnection(snesWindowTitle, httpContext);
                }
                else if (Controller.MatchPath(httpContext))
                {
                    await Controller.Handle(httpContext);
                }
                else
                {
                    httpContext.Response.StatusCode = 400;
                    httpContext.Response.Close();
                }
            }
        }
    }

    internal static async Task HandleWebSocketConnection(string? windowTitle, HttpListenerContext httpContext)
    {
        byte[] buffer = new byte[1024];
        var webSocketContext = await httpContext.AcceptWebSocketAsync(null);
        var webSocket = webSocketContext.WebSocket;

        Log.Socket("Client connected");

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Log.Socket("Client initiated close handshake");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    Log.Socket("Client disconnected");
                }
                else
                {
                    string payload = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    var socketPayload = JsonSerializer.Deserialize<SocketPayload>(payload);

                    if (socketPayload == null)
                    {
                        Log.Socket("Payload malformed");
                    }
                    else if (
                        Enum.IsDefined(typeof(Commands), socketPayload.command) && Enum.IsDefined(typeof(Actions), socketPayload.action))
                    {
                        var snesWindow = Snes9x.GetSnesWindowByTitle(windowTitle);
                        SimulateKeyPress(snesWindow, socketPayload);
                    }
                    else
                    {
                        Log.Socket("Invalid action and/or command");
                    }

                }
            }
        }
        catch (WebSocketException ex)
        {
            Log.Socket($"\n Error: {ex.Message} \n State: {webSocket.State}");
        }
        finally
        {
            if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
            {
                Log.Socket("Closing WebSocket due to error or client disconnection");
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing due to error", CancellationToken.None);
            }
        }
    }

    static void SimulateKeyPress(IntPtr snesWindow, SocketPayload payload)
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

            IntPtr hWnd = snesWindow;

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
}

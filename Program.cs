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

        Input.RequestWindowTitle();

        var snesWindowTitles = GetSnesWindowTitlev2();
        var selectedWindowTitle = "";
        var continueRunning = false;

        while (continueRunning)
        {
            Console.Clear();

            if (snesWindowTitles.Count == 0)
            {
                Console.WriteLine("No Snes9x windows were found");
                Console.WriteLine("Press U to update \nPress E to exit");

                var notFoundKey = Console.ReadKey(true);

                if (notFoundKey.Key == ConsoleKey.U)
                {
                    DisplayUpdating();
                    snesWindowTitles = GetSnesWindowTitlev2();

                }
                else if (notFoundKey.Key == ConsoleKey.E)
                {
                    continueRunning = false;
                }

            }
            else if (snesWindowTitles.Count == 1)
            {
                selectedWindowTitle = snesWindowTitles[0];
                continueRunning = false;
            }
            else
            {
                DisplayWindowTitles(snesWindowTitles);

                Console.WriteLine(
                "Press a number to select the window" +
                "\n Press U to update" +
                "\n Press E to exit"
  );

                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.U)
                {
                    DisplayUpdating();

                    snesWindowTitles = GetSnesWindowTitlev2();
                }
                else if (key.Key == ConsoleKey.E)
                {
                    continueRunning = false;
                }
                else if (key.Key >= ConsoleKey.D1 && key.Key <= ConsoleKey.D9)
                {
                    int choice = key.Key - ConsoleKey.D1 + 1;

                    if (choice > 0 && choice <= snesWindowTitles.Count)
                    {
                        Console.Clear();
                        selectedWindowTitle = snesWindowTitles[choice - 1];
                        continueRunning = false;
                    }
                }
            }
        }


        if (!String.IsNullOrEmpty(selectedWindowTitle))
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
                    await HandleWebSocketConnection(httpContext);
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

    static void DisplayUpdating()
    {
        string baseMessage = "Updating";
        char[] spinner = { '|', '/', '-', '\\' };

        for (int i = 0; i < spinner.Length; i++)
        {
            Console.Clear();
            Console.WriteLine($" {spinner[i]} {baseMessage}");
            Thread.Sleep(333);
        }
    }

    static void DisplayWindowTitles(List<string> windowTitles)
    {
        Console.WriteLine("Escolha uma janela da lista abaixo:");

        for (int i = 0; i < windowTitles.Count; i++)
        {
            Console.WriteLine($" {i + 1}. {windowTitles[i]}");
        }
    }

    internal static async Task HandleWebSocketConnection(HttpListenerContext httpContext)
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

                        SimulateKeyPress(socketPayload);
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

    static List<string> GetSnesWindowTitlev2()
    {
        List<string> titles = [];

        EnumWindows((hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd))
            {
                StringBuilder windowText = new(256);

                GetWindowText(hWnd, windowText, windowText.Capacity);

                string windowTitle = windowText.ToString();

                if (!string.IsNullOrEmpty(windowTitle) && windowTitle.Contains("Snes9x"))
                {
                    titles.Add(windowTitle);
                }
            }

            return true;

        }, IntPtr.Zero);


        return titles;
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

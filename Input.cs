using static System.Console;
using static System.ConsoleKey;

namespace Project1
{
    internal class Input
    {
        public static string? RequestWindowTitle()
        {
            var continueRunning = true;
            string? selectedWindow = null;
            List<string>? snes9xWindows = Snes9x.GetSnesWindowTitlev2();

            while (continueRunning)
            {
                Clear();

                switch (snes9xWindows.Count)
                {
                    case 0:
                        NotFound(ref snes9xWindows, ref continueRunning);
                        break;
                    case 1:
                        FoundOne(snes9xWindows, ref selectedWindow, ref continueRunning);
                        break;
                    default:
                        FoundMany(ref snes9xWindows, ref selectedWindow, ref continueRunning);
                        break;
                }
            }

            return selectedWindow;
        }

        private static void FoundMany(ref List<string> snes9xWindows, ref string? selectedWindow, ref bool continueRunning)
        {
            var selectedIndex = 0;

            while (continueRunning)
            {
                Clear();
                WriteLine("There are more than one Snes9x window, select one:");

                for (var i = 0; i < snes9xWindows.Count; i++)
                {
                    string optionLabel = selectedIndex == i ? $"> {snes9xWindows[i]}" : $"  {snes9xWindows[i]}";
                    WriteLine(optionLabel);
                }

                var consoleKey = ReadKey(true);

                switch (consoleKey.Key)
                {
                    case UpArrow:
                        selectedIndex = selectedIndex - 1 < 0 ? snes9xWindows.Count - 1 : selectedIndex - 1;
                        break;

                    case DownArrow:
                        selectedIndex = selectedIndex + 1 > snes9xWindows.Count - 1 ? 0 : selectedIndex + 1;
                        break;

                    case Enter:
                        {
                            selectedWindow = snes9xWindows[selectedIndex];
                            continueRunning = false;
                        }
                        break;
                }
            }
        }

        private static void FoundOne(List<string> snes9xWindows, ref string? selectedWindow, ref bool continueRunning)
        {
            selectedWindow = snes9xWindows[0];
            continueRunning = false;
        }

        private static void NotFound(ref List<string> snes9xWindows, ref bool continueRunning)
        {
            WriteLine("No Snes9x windows were found");

            DisplayUpdate();

            var consoleKey = ReadKey(true);

            if (consoleKey.Key == U)
            {
                snes9xWindows = Snes9x.GetSnesWindowTitlev2();
            }
            else if (consoleKey.Key == E)
            {
                continueRunning = false;
            }
        }

        private static void DisplayUpdate()
        {
            WriteLine("Press U to update \nPress E to exit");
        }

        static private void DisplayUpdating()
        {
            char[] spinner = ['|', '/', '-', '\\'];

            for (int i = 0; i < spinner.Length; i++)
            {
                Clear();
                WriteLine($" {spinner[i]} Updating");
                Thread.Sleep(333);
            }

            Clear();
        }

        public static string? HandleWindows(List<string> windows, Action UHandler, Action EHandler)
        {
            var selectedIndex = 0;
            var continueRunning = true;
            string? selectedValue = null;

            while (continueRunning)
            {
                Clear();
                WriteLine("There are more than one Snes9x window, select one:");

                for (var i = 0; i < windows.Count; i++)
                {
                    string optionLabel = selectedIndex == i ? $"> {windows[i]}" : $"  {windows[i]}";
                    WriteLine(optionLabel);
                }

                WriteLine();
                DisplayUpdate();
                var consoleKey = ReadKey(true);

                switch (consoleKey.Key)
                {
                    case UpArrow:
                        selectedIndex = selectedIndex - 1 < 0 ? windows.Count - 1 : selectedIndex - 1;
                        break;

                    case DownArrow:
                        selectedIndex = selectedIndex + 1 > windows.Count - 1 ? 0 : selectedIndex + 1;
                        break;

                    case U:
                        {
                            DisplayUpdating();
                            UHandler();
                        }
                        break;

                    case E:
                        EHandler();
                        break;

                    case Enter:
                        {
                            selectedValue = windows[selectedIndex];
                            continueRunning = false;
                        }
                        break;
                }
            }

            return selectedValue;

        }
    }
}

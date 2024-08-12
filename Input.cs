using static System.Console;
using static System.ConsoleKey;

namespace Project1
{
    internal class Input
    {

        public static void Init()
        {
            var snes9xWindows = Snes9x.GetSnesWindowTitlev2();
            var continueRunning = true;
            string? selectedWindowTitle = null;


            while (continueRunning)
            {
                Clear();

                switch (snes9xWindows.Count)
                {
                    case 0:
                        HandleEmpty(() =>
                            {
                                DisplayUpdating();
                                snes9xWindows = Snes9x.GetSnesWindowTitlev2();
                            },
                            () => continueRunning = false
                        );
                        break;

                    case 1:
                        selectedWindowTitle = snes9xWindows[0];
                        break;
                    default:
                        DisplayWindows(snes9xWindows);
                        break;
                }

                if (snes9xWindows.Count == 0)
                {
                    HandleEmpty(() =>
                    {
                        DisplayUpdating();
                        snes9xWindows = Snes9x.GetSnesWindowTitlev2();
                    },
                    () => continueRunning = false);
                }

            }
        }

        private static void HandleEmpty(Action UHandler, Action EHandler)
        {
            WriteLine("No Snes9x windows were found");
            WriteLine("Press U to update \nPress E to exit");
            var consoleKey = ReadKey(true);

            switch (consoleKey.Key)
            {
                case U:
                    UHandler();
                    break;
                case E:
                    EHandler();
                    break;
            }

        }

        static void DisplayUpdating()
        {
            char[] spinner = ['|', '/', '-', '\\'];

            for (int i = 0; i < spinner.Length; i++)
            {
                Clear();
                WriteLine($" {spinner[i]} Updating");
                Thread.Sleep(333);
            }
        }

        public static string? DisplayWindows(List<string> windows)
        {
            var selectedIndex = 0;
            var continueRunning = true;
            string? selectedValue = null;

            while (continueRunning)
            {
                Clear();
                WriteLine("Please select one option:");



                for (var i = 0; i < windows.Count; i++)
                {
                    string optionLabel = selectedIndex == i ? $"> {windows[i]}" : $"  {windows[i]}";
                    WriteLine(optionLabel);
                }

                var consoleKey = ReadKey(true);

                switch (consoleKey.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = selectedIndex - 1 < 0 ? windows.Count - 1 : selectedIndex - 1;
                        break;

                    case ConsoleKey.DownArrow:
                        selectedIndex = selectedIndex + 1 > windows.Count - 1 ? 0 : selectedIndex + 1;
                        break;

                    case ConsoleKey.Enter:
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

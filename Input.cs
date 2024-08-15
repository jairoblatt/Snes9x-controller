using static System.Console;
using static System.ConsoleKey;

namespace Project1
{
    internal class Input
    {
        private const ConsoleKey KEY_EXIT = E;
        private const ConsoleKey KEY_UPDATE = U;
        private bool IsRequestRunning = true;
        private string? selectedWindowTitle = null;
        private List<string> snes9xWindows = Snes9x.GetSnesWindowTitles();

        public string? RequestWindowTitle()
        {
            while (IsRequestRunning)
            {
                Clear();

                switch (snes9xWindows.Count)
                {
                    case 0:
                        FoundNone();
                        break;
                    case 1:
                        FoundOne();
                        break;
                    default:
                        FoundMany();
                        break;
                }
            }

            Clear();

            return selectedWindowTitle;
        }

        private void FoundNone()
        {
            WriteLine("No Snes9x windows were found");
            WriteUpdateOrExit();
            HandleUpdateOrExit(null);
        }

        private void FoundOne()
        {
            selectedWindowTitle = snes9xWindows[0];
            IsRequestRunning = false;
        }

        private void FoundMany()
        {
            var selectedIndex = 0;
            var continueRunning = true;

            while (continueRunning)
            {
                Clear();
                WriteLine("There are more than one Snes9x window, select one:");

                for (var i = 0; i < snes9xWindows.Count; i++)
                {
                    var sn = snes9xWindows[i];
                    WriteLine(selectedIndex == i ? $"> {sn}" : $"  {sn}");
                }

                WriteUpdateOrExit();
                var consoleKey = ReadKey(true);

                switch (consoleKey.Key)
                {
                    case KEY_EXIT:
                    case KEY_UPDATE:
                        HandleUpdateOrExit(consoleKey);
                        continueRunning = false;
                        break;

                    case UpArrow:
                        selectedIndex = selectedIndex - 1 < 0 ? snes9xWindows.Count - 1 : selectedIndex - 1;
                        break;

                    case DownArrow:
                        selectedIndex = selectedIndex + 1 > snes9xWindows.Count - 1 ? 0 : selectedIndex + 1;
                        break;

                    case Enter:
                        {
                            selectedWindowTitle = snes9xWindows[selectedIndex];
                            continueRunning = false;
                            IsRequestRunning = false;
                        }
                        break;
                }
            }
        }

        private static void WriteUpdateOrExit()
        {
            WriteLine($"Press {KEY_UPDATE} to update \nPress {KEY_EXIT} to exit");
        }

        private void HandleUpdateOrExit(ConsoleKeyInfo? consoleKey)
        {
            var _consoleKey = consoleKey ?? ReadKey(true);

            if (_consoleKey.Key == KEY_UPDATE)
            {
                WriteUpdatingSpinner();
                snes9xWindows = Snes9x.GetSnesWindowTitles();
            }
            else if (_consoleKey.Key == KEY_EXIT)
            {
                IsRequestRunning = false;
            }
        }


        private static void WriteUpdatingSpinner()
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
    }
}

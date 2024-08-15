using static System.Console;
using static System.ConsoleKey;

namespace Project1
{
    internal class SelectWindow(string property, Func<List<string>> getWindows)
    {
        private const ConsoleKey KEY_EXIT = E;
        private const ConsoleKey KEY_UPDATE = U;
        private bool isRequestRunning = true;
        private string? selectedWindowTitle = null;
        private List<string> windows = getWindows();

        public string? Request()
        {
            while (isRequestRunning)
            {
                Clear();

                switch (windows.Count)
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
            WriteLine($"No {property} windows were found");
            WriteUpdateOrExit();
            HandleUpdateOrExit(null);
        }

        private void FoundOne()
        {
            selectedWindowTitle = windows[0];
            isRequestRunning = false;
        }

        private void FoundMany()
        {
            var selectedIndex = 0;
            var continueRunning = true;

            while (continueRunning)
            {
                Clear();
                WriteLine($"There is more than one {property} window running, select one:");

                for (var i = 0; i < windows.Count; i++)
                {
                    var win = windows[i];
                    WriteLine(selectedIndex == i ? $"> {win}" : $"  {win}");
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
                        selectedIndex = selectedIndex - 1 < 0 ? windows.Count - 1 : selectedIndex - 1;
                        break;

                    case DownArrow:
                        selectedIndex = selectedIndex + 1 > windows.Count - 1 ? 0 : selectedIndex + 1;
                        break;

                    case Enter:
                        {
                            selectedWindowTitle = windows[selectedIndex];
                            continueRunning = false;
                            isRequestRunning = false;
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
                windows = getWindows();
            }
            else if (_consoleKey.Key == KEY_EXIT)
            {
                isRequestRunning = false;
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

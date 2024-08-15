using static System.Console;
using static System.ConsoleKey;

namespace Project1
{
    internal class Input
    {
        private bool IsRequestRunning = true;
        private string? selectedWindow = null;
        private List<string> snes9xWindows = Snes9x.GetSnesWindowTitlev2();

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

            return selectedWindow;
        }

        private void FoundNone()
        {
            WriteLine("No Snes9x windows were found");
            UpdateDisplay();
            UpdateHandleKey(null);
        }

        private void FoundOne()
        {
            selectedWindow = snes9xWindows?[0];
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
                    string optionLabel = selectedIndex == i ? $"> {sn}" : $"  {sn}";
                    WriteLine(optionLabel);
                }

                UpdateDisplay();
                var consoleKey = ReadKey(true);

                switch (consoleKey.Key)
                {
                    case U:
                    case E:
                        UpdateHandleKey(consoleKey);
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
                            selectedWindow = snes9xWindows[selectedIndex];
                            continueRunning = false;
                            IsRequestRunning = false;
                        }
                        break;
                }
            }
        }

        private static void UpdateDisplay()
        {
            WriteLine("Press U to update \nPress E to exit");
        }

        private void UpdateHandleKey(ConsoleKeyInfo? consoleKey)
        {
            var key = consoleKey ?? ReadKey(true);

            if (key.Key == U)
            {
                DisplayUpdating();
                snes9xWindows = Snes9x.GetSnesWindowTitlev2();
            }

            if (key.Key == E)
            {
                IsRequestRunning = false;
            }
        }


        private static void DisplayUpdating()
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

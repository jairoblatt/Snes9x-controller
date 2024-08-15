using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Project1
{
    internal class Snes9x
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public static List<string> GetSnesWindowTitles()
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

        public static IntPtr GetSnesWindowByTitle(string? snesWindowTitle)
        {

            if (String.IsNullOrEmpty(snesWindowTitle))
            {
                return IntPtr.Zero;

            }

            return FindWindow(null, snesWindowTitle);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Runtime;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace OpenInPHPStorm
{
    class Program
    {
        private enum WindowShowStyle : uint
        {
            /// <summary>Hides the window and activates another window.</summary>
            /// <remarks>See SW_HIDE</remarks>
            Hide = 0,
            /// <summary>Activates and displays a window. If the window is minimized
            /// or maximized, the system restores it to its original size and
            /// position. An application should specify this flag when displaying
            /// the window for the first time.</summary>
            /// <remarks>See SW_SHOWNORMAL</remarks>
            ShowNormal = 1,
            /// <summary>Activates the window and displays it as a minimized window.</summary>
            /// <remarks>See SW_SHOWMINIMIZED</remarks>
            ShowMinimized = 2,
            /// <summary>Activates the window and displays it as a maximized window.</summary>
            /// <remarks>See SW_SHOWMAXIMIZED</remarks>
            ShowMaximized = 3,
            /// <summary>Maximizes the specified window.</summary>
            /// <remarks>See SW_MAXIMIZE</remarks>
            Maximize = 3,
            /// <summary>Displays a window in its most recent size and position.
            /// This value is similar to "ShowNormal", except the window is not
            /// actived.</summary>
            /// <remarks>See SW_SHOWNOACTIVATE</remarks>
            ShowNormalNoActivate = 4,
            /// <summary>Activates the window and displays it in its current size
            /// and position.</summary>
            /// <remarks>See SW_SHOW</remarks>
            Show = 5,
            /// <summary>Minimizes the specified window and activates the next
            /// top-level window in the Z order.</summary>
            /// <remarks>See SW_MINIMIZE</remarks>
            Minimize = 6,
            /// <summary>Displays the window as a minimized window. This value is
            /// similar to "ShowMinimized", except the window is not activated.</summary>
            /// <remarks>See SW_SHOWMINNOACTIVE</remarks>
            ShowMinNoActivate = 7,
            /// <summary>Displays the window in its current size and position. This
            /// value is similar to "Show", except the window is not activated.</summary>
            /// <remarks>See SW_SHOWNA</remarks>
            ShowNoActivate = 8,
            /// <summary>Activates and displays the window. If the window is
            /// minimized or maximized, the system restores it to its original size
            /// and position. An application should specify this flag when restoring
            /// a minimized window.</summary>
            /// <remarks>See SW_RESTORE</remarks>
            Restore = 9,
            /// <summary>Sets the show state based on the SW_ value specified in the
            /// STARTUPINFO structure passed to the CreateProcess function by the
            /// program that started the application.</summary>
            /// <remarks>See SW_SHOWDEFAULT</remarks>
            ShowDefault = 10,
            /// <summary>Windows 2000/XP: Minimizes a window, even if the thread
            /// that owns the window is hung. This flag should only be used when
            /// minimizing windows from a different thread.</summary>
            /// <remarks>See SW_FORCEMINIMIZE</remarks>
            ForceMinimized = 11
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

        static int Main(string[] args)
        {
            Debug.WriteLine("Starts!");
            if (args.Length != 1)
                return 1;

            Console.WriteLine(args[0]);
            string pattern = @"^phpstorm:\/\/open\/?\?(url=file:\/\/|file=)(.+)&line=(\d+)$";
            Regex rgx = new Regex(pattern);
            string[] matches = preg_split(pattern, args[0]);

            if (matches.Length != 5)
            {
                return 1;
            }

            RegistryKey OurKey = Registry.LocalMachine;
            OurKey = OurKey.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", false);

            Regex stormName = new Regex("^PhpStorm ");
            String path = "";
            foreach (string Keyname in OurKey.GetSubKeyNames())
            {

                if (stormName.IsMatch(Keyname)){
                    RegistryKey key = OurKey.OpenSubKey(Keyname);

                    path = key.GetValue("DisplayIcon").ToString();
                    break;
                }
               
            }

            String filePath = matches[2];
            filePath = filePath.Replace("\\\\","\\").Replace("/", @"\");
            Console.WriteLine("File to open: {0}", filePath);

            Process[] processlist = Process.GetProcesses();

            foreach (Process process in processlist)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle))
                {
                    if (process.ProcessName.Contains("phpstorm64"))
                    {
                        string StormExcecutable = process.MainModule.FileName;
                        Console.WriteLine(process.Handle);
                        Process.Start(String.IsNullOrEmpty(path) ? StormExcecutable : path, String.Format("--line {0} {1}", matches[3], filePath));
                        ShowWindow(process.Handle, WindowShowStyle.ShowNormalNoActivate);

                        PostMessage(process.Handle, 0x0112, 0x0112, 0);
                        break;
                    }
                        
                    Debug.WriteLine("Process: {0} ID: {1} Window title: {2}", process.ProcessName, process.Id, process.MainWindowTitle);

                }
            }
            return 0;
        }

        public static string[] preg_split(string regex, string input)
        {
            Regex regexObj = new Regex(regex);
            List<string> result = new List<string>();

            string[] pre_result = Regex.Split(input, regex);


            foreach (var item in pre_result)
            {
                if (!regexObj.Match(item).Success)
                {
                    result.Add(item);
                }
            }

            return result.ToArray();
        }
    }
}

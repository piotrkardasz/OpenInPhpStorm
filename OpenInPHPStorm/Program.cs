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
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        static int Main(string[] args)
        {
            Debug.WriteLine("Starts!");
            if (args.Length != 1)
                 return 1;
            
            string pattern = @"^phpstorm:\/\/open\/?\?(url=file:\/\/|file=)(.+)&line=(\d+)$";
            Regex rgx = new Regex(pattern);

            string[] matches = PregSplit(pattern, args[0]);

            if (matches.Length == 5)
            {
                return HandleByProtocol(matches);
            }

            return HandleByPath(args[0]);
        }

        public static int HandleByPath(string path)
        {
            Console.WriteLine(path);
            Regex regex = new Regex(@"^(/[^/ ]*)+/?$");
            Match match = regex.Match(path);
            string distro = "Ubuntu";

            if (match.Success)
            {
                string wslPath = String.Concat(@"\\wsl$\", distro, path.Replace("/", @"\"));
                Console.WriteLine(wslPath);

                FileAttributes attr = File.GetAttributes(wslPath);
                if (attr.HasFlag(FileAttributes.Directory))
                    runPhpStorm(wslPath);
                else
                    runPhpStorm(String.Format("--line {0} {1}", 1, wslPath));
            }

            return 0;
        }

        public static int HandleByProtocol(string[] matches)
        {
            String filePath = matches[2];
            filePath = filePath.Replace("\\\\", "\\").Replace("/", @"\");
            Console.WriteLine("File to open: {0}", filePath);

            runPhpStorm(String.Format("--line {0} {1}", matches[3], filePath));
            
            return 0;
        }

        public static void runPhpStorm(string args)
        {
            RegistryKey OurKey = Registry.LocalMachine;
            OurKey = OurKey.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", false);

            Regex stormName = new Regex("^PhpStorm ");
            String StormExcecutable = "";
            foreach (string Keyname in OurKey.GetSubKeyNames())
            {

                if (stormName.IsMatch(Keyname))
                {
                    RegistryKey key = OurKey.OpenSubKey(Keyname);

                    StormExcecutable = key.GetValue("DisplayIcon").ToString();
                    break;
                }

            }

            if (StormExcecutable == "") { 
                string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string jetbrainsToolbox = string.Join("", appDataDirectory, @"\JetBrains\Toolbox");
                string jetbrainsToolboxPhpStorm = string.Join("", jetbrainsToolbox, @"\apps\PhpStorm\ch-0");

                string[] dirs = Directory.GetDirectories(jetbrainsToolboxPhpStorm);
                dirs = dirs.OrderByDescending(c => c).ToArray();
                foreach (string phpStormVersionDir in dirs)
                {
                    string StormToolboxExectutable = String.Join("", phpStormVersionDir, @"\bin\phpstorm64.exe");
                    if (File.Exists(StormToolboxExectutable)){
                        StormExcecutable = StormToolboxExectutable;

                        break;
                    }
                }
            }

            Process[] processlist = Process.GetProcesses();

            foreach (Process process in processlist)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle))
                {
                    if (process.ProcessName.Contains("phpstorm64"))
                    {
                        StormExcecutable = process.MainModule.FileName;
                    }                    
                }
            }

            if (StormExcecutable != "")
            {
                Process StormProcess = Process.Start(StormExcecutable, args);
                Console.WriteLine(StormProcess + "" + args);
                return;
            }
        }

        public static string[] PregSplit(string regex, string input)
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

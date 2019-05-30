using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Ascentis.CmdTools
{
    internal class Abort : Exception
    {
        public Abort() : base(""){}
    }

    // ReSharper disable once InconsistentNaming
    internal class GACNativize
    {
        enum OperationMode
        {
            Install,
            Uninstall
        }

        private static readonly string ProgramFilesFolder = Environment.ExpandEnvironmentVariables("%systemdrive%\\Program Files (x86)");
        private static readonly ConsoleColor DefaultForegroundConsoleColor = Console.ForegroundColor;

        static void DisplayHelp()
        {
            const string help = @"Usage: GACNativize <folder> <filemask> install|uninstall [[<winversion>]|<winversion>[<.net version>]]";
            Console.WriteLine(help);
        }

        private static void Wl(string s, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(s);
        }

        // ReSharper disable once UnusedMember.Local
        private static void Wl(string s)
        {
            Wl(s, DefaultForegroundConsoleColor);
        }

        private static Process BuildProcess(string processParams, string winVersion, string netVersion)
        {
            return new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = $@"{ProgramFilesFolder}\Microsoft SDKs\Windows\{winVersion}\bin\NETFX {netVersion} Tools\GACUTIL.exe",
                    Arguments = processParams
                }
            };
        }

        private static void RunProcess(Process p, string file, List<string> failedFilesList)
        {
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0)
                failedFilesList.Add(file);
            Wl(output, p.ExitCode == 0 ? ConsoleColor.Green : ConsoleColor.Red);
        }

        // ReSharper disable once InconsistentNaming
        private static List<string> GACInstall(IEnumerable<string> files, List<string> failedFilesList, string winVersion, string netVersion)
        {
            if (failedFilesList == null)
                failedFilesList = new List<string>();
            foreach (var file in files)
            {
                if (failedFilesList.Contains(file))
                {
                    Wl($"Excluding file \"{file}\"\r\n", ConsoleColor.Yellow);
                    continue;
                }

                var p = BuildProcess($"/i {file} /nologo", winVersion, netVersion);
                Wl($"Processing: {file}", ConsoleColor.White);
                RunProcess(p, file, failedFilesList);
            }

            return failedFilesList;
        }

        // ReSharper disable once InconsistentNaming
        private static List<string> GACUninstall(IEnumerable<string> files, List<string> failedFilesList, string winVersion, string netVersion)
        {
            if (failedFilesList == null)
                failedFilesList = new List<string>();
            foreach (var file in files)
            {
                if (failedFilesList.Contains(file))
                {
                    Wl($"Excluding file \"{file}\"\r\n", ConsoleColor.Yellow);
                    continue;
                }

                Assembly assembly;
                try
                {
                    assembly = Assembly.ReflectionOnlyLoadFrom(file);
                }
                catch (Exception e)
                {
                    Wl($"Exception processing file \"{file}\". Message: {e.Message}", ConsoleColor.Red);
                    continue;
                }

                var p = BuildProcess($"/u {assembly.FullName.Split(',')[0]} /nologo", winVersion, netVersion);
                Wl($"Processing: {file} ({assembly.FullName.Split(',')[0]})", ConsoleColor.White);
                RunProcess(p, file, failedFilesList);
            }

            return failedFilesList;
        }

        private static void Main(string[] args)
        {
            try
            {
                if(args.Length < 3)
                    DisplayHelp();
                var path = args[0];
                var mask = args[1];
                var winVersion = args.Length == 4 ? args[3] : "v10.0A";
                var netVersion = args.Length == 5 ? args[4] : "4.7.1";
                OperationMode operationMode;
                switch (args[2])
                {
                    case "install":
                        operationMode = OperationMode.Install;
                        break;
                    case "uninstall":
                        operationMode = OperationMode.Uninstall;
                        break;
                    default: DisplayHelp();
                        throw new Abort();
                }
                
                var failedFilesLog = Path.Combine(path, "GACNativize.FailedInstall.log");
                List<string> failedFilesList = null;
                if (File.Exists(failedFilesLog))
                    failedFilesList = File.ReadLines(failedFilesLog).ToList();
                var sourceAssemblies = Directory.EnumerateFiles(path, mask);
                switch (operationMode)
                {
                    case OperationMode.Install:
                    {
                        failedFilesList = GACInstall(sourceAssemblies, failedFilesList, winVersion, netVersion);
                        if (failedFilesList.Count > 0)
                            File.WriteAllLines(failedFilesLog, failedFilesList);
                        break;
                    }

                    case OperationMode.Uninstall:
                    {
                        var failedUninstallFilesLog = Path.Combine(path, "GACNativize.FailedUninstall.log");
                        failedFilesList = GACUninstall(sourceAssemblies, failedFilesList, winVersion, netVersion);
                        if (failedFilesList.Count > 0)
                            File.WriteAllLines(failedUninstallFilesLog, failedFilesList);
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                if (!(e is Abort))
                    Console.WriteLine(e.Message);
            }
            Console.ReadLine();
        }
    }
}

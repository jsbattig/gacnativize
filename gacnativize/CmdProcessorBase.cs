using System;
using System.Collections.Generic;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace Ascentis.CmdTools
{
    public class CmdProcessorBase
    {
        private static readonly ConsoleColor DefaultForegroundConsoleColor = Console.ForegroundColor;
        protected List<string> FailedFilesList;
        protected List<string> ExceptionList;

        protected CmdProcessorBase(List<string> failedFilesList, List<string> exceptionList)
        {
            FailedFilesList = failedFilesList;
            ExceptionList = exceptionList;
        }

        protected static void Wl(string s, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(s);
        }

        // ReSharper disable once UnusedMember.Local
        protected static void Wl(string s)
        {
            Wl(s, DefaultForegroundConsoleColor);
        }

        protected static string ProcessFileName(string fileName)
        {
            return fileName[0] == '-' ? "" : fileName;
        }

        protected static Process BuildProcess(string processFile, string processParams)
        {
            return new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = processFile,
                    Arguments = processParams
                }
            };
        }

        protected bool ExcludeOrIgnore(string file)
        {
            if (ExceptionList == null)
                return false;
            if (ExceptionList.Contains("-" + file)) // Ignoring file completely
                return true;
            if (!ExceptionList.Contains(file)) 
                return false;
            Wl($"Excluding file \"{file}\"\r\n", ConsoleColor.Yellow);
            return true;
        }

        protected void RunProcess(Process p, string file)
        {
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0)
                FailedFilesList.Add(file);
            Wl(output, p.ExitCode == 0 ? ConsoleColor.Green : ConsoleColor.Red);
        }
    }
}

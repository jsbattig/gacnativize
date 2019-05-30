using System;
using System.Collections.Generic;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace Ascentis.CmdTools
{
    public class CmdProcessorBase
    {
        private static readonly ConsoleColor DefaultForegroundConsoleColor = Console.ForegroundColor;
        protected List<string> ExceptionList;
        protected List<string> FailedFilesList = new List<string>();

        protected CmdProcessorBase(List<string> exceptionList)
        {
            ExceptionList = exceptionList;
        }

        public static void Wl(string s, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(s);
        }

        // ReSharper disable once UnusedMember.Local
        public static void Wl(string s)
        {
            Wl(s, DefaultForegroundConsoleColor);
        }

        private static string ProcessFileName(string fileName)
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

        protected string ExcludeOrIgnore(string file)
        {
            var newFileName = ProcessFileName(file);
            if (newFileName == "" || ExceptionList.Contains("-" + file))
            {
                FailedFilesList.Add(file);
                return "";
            }
            if (!ExceptionList.Contains(file)) 
                return newFileName;
            FailedFilesList.Add(file);
            Wl($"Excluding file \"{file}\"\r\n", ConsoleColor.Yellow);
            return "";
        }

        protected virtual void PreprocessOutput(string fileName, string output, out ConsoleColor consoleColor)
        {
            consoleColor = ConsoleColor.Green;
        }

        protected void RunProcess(Process p, string file)
        {
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            PreprocessOutput(file, output, out var consoleColor);
            p.WaitForExit();
            if (p.ExitCode != 0)
                FailedFilesList.Add(file);
            Wl(output, p.ExitCode == 0 ? consoleColor : ConsoleColor.Red);
        }
    }
}

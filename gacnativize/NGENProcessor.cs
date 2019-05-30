using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Ascentis.CmdTools
{
    // ReSharper disable once InconsistentNaming
    public class NGENProcessor : CmdProcessorBase
    {
        private static readonly string WindowsFolder = Environment.ExpandEnvironmentVariables("%windir%");

        public NGENProcessor(List<string> exceptionList) : base(exceptionList){}

        protected override void PreprocessOutput(string fileName, string output, out ConsoleColor consoleColor)
        {
            var fileDirectory = Path.GetDirectoryName(fileName);
            var lineCount = output.Count(c => c == '\r');
            if (lineCount <= 3)
            {
                consoleColor = ConsoleColor.Green;
                return;
            }

            consoleColor = ConsoleColor.Yellow;
            // ReSharper disable once AssignNullToNotNullAttribute
            var logFile = Path.Combine(fileDirectory, "GACNat.log", $"{Path.GetFileName(fileName)}.ngenwarnings.log");
            // ReSharper disable once AssignNullToNotNullAttribute
            Directory.CreateDirectory(Path.GetDirectoryName(logFile));
            File.WriteAllText(logFile, output);
        }

        // ReSharper disable once InconsistentNaming
        public List<string> NGENInstall(IEnumerable<string> files, string frameworkVersion)
        {
            var processFile = $@"{WindowsFolder}\Microsoft.NET\Framework\{frameworkVersion}\ngen.exe";
            foreach (var file in files)
            {
                string fileName;
                if ((fileName = ExcludeOrIgnore(file)) == "")
                    continue;
                var p = BuildProcess(processFile, $"install \"{fileName}\" /AppBase:{Path.GetDirectoryName(fileName)}");
                Wl($"NGEN installing: {fileName}", ConsoleColor.White);
                RunProcess(p, fileName);
            }

            return FailedFilesList;
        }

        // ReSharper disable once InconsistentNaming
        public void NGENUninstall(IEnumerable<string> files, string frameworkVersion)
        {
            var processFile = $@"{WindowsFolder}\Microsoft.NET\Framework\{frameworkVersion}\ngen.exe";
            foreach (var file in files)
            {
                string fileName;
                if ((fileName = ExcludeOrIgnore(file)) == "")
                    continue;
                Assembly assembly;
                try
                {
                    assembly = Assembly.ReflectionOnlyLoadFrom(fileName);
                }
                catch (Exception e)
                {
                    Wl($"Exception processing file \"{fileName}\". Message: {e.Message}", ConsoleColor.Red);
                    continue;
                }

                var p = BuildProcess(processFile, $"uninstall \"{fileName}\"");
                Wl($"NGEN uninstalling: {fileName} ({assembly.FullName.Split(',')[0]})", ConsoleColor.White);
                RunProcess(p, fileName);
            }
        }
    }
}

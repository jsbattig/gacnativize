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

        public NGENProcessor(List<string> exceptionList, string logFolder, bool useX64Tooling) : base(exceptionList, logFolder, useX64Tooling){}

        protected override void PreprocessOutput(string fileName, string output, out ConsoleColor consoleColor)
        {
            var fileDirectory = Path.GetDirectoryName(fileName);
            var failureExistence = output.IndexOf("Failed to load dependency", StringComparison.Ordinal);
            if (failureExistence < 0)
            {
                consoleColor = ConsoleColor.Green;
                return;
            }

            consoleColor = ConsoleColor.Yellow;
            // ReSharper disable once AssignNullToNotNullAttribute
            var logFile = Path.Combine(fileDirectory, LogFolder, $"{Path.GetFileName(fileName)}.ngenwarnings.log");
            // ReSharper disable once AssignNullToNotNullAttribute
            Directory.CreateDirectory(Path.GetDirectoryName(logFile));
            File.WriteAllText(logFile, output);
        }

        // ReSharper disable once InconsistentNaming
        public List<string> NGENInstall(IEnumerable<string> files, string frameworkVersion, string templateAppConfig)
        {
            var x64SubPath = UseX64Tooling ? "64" : "";
            var processFile = $@"{WindowsFolder}\Microsoft.NET\Framework{x64SubPath}\{frameworkVersion}\ngen.exe";
            foreach (var file in files)
            {
                string fileName;
                if ((fileName = ExcludeOrIgnore(file)) == "")
                    continue;
                var fakeExe = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(templateAppConfig)) + ".exe");
                File.Copy(fileName, fakeExe, true);
                try
                {
                    var p = BuildProcess(processFile,
                        $"install \"{fileName}\" /ExeConfig:{fakeExe}"); // /AppBase:{Path.GetDirectoryName(fileName)}
                    Wl($"NGEN installing: {fileName}", ConsoleColor.White);
                    RunProcess(p, fileName);
                }
                finally
                {
                    File.Delete(fakeExe);
                }
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

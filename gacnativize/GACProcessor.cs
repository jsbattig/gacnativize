using System;
using System.Collections.Generic;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Ascentis.CmdTools
{
    // ReSharper disable once InconsistentNaming
    public class GACProcessor : CmdProcessorBase
    {
        private static readonly string ProgramFilesFolder = Environment.ExpandEnvironmentVariables("%systemdrive%\\Program Files (x86)");

        public GACProcessor(List<string> failedFilesList, List<string> exceptionList) : base(failedFilesList, exceptionList){}

        // ReSharper disable once InconsistentNaming
        public void GACInstall(IEnumerable<string> files, string winVersion, string netVersion)
        {
            var processFile = $@"{ProgramFilesFolder}\Microsoft SDKs\Windows\{winVersion}\bin\NETFX {netVersion} Tools\GACUTIL.exe";
            foreach (var file in files)
            {
                string fileName;
                if ((fileName = ProcessFileName(file)) == "")
                {
                    FailedFilesList.Add(file);
                    continue;
                }

                if (ExcludeOrIgnore(fileName))
                    continue;
                var p = BuildProcess(processFile, $"/i {fileName} /nologo");
                Wl($"GAC installing: {fileName}", ConsoleColor.White);
                RunProcess(p, fileName);
            }
        }

        // ReSharper disable once InconsistentNaming
        public void GACUninstall(IEnumerable<string> files, string winVersion, string netVersion)
        {
            var processFile = $@"{ProgramFilesFolder}\Microsoft SDKs\Windows\{winVersion}\bin\NETFX {netVersion} Tools\GACUTIL.exe";
            foreach (var file in files)
            {
                string fileName;
                if ((fileName = ProcessFileName(file)) == "")
                {
                    FailedFilesList.Add(file);
                    continue;
                }
                if (ExcludeOrIgnore(fileName))
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

                var p = BuildProcess(processFile, $"/u {assembly.FullName.Split(',')[0]} /nologo");
                Wl($"GAC uninstalling: {fileName} ({assembly.FullName.Split(',')[0]})", ConsoleColor.White);
                RunProcess(p, fileName);
            }
        }
    }
}

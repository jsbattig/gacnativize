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

        public GACProcessor(List<string> exceptionList) : base(exceptionList){}

        // ReSharper disable once InconsistentNaming
        public List<string> GACInstall(IEnumerable<string> files, string winVersion, string netVersion)
        {
            var processFile = $@"{ProgramFilesFolder}\Microsoft SDKs\Windows\{winVersion}\bin\NETFX {netVersion} Tools\GACUTIL.exe";
            foreach (var file in files)
            {
                string fileName;
                if ((fileName = ExcludeOrIgnore(file)) == "")
                    continue;
                var p = BuildProcess(processFile, $"/i \"{fileName}\" /nologo");
                Wl($"GAC installing: {fileName}", ConsoleColor.White);
                RunProcess(p, fileName);
            }

            return FailedFilesList;
        }

        // ReSharper disable once InconsistentNaming
        public void GACUninstall(IEnumerable<string> files, string winVersion, string netVersion)
        {
            var processFile = $@"{ProgramFilesFolder}\Microsoft SDKs\Windows\{winVersion}\bin\NETFX {netVersion} Tools\GACUTIL.exe";
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

                var p = BuildProcess(processFile, $"/u {assembly.FullName.Split(',')[0]} /nologo");
                Wl($"GAC uninstalling: {fileName} ({assembly.FullName.Split(',')[0]})", ConsoleColor.White);
                RunProcess(p, fileName);
            }
        }
    }
}

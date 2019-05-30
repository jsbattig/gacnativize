using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ascentis.CmdTools
{
    public class NGENProcessor : CmdProcessorBase
    {
        public NGENProcessor(List<string> failedFilesList, List<string> exceptionList) : base(failedFilesList, exceptionList){}

        // ReSharper disable once InconsistentNaming
        public void NGENInstall(IEnumerable<string> files, string frameworkVersion)
        {
            var processFile = $@"%WINDIR%\Microsoft.NET\Framework\{frameworkVersion}\ngen.exe";
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
                Wl($"Processing: {fileName}", ConsoleColor.White);
                RunProcess(p, fileName);
            }
        }

        // ReSharper disable once InconsistentNaming
        public void NGENUninstall(IEnumerable<string> files, string frameworkVersion)
        {
            var processFile = $@"%WINDIR%\Microsoft.NET\Framework\{frameworkVersion}\ngen.exe";
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
                Wl($"Processing: {fileName} ({assembly.FullName.Split(',')[0]})", ConsoleColor.White);
                RunProcess(p, fileName);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Ascentis.CmdTools
{
    // ReSharper disable once InconsistentNaming
    class Program
    {
        // ReSharper disable once UnusedMember.Local
        private static int Main(string mainCommand, 
                        string sourcePath = ".\\", 
                        OperationMode operationMode = OperationMode.install, 
                        string fileMask = "", 
                        string winVersion = "v10.0A", 
                        string netVersion = "4.7.1",
                        string frameworkVersion = "v4.0.30319")
        {
            try
            {
                string[] sourceFiles;
                IEnumerable<string> sourceAssemblies;
                List<string> failedFilesList;
                List<string> exceptionsLogList = null;
                var exceptionsLog = Path.Combine(sourcePath, "GACNativize.log", "Exceptions.log");
                int fileCount;

                switch (mainCommand)
                {
                    case "retry":
                        if (!File.Exists(exceptionsLog))
                            return 0;
                        operationMode = OperationMode.install;
                        sourceFiles = File.ReadLines(exceptionsLog).ToArray();
                        fileCount = sourceFiles.Length;
                        sourceAssemblies = sourceFiles;
                        failedFilesList = new List<string>();
                        mainCommand = "gn";
                        break;
                    case "g":
                    case "gn":
                    case "n":
                        sourceFiles = Directory.EnumerateFiles(sourcePath, fileMask).ToArray();
                        fileCount = sourceFiles.Length;
                        sourceAssemblies = sourceFiles;
                        failedFilesList = File.Exists(exceptionsLog)
                            ? File.ReadLines(exceptionsLog).ToList()
                            : new List<string>();
                        exceptionsLogList = failedFilesList;
                        break;
                    default:
                        DisplayHelp();
                        throw new Abort();
                }

                switch (operationMode)
                {
                    case OperationMode.install:
                    {
                        if (mainCommand.Contains("g"))
                        {
                            new GACProcessor(failedFilesList, exceptionsLogList).GACInstall(sourceAssemblies,
                                winVersion, netVersion);
                            exceptionsLogList = failedFilesList;
                        }

                        if (mainCommand.Contains("n"))
                            new NGENProcessor(failedFilesList, exceptionsLogList).NGENInstall(sourceAssemblies,
                                frameworkVersion);
                        break;
                    }

                    case OperationMode.uninstall:
                    {
                        if (mainCommand.Contains("g"))
                        {
                            new GACProcessor(failedFilesList, exceptionsLogList).GACUninstall(sourceAssemblies,
                                winVersion, netVersion);
                            exceptionsLogList = failedFilesList;
                        }

                        if (mainCommand.Contains("n"))
                            new NGENProcessor(failedFilesList, exceptionsLogList).NGENUninstall(sourceAssemblies,
                                frameworkVersion);
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Directory.CreateDirectory(Path.Combine(sourcePath, "GACNativize.log\\"));
                if (failedFilesList.Count > 0)
                    File.WriteAllLines(exceptionsLog, failedFilesList);
                else
                    File.Delete(exceptionsLog);
                CmdProcessorBase.Wl($"Completed gacnativize process for {fileCount} input assemblies");
            }
            catch (Exception e)
            {
                if (!(e is Abort))
                    Console.WriteLine(e.Message);
                Console.ReadLine();
                return -1;
            }
            Console.ReadLine();
            return 0;
        }

        enum OperationMode
        {
            install,
            // ReSharper disable once InconsistentNaming
            uninstall
        }

        private static void DisplayHelp()
        {
            const string help = @"Welcome to Ascentis GACNativize! 
Usage: GACNativize <parameters>

--main-command      retry|g|gn|n
--source-path       Root folder where to look for assembly. Default = .\
--operation-mode    install|uninstall. Default = install
--file-mask         Specifies the mask to match assemblies in folder. Default = *.dll
--win-version       Windows SDK version. Default = v10.0A
--net-version       .NET target version for GAC operation. Default = 4.7.1
--framework-version .NET framework tooling version. Default = v4.0.30319

--main-command values reference:
retry  Retries failed operations logged in Exceptions.log file
g      Performs GAC install only
gn     Performs GAC and NGEN of matching assemblies
n      Performs NGEN install only
";
            Console.WriteLine(help);
        }
    }
}
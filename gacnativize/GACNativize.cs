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
                                string fileMask = "*.dll", 
                                string winVersion = "v10.0A", 
                                string netVersion = "4.7.1",
                                string frameworkVersion = "v4.0.30319")
        {
            try
            {
                string[] sourceFiles;
                IEnumerable<string> sourceAssemblies;
                List<string> failedFilesList = null;
                var exceptionsLogList = new List<string>();
                var exceptionsLog = Path.Combine(sourcePath, "GACNat.log", "Exceptions.log");
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
                        mainCommand = "gn";
                        break;
                    case "g":
                    case "gn":
                    case "n":
                        sourceFiles = Directory.EnumerateFiles(sourcePath, fileMask).ToArray();
                        fileCount = sourceFiles.Length;
                        sourceAssemblies = sourceFiles;
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
                            failedFilesList = new GACProcessor(exceptionsLogList).GACInstall(sourceAssemblies, winVersion, netVersion);
                            exceptionsLogList = failedFilesList;
                        }
                        if (mainCommand.Contains("n"))
                            failedFilesList = new NGENProcessor(exceptionsLogList).NGENInstall(sourceAssemblies, frameworkVersion);
                        break;
                    }

                    case OperationMode.uninstall:
                    {
                        if (mainCommand.Contains("n"))
                            new NGENProcessor(exceptionsLogList).NGENUninstall(sourceAssemblies, frameworkVersion);
                        if (mainCommand.Contains("g"))
                            new GACProcessor(exceptionsLogList).GACUninstall(sourceAssemblies,
                                winVersion, netVersion);
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Directory.CreateDirectory(Path.Combine(sourcePath, "GACNat.log\\"));
                if (failedFilesList != null)
                    if (failedFilesList.Count > 0)
                        File.WriteAllLines(exceptionsLog, failedFilesList);
                    else
                        File.Delete(exceptionsLog);
                CmdProcessorBase.Wl($"Completed gacnat process for {fileCount} input assemblies");
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
            const string help = @"Welcome to Ascentis GACNat! 
Usage: GACNat <parameters>

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
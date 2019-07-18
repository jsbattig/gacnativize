using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using gacnativize;

// ReSharper disable once CheckNamespace
namespace Ascentis.CmdTools
{
    // ReSharper disable once InconsistentNaming
    class Program
    {
        enum OperationMode
        {
            install,
            uninstall,
            reinstall
        }

        // ReSharper disable once UnusedMember.Local
        private static int Main(string mainCommand, 
                                string sourcePath = ".\\", 
                                OperationMode operationMode = OperationMode.install, 
                                string fileMask = "*.dll", 
                                string winVersion = "v10.0A", 
                                string netVersion = "4.7.1",
                                string frameworkVersion = "v4.0.30319",
                                string templateAppConfig = "",
                                string logFolder = "GACNat.log",
                                bool useX64Tooling = true)
        {
            try
            {
                string[] sourceFiles;
                IEnumerable<string> sourceAssemblies;
                List<string> failedFilesList = null;
                List<string> exceptionsLogList;
                var exceptionsLog = Path.Combine(sourcePath, logFolder, "Exceptions.log");
                int fileCount;

                switch (mainCommand)
                {
                    case "retry":
                        if (!File.Exists(exceptionsLog))
                            return 0;
                        operationMode = OperationMode.install;
                        sourceFiles = File.ReadLines(exceptionsLog).ToArray();
                        exceptionsLogList = new List<string>();
                        fileCount = sourceFiles.Length;
                        sourceAssemblies = sourceFiles;
                        mainCommand = "gn";
                        break;
                    case "g":
                    case "gn":
                    case "n":
                        sourceFiles = Directory.EnumerateFiles(sourcePath, fileMask).ToArray();
                        exceptionsLogList = File.Exists(exceptionsLog) ? File.ReadLines(exceptionsLog).ToList() : new List<string>();
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
                            failedFilesList = new GACProcessor(exceptionsLogList, logFolder, useX64Tooling).GACInstall(sourceAssemblies, winVersion, netVersion);
                            exceptionsLogList = failedFilesList;
                        }
                        if (mainCommand.Contains("n"))
                            failedFilesList = new NGENProcessor(exceptionsLogList, logFolder, useX64Tooling).NGENInstall(sourceAssemblies, frameworkVersion, templateAppConfig);
                        break;
                    }

                    case OperationMode.uninstall:
                    {
                        if (mainCommand.Contains("n"))
                            new NGENProcessor(exceptionsLogList, logFolder, useX64Tooling).NGENUninstall(sourceAssemblies, frameworkVersion);
                        if (mainCommand.Contains("g"))
                            new GACProcessor(exceptionsLogList, logFolder, useX64Tooling).GACUninstall(sourceAssemblies, winVersion, netVersion);
                        break;
                    }

                    case OperationMode.reinstall:
                    {
                        if (mainCommand.Contains("g"))
                        {
                            new GACProcessor(exceptionsLogList, logFolder, useX64Tooling).GACUninstall(sourceAssemblies, winVersion, netVersion);
                            failedFilesList = new GACProcessor(exceptionsLogList, logFolder, useX64Tooling).GACInstall(sourceAssemblies, winVersion, netVersion);
                            exceptionsLogList = failedFilesList;
                        }

                        if (mainCommand.Contains("n"))
                        {
                            new GACProcessor(exceptionsLogList, logFolder, useX64Tooling).GACUninstall(sourceAssemblies, winVersion, netVersion);
                            failedFilesList = new NGENProcessor(exceptionsLogList, logFolder, useX64Tooling).NGENInstall(sourceAssemblies, frameworkVersion, templateAppConfig);
                        }

                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Directory.CreateDirectory(Path.Combine(sourcePath, logFolder + "\\"));
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
                // Console.ReadLine();
                return -1;
            }
            // Console.ReadLine();
            return 0;
        }

        private static void DisplayHelp()
        {
            var help = GacnatResources.HelpText;
            Console.WriteLine(help);
        }
    }}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Ascentis.CmdTools
{
    internal class Abort : Exception
    {
        public Abort() : base(""){}
    }

    // ReSharper disable once InconsistentNaming
    internal class GACNativize
    {
        enum OperationMode
        {
            Install,
            Uninstall
        }

        private static void DisplayHelp()
        {
            const string help = @"Welcome to Ascentis GACNativize! 
This is Open Source software released under MIT license
Usage: 

GACNativize -g|-gn|-n <folder> <filemask> install|uninstall [[<winversion>]|<winversion>[<.net version>]]
GACNativize -retry <folder> install|uninstall [[<winversion>]|<winversion>[<.net version>]]

-g Performs    GAC install only
-gn Performs   GAC and NGEN of matching assemblies
-n Performs    NGEN install only

<folder>       Root folder where to look for assembly files
<filemask>     The mask to match within the specified folder
<winversion>   A string specifying the Windows SDK version. Default: v10.0A
<.net version> .NET Framework version for NGEN tool. Default: 4.7.1
";
            Console.WriteLine(help);
        }

        private static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2)
                {
                    DisplayHelp();
                    throw new Abort();
                }

                var path = args[1];
                OperationMode operationMode;
                IEnumerable<string> sourceAssemblies;
                List<string> failedFilesList;
                List<string> exceptionsLogList = null;
                var exceptionsLog = Path.Combine(path, "GACNativize.log", "Exceptions.log");
                var winVersion = "v10.0A";
                var netVersion = "4.7.1";
                var frameworkVersion = "";
                var mainCommand = args[0];
                
                switch (mainCommand)
                {
                    case "-retry":
                        operationMode = OperationMode.Install;
                        sourceAssemblies = File.ReadLines(exceptionsLog);
                        failedFilesList = new List<string>();
                        if (args.Length >= 4)
                            winVersion = args[3];
                        if (args.Length == 5)
                            netVersion = args[4];
                        mainCommand = "-gn";
                        break;
                    case "-g":
                    case "-gn":
                    case "-n":
                        var mask = args[2];
                        sourceAssemblies = Directory.EnumerateFiles(path, mask);
                        failedFilesList = File.Exists(exceptionsLog) ? File.ReadLines(exceptionsLog).ToList() : new List<string>();
                        exceptionsLogList = failedFilesList; 
                        if (args.Length >= 5)
                            winVersion = args[4];
                        if (args.Length == 6)
                            netVersion = args[5];
                        switch (args[3])
                        {
                            case "install":
                                operationMode = OperationMode.Install;
                                break;
                            case "uninstall":
                                operationMode = OperationMode.Uninstall;
                                break;
                            default: 
                                DisplayHelp();
                                throw new Abort();
                        }
                        break;
                    default:
                        DisplayHelp();
                        throw new Abort();
                }

                switch (operationMode)
                {
                    case OperationMode.Install:
                    {
                        if (mainCommand.Contains("g"))
                            new GACProcessor(failedFilesList, exceptionsLogList).GACInstall(sourceAssemblies, winVersion, netVersion);
                        break;
                    }

                    case OperationMode.Uninstall:
                    {
                        if (mainCommand.Contains("g"))
                            new GACProcessor(failedFilesList, exceptionsLogList).GACUninstall(sourceAssemblies, winVersion, netVersion);
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Directory.CreateDirectory(Path.Combine(path, "GACNativize.log\\"));
                if (failedFilesList.Count > 0)
                    File.WriteAllLines(exceptionsLog, failedFilesList);
                else 
                    File.Delete(exceptionsLog);
            }
            catch (Exception e)
            {
                if (!(e is Abort))
                    Console.WriteLine(e.Message);
            }
            Console.ReadLine();
        }
    }
}